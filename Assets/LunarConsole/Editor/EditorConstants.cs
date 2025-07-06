//
//  EditorConstants.cs
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


﻿using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.IO;

using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    static class EditorConstants
    {
        private static string pluginRootDirectory;

        public static string PrefabPath
        {
            get
            {
                var pluginRoot = PluginRootDirectory;
                return pluginRoot != null ? FileUtils.GetAssetPath(pluginRoot, "Scripts", Constants.PluginName + ".prefab") : null;
            }
        }

        public static string EditorPathIOS
        {
            get
            {
                var pluginEditorRoot = PluginEditorRootDirectory;
                return pluginEditorRoot != null ? FileUtils.GetAssetPath(pluginEditorRoot, "iOS") : null;
            }
        }

        public static string EditorPathAndroidAAR
        {
            get
            {
                var pluginEditorRoot = PluginEditorRootDirectory;
                return pluginEditorRoot != null ? FileUtils.GetAssetPath(pluginEditorRoot, "Android", "lunar-console.aar") : null;
            }
        }

        private static string PluginEditorRootDirectory
        {
            get
            {
                var pluginRoot = PluginRootDirectory;
                return pluginRoot != null ? Path.Combine(pluginRoot, "Editor") : null;
            }
        }

        private static string PluginRootDirectory
        {
            get
            {
                if (pluginRootDirectory == null)
                {
                    pluginRootDirectory = ResolvePluginRootDirectory();
                    if (pluginRootDirectory == null)
                    {
                        Debug.LogErrorFormat("Unable to resolve plugin root directory. Re-install {0} to fix the issue", Constants.PluginDisplayName);
                        return null;
                    }
                }
                return pluginRootDirectory;
            }
        }

        private static string ResolvePluginRootDirectory()
        {
            try
            {
                string currentFile = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                if (currentFile != null && File.Exists(currentFile))
                {
                    var currentDirectory = new FileInfo(currentFile).Directory;
                    if (currentDirectory.Name != "Editor")
                    {
                        return null;
                    }

                    return currentDirectory.Parent.FullName;
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while resolving plugin files location");
            }

            return null;
        }
    }
}
