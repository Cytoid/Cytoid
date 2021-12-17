//
//  Utils.cs
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
using System.Collections.Generic;
using System.Reflection;

namespace LunarConsoleEditorInternal
{
    struct DialogButton
    {
        public readonly string title;
        public readonly Action<string> action;
        
        public DialogButton(string title, Action action)
        {
            this.title = title;
            this.action = action != null ? (Action<string>)(delegate(string obj) { action(); }) : null;
        }
        
        public DialogButton(string title, Action<string> action = null)
        {
            this.title = title;
            this.action = action;
        }
        
        internal void PerformAction()
        {
            try
            {
                if (action != null)
                {
                    action(title);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    static class Utils
    {
        #region Dialogs
        
        internal static bool ShowDialog(string title, string message)
        {
            return EditorUtility.DisplayDialog(title, message, "Ok", "Cancel");
        }

        internal static bool ShowDialog(string title, string message, string ok)
        {
            return EditorUtility.DisplayDialog(title, message, ok);
        }
        
        internal static void ShowDialog(string title, string message, DialogButton buttonOk)
        {
            if (EditorUtility.DisplayDialog(title, message, buttonOk.title))
            {
                buttonOk.PerformAction();
            }
        }
        
        internal static void ShowDialog(string title, string message, DialogButton buttonOk, DialogButton buttonCancel)
        {
            if (EditorUtility.DisplayDialog(title, message, buttonOk.title, buttonCancel.title))
            {
                buttonOk.PerformAction();
            }
            else
            {
                buttonCancel.PerformAction();
            }
        }
        
        internal static void ShowDialog(string title, string message, DialogButton buttonOk, DialogButton buttonCancel, DialogButton buttonAlt)
        {
            int choice = EditorUtility.DisplayDialogComplex(title, message, buttonOk.title, buttonCancel.title, buttonAlt.title);
            switch (choice)
            {
                case 0:
                    buttonOk.PerformAction();
                    break;
                case 1:
                    buttonCancel.PerformAction();
                    break;
                case 2:
                    buttonAlt.PerformAction();
                    break;
            }
        }
        
        internal static void ShowMessageDialog(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }
        
        #endregion

        #region Dispatcher
        
        private static Queue<Action> s_dispatchQueue = new Queue<Action>();
        
        public static void DispatchOnMainThread(Action action)
        {
            lock (s_dispatchQueue)
            {
                s_dispatchQueue.Enqueue(action);
                if (s_dispatchQueue.Count == 1)
                {
                    EditorApplication.update += RunDispatch;
                }
            }
        }
        
        private static void RunDispatch()
        {
            lock (s_dispatchQueue)
            {
                while (s_dispatchQueue.Count > 0)
                {
                    Action action = s_dispatchQueue.Dequeue();
                    action();
                }
                
                EditorApplication.update -= RunDispatch;
            }
        }
        
        #endregion
    }
}
