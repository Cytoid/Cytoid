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
using System;
using System.Collections.Generic;

namespace DragonBones
{
    /// <summary>
    /// - The armature data.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 骨架数据。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class ArmatureData : BaseObject
    {
        /// <private/>
        public ArmatureType type;
        /// <summary>
        /// - The animation frame rate.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画帧率。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public uint frameRate;
        /// <private/>
        public uint cacheFrameRate;
        /// <private/>
        public float scale;
        /// <summary>
        /// - The armature name.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 骨架名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <private/>
        public readonly Rectangle aabb = new Rectangle();
        /// <summary>
        /// - The names of all the animation data.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 所有的动画数据名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public readonly List<string> animationNames = new List<string>();
        /// <private/>
        public readonly List<BoneData> sortedBones = new List<BoneData>();
        /// <private/>
        public readonly List<SlotData> sortedSlots = new List<SlotData>();
        /// <private/>
        public readonly List<ActionData> defaultActions = new List<ActionData>();
        /// <private/>
        public readonly List<ActionData> actions = new List<ActionData>();
        /// <private/>
        public readonly Dictionary<string, BoneData> bones = new Dictionary<string, BoneData>();
        /// <private/>
        public readonly Dictionary<string, SlotData> slots = new Dictionary<string, SlotData>();

        /// <private/>
        public readonly Dictionary<string, ConstraintData> constraints = new Dictionary<string, ConstraintData>();
        /// <private/>
        public readonly Dictionary<string, SkinData> skins = new Dictionary<string, SkinData>();
        /// <private/>
        public readonly Dictionary<string, AnimationData> animations = new Dictionary<string, AnimationData>();

        /// <summary>
        /// - The default skin data.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 默认插槽数据。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public SkinData defaultSkin = null;
        /// <summary>
        /// - The default animation data.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 默认动画数据。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public AnimationData defaultAnimation = null;
        /// <private/>
        public CanvasData canvas = null; // Initial value.
        /// <private/>
        public UserData userData = null; // Initial value.
        /// <private/>
        public DragonBonesData parent;
        /// <inheritDoc/>
        protected override void _OnClear()
        {
            foreach (var action in this.defaultActions)
            {
                action.ReturnToPool();
            }

            foreach (var action in this.actions)
            {
                action.ReturnToPool();
            }

            foreach (var k in this.bones.Keys)
            {
                this.bones[k].ReturnToPool();
            }

            foreach (var k in this.slots.Keys)
            {
                this.slots[k].ReturnToPool();
            }

            foreach (var k in this.constraints.Keys)
            {
                this.constraints[k].ReturnToPool();
            }

            foreach (var k in this.skins.Keys)
            {
                this.skins[k].ReturnToPool();
            }

            foreach (var k in this.animations.Keys)
            {
                this.animations[k].ReturnToPool();
            }

            if (this.canvas != null)
            {
                this.canvas.ReturnToPool();
            }

            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.type = ArmatureType.Armature;
            this.frameRate = 0;
            this.cacheFrameRate = 0;
            this.scale = 1.0f;
            this.name = "";
            this.aabb.Clear();
            this.animationNames.Clear();
            this.sortedBones.Clear();
            this.sortedSlots.Clear();
            this.defaultActions.Clear();
            this.actions.Clear();
            this.bones.Clear();
            this.slots.Clear();
            this.constraints.Clear();
            this.skins.Clear();
            this.animations.Clear();
            this.defaultSkin = null;
            this.defaultAnimation = null;
            this.canvas = null;
            this.userData = null;
            this.parent = null; //
        }

        /// <internal/>
        /// <private/>
        public void SortBones()
        {
            var total = this.sortedBones.Count;
            if (total <= 0)
            {
                return;
            }

            var sortHelper = this.sortedBones.ToArray();
            var index = 0;
            var count = 0;
            this.sortedBones.Clear();
            while (count < total)
            {
                var bone = sortHelper[index++];
                if (index >= total)
                {
                    index = 0;
                }

                if (this.sortedBones.Contains(bone))
                {
                    continue;
                }

                var flag = false;
                foreach (var constraint in this.constraints.Values)
                {
                    // Wait constraint.
                    if (constraint.root == bone && !this.sortedBones.Contains(constraint.target))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    continue;
                }
                if (bone.parent != null && !this.sortedBones.Contains(bone.parent))
                {
                    // Wait parent.
                    continue;
                }

                this.sortedBones.Add(bone);
                count++;
            }
        }

        /// <internal/>
        /// <private/>
        public void CacheFrames(uint frameRate)
        {
            if (this.cacheFrameRate > 0)
            {
                // TODO clear cache.
                return;
            }

            this.cacheFrameRate = frameRate;
            foreach (var k in this.animations.Keys)
            {
                this.animations[k].CacheFrames(this.cacheFrameRate);
            }
        }

        /// <internal/>
        /// <private/>
        public int SetCacheFrame(Matrix globalTransformMatrix, Transform transform)
        {
            var dataArray = this.parent.cachedFrames;
            var arrayOffset = dataArray.Count;

            dataArray.ResizeList(arrayOffset + 10, 0.0f);

            dataArray[arrayOffset] = globalTransformMatrix.a;
            dataArray[arrayOffset + 1] = globalTransformMatrix.b;
            dataArray[arrayOffset + 2] = globalTransformMatrix.c;
            dataArray[arrayOffset + 3] = globalTransformMatrix.d;
            dataArray[arrayOffset + 4] = globalTransformMatrix.tx;
            dataArray[arrayOffset + 5] = globalTransformMatrix.ty;
            dataArray[arrayOffset + 6] = transform.rotation;
            dataArray[arrayOffset + 7] = transform.skew;
            dataArray[arrayOffset + 8] = transform.scaleX;
            dataArray[arrayOffset + 9] = transform.scaleY;

            return arrayOffset;
        }

        /// <internal/>
        /// <private/>
        public void GetCacheFrame(Matrix globalTransformMatrix, Transform transform, int arrayOffset)
        {
            var dataArray = this.parent.cachedFrames;
            globalTransformMatrix.a = dataArray[arrayOffset];
            globalTransformMatrix.b = dataArray[arrayOffset + 1];
            globalTransformMatrix.c = dataArray[arrayOffset + 2];
            globalTransformMatrix.d = dataArray[arrayOffset + 3];
            globalTransformMatrix.tx = dataArray[arrayOffset + 4];
            globalTransformMatrix.ty = dataArray[arrayOffset + 5];
            transform.rotation = dataArray[arrayOffset + 6];
            transform.skew = dataArray[arrayOffset + 7];
            transform.scaleX = dataArray[arrayOffset + 8];
            transform.scaleY = dataArray[arrayOffset + 9];
            transform.x = globalTransformMatrix.tx;
            transform.y = globalTransformMatrix.ty;
        }

        /// <internal/>
        /// <private/>
        public void AddBone(BoneData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.bones.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same bone: " + value.name);
                    this.bones[value.name].ReturnToPool();
                }

                this.bones[value.name] = value;
                this.sortedBones.Add(value);
            }
        }
        /// <internal/>
        /// <private/>
        public void AddSlot(SlotData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.slots.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same slot: " + value.name);
                    this.slots[value.name].ReturnToPool();
                }

                this.slots[value.name] = value;
                this.sortedSlots.Add(value);
            }
        }
        /// <internal/>
        /// <private/>
        public void AddConstraint(ConstraintData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.constraints.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same constraint: " + value.name);
                    this.slots[value.name].ReturnToPool();
                }

                this.constraints[value.name] = value;
            }
        }
        /// <internal/>
        /// <private/>
        public void AddSkin(SkinData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.skins.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same slot: " + value.name);
                    this.skins[value.name].ReturnToPool();
                }

                value.parent = this;
                this.skins[value.name] = value;
                if (this.defaultSkin == null)
                {
                    this.defaultSkin = value;
                }

                if (value.name == "default")
                {
                    this.defaultSkin = value;
                }
            }
        }
        /// <internal/>
        /// <private/>
        public void AddAnimation(AnimationData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.animations.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same animation: " + value.name);
                    this.animations[value.name].ReturnToPool();
                }

                value.parent = this;
                this.animations[value.name] = value;
                this.animationNames.Add(value.name);
                if (this.defaultAnimation == null)
                {
                    this.defaultAnimation = value;
                }
            }
        }
        /// <internal/>
        /// <private/>
        internal void AddAction(ActionData value, bool isDefault)
        {
            if (isDefault)
            {
                this.defaultActions.Add(value);
            }
            else
            {
                this.actions.Add(value);
            }
        }

        /// <summary>
        /// - Get a specific done data.
        /// </summary>
        /// <param name="boneName">- The bone name.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 获取特定的骨骼数据。
        /// </summary>
        /// <param name="boneName">- 骨骼名称。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public BoneData GetBone(string boneName)
        {
            return (!string.IsNullOrEmpty(boneName) && bones.ContainsKey(boneName)) ? bones[boneName] : null;
        }
        /// <summary>
        /// - Get a specific slot data.
        /// </summary>
        /// <param name="slotName">- The slot name.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 获取特定的插槽数据。
        /// </summary>
        /// <param name="slotName">- 插槽名称。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public SlotData GetSlot(string slotName)
        {
            return (!string.IsNullOrEmpty(slotName) && slots.ContainsKey(slotName)) ? slots[slotName] : null;
        }
        /// <private/>
        public ConstraintData GetConstraint(string constraintName)
        {
            return this.constraints.ContainsKey(constraintName) ? this.constraints[constraintName] : null;
        }
        /// <summary>
        /// - Get a specific skin data.
        /// </summary>
        /// <param name="skinName">- The skin name.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 获取特定皮肤数据。
        /// </summary>
        /// <param name="skinName">- 皮肤名称。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public SkinData GetSkin(string skinName)
        {
            return !string.IsNullOrEmpty(skinName) ? (skins.ContainsKey(skinName) ? skins[skinName] : null) : defaultSkin;
        }

        /// <private/>
        public MeshDisplayData GetMesh(string skinName, string slotName, string meshName)
        {
            var skin = this.GetSkin(skinName);
            if (skin == null)
            {
                return null;
            }

            return skin.GetDisplay(slotName, meshName) as MeshDisplayData;
        }
        /// <summary>
        /// - Get a specific animation data.
        /// </summary>
        /// <param name="animationName">- The animation animationName.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 获取特定的动画数据。
        /// </summary>
        /// <param name="animationName">- 动画名称。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public AnimationData GetAnimation(string animationName)
        {
            return !string.IsNullOrEmpty(animationName) ? (animations.ContainsKey(animationName) ? animations[animationName] : null) : defaultAnimation;
        }
    }

    /// <summary>
    /// - The bone data.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 骨骼数据。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class BoneData : BaseObject
    {
        /// <private/>
        public bool inheritTranslation;
        /// <private/>
        public bool inheritRotation;
        /// <private/>
        public bool inheritScale;
        /// <private/>
        public bool inheritReflection;
        /// <summary>
        /// - The bone length.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 骨骼长度。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float length;
        /// <summary>
        /// - The bone name.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 骨骼名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <private/>
        public readonly Transform transform = new Transform();
        /// <private/>
        public UserData userData = null; // Initial value.
        /// <summary>
        /// - The parent bone data.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 父骨骼数据。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public BoneData parent = null;

        /// <inheritDoc/>
        protected override void _OnClear()
        {
            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.inheritTranslation = false;
            this.inheritRotation = false;
            this.inheritScale = false;
            this.inheritReflection = false;
            this.length = 0.0f;
            this.name = "";
            this.transform.Identity();
            this.userData = null;
            this.parent = null;
        }
    }

    /// <internal/>
    /// <private/>
    public class SurfaceData : BoneData
    {
        public float vertexCountX;
        public float vertexCountY;
        public readonly List<float> vertices = new List<float>();
        /// <inheritDoc/>
        protected override void _OnClear()
        {
            base._OnClear();

            this.vertexCountX = 0;
            this.vertexCountY = 0;
            this.vertices.Clear();
        }
    }

    /// <summary>
    /// - The slot data.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 插槽数据。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class SlotData : BaseObject
    {
        /// <internal/>
        /// <private/>
        public static readonly ColorTransform DEFAULT_COLOR = new ColorTransform();

        /// <internal/>
        /// <private/>
        public static ColorTransform CreateColor()
        {
            return new ColorTransform();
        }

        /// <private/>
        public BlendMode blendMode;
        /// <private/>
        public int displayIndex;
        /// <private/>
        public int zOrder;
        /// <summary>
        /// - The slot name.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <private/>
        public ColorTransform color = null; // Initial value.
        /// <private/>
        public UserData userData = null; // Initial value.
        /// <summary>
        /// - The parent bone data.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 父骨骼数据。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public BoneData parent;
        /// <inheritDoc/>
        protected override void _OnClear()
        {
            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.blendMode = BlendMode.Normal;
            this.displayIndex = 0;
            this.zOrder = 0;
            this.name = "";
            this.color = null; //
            this.userData = null;
            this.parent = null; //
        }
    }
}