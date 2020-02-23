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
    /// - The skin data, typically a armature data instance contains at least one skinData.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 皮肤数据，通常一个骨架数据至少包含一个皮肤数据。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class SkinData : BaseObject
    {
        /// <summary>
        /// - The skin name.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 皮肤名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <private/>
        public readonly Dictionary<string, List<DisplayData>> displays = new Dictionary<string, List<DisplayData>>();
        /// <private/>
        public ArmatureData parent;

        /// <inheritDoc/>
        protected override void _OnClear()
        {
            foreach (var list in this.displays.Values)
            {
                foreach (var display in list)
                {
                    display.ReturnToPool();
                }
            }

            this.name = "";
            this.displays.Clear();
            this.parent = null;
        }

        /// <internal/>
        /// <private/>
        public void AddDisplay(string slotName, DisplayData value)
        {
            if (!string.IsNullOrEmpty(slotName) && value != null && !string.IsNullOrEmpty(value.name))
            {
                if (!this.displays.ContainsKey(slotName))
                {
                    this.displays[slotName] = new List<DisplayData>();
                }

                if (value != null)
                {
                    value.parent = this;
                }

                var slotDisplays = this.displays[slotName]; // TODO clear prev
                slotDisplays.Add(value);
            }
        }
        /// <private/>
        public DisplayData GetDisplay(string slotName, string displayName)
        {
            var slotDisplays = this.GetDisplays(slotName);
            if (slotDisplays != null)
            {
                foreach (var display in slotDisplays)
                {
                    if (display != null && display.name == displayName)
                    {
                        return display;
                    }
                }
            }

            return null;
        }
        /// <private/>
        public List<DisplayData> GetDisplays(string slotName)
        {
            if (string.IsNullOrEmpty(slotName) || !this.displays.ContainsKey(slotName))
            {
                return null;
            }

            return this.displays[slotName];
        }

    }
}
