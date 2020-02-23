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
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace DragonBones
{
    public class AssetProcess : AssetPostprocessor
    {
        [System.Serializable]
        struct SubTextureClass
        {
            public string name;
            public float x, y, width, height, frameX, frameY, frameWidth, frameHeight;
        }

        [System.Serializable]
        class TextureDataClass
        {
            public string name=null;
            public string imagePath=null;
            public int width=0,height=0;
            public List<SubTextureClass> SubTexture=null;
        }

        public static void OnPostprocessAllAssets(string[]imported,string[] deletedAssets,string[] movedAssets,string[]movedFromAssetPaths)  
        {
            if (imported.Length == 0)
            {
                return;
            }

            var atlasPaths = new List<string>();
            var imagePaths = new List<string>();
            var skeletonPaths = new List<string>();

            foreach (string str in imported)
            {
                string extension = Path.GetExtension(str).ToLower();
                switch (extension)
                {
                case ".png":
                    imagePaths.Add(str);
                    break;
                case ".json":
                    if (str.EndsWith("_tex.json", System.StringComparison.Ordinal))
                    {
                        atlasPaths.Add(str);
                    }
                    else if (IsValidDragonBonesData((TextAsset)AssetDatabase.LoadAssetAtPath(str, typeof(TextAsset))))
                    {
                        skeletonPaths.Add(str);
                    }
                    else
                    {
                        atlasPaths.Add(str);
                    }
                    break;
                case ".dbbin":
                    if (File.Exists(str)){
                        string bytesPath = Path.GetDirectoryName(str) + "/" + Path.GetFileNameWithoutExtension(str) + ".bytes";
                        File.Move(str,bytesPath);
                        AssetDatabase.Refresh();
                        skeletonPaths.Add(bytesPath);
                    }
                    break;
                case ".bytes":
                    if (IsValidDragonBonesData((TextAsset)AssetDatabase.LoadAssetAtPath(str, typeof(TextAsset)))){
                        skeletonPaths.Add(str);
                    }
                    break;
                }
            }
            if (skeletonPaths.Count == 0)
            {
                return;
            }

            foreach(string skeletonPath in skeletonPaths)
            {
                List<string> imgPaths = new List<string>();
                List<string> atlPaths = new List<string>();
                foreach(string atlasPath in atlasPaths)
                {
                    if(atlasPath.IndexOf(skeletonPath.Substring(0,skeletonPath.LastIndexOf("/")))==0)
                    {
                        atlPaths.Add(atlasPath);
                        imgPaths.Add(atlasPath.Substring(0,atlasPath.LastIndexOf(".json"))+".png");
                    }
                }

                ProcessTextureAtlasData(atlPaths);
            }
        }

        public static bool IsValidDragonBonesData (TextAsset asset)
        {
            if (asset.name.Contains("_ske"))
            {
                return true;
            }

            if(asset.text == "DBDT")
            {
                return true;
            }

            if (asset.text.IndexOf("\"armature\":") > 0)
            {
                return true;
            }


            return false;
        }

        static void ProcessTextureAtlasData(List<string> atlasPaths)
        {
            foreach(string path in atlasPaths)
            {
                TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if(ta)
                {
                    TextureDataClass tdc = JsonUtility.FromJson<TextureDataClass>(ta.text);
                    if(tdc != null && (tdc.width == 0 || tdc.height == 0))
                    {
                        //add width and height
                        string imgPath = path.Substring(0,path.IndexOf(".json"))+".png";
                        Texture2D texture = LoadPNG(Application.dataPath+"/"+ imgPath.Substring(6));
                        if(texture)
                        {
                            tdc.width = texture.width;
                            tdc.height = texture.height;
                            //save
                            string json = JsonUtility.ToJson(tdc);
                            File.WriteAllText(path,json);
                            EditorUtility.SetDirty(ta);

                            GameObject.DestroyImmediate(texture);
                        }
                    }
                }
            }
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
            }
            return tex;
        }
    }
}