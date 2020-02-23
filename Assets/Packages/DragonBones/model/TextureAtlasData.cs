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
﻿using System.Collections.Generic;

namespace DragonBones
{
    /// <summary>
    /// - The texture atlas data.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 贴图集数据。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public abstract class TextureAtlasData : BaseObject
    {
        /// <private/>
        public bool autoSearch;
        /// <private/>
        public uint width;
        /// <private/>
        public uint height;
        /// <private/>
        public float scale;
        /// <summary>
        /// - The texture atlas name.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 贴图集名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <summary>
        /// - The image path of the texture atlas.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 贴图集图片路径。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string imagePath;
        /// <private/>
        public readonly Dictionary<string, TextureData> textures = new Dictionary<string, TextureData>();
        public TextureAtlasData()
        {
        }
        /// <inheritDoc/>
        protected override void _OnClear()
        {
            foreach (var value in this.textures.Values)
            {
                value.ReturnToPool();
            }

            this.autoSearch = false;
            this.width = 0;
            this.height = 0;
            this.scale = 1.0f;
            this.textures.Clear();
            this.name = "";
            this.imagePath = "";
        }
        /// <private/>
        public void CopyFrom(TextureAtlasData value)
        {
            this.autoSearch = value.autoSearch;
            this.scale = value.scale;
            this.width = value.width;
            this.height = value.height;
            this.name = value.name;
            this.imagePath = value.imagePath;

            foreach (var texture in this.textures.Values)
            {
                texture.ReturnToPool();
            }

            this.textures.Clear();

            foreach (var pair in value.textures)
            {
                var texture = CreateTexture();
                texture.CopyFrom(pair.Value);
                textures[pair.Key] = texture;
            }
        }
        /// <internal/>
        /// <private/>
        public abstract TextureData CreateTexture();
        /// <internal/>
        /// <private/>
        public void AddTexture(TextureData value)
        {
            if (value != null)
            {
                if (this.textures.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same texture: " + value.name);
                    this.textures[value.name].ReturnToPool();
                }

                value.parent = this;
                this.textures[value.name] = value;
            }
        }
        /// <private/>
        public TextureData GetTexture(string name)
        {
            return textures.ContainsKey(name) ? textures[name] : null;
        }
        
    }
    /// <internal/>
    /// <private/>
    public abstract class TextureData : BaseObject
    {
        public static Rectangle CreateRectangle()
        {
            return new Rectangle();
        }

        public bool rotated;
        public string name;
        public readonly Rectangle region = new Rectangle();
        public TextureAtlasData parent;
        public Rectangle frame = null; // Initial value.

        protected override void _OnClear()
        {
            this.rotated = false;
            this.name = "";
            this.region.Clear();
            this.parent = null; //
            this.frame = null;
        }

        public virtual void CopyFrom(TextureData value)
        {
            this.rotated = value.rotated;
            this.name = value.name;
            this.region.CopyFrom(value.region);
            this.parent = value.parent;

            if (this.frame == null && value.frame != null)
            {
                this.frame = TextureData.CreateRectangle();
            }
            else if (this.frame != null && value.frame == null)
            {
                this.frame = null;
            }

            if (this.frame != null && value.frame != null)
            {
                this.frame.CopyFrom(value.frame);
            }
        }
    }
}