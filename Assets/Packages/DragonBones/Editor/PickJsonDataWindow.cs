/**
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2017 DragonBones team and other contributors
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DragonBones
{
    /// <summary>
    /// JSON数据拾取，为UnityArmatureComponent创建UnityDragonBonesData
    /// </summary>
    public class PickJsonDataWindow : EditorWindow
    {
        private const string ObjectSelectorUpdated = "ObjectSelectorUpdated";
        private const string ObjectSelectorClosed = "ObjectSelectorClosed";
        //private const string PickFileFileter = "_ske t:TextAsset";
        private const string PickFileFileter = "t:TextAsset";


        private UnityArmatureComponent _armatureComp;
        private TextAsset _dragonBoneJSONData;

        private bool _isOpenPickWindow = false;
        private int _controlID;

        //
        public static void OpenWindow(UnityArmatureComponent armatureComp)
        {
            if (armatureComp == null)
            {
                return;
            }

            //
            var win = GetWindow<PickJsonDataWindow>();
            win._armatureComp = armatureComp;
        }

        private void OnDestroy()
        {
            _armatureComp = null;
            _dragonBoneJSONData = null;

            _isOpenPickWindow = false;
            _controlID = 0;
        }

        private void Awake()
        {
            _dragonBoneJSONData = null;

            _isOpenPickWindow = false;
            _controlID = 0;

            this.maxSize = Vector2.one;
            this.minSize = Vector2.one;
        }

        private void OnGUI()
        {
            ShowPickJsonWindow();

            string commandName = Event.current.commandName;
            if (commandName == ObjectSelectorUpdated)
            {
                //更新JSON数据
                _dragonBoneJSONData = EditorGUIUtility.GetObjectPickerObject() as TextAsset;
            }
            else if (commandName == ObjectSelectorClosed)
            {
                //根据选择的JSON数据设置DragonBonesData

                //这里不仅创建了DragonBonesData,并且更新了场景中的显示对象
                //UnityEditor.ChangeDragonBonesData(_armatureComp, _dragonBoneJSONData);

                if (_dragonBoneJSONData != null)
                {
                    SetUnityDragonBonesData();
                }

                Repaint();

                this.Close();
            }
        }

        private void ShowPickJsonWindow()
        {
            if (_isOpenPickWindow)
            {
                return;
            }

            _controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<TextAsset>(null, false, PickFileFileter, _controlID);

            _isOpenPickWindow = true;
        }

        private void SetUnityDragonBonesData()
        {
            List<string> textureAtlasJSONs = new List<string>();
            UnityEditor.GetTextureAtlasConfigs(textureAtlasJSONs, AssetDatabase.GetAssetPath(_dragonBoneJSONData.GetInstanceID()));
            UnityDragonBonesData.TextureAtlas[] textureAtlas = UnityEditor.GetTextureAtlasByJSONs(textureAtlasJSONs);

            UnityDragonBonesData data = UnityEditor.CreateUnityDragonBonesData(_dragonBoneJSONData, textureAtlas);
            _armatureComp.unityData = data;
        }
    }
}
