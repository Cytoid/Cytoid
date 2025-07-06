//
//  FileUtils.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


﻿#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

using System;

namespace LunarConsolePluginInternal
{
    public static class FileUtils
    {
        private static string projectDir;
        private static string assetsDir;

        static FileUtils()
        {
            projectDir = new DirectoryInfo(Application.dataPath).Parent.FullName; 
            assetsDir = new DirectoryInfo(Application.dataPath).FullName;
        }

        public static string GetAssetPath(params string[] pathComponents)
        {
            string path = pathComponents[0];
            for (int i = 1; i < pathComponents.Length; ++i)
            {
                path = Path.Combine(path, pathComponents[i]);
            }

            return FixPath(MakeRelativePath(assetsDir, path));
        }

        public static bool AssetPathExists(string assetPath)
        {
            var fullPath = GetFullAssetPath(assetPath);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        public static string GetFullAssetPath(string path)
        {
            return FixPath(Path.Combine(projectDir, path));
        }

        public static string FixAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }

        public static string FixPath(string path)
        {
            #if UNITY_EDITOR_WIN
            return path.Replace('/', '\\');
            #else
            return path.Replace('\\', '/');
            #endif
        }

        #pragma warning disable 0612
        #pragma warning disable 0618

        public static string MakeRelativePath(string parentPath, string filePath)
        {
            return Uri.UnescapeDataString(new Uri(parentPath, false).MakeRelative(new Uri(filePath)));
        }

        #pragma warning restore 0612
        #pragma warning restore 0618
    }
}

#endif // UNITY_EDITOR
