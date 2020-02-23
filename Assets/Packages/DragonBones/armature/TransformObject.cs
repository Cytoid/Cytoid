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
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DragonBones
{
    /// <summary>
    /// - The base class of the transform object.
    /// </summary>
    /// <see cref="DragonBones.Transform"/>
    /// <version>DragonBones 4.5</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 变换对象的基类。
    /// </summary>
    /// <see cref="DragonBones.Transform"/>
    /// <version>DragonBones 4.5</version>
    /// <language>zh_CN</language>
    public abstract class TransformObject : BaseObject
    {
        /// <private/>
        protected static readonly Matrix _helpMatrix  = new Matrix();
        /// <private/>
        protected static readonly Transform _helpTransform  = new Transform();
        /// <private/>
        protected static readonly Point _helpPoint = new Point();
        /// <summary>
        /// - A matrix relative to the armature coordinate system.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 相对于骨架坐标系的矩阵。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public readonly Matrix globalTransformMatrix = new Matrix();
        /// <summary>
        /// - A transform relative to the armature coordinate system.
        /// </summary>
        /// <see cref="UpdateGlobalTransform()"/>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 相对于骨架坐标系的变换。
        /// </summary>
        /// <see cref="UpdateGlobalTransform()"/>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public readonly Transform global = new Transform();
        /// <summary>
        /// - The offset transform relative to the armature or the parent bone coordinate system.
        /// </summary>
        /// <see cref="dragonBones.Bone.InvalidUpdate()"/>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 相对于骨架或父骨骼坐标系的偏移变换。
        /// </summary>
        /// <see cref="dragonBones.Bone.InvalidUpdate()"/>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public readonly Transform offset = new Transform();
        /// <private/>
        public Transform origin;
        /// <private/>
        public object userData;
        /// <private/>
        protected bool _globalDirty;
        /// <internal/>
        /// <private/>
        internal Armature _armature;
        /// <private/>
        protected override void _OnClear()
        {
            this.globalTransformMatrix.Identity();
            this.global.Identity();
            this.offset.Identity();
            this.origin = null; //
            this.userData = null;

            this._globalDirty = false;
            this._armature = null; //
        }
        /// <summary>
        /// - For performance considerations, rotation or scale in the {@link #global} attribute of the bone or slot is not always properly accessible,
        /// some engines do not rely on these attributes to update rendering, such as Egret.
        /// The use of this method ensures that the access to the {@link #global} property is correctly rotation or scale.
        /// </summary>
        /// <example>
        /// TypeScript style, for reference only.
        /// <pre>
        ///     bone.updateGlobalTransform();
        ///     let rotation = bone.global.rotation;
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 出于性能的考虑，骨骼或插槽的 {@link #global} 属性中的旋转或缩放并不总是正确可访问的，有些引擎并不依赖这些属性更新渲染，比如 Egret。
        /// 使用此方法可以保证访问到 {@link #global} 属性中正确的旋转或缩放。
        /// </summary>
        /// <example>
        /// TypeScript 风格，仅供参考。
        /// <pre>
        ///     bone.updateGlobalTransform();
        ///     let rotation = bone.global.rotation;
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void UpdateGlobalTransform()
        {
            if (this._globalDirty)
            {
                this._globalDirty = false;
                this.global.FromMatrix(this.globalTransformMatrix);
            }
        }
        /// <summary>
        /// - The armature to which it belongs.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 所属的骨架。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public Armature armature
        {
            get{ return this._armature; }
        }
    }
}
