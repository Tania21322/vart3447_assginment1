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

using Meta.XR.Editor.Utils;
using UnityEditor;

namespace Meta.XR.BuildingBlocks.Editor
{
    /// <summary>
    /// The Meta XR Movement SDK is distributed as a GitHub package rather than via
    /// Unity's UPM registry. Register custom installation instructions so that the
    /// Building Blocks UI directs developers to the correct GitHub source instead of
    /// telling them to add it by name from the Package Manager.
    /// </summary>
    [InitializeOnLoad]
    internal static class MovementCustomPackageDependency
    {
        private const string MOVEMENT_PACKAGE_DEP_ID = "com.meta.xr.sdk.movement";
        static MovementCustomPackageDependency()
        {
            CustomPackageDependencyRegistry.RegisterCustomPackageDependency(MOVEMENT_PACKAGE_DEP_ID, new CustomPackageDependencyInfo()
            {
                PackageDisplayName = "Meta XR Movement SDK",
                IsPackageInstalled = () => PackageList.IsPackageInstalledWithValidVersion(MOVEMENT_PACKAGE_DEP_ID),
                InstallationInstructions = "Add the package from GitHub via Package Manager > Add package from git URL: https://github.com/oculus-samples/Unity-Movement.git"
            });
        }
    }
}
