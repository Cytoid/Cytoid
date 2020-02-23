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
    /// - Bone is one of the most important logical units in the armature animation system,
    /// and is responsible for the realization of translate, rotation, scaling in the animations.
    /// A armature can contain multiple bones.
    /// </summary>
    /// <see cref="DragonBones.BoneData"/>
    /// <see cref="DragonBones.Armature"/>
    /// <see cref="DragonBones.Slot"/>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 骨骼在骨骼动画体系中是最重要的逻辑单元之一，负责动画中的平移、旋转、缩放的实现。
    /// 一个骨架中可以包含多个骨骼。
    /// </summary>
    /// <see cref="DragonBones.BoneData"/>
    /// <see cref="DragonBones.Armature"/>
    /// <see cref="DragonBones.Slot"/>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class Bone : TransformObject
    {
        /// <summary>
        /// - The offset mode.
        /// </summary>
        /// <see cref="offset"/>
        /// <version>DragonBones 5.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 偏移模式。
        /// </summary>
        /// <see cref="offset"/>
        /// <version>DragonBones 5.5</version>
        /// <language>zh_CN</language>
        internal OffsetMode offsetMode;
        /// <internal/>
        /// <private/>
        internal readonly Transform animationPose = new Transform();
        /// <internal/>
        /// <private/>
        internal bool _transformDirty;
        /// <internal/>
        /// <private/>
        internal bool _childrenTransformDirty;
        private bool _localDirty;

        /// <internal/>
        /// <private/>
        internal bool _hasConstraint;
        private bool _visible;
        private int _cachedFrameIndex;
        /// <internal/>
        /// <private/>
        internal readonly BlendState _blendState = new BlendState();
        /// <internal/>
        /// <private/>
        internal BoneData _boneData;
        /// <private/>
        protected Bone _parent;
        /// <internal/>
        /// <private/>
        internal List<int> _cachedFrameIndices = new List<int>();
        /// <inheritDoc/>
        protected override void _OnClear()
        {
            base._OnClear();

            this.offsetMode = OffsetMode.Additive;
            this.animationPose.Identity();

            this._transformDirty = false;
            this._childrenTransformDirty = false;
            this._localDirty = true;
            this._hasConstraint = false;
            this._visible = true;
            this._cachedFrameIndex = -1;
            this._blendState.Clear();
            this._boneData = null; //
            this._parent = null;
            this._cachedFrameIndices = null;
        }
        /// <private/>
        private void _UpdateGlobalTransformMatrix(bool isCache)
        {
            var boneData = this._boneData;
            var parent = this._parent;
            var flipX = this._armature.flipX;
            var flipY = this._armature.flipY == DragonBones.yDown;
            var rotation = 0.0f;
            var global = this.global;
            var inherit = parent != null;
            var globalTransformMatrix = this.globalTransformMatrix;

            if (this.offsetMode == OffsetMode.Additive)
            {
                if (this.origin != null)
                {
                    //global.CopyFrom(this.origin).Add(this.offset).Add(this.animationPose);
                    global.x = this.origin.x + this.offset.x + this.animationPose.x;
                    global.y = this.origin.y + this.offset.y + this.animationPose.y;
                    global.skew = this.origin.skew + this.offset.skew + this.animationPose.skew;
                    global.rotation = this.origin.rotation + this.offset.rotation + this.animationPose.rotation;
                    global.scaleX = this.origin.scaleX * this.offset.scaleX * this.animationPose.scaleX;
                    global.scaleY = this.origin.scaleY * this.offset.scaleY * this.animationPose.scaleY;
                }
                else
                {
                    global.CopyFrom(this.offset).Add(this.animationPose);
                }

            }
            else if (this.offsetMode == OffsetMode.None)
            {
                if (this.origin != null)
                {
                    global.CopyFrom(this.origin).Add(this.animationPose);
                }
                else
                {
                    global.CopyFrom(this.animationPose);
                }
            }
            else
            {
                inherit = false;
                global.CopyFrom(this.offset);
            }

            if (inherit)
            {
                var parentMatrix = parent.globalTransformMatrix;
                if (boneData.inheritScale)
                {
                    if (!boneData.inheritRotation)
                    {
                        parent.UpdateGlobalTransform();

                        if (flipX && flipY)
                        {
                            rotation = global.rotation - (parent.global.rotation + Transform.PI);
                        }
                        else if (flipX)
                        {
                            rotation = global.rotation + parent.global.rotation + Transform.PI;
                        }
                        else if (flipY)
                        {
                            rotation = global.rotation + parent.global.rotation;
                        }
                        else
                        {
                            rotation = global.rotation - parent.global.rotation;
                        }

                        global.rotation = rotation;
                    }

                    global.ToMatrix(globalTransformMatrix);
                    globalTransformMatrix.Concat(parentMatrix);

                    if (this._boneData.inheritTranslation)
                    {
                        global.x = globalTransformMatrix.tx;
                        global.y = globalTransformMatrix.ty;
                    }
                    else
                    {
                        globalTransformMatrix.tx = global.x;
                        globalTransformMatrix.ty = global.y;
                    }

                    if (isCache)
                    {
                        global.FromMatrix(globalTransformMatrix);
                    }
                    else
                    {
                        this._globalDirty = true;
                    }
                }
                else
                {
                    if (boneData.inheritTranslation)
                    {
                        var x = global.x;
                        var y = global.y;
                        global.x = parentMatrix.a * x + parentMatrix.c * y + parentMatrix.tx;
                        global.y = parentMatrix.b * x + parentMatrix.d * y + parentMatrix.ty;
                    }
                    else
                    {
                        if (flipX)
                        {
                            global.x = -global.x;
                        }

                        if (flipY)
                        {
                            global.y = -global.y;
                        }
                    }

                    if (boneData.inheritRotation)
                    {
                        parent.UpdateGlobalTransform();
                        if (parent.global.scaleX < 0.0)
                        {
                            rotation = global.rotation + parent.global.rotation + Transform.PI;
                        }
                        else
                        {
                            rotation = global.rotation + parent.global.rotation;
                        }

                        if (parentMatrix.a * parentMatrix.d - parentMatrix.b * parentMatrix.c < 0.0)
                        {
                            rotation -= global.rotation * 2.0f;

                            if (flipX != flipY || boneData.inheritReflection)
                            {
                                global.skew += Transform.PI;
                            }
                        }

                        global.rotation = rotation;
                    }
                    else if (flipX || flipY)
                    {
                        if (flipX && flipY)
                        {
                            rotation = global.rotation + Transform.PI;
                        }
                        else
                        {
                            if (flipX)
                            {
                                rotation = Transform.PI - global.rotation;
                            }
                            else
                            {
                                rotation = -global.rotation;
                            }

                            global.skew += Transform.PI;
                        }

                        global.rotation = rotation;
                    }

                    global.ToMatrix(globalTransformMatrix);
                }
            }
            else
            {
                if (flipX || flipY)
                {
                    if (flipX)
                    {
                        global.x = -global.x;
                    }

                    if (flipY)
                    {
                        global.y = -global.y;
                    }

                    if (flipX && flipY)
                    {
                        rotation = global.rotation + Transform.PI;
                    }
                    else
                    {
                        if (flipX)
                        {
                            rotation = Transform.PI - global.rotation;
                        }
                        else
                        {
                            rotation = -global.rotation;
                        }

                        global.skew += Transform.PI;
                    }

                    global.rotation = rotation;
                }

                global.ToMatrix(globalTransformMatrix);
            }
        }
        /// <internal/>
        /// <private/>
        internal void Init(BoneData boneData, Armature armatureValue)
        {
            if (this._boneData != null)
            {
                return;
            }

            this._boneData = boneData;
            this._armature = armatureValue;

            if (this._boneData.parent != null)
            {
                this._parent = this._armature.GetBone(this._boneData.parent.name);
            }

            this._armature._AddBone(this);
            //
            this.origin = this._boneData.transform;
        }
        /// <internal/>
        /// <private/>
        internal void Update(int cacheFrameIndex)
        {
            this._blendState.dirty = false;

            if (cacheFrameIndex >= 0 && this._cachedFrameIndices != null)
            {
                var cachedFrameIndex = this._cachedFrameIndices[cacheFrameIndex];

                if (cachedFrameIndex >= 0 && this._cachedFrameIndex == cachedFrameIndex)
                {
                    // Same cache.
                    this._transformDirty = false;
                }
                else if (cachedFrameIndex >= 0)
                {
                    // Has been Cached.
                    this._transformDirty = true;
                    this._cachedFrameIndex = cachedFrameIndex;
                }
                else
                {
                    if (this._hasConstraint)
                    {
                        // Update constraints.
                        foreach (var constraint in this._armature._constraints)
                        {
                            if (constraint._root == this)
                            {
                                constraint.Update();
                            }
                        }
                    }

                    if (this._transformDirty || (this._parent != null && this._parent._childrenTransformDirty))
                    {
                        // Dirty.
                        this._transformDirty = true;
                        this._cachedFrameIndex = -1;
                    }
                    else if (this._cachedFrameIndex >= 0)
                    {
                        // Same cache, but not set index yet.
                        this._transformDirty = false;
                        this._cachedFrameIndices[cacheFrameIndex] = this._cachedFrameIndex;
                    }
                    else
                    {
                        // Dirty.
                        this._transformDirty = true;
                        this._cachedFrameIndex = -1;
                    }
                }
            }
            else
            {
                if (this._hasConstraint)
                {
                    // Update constraints.
                    foreach (var constraint in this._armature._constraints)
                    {
                        if (constraint._root == this)
                        {
                            constraint.Update();
                        }
                    }
                }

                if (this._transformDirty || (this._parent != null && this._parent._childrenTransformDirty))
                {
                    // Dirty.
                    cacheFrameIndex = -1;
                    this._transformDirty = true;
                    this._cachedFrameIndex = -1;
                }
            }

            if (this._transformDirty)
            {
                this._transformDirty = false;
                this._childrenTransformDirty = true;

                if (this._cachedFrameIndex < 0)
                {
                    var isCache = cacheFrameIndex >= 0;
                    if (this._localDirty)
                    {
                        this._UpdateGlobalTransformMatrix(isCache);
                    }

                    if (isCache && this._cachedFrameIndices != null)
                    {
                        this._cachedFrameIndex = this._cachedFrameIndices[cacheFrameIndex] = this._armature._armatureData.SetCacheFrame(this.globalTransformMatrix, this.global);
                    }
                }
                else
                {
                    this._armature._armatureData.GetCacheFrame(this.globalTransformMatrix, this.global, this._cachedFrameIndex);
                }
            }
            else if (this._childrenTransformDirty)
            {
                this._childrenTransformDirty = false;
            }

            this._localDirty = true;
        }
        /// <internal/>
        /// <private/>
        internal void UpdateByConstraint()
        {
            if (this._localDirty)
            {
                this._localDirty = false;

                if (this._transformDirty || (this._parent != null && this._parent._childrenTransformDirty))
                {
                    this._UpdateGlobalTransformMatrix(true);
                }

                this._transformDirty = true;
            }
        }
        /// <summary>
        /// - Forces the bone to update the transform in the next frame.
        /// When the bone is not animated or its animation state is finished, the bone will not continue to update,
        /// and when the skeleton must be updated for some reason, the method needs to be called explicitly.
        /// </summary>
        /// <example>
        /// TypeScript style, for reference only.
        /// <pre>
        ///     let bone = armature.getBone("arm");
        ///     bone.offset.scaleX = 2.0;
        ///     bone.invalidUpdate();
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 强制骨骼在下一帧更新变换。
        /// 当该骨骼没有动画状态或其动画状态播放完成时，骨骼将不在继续更新，而此时由于某些原因必须更新骨骼时，则需要显式调用该方法。
        /// </summary>
        /// <example>
        /// TypeScript 风格，仅供参考。
        /// <pre>
        ///     let bone = armature.getBone("arm");
        ///     bone.offset.scaleX = 2.0;
        ///     bone.invalidUpdate();
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void InvalidUpdate()
        {
            this._transformDirty = true;
        }
        /// <summary>
        /// - Check whether the bone contains a specific bone or slot.
        /// </summary>
        /// <see cref="DragonBones.Bone"/>
        /// <see cref="DragonBones.Slot"/>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查该骨骼是否包含特定的骨骼或插槽。
        /// </summary>
        /// <see cref="DragonBones.Bone"/>
        /// <see cref="DragonBones.Slot"/>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public bool Contains(Bone value)
        {
            if (value == this)
            {
                return false;
            }

            Bone ancestor = value;
            while (ancestor != this && ancestor != null)
            {
                ancestor = ancestor.parent;
            }

            return ancestor == this;
        }
        /// <summary>
        /// - The bone data.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 骨骼数据。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public BoneData boneData
        {
            get { return this._boneData; }
        }

        /// <summary>
        /// - The visible of all slots in the bone.
        /// </summary>
        /// <default>true</default>
        /// <see cref="DragonBones.Slot.visible"/>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 此骨骼所有插槽的可见。
        /// </summary>
        /// <default>true</default>
        /// <see cref="DragonBones.Slot.visible"/>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public bool visible
        {
            get { return this._visible; }
            set
            {
                if (this._visible == value)
                {
                    return;
                }

                this._visible = value;

                foreach (var slot in this._armature.GetSlots())
                {
                    if (slot.parent == this)
                    {
                        slot._UpdateVisible();
                    }
                }
            }
        }

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
        public string name
        {
            get { return this._boneData.name; }
        }

        /// <summary>
        /// - The parent bone to which it belongs.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 所属的父骨骼。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public Bone parent
        {
            get { return this._parent; }
        }
        /// <summary>
        /// - Deprecated, please refer to {@link dragonBones.Armature#getSlot()}.
        /// </summary>
        /// <language>en_US</language>

        /// <summary>
        /// - 已废弃，请参考 {@link dragonBones.Armature#getSlot()}。
        /// </summary>
        /// <language>zh_CN</language>
        [System.Obsolete("")]
        public Slot slot
        {
            get
            {
                foreach (var slot in this._armature.GetSlots())
                {
                    if (slot.parent == this)
                    {
                        return slot;
                    }
                }

                return null;
            }
        }
    }
}
