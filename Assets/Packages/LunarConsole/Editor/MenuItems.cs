//
//  MenuItems.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2019 Alex Lementuev, SpaceMadness.
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

﻿//
//  MenuItems.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2019 Alex Lementuev, SpaceMadness.
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

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    static class MenuItems
    {
        [MenuItem("Window/Lunar Mobile Console/Install...")]
        static void Install()
        {
            bool silent = !InternalEditorUtility.isHumanControllingUs;
            Installer.Install(silent);
        }

        [MenuItem("Window/Lunar Mobile Console/Actions and Variables", true)]
        static bool ShowActionsAndWariablesFunc()
        {
            return LunarConsoleConfig.fullVersion && LunarConsoleConfig.consoleEnabled;
        }

        [MenuItem("Window/Lunar Mobile Console/Actions and Variables")]
        static void ShowActionsAndWariables()
        {
            ActionsAndVariablesWindow.ShowWindow();
        }

        [MenuItem("Window/Lunar Mobile Console/Check for updates...")]
        static void CheckForUpdates()
        {
            LunarConsoleEditorAnalytics.TrackEvent("Version", "updater_check");
            Updater.CheckForUpdates(false);
        }

        [MenuItem("Window/Lunar Mobile Console/Report bug...")]
        static void RequestFeature()
        {
            Application.OpenURL("https://github.com/SpaceMadness/lunar-unity-console/issues/new");
        }

        #if LUNAR_CONSOLE_DEVELOPMENT
        [MenuItem("Window/Lunar Mobile Console/Reset")]
        static void Reset()
        {
            Updater.Reset();
        }
        #endif
    }
}
