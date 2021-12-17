//
//  BuildPostProcessor.cs
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
using UnityEditor.Callbacks;
using System;

#if UNITY_IOS || UNITY_IPHONE
using UnityEditor.iOS.Xcode;
#endif

using System.Collections;
using System.IO;

using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    static class BuildPostProcessor
    {
        #if UNITY_IOS || UNITY_IPHONE
        [PostProcessBuild(1000)]
        static void OnPostprocessBuild(BuildTarget target, string buildPath)
        {
            if (LunarConsoleConfig.consoleEnabled)
            {
                if (target == BuildTarget.iOS)
                {
                    OnPostprocessIOS(buildPath);
                }
            }
        }

        static void OnPostprocessIOS(string buildPath)
        {
            // Workaround for:
            // FileNotFoundException: Could not load file or assembly 'UnityEditor.iOS.Extensions.Xcode, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' or one of its dependencies.
            // For more information see: http://answers.unity3d.com/questions/1016975/filenotfoundexception-when-using-xcode-api.html
            // Copy plugin files to the build directory so you can later move to another machine and build it there
            #if LUNAR_CONSOLE_EXPORT_IOS_FILES
            var pluginPath = Path.Combine(buildPath, Constants.PluginName);
            FileUtil.DeleteFileOrDirectory(pluginPath);
            FileUtil.CopyFileOrDirectory(EditorConstants.EditorPathIOS, pluginPath);
            // Clean up meta files
            string[] files = Directory.GetFiles(pluginPath, "*.meta", System.IO.SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileUtil.DeleteFileOrDirectory(file);
            }
            #else  // LUNAR_CONSOLE_EXPORT_IOS_FILES
            var pluginPath = EditorConstants.EditorPathIOS;
            #endif // LUNAR_CONSOLE_EXPORT_IOS_FILES

            var projectMod = new XcodeProjMod(buildPath, pluginPath);
            projectMod.UpdateProject();
        }
        #endif //UNITY_IOS || UNITY_IPHONE
    }

    #if UNITY_IOS || UNITY_IPHONE
    class XcodeProjMod
    {
        private readonly string m_buildPath;
        private readonly string m_projectPath;
        private readonly string m_pluginPath;

        public XcodeProjMod(string buildPath, string pluginPath)
        {
            m_buildPath = buildPath;
            m_pluginPath = pluginPath;
            m_projectPath = PBXProject.GetPBXProjectPath(buildPath);
        }

        public void UpdateProject()
        {
            var project = new PBXProject();
            project.ReadFromFile(m_projectPath);

            string[] files = Directory.GetFiles(m_pluginPath, "*.projmods", System.IO.SearchOption.AllDirectories);
            foreach (string file in files)
            {
                ApplyMod(project, file);
            }

            project.WriteToFile(m_projectPath);
        }

        void ApplyMod(PBXProject project, string modFile)
        {
            var json = File.ReadAllText(modFile);
            var mod = JsonUtility.FromJson<ProjMod>(json);
            var sourceDir = Directory.GetParent(modFile).FullName;
            var targetGroup = "Libraries/" + mod.group;
            var sourcesTargetGuid = GetSourcesTargetGuid(project);
            var resourcesTargetGuid = GetResourcesTargetGuid(project);
            var dirProject = Directory.GetParent(PBXProject.GetPBXProjectPath(m_buildPath)).FullName;
            foreach (var file in mod.files)
            {
                var filename = Path.GetFileName(file);
                var fileGuid = project.AddFile(FileUtils.FixPath(FileUtils.MakeRelativePath(dirProject, sourceDir + "/" + file)), targetGroup + "/" + filename, PBXSourceTree.Source);
                if (filename.EndsWith(".h"))
                {
                    continue;
                }

                var targetGuid = IsSourceFile(filename) ? sourcesTargetGuid : resourcesTargetGuid;
                project.AddFileToBuild(targetGuid, fileGuid);
            }
            foreach (var framework in mod.frameworks)
            {
                project.AddFrameworkToProject(sourcesTargetGuid, framework, false);
            }
        }

        static bool IsSourceFile(string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            return ext == ".m" || ext == ".mm" || ext == ".swift" || ext == ".c" || ext == ".cpp";
        }

        static string GetResourcesTargetGuid(PBXProject project)
        {
#if UNITY_2019_3_OR_NEWER
            return project.GetUnityMainTargetGuid();
#else
            return project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
        }

        static string GetSourcesTargetGuid(PBXProject project)
        {
#if UNITY_2019_3_OR_NEWER
            return project.GetUnityFrameworkTargetGuid();
#else
            return project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
        }
    }

    #pragma warning disable 0649
    #pragma warning disable 0414

    [System.Serializable]
    class ProjMod
    {
        public string group;
        public string[] frameworks;
        public string[] files;
    }

    #pragma warning restore 0649
    #pragma warning restore 0414

    #endif // UNITY_IOS || UNITY_IPHONE
}
