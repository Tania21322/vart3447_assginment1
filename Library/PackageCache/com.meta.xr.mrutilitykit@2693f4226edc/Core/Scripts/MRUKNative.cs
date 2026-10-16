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
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Provides cross-platform native library loading functionality for MRUtilityKit.
    /// This class handles dynamic loading and unloading of the MRUtilityKit shared library
    /// across different platforms (Windows, macOS, and Android) using platform-specific APIs.
    /// </summary>
    internal static class MRUKNative
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        [DllImport("libdl.dylib")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.dylib")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.dylib")]
        private static extern int dlclose(IntPtr handle);
#elif UNITY_ANDROID || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        [DllImport("libdl.so")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl.so")]
        private static extern int dlclose(IntPtr handle);
#else
#warning "Unsupported platform, mr utility kit will still compile but you will get errors at runtime if you try to use it"
#endif
        private static IntPtr _nativeLibraryPtr;

        /// <summary>
        /// Cross-platform abstraction for loading a DLL or shared object.
        /// </summary>
        /// <param name="path">The file path to the native library to load.</param>
        /// <returns>A handle to the loaded library, or IntPtr.Zero if loading failed or platform is unsupported.</returns>
        private static IntPtr GetDllHandle(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return LoadLibrary(path);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            const int RTLD_NOW = 2;
            return dlopen(path, RTLD_NOW);
#else
            return IntPtr.Zero;
#endif
        }

        /// <summary>
        /// Cross-platform abstraction for accessing a symbol within a DLL or shared object.
        /// </summary>
        /// <param name="dllHandle">Handle to the loaded library obtained from GetDllHandle.</param>
        /// <param name="name">The name of the symbol/function to retrieve from the library.</param>
        /// <returns>A pointer to the requested symbol, or IntPtr.Zero if the symbol is not found or platform is unsupported.</returns>
        private static IntPtr GetDllExport(IntPtr dllHandle, string name)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return GetProcAddress(dllHandle, name);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return dlsym(dllHandle, name);
#else
            return IntPtr.Zero;
#endif
        }

        /// <summary>
        /// Cross-platform abstraction for freeing/closing a DLL or shared object.
        /// </summary>
        /// <param name="dllHandle">Handle to the loaded library to be freed.</param>
        /// <returns>True if the library was successfully freed, false otherwise or if platform is unsupported.</returns>
        private static bool FreeDllHandle(IntPtr dllHandle)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return FreeLibrary(dllHandle);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return dlclose(dllHandle) == 0;
#else
            return false;
#endif
        }

        /// <summary>
        /// Loads the MRUtilityKit shared library for the current platform.
        /// This method determines the appropriate library path based on the platform and loads it into memory.
        /// </summary>
        /// <remarks>
        /// The method handles different platforms (Windows, macOS, Android) and their respective library formats.
        /// If the library is already loaded, this method returns without performing any operations.
        /// On failure, an error is logged to the Unity console.
        /// </remarks>
        internal static void LoadMRUKSharedLibrary()
        {
            if (_nativeLibraryPtr != IntPtr.Zero)
            {
                return;
            }

            var path = string.Empty;
#if UNITY_EDITOR_WIN
            path = Path.GetFullPath("Packages/com.meta.xr.mrutilitykit/Plugins/Win64/mrutilitykitshared.dll");
#elif UNITY_EDITOR_OSX
            string folder = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "MacArm" : "Mac";
            path = Path.GetFullPath($"Packages/com.meta.xr.mrutilitykit/Plugins/{folder}/libmrutilitykitshared.dylib");
#elif UNITY_STANDALONE_WIN
            path = Path.Join(Application.dataPath, "Plugins/x86_64/mrutilitykitshared.dll");
#elif UNITY_STANDALONE_OSX
            // NOTE: This only works for Arm64 Macs
            path = Path.Join(Application.dataPath, "Plugins/ARM64/libmrutilitykitshared.dylib");
#elif UNITY_ANDROID
            path = "libmrutilitykitshared.so";
#endif

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"mr utility kit shared library is not supported on this platform: '{Application.platform}'");
                return;
            }

            _nativeLibraryPtr = GetDllHandle(path);

            if (_nativeLibraryPtr == IntPtr.Zero)
            {
                Debug.LogError($"Failed to load mr utility kit shared library from '{path}'");
            }
            else
            {
                MRUKNativeFuncs.LoadNativeFunctions();
            }
        }

        /// <summary>
        /// Frees the MRUtilityKit shared library and unloads all native functions.
        /// This method should be called when the library is no longer needed to prevent memory leaks.
        /// </summary>
        /// <remarks>
        /// This method first unloads all native function delegates, then attempts to free the library handle.
        /// If the library is not currently loaded, this method returns without performing any operations.
        /// If freeing the library fails, an error is logged to the Unity console.
        /// </remarks>
        internal static void FreeMRUKSharedLibrary()
        {
            MRUKNativeFuncs.UnloadNativeFunctions();

            if (_nativeLibraryPtr == IntPtr.Zero)
            {
                return;
            }

            if (!FreeDllHandle(_nativeLibraryPtr))
            {
                Debug.LogError("Failed to free mr utility kit shared library");
            }

            _nativeLibraryPtr = IntPtr.Zero;
        }

        /// <summary>
        /// Loads a function from the MRUtilityKit shared library and returns it as a delegate of type T.
        /// </summary>
        /// <typeparam name="T">The delegate type that matches the signature of the native function.</typeparam>
        /// <param name="name">The name of the function to load from the shared library.</param>
        /// <returns>
        /// A delegate of type T that can be used to call the native function,
        /// or the default value of T if loading fails.
        /// </returns>
        /// <remarks>
        /// This method requires that the MRUtilityKit shared library has been successfully loaded
        /// via LoadMRUKSharedLibrary(). If the library is not loaded or the function cannot be found,
        /// a warning is logged and the default value is returned.
        /// </remarks>
        internal static T LoadFunction<T>(string name)
        {
            if (_nativeLibraryPtr == IntPtr.Zero)
            {
                Debug.LogWarning($"Failed to load {name} because mr utility kit shared library is not loaded");
                return default;
            }
            IntPtr funcPtr = GetDllExport(_nativeLibraryPtr, name);
            if (funcPtr == IntPtr.Zero)
            {
                Debug.LogWarning($"Could not find {name} in mr utility kit shared library");
                return default;
            }
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
    }
}
