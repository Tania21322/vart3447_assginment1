// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Platform
{
    static class InstallAIRulesAndSkills
    {
        private const string ClaudeDir = ".claude";
        private const string SkillsSubDir = "skills";
        private const string TemplatesSubDir = "AITemplates";
        private const string SkillPrefix = "hzpsdk-";

        private static string ProjectRoot => Path.GetDirectoryName(UnityEngine.Application.dataPath);

        // Moves with Edit Settings: leaving a single entry behind under a top-level "Meta" would
        // keep that menu on screen and defeat the consolidation Unity asked for.
        [MenuItem("Window/Meta/Platform/Install AI Rules && Skills", false, 3300)]
        static void Install()
        {
            if (IsInstalled())
            {
                var overwrite = EditorUtility.DisplayDialog(
                    "AI Rules & Skills Already Installed",
                    "AI rules and skills are already installed in this project.\n\n" +
                    "Overwrite with the latest templates?",
                    "Overwrite",
                    "Cancel");

                if (!overwrite) return;
            }

            try
            {
                int skillCount = InstallTemplates();
                if (skillCount == 0)
                {
                    EditorUtility.DisplayDialog(
                        "No Skills Installed",
                        "No HzPSDK skills were found in the package templates directory. " +
                        "The Meta XR Platform package may be incomplete — please reinstall it.",
                        "OK");
                    return;
                }

                EditorUtility.DisplayDialog(
                    "AI Rules & Skills Installed",
                    $"Successfully installed {skillCount} HzPSDK AI skill(s) to:\n\n" +
                    $"  <project>/{ClaudeDir}/{SkillsSubDir}/\n\n" +
                    "Claude Code will now auto-discover HzPSDK patterns.",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Meta Platform] Failed to install AI rules & skills: {e}");
                EditorUtility.DisplayDialog(
                    "Installation Failed",
                    $"Failed to install AI rules & skills:\n\n{e.Message}",
                    "OK");
            }
        }

        static bool IsInstalled()
        {
            var skillsDir = Path.Combine(ProjectRoot, ClaudeDir, SkillsSubDir);
            if (!Directory.Exists(skillsDir)) return false;
            foreach (var dir in Directory.GetDirectories(skillsDir))
            {
                if (Path.GetFileName(dir).StartsWith(SkillPrefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static int InstallTemplates()
        {
            var templatesDir = FindTemplatesDirectory();
            if (templatesDir == null)
            {
                throw new InvalidOperationException(
                    "Could not locate AITemplates directory next to InstallAIRulesAndSkills.cs. " +
                    "Ensure the Meta XR Platform package is properly installed.");
            }

            var claudeSrcDir = Path.Combine(templatesDir, "claude");
            int skillCount = 0;

            // Copy all skills (each subdirectory under claude/skills/)
            var skillsSrcDir = Path.Combine(claudeSrcDir, "skills");
            if (Directory.Exists(skillsSrcDir))
            {
                foreach (var skillDir in Directory.GetDirectories(skillsSrcDir))
                {
                    if (IsMetaFile(skillDir)) continue;
                    var skillName = Path.GetFileName(skillDir);
                    var destDir = Path.Combine(ProjectRoot, ClaudeDir, SkillsSubDir, skillName);
                    CopyDirectory(skillDir, destDir);
                    skillCount++;
                }
            }

            Debug.Log($"[Meta Platform] Installed {skillCount} skill(s) to {ProjectRoot}/{ClaudeDir}/");
            return skillCount;
        }

        static bool IsMetaFile(string path)
        {
            return path.EndsWith(".meta");
        }

        static void CopyDirectory(string srcDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(srcDir))
            {
                if (IsMetaFile(file)) continue;
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            }
            foreach (var dir in Directory.GetDirectories(srcDir))
            {
                if (IsMetaFile(dir)) continue;
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        static string FindTemplatesDirectory()
        {
            // Resolve the Editor directory via the canonical MonoScript pattern
            // (see OculusPlatformSettingsEditor.GetPlatformRootPath). Reuses the
            // existing OculusPluginUpdaterStub which lives in the same Editor folder.
            var stub = ScriptableObject.CreateInstance(typeof(OculusPluginUpdaterStub));
            var script = MonoScript.FromScriptableObject(stub);
            var assetPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(assetPath)) return null;

            var editorDir = Directory.GetParent(assetPath).FullName;
            var candidate = Path.Combine(editorDir, TemplatesSubDir);
            return Directory.Exists(candidate) ? candidate : null;
        }
    }
}
