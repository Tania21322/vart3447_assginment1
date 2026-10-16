// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Horizon.Platform.Tools
{
    [InitializeOnLoad]
    public class Exporter
    {
        private const string HorizonPlatformPath = "Assets/Horizon/Platform/";

        public static void GenerateMetaFiles()
        {
            Debug.Log("Generating .meta files for Horizon Platform SDK...");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Configure Android plugin architectures to avoid collision errors
            ConfigureAndroidPluginArchitectures();

            AssetDatabase.SaveAssets();
            Debug.Log("Finished generating .meta files");
        }

        private static void ConfigureAndroidPluginArchitectures()
        {
            // Configure Android32 (ARMv7) plugin
            string android32Path = HorizonPlatformPath + "Plugins/Android32/libovrplatformloader.so";
            ConfigureAndroidPlugin(android32Path, "ARMv7");

            // Configure Android64 (ARM64) plugin
            string android64Path = HorizonPlatformPath + "Plugins/Android64/libovrplatformloader.so";
            ConfigureAndroidPlugin(android64Path, "ARM64");
        }

        private static void ConfigureAndroidPlugin(string assetPath, string cpu)
        {
            PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not find plugin at path: {assetPath}");
                return;
            }

            // Set Android platform compatibility
            importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
            importer.SetPlatformData(BuildTarget.Android, "CPU", cpu);

            // Disable for other platforms
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            importer.SetCompatibleWithPlatform(BuildTarget.iOS, false);

            importer.SaveAndReimport();
            Debug.Log($"Configured Android plugin {assetPath} with CPU: {cpu}");
        }

        [MenuItem("Export Tools/Export Horizon Platform Package")]
        public static void ExportPackage()
        {
            Debug.Log("Exporting Horizon Platform SDK Package...");

            var unityPackagePath = "../UnityPrebuilt/HorizonPlatformSDK.unitypackage";

            var platformFiles = Directory.GetFiles(HorizonPlatformPath, "*.*", SearchOption.AllDirectories);

            var files = new List<String>();
            files.AddRange(platformFiles);

            // Filter out files that should not be exported
            files.RemoveAll((String filePath) =>
            {
                filePath = filePath.Replace("\\", "/");
                if (filePath.EndsWith("OculusPlatform.asset") || filePath.EndsWith("OculusPlatform.asset.meta"))
                {
                    return true;
                }
                if (filePath.EndsWith("OculusPlatformSettings.asset") || filePath.EndsWith("OculusPlatformSettings.asset.meta"))
                {
                    return true;
                }
                if (filePath.EndsWith("Presign.cs") || filePath.EndsWith("Presign.cs.meta"))
                {
                    return true;
                }
                return filePath.Contains("Android/Assets/oculussig_");
            });

            AssetDatabase.ExportPackage(
                files.ToArray(),
                unityPackagePath,
                ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse
            );
            Debug.Log("Finished exporting to " + unityPackagePath);
        }
    }
}
#endif
