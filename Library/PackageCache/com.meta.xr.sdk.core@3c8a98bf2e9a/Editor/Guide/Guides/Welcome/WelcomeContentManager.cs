/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading.Tasks;
using Meta.XR.Editor.RemoteContent;
using Meta.XR.Editor.ToolingSupport;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Guides.Editor.Welcome
{
    internal static class WelcomeContentManager
    {
        #region JSON Structs

        // Wire format of the remote blob: a list of version-scoped content variants. The client
        // downloads the whole blob and selects the variant whose SDK window contains the running SDK
        // (SelectVariant), so one blob can serve multiple SDK versions.
        [Serializable]
        internal struct WelcomeBlob
        {
            public int schemaVersion;
            public WelcomeContent[] variants;
        }

        [Serializable]
        internal struct WelcomeContent
        {
            // SDK-version window for this variant (minor version, e.g. 207 = v207). The client picks
            // the variant with the highest minSdkVersion whose window contains the running SDK.
            // minSdkVersion 0 = no floor; maxSdkVersion 0 = no cap.
            public int minSdkVersion;
            public int maxSdkVersion;

            public CoverContent cover;

            // Version-scoped highlights. Each entry carries its own SDK window (independent of the
            // variant window above); the client shows the entry whose window contains the running SDK
            // (SelectFeatured), or hides the section when none match. One variant can thus vary the
            // highlight per SDK version while keeping cover/resources/nux constant.
            public FeaturedContent[] featured;

            public ResourceContent[] resources;
            public string[] xrToolsOrder;
            public XrToolContent[] xrTools;
            public NuxContent nux;
        }

        [Serializable]
        internal struct CoverContent
        {
            // Title is intentionally not remote-controlled — the cover title is fixed
            // product branding ("Meta XR SDK"). Only the body copy is remotely managed.
            public string subtitle;
        }

        [Serializable]
        internal struct FeaturedContent
        {
            // SDK-version window for THIS highlight, independent of the variant window. The client
            // shows the entry with the highest minSdkVersion whose window contains the running SDK.
            // minSdkVersion 0 = no floor; maxSdkVersion 0 = no cap.
            public int minSdkVersion;
            public int maxSdkVersion;

            public string header;
            public string title;
            public string description;
            public ulong imageContentId;
            public ButtonContent[] buttons;
        }

        [Serializable]
        internal struct ButtonContent
        {
            public string label;
            public string type;
            public string value;
        }

        [Serializable]
        internal struct ResourceContent
        {
            public string label;
            public string description;
            public string linkText;
            public string url;
        }

        [Serializable]
        internal struct XrToolContent
        {
            // Stable key matching XrToolDef.RegistryNameHint. The key is also used for telemetry and
            // is intentionally not presented as user-facing copy.
            public string id;
            public string label;
            public string description;
            public string activeBadgeText;
            public string inactiveBadgeText;
            public string activeCtaText;
            public string inactiveCtaText;
            public string activeUrl;
            public string inactiveUrl;
            public bool enableDirectInstall;
        }

        [Serializable]
        internal struct NuxContent
        {
            public string introTitle;
            public string introSubtitle;
            public string whatsIncludedHeader;
            public NuxFeatureItem[] whatsIncludedItems;
            public string skillLevelTitle;
            public string skillLevelSubtitle;
            public NuxSkillLevel[] skillLevels;
            public string roleTitle;
            public string roleSubtitle;
            public NuxRole[] roles;
        }

        [Serializable]
        internal struct NuxFeatureItem
        {
            public string text;
            public string iconName;
        }

        [Serializable]
        internal struct NuxSkillLevel
        {
            public string id;
            public string label;
            public string description;
        }

        [Serializable]
        internal struct NuxRole
        {
            public string id;
            public string label;
            public string iconName;
        }

        #endregion

        #region Default Content

        private static readonly WelcomeContent DefaultContent = new()
        {
            // The compiled fallback, used when no remote variant applies and as the base that
            // remote/override content is field-merged over. minSdkVersion/maxSdkVersion are unused
            // for the compiled default (only remote variants are gated, via SelectVariant) — kept at
            // 0 so it reads as "always applies".
            minSdkVersion = 0,
            maxSdkVersion = 0,
            cover = new CoverContent
            {
                subtitle = "Build immersive experiences for Quest and Meta devices with Unity."
            },
            // No compiled highlight: the featured section is entirely remote-driven (a version-scoped
            // list in the blob). Empty here means the section is hidden until a remote entry applies.
            featured = Array.Empty<FeaturedContent>(),
            resources = new[]
            {
                new ResourceContent
                {
                    label = "Samples and Showcases",
                    description = "Working examples and best practices for Meta XR development.",
                    linkText = "Browse samples",
                    url = "https://developers.meta.com/horizon/code-samples/unity"
                },
                new ResourceContent
                {
                    label = "Building with Unity",
                    description = "Guides and tutorials to get started with Meta XR SDK.",
                    linkText = "Read documentation",
                    url = "https://developers.meta.com/horizon/develop/unity"
                },
                new ResourceContent
                {
                    label = "API Reference",
                    description = "Detailed API documentation for all Meta XR SDK modules.",
                    linkText = "View API reference",
                    url = "https://developers.meta.com/horizon/reference/unity"
                }
            },
            xrToolsOrder = new[]
            {
                "AI Tools",
                "Meta VR CLI",
                "Hands readiness",
                "Runtime Optimizer",
                "Immersive Debugger",
                "Meta XR Simulator",
                "Meta Quest Developer Hub",
                "Meta Quest Link",
                "RenderDoc",
                "Meta Haptics Studio"
            },
            xrTools = new[]
            {
                new XrToolContent
                {
                    id = "Meta VR CLI",
                    label = "Meta VR CLI",
                    description = "Use Quest developer tools from your terminal or AI assistant to find documentation and assets, manage devices, and analyze performance.",
                    activeBadgeText = "Installed",
                    inactiveBadgeText = "Not installed",
                    activeCtaText = "View docs",
                    inactiveCtaText = "Download",
                    activeUrl = WelcomeSettings.MetaVrCliDocsUrl,
                    inactiveUrl = WelcomeSettings.MetaVrCliDownloadUrl,
                    enableDirectInstall = false
                }
            },
            nux = new NuxContent
            {
                introTitle = "Welcome to Meta XR SDK",
                introSubtitle = "Build immersive experiences for Quest and Meta devices with Unity.",
                whatsIncludedHeader = "What’s included",
                whatsIncludedItems = new[]
                {
                    new NuxFeatureItem
                    {
                        text = "Pre-built XR Building Blocks — drag and drop to create interactions, tracking, and UI",
                        iconName = "default-app"
                    },
                    new NuxFeatureItem
                    {
                        text = "Automated Unity setup and testing tools",
                        iconName = "tools"
                    },
                    new NuxFeatureItem
                    {
                        text = "AI-powered debugging and optimization for Meta devices",
                        iconName = "ai-agent"
                    },
                    new NuxFeatureItem
                    {
                        text = "Official standards testing to ship faster and pass review",
                        iconName = "list-checked"
                    }
                },
                skillLevelTitle = "What’s your experience level?",
                skillLevelSubtitle = "We’ll suggest tools and resources based on your experience.",
                skillLevels = new[]
                {
                    new NuxSkillLevel { id = "beginner", label = "Beginner", description = "I’m new to XR development" },
                    new NuxSkillLevel { id = "intermediate", label = "Intermediate", description = "I’ve built XR experiences before" },
                    new NuxSkillLevel { id = "advanced", label = "Advanced", description = "I’m an experienced XR developer" }
                },
                roleTitle = "What’s your role?",
                roleSubtitle = "This helps us show you relevant tools, examples, and documentation.",
                roles = new[]
                {
                    new NuxRole { id = "unity_developer", label = "Unity Developer", iconName = "ibeam-cursor" },
                    new NuxRole { id = "xr_developer", label = "XR Developer", iconName = "headset-alt" },
                    new NuxRole { id = "3d_artist", label = "3D Artist", iconName = "vr-object" },
                    new NuxRole { id = "game_designer", label = "Game Designer", iconName = "gamepad" },
                    new NuxRole { id = "technical_artist", label = "Technical Artist", iconName = "media-immersive-photo" },
                    new NuxRole { id = "tools_engineer", label = "Tools Engineer", iconName = "editor" },
                    new NuxRole { id = "qa_live_ops", label = "QA/Live ops", iconName = "graphs" },
                    new NuxRole { id = "animator", label = "Animator", iconName = "avatar-emote" },
                    new NuxRole { id = "other", label = "Other", iconName = "category-basic" }
                }
            }
        };

        #endregion

        // Content ID for the remote Welcome content blob (an EntBuildingBlocksContent uploaded via
        // the Building Blocks Content CMS, served by the remote_content_fetch endpoint). 0 disables
        // the remote fetch (compiled default only). static readonly (not const) so the `ContentId == 0`
        // guards aren't constant-folded into unreachable-code (CS0162) errors.
        private static readonly ulong ContentId = 37573278012285724UL;
        private const int SupportedSchemaVersion = 2;

        public static WelcomeContent Content { get; private set; } = DefaultContent;

        public static event Action OnContentChanged;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (ContentId == 0) return;
            FetchRemoteContent();
        }

        private static async void FetchRemoteContent() => await FetchAndApply();

        // Downloads the remote blob, resolves the variant for the running SDK, and applies it on
        // success — leaving current content in place on any failure. Wrapped in try/catch because it
        // runs fire-and-forget on editor load (async void, from an [InitializeOnLoadMethod]); an
        // unhandled fault would otherwise surface as an editor exception. clearCache forces a fresh
        // download past the cache TTL.
        private static async Task<bool> FetchAndApply(bool clearCache = false)
        {
            try
            {
                // Bail when the running SDK version is unknown rather than coercing to 0: a 0 floor
                // would make every floorless variant apply for an unidentified editor.
                var sdkVersion = ToolUsage.GetSdkVersion();
                if (sdkVersion == null) return false;

                var downloader = new RemoteJsonContentDownloader("welcome_content.json", ContentId)
                    .WithCacheDuration(TimeSpan.FromHours(6));
                if (clearCache) downloader.ClearCache();

                var result = await downloader.Fetch();
                if (!result.IsSuccess) return false;
                if (!TryResolve(result.Content, sdkVersion.Value, out var content)) return false;

                Content = content;
                OnContentChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Welcome] Remote content fetch failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parse the blob and resolve the variant for the given SDK version, field-merged over the
        /// compiled default. Returns false (leaving the default in place) when the JSON is malformed,
        /// the schema is unsupported, or no variant's SDK window contains the running version.
        /// </summary>
        internal static bool TryResolve(string json, int sdkVersion, out WelcomeContent content)
        {
            content = default;

            WelcomeBlob blob;
            try
            {
                blob = JsonUtility.FromJson<WelcomeBlob>(json);
            }
            catch (Exception)
            {
                return false;
            }

            // Reject schemas we don't understand. A missing/zero schemaVersion (e.g. an empty
            // "{}" blob) is treated as malformed and ignored rather than blanking the UI.
            if (blob.schemaVersion < 1 || blob.schemaVersion > SupportedSchemaVersion) return false;

            var variant = SelectVariant(blob.variants, sdkVersion);
            if (variant == null) return false;

            content = Normalize(variant.Value);
            return true;
        }

        /// <summary>
        /// Whether an SDK window [min, max] includes the given SDK minor version.
        /// min 0 = no floor; max 0 = no cap.
        /// </summary>
        private static bool InSdkWindow(int minSdkVersion, int maxSdkVersion, int sdkVersion)
        {
            if (sdkVersion < minSdkVersion) return false;
            if (maxSdkVersion > 0 && sdkVersion > maxSdkVersion) return false;
            return true;
        }

        /// <summary>Whether a variant's SDK window includes the given SDK minor version.</summary>
        internal static bool AppliesToSdk(WelcomeContent variant, int sdkVersion) =>
            InSdkWindow(variant.minSdkVersion, variant.maxSdkVersion, sdkVersion);

        /// <summary>
        /// Pick the applicable variant with the highest minSdkVersion (the most specific match for
        /// the running SDK), or null when none applies. On a tie (two applicable variants share the
        /// same minSdkVersion) the first in array order wins, so blob authors should not ship two
        /// variants with the same floor.
        /// </summary>
        internal static WelcomeContent? SelectVariant(WelcomeContent[] variants, int sdkVersion)
        {
            WelcomeContent? best = null;
            if (variants == null) return null;

            foreach (var variant in variants)
            {
                if (!AppliesToSdk(variant, sdkVersion)) continue;
                if (best == null || variant.minSdkVersion > best.Value.minSdkVersion)
                {
                    best = variant;
                }
            }

            return best;
        }

        /// <summary>
        /// Pick the highlight whose SDK window contains the running SDK, highest minSdkVersion first
        /// (most specific match), or null when none applies (section hidden). On a tie the first in
        /// array order wins, so blob authors should not ship two highlights with the same floor.
        /// </summary>
        internal static FeaturedContent? SelectFeatured(FeaturedContent[] featured, int sdkVersion)
        {
            FeaturedContent? best = null;
            if (featured == null) return null;

            foreach (var entry in featured)
            {
                if (!InSdkWindow(entry.minSdkVersion, entry.maxSdkVersion, sdkVersion)) continue;
                if (best == null || entry.minSdkVersion > best.Value.minSdkVersion)
                {
                    best = entry;
                }
            }

            return best;
        }

        // Field-level merge over DefaultContent. A variant only needs to carry the deltas it wants
        // to change — every field it omits (JsonUtility leaves these null/empty/0) falls back to the
        // compiled default. This lets a version-specific variant change just the featured section
        // without re-stating cover/resources/NUX, and keeps every collection non-null so consumers
        // never NRE on a partial variant.
        internal static WelcomeContent Normalize(WelcomeContent content)
        {
            content.cover.subtitle = Coalesce(content.cover.subtitle, DefaultContent.cover.subtitle);

            // featured is the authoritative version-scoped list — NOT merged with the default (there is
            // no compiled highlight). An omitted/empty list means "no highlight for this variant";
            // kept non-null so SelectFeatured never NREs.
            content.featured ??= Array.Empty<FeaturedContent>();

            content.resources = Coalesce(content.resources, DefaultContent.resources);
            content.xrToolsOrder = Coalesce(content.xrToolsOrder, DefaultContent.xrToolsOrder);
            content.xrTools = Coalesce(content.xrTools, DefaultContent.xrTools);

            var nux = content.nux;
            var defaultNux = DefaultContent.nux;
            nux.introTitle = Coalesce(nux.introTitle, defaultNux.introTitle);
            nux.introSubtitle = Coalesce(nux.introSubtitle, defaultNux.introSubtitle);
            nux.whatsIncludedHeader = Coalesce(nux.whatsIncludedHeader, defaultNux.whatsIncludedHeader);
            nux.whatsIncludedItems = Coalesce(nux.whatsIncludedItems, defaultNux.whatsIncludedItems);
            nux.skillLevelTitle = Coalesce(nux.skillLevelTitle, defaultNux.skillLevelTitle);
            nux.skillLevelSubtitle = Coalesce(nux.skillLevelSubtitle, defaultNux.skillLevelSubtitle);
            nux.skillLevels = Coalesce(nux.skillLevels, defaultNux.skillLevels);
            nux.roleTitle = Coalesce(nux.roleTitle, defaultNux.roleTitle);
            nux.roleSubtitle = Coalesce(nux.roleSubtitle, defaultNux.roleSubtitle);
            nux.roles = Coalesce(nux.roles, defaultNux.roles);
            content.nux = nux;

            return content;
        }

        private static string Coalesce(string value, string fallback) =>
            string.IsNullOrEmpty(value) ? fallback : value;

        // A null OR empty array falls back to the default: a variant replaces a section by shipping
        // the full list, not by shipping an empty one to hide it (hiding a section isn't a supported
        // authoring move). Switch to `value ?? fallback` if empty should ever mean "hide".
        private static T[] Coalesce<T>(T[] value, T[] fallback) =>
            value is { Length: > 0 } ? value : fallback;

    }
}
