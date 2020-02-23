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
    /// - The slot attached to the armature, controls the display status and properties of the display object.
    /// A bone can contain multiple slots.
    /// A slot can contain multiple display objects, displaying only one of the display objects at a time,
    /// but you can toggle the display object into frame animation while the animation is playing.
    /// The display object can be a normal texture, or it can be a display of a child armature, a grid display object,
    /// and a custom other display object.
    /// </summary>
    /// <see cref="DragonBones.Armature"/>
    /// <see cref="DragonBones.Bone"/>
    /// <see cref="DragonBones.SlotData"/>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 插槽附着在骨骼上，控制显示对象的显示状态和属性。
    /// 一个骨骼上可以包含多个插槽。
    /// 一个插槽中可以包含多个显示对象，同一时间只能显示其中的一个显示对象，但可以在动画播放的过程中切换显示对象实现帧动画。
    /// 显示对象可以是普通的图片纹理，也可以是子骨架的显示容器，网格显示对象，还可以是自定义的其他显示对象。
    /// </summary>
    /// <see cref="DragonBones.Armature"/>
    /// <see cref="DragonBones.Bone"/>
    /// <see cref="DragonBones.SlotData"/>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public abstract class Slot : TransformObject
    {
        /// <summary>
        /// - Displays the animated state or mixed group name controlled by the object, set to null to be controlled by all animation states.
        /// </summary>
        /// <default>null</default>
        /// <see cref="DragonBones.AnimationState.displayControl"/>
        /// <see cref="DragonBones.AnimationState.name"/>
        /// <see cref="DragonBones.AnimationState.group"/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 显示对象受到控制的动画状态或混合组名称，设置为 null 则表示受所有的动画状态控制。
        /// </summary>
        /// <default>null</default>
        /// <see cref="DragonBones.AnimationState.displayControl"/>
        /// <see cref="DragonBones.AnimationState.name"/>
        /// <see cref="DragonBones.AnimationState.group"/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public string displayController;
        /// <private/>
        protected bool _displayDirty;
        /// <private/>
        protected bool _zOrderDirty;
        /// <private/>
        protected bool _visibleDirty;
        /// <private/>
        protected bool _blendModeDirty;
        /// <internal/>
        /// <private/>
        internal bool _colorDirty;
        /// <private/>
        internal bool _transformDirty;
        /// <private/>
        protected bool _visible;
        /// <private/>
        internal BlendMode _blendMode;
        /// <private/>
        protected int _displayIndex;
        /// <private/>
        protected int _animationDisplayIndex;
        /// <internal/>
        /// <private/>
        internal int _zOrder;
        /// <private/>
        protected int _cachedFrameIndex;
        /// <internal/>
        /// <private/>
        internal float _pivotX;
        /// <internal/>
        /// <private/>
        internal float _pivotY;
        /// <private/>
        protected readonly Matrix _localMatrix = new Matrix();
        /// <internal/>
        /// <private/>
        internal readonly ColorTransform _colorTransform = new ColorTransform();
        /// <private/>
        internal readonly List<DisplayData> _displayDatas = new List<DisplayData>();
        /// <private/>
        protected readonly List<object> _displayList = new List<object>();
        /// <internal/>
        /// <private/>
        internal SlotData _slotData;
        /// <private/>
        protected List<DisplayData> _rawDisplayDatas;
        /// <internal/>
        /// <private/>
        protected DisplayData _displayData;
        /// <private/>
        protected BoundingBoxData _boundingBoxData;
        /// <private/>
        protected TextureData _textureData;
        /// <internal/>
        public DeformVertices _deformVertices;
        /// <private/>
        protected object _rawDisplay;
        /// <private/>
        protected object _meshDisplay;
        /// <private/>
        protected object _display;
        /// <private/>
        protected Armature _childArmature;
        /// <private/>
        protected Bone _parent;
        /// <internal/>
        /// <private/>
        internal List<int> _cachedFrameIndices = new List<int>();
        public Slot()
        {
        }
        /// <inheritDoc/>
        protected override void _OnClear()
        {
            base._OnClear();

            var disposeDisplayList = new List<object>();
            for (int i = 0, l = _displayList.Count; i < l; ++i)
            {
                var eachDisplay = _displayList[i];
                if (eachDisplay != _rawDisplay && eachDisplay != _meshDisplay && !disposeDisplayList.Contains(eachDisplay))
                {
                    disposeDisplayList.Add(eachDisplay);
                }
            }

            for (int i = 0, l = disposeDisplayList.Count; i < l; ++i)
            {
                var eachDisplay = disposeDisplayList[i];
                if (eachDisplay is Armature)
                {
                    (eachDisplay as Armature).Dispose();
                }
                else
                {
                    this._DisposeDisplay(eachDisplay, true);
                }
            }

            if (this._deformVertices != null)
            {
                this._deformVertices.ReturnToPool();
            }

            if (this._meshDisplay != null && this._meshDisplay != this._rawDisplay)
            {
                // May be _meshDisplay and _rawDisplay is the same one.
                this._DisposeDisplay(this._meshDisplay, false);
            }

            if (this._rawDisplay != null)
            {
                this._DisposeDisplay(this._rawDisplay, false);
            }

            this.displayController = null;

            this._displayDirty = false;
            this._zOrderDirty = false;
            this._blendModeDirty = false;
            this._colorDirty = false;
            this._transformDirty = false;
            this._visible = true;
            this._blendMode = BlendMode.Normal;
            this._displayIndex = -1;
            this._animationDisplayIndex = -1;
            this._zOrder = 0;
            this._cachedFrameIndex = -1;
            this._pivotX = 0.0f;
            this._pivotY = 0.0f;
            this._localMatrix.Identity();
            this._colorTransform.Identity();
            this._displayList.Clear();
            this._displayDatas.Clear();
            this._slotData = null; //
            this._rawDisplayDatas = null; //
            this._displayData = null;
            this._boundingBoxData = null;
            this._textureData = null;
            this._deformVertices = null;
            this._rawDisplay = null;
            this._meshDisplay = null;
            this._display = null;
            this._childArmature = null;
            this._parent = null;
            this._cachedFrameIndices = null;
        }

        /// <private/>
        protected abstract void _InitDisplay(object value, bool isRetain);
        /// <private/>
        protected abstract void _DisposeDisplay(object value, bool isRelease);
        /// <private/>
        protected abstract void _OnUpdateDisplay();
        /// <private/>
        protected abstract void _AddDisplay();
        /// <private/>
        protected abstract void _ReplaceDisplay(object value);
        /// <private/>
        protected abstract void _RemoveDisplay();
        /// <private/>
        protected abstract void _UpdateZOrder();
        /// <private/>
        internal abstract void _UpdateVisible();
        /// <private/>
        internal abstract void _UpdateBlendMode();
        /// <private/>
        protected abstract void _UpdateColor();
        /// <private/>
        protected abstract void _UpdateFrame();
        /// <private/>
        protected abstract void _UpdateMesh();
        /// <private/>
        protected abstract void _UpdateTransform();
        /// <private/>
        protected abstract void _IdentityTransform();

        /// <summary>
        /// - Support default skin data.
        /// </summary>
        /// <private/>
        protected DisplayData _GetDefaultRawDisplayData(int displayIndex)
        {
            var defaultSkin = this._armature._armatureData.defaultSkin;
            if (defaultSkin != null)
            {
                var defaultRawDisplayDatas = defaultSkin.GetDisplays(this._slotData.name);
                if (defaultRawDisplayDatas != null)
                {
                    return displayIndex < defaultRawDisplayDatas.Count ? defaultRawDisplayDatas[displayIndex] : null;
                }
            }

            return null;
        }

        /// <private/>
        protected void _UpdateDisplayData()
        {
            var prevDisplayData = this._displayData;
            var prevVerticesData = this._deformVertices != null ? this._deformVertices.verticesData : null;
            var prevTextureData = this._textureData;

            DisplayData rawDisplayData = null;
            VerticesData currentVerticesData = null;

            this._displayData = null;
            this._boundingBoxData = null;
            this._textureData = null;

            if (this._displayIndex >= 0)
            {
                if (this._rawDisplayDatas != null)
                {
                    rawDisplayData = this._displayIndex < this._rawDisplayDatas.Count ? this._rawDisplayDatas[this._displayIndex] : null;
                }

                if (rawDisplayData == null)
                {
                    rawDisplayData = this._GetDefaultRawDisplayData(this._displayIndex);
                }

                if (this._displayIndex < this._displayDatas.Count)
                {
                    this._displayData = this._displayDatas[this._displayIndex];
                }
            }

            // Update texture and mesh data.
            if (this._displayData != null)
            {
                if (this._displayData.type == DisplayType.Mesh)
                {
                    currentVerticesData = (this._displayData as MeshDisplayData).vertices;
                }
                else if (this._displayData.type == DisplayType.Path)
                {
                    currentVerticesData = (this._displayData as PathDisplayData).vertices;
                }
                else if (rawDisplayData != null)
                {
                    if (rawDisplayData.type == DisplayType.Mesh)
                    {
                        currentVerticesData = (rawDisplayData as MeshDisplayData).vertices;
                    }
                    else if (rawDisplayData.type == DisplayType.Path)
                    {
                        currentVerticesData = (rawDisplayData as PathDisplayData).vertices;
                    }
                }

                if (this._displayData.type == DisplayType.BoundingBox)
                {
                    this._boundingBoxData = (this._displayData as BoundingBoxDisplayData).boundingBox;
                }
                else if (rawDisplayData != null)
                {
                    if (rawDisplayData.type == DisplayType.BoundingBox)
                    {
                        this._boundingBoxData = (rawDisplayData as BoundingBoxDisplayData).boundingBox;
                    }
                }

                if (this._displayData.type == DisplayType.Image)
                {
                    this._textureData = (this._displayData as ImageDisplayData).texture;
                }
                else if (this._displayData.type == DisplayType.Mesh)
                {
                    this._textureData = (this._displayData as MeshDisplayData).texture;
                }
            }

            if (this._displayData != prevDisplayData || currentVerticesData != prevVerticesData || this._textureData != prevTextureData)
            {
                // Update pivot offset.
                if (currentVerticesData == null && this._textureData != null)
                {
                    var imageDisplayData = this._displayData as ImageDisplayData;
                    var scale = this._textureData.parent.scale * this._armature._armatureData.scale;
                    var frame = this._textureData.frame;

                    this._pivotX = imageDisplayData.pivot.x;
                    this._pivotY = imageDisplayData.pivot.y;

                    var rect = frame != null ? frame : this._textureData.region;
                    var width = rect.width;
                    var height = rect.height;

                    if (this._textureData.rotated && frame == null)
                    {
                        width = rect.height;
                        height = rect.width;
                    }

                    this._pivotX *= width * scale;
                    this._pivotY *= height * scale;

                    if (frame != null)
                    {
                        this._pivotX += frame.x * scale;
                        this._pivotY += frame.y * scale;
                    }

                    // Update replace pivot. TODO
                    if (this._displayData != null && rawDisplayData != null && this._displayData != rawDisplayData)
                    {
                        rawDisplayData.transform.ToMatrix(Slot._helpMatrix);
                        Slot._helpMatrix.Invert();
                        Slot._helpMatrix.TransformPoint(0.0f, 0.0f, Slot._helpPoint);
                        this._pivotX -= Slot._helpPoint.x;
                        this._pivotY -= Slot._helpPoint.y;

                        this._displayData.transform.ToMatrix(Slot._helpMatrix);
                        Slot._helpMatrix.Invert();
                        Slot._helpMatrix.TransformPoint(0.0f, 0.0f, Slot._helpPoint);
                        this._pivotX += Slot._helpPoint.x;
                        this._pivotY += Slot._helpPoint.y;
                    }

                    if (!DragonBones.yDown)
                    {
                        this._pivotY = (this._textureData.rotated ? this._textureData.region.width : this._textureData.region.height) * scale - this._pivotY;
                    }
                }
                else
                {
                    this._pivotX = 0.0f;
                    this._pivotY = 0.0f;
                }

                // Update original transform.
                if (rawDisplayData != null)
                {
                    // Compatible.
                    this.origin = rawDisplayData.transform;
                }
                else if (this._displayData != null)
                {
                    // Compatible.
                    this.origin = this._displayData.transform;
                }
                else
                {
                    this.origin = null;
                }

                // Update vertices.
                if (currentVerticesData != prevVerticesData)
                {
                    if (this._deformVertices == null)
                    {
                        this._deformVertices = BaseObject.BorrowObject<DeformVertices>();
                    }

                    this._deformVertices.init(currentVerticesData, this._armature);
                }
                else if (this._deformVertices != null && this._textureData != prevTextureData)
                {
                    // Update mesh after update frame.
                    this._deformVertices.verticesDirty = true;
                }

                this._displayDirty = true;
                this._transformDirty = true;
            }
        }

        /// <private/>
        protected void _UpdateDisplay()
        {
            var prevDisplay = this._display != null ? this._display : this._rawDisplay;
            var prevChildArmature = this._childArmature;

            // Update display and child armature.
            if (this._displayIndex >= 0 && this._displayIndex < this._displayList.Count)
            {
                this._display = this._displayList[this._displayIndex];
                if (this._display != null && this._display is Armature)
                {
                    this._childArmature = this._display as Armature;
                    this._display = this._childArmature.display;
                }
                else
                {
                    this._childArmature = null;
                }
            }
            else
            {
                this._display = null;
                this._childArmature = null;
            }

            // Update display.
            var currentDisplay = this._display != null ? this._display : this._rawDisplay;
            if (currentDisplay != prevDisplay)
            {
                this._OnUpdateDisplay();
                this._ReplaceDisplay(prevDisplay);

                this._transformDirty = true;
                this._visibleDirty = true;
                this._blendModeDirty = true;
                this._colorDirty = true;
            }

            // Update frame.
            if (currentDisplay == this._rawDisplay || currentDisplay == this._meshDisplay)
            {
                this._UpdateFrame();
            }

            // Update child armature.
            if (this._childArmature != prevChildArmature)
            {
                if (prevChildArmature != null)
                {
                    // Update child armature parent.
                    prevChildArmature._parent = null;
                    prevChildArmature.clock = null;
                    if (prevChildArmature.inheritAnimation)
                    {
                        prevChildArmature.animation.Reset();
                    }
                }

                if (this._childArmature != null)
                {
                    // Update child armature parent.
                    this._childArmature._parent = this;
                    this._childArmature.clock = this._armature.clock;
                    if (this._childArmature.inheritAnimation)
                    {
                        // Set child armature cache frameRate.
                        if (this._childArmature.cacheFrameRate == 0)
                        {
                            var cacheFrameRate = this._armature.cacheFrameRate;
                            if (cacheFrameRate != 0)
                            {
                                this._childArmature.cacheFrameRate = cacheFrameRate;
                            }
                        }

                        // Child armature action.
                        List<ActionData> actions = null;
                        if (this._displayData != null && this._displayData.type == DisplayType.Armature)
                        {
                            actions = (this._displayData as ArmatureDisplayData).actions;
                        }
                        else if (this._displayIndex >= 0 && this._rawDisplayDatas != null)
                        {
                            var rawDisplayData = this._displayIndex < this._rawDisplayDatas.Count ? this._rawDisplayDatas[this._displayIndex] : null;

                            if (rawDisplayData == null)
                            {
                                rawDisplayData = this._GetDefaultRawDisplayData(this._displayIndex);
                            }

                            if (rawDisplayData != null && rawDisplayData.type == DisplayType.Armature)
                            {
                                actions = (rawDisplayData as ArmatureDisplayData).actions;
                            }
                        }

                        if (actions != null && actions.Count > 0)
                        {
                            foreach (var action in actions)
                            {
                                var eventObject = BaseObject.BorrowObject<EventObject>();
                                EventObject.ActionDataToInstance(action, eventObject, this._armature);
                                eventObject.slot = this;
                                this._armature._BufferAction(eventObject, false);
                            }
                        }
                        else
                        {
                            this._childArmature.animation.Play();
                        }
                    }
                }
            }
        }

        /// <private/>
        protected void _UpdateGlobalTransformMatrix(bool isCache)
        {
            this.globalTransformMatrix.CopyFrom(this._localMatrix);
            this.globalTransformMatrix.Concat(this._parent.globalTransformMatrix);
            if (isCache)
            {
                this.global.FromMatrix(this.globalTransformMatrix);
            }
            else
            {
                this._globalDirty = true;
            }
        }
        /// <internal/>
        /// <private/>
        internal bool _SetDisplayIndex(int value, bool isAnimation = false)
        {
            if (isAnimation)
            {
                if (this._animationDisplayIndex == value)
                {
                    return false;
                }

                this._animationDisplayIndex = value;
            }

            if (this._displayIndex == value)
            {
                return false;
            }

            this._displayIndex = value;
            this._displayDirty = true;

            this._UpdateDisplayData();

            return this._displayDirty;
        }

        /// <internal/>
        /// <private/>
        internal bool _SetZorder(int value)
        {
            if (this._zOrder == value)
            {
                //return false;
            }

            this._zOrder = value;
            this._zOrderDirty = true;

            return this._zOrderDirty;
        }

        /// <internal/>
        /// <private/>
        internal bool _SetColor(ColorTransform value)
        {
            this._colorTransform.CopyFrom(value);
            this._colorDirty = true;

            return this._colorDirty;
        }
        /// <internal/>
        /// <private/>
        internal bool _SetDisplayList(List<object> value)
        {
            if (value != null && value.Count > 0)
            {
                if (this._displayList.Count != value.Count)
                {
                    this._displayList.ResizeList(value.Count);
                }

                for (int i = 0, l = value.Count; i < l; ++i)
                {
                    // Retain input render displays.
                    var eachDisplay = value[i];
                    if (eachDisplay != null &&
                        eachDisplay != this._rawDisplay &&
                        eachDisplay != this._meshDisplay &&
                        !(eachDisplay is Armature) && this._displayList.IndexOf(eachDisplay) < 0)
                    {
                        this._InitDisplay(eachDisplay, true);
                    }

                    this._displayList[i] = eachDisplay;
                }
            }
            else if (this._displayList.Count > 0)
            {
                this._displayList.Clear();
            }

            if (this._displayIndex >= 0 && this._displayIndex < this._displayList.Count)
            {
                this._displayDirty = this._display != this._displayList[this._displayIndex];
            }
            else
            {
                this._displayDirty = this._display != null;
            }

            this._UpdateDisplayData();

            return this._displayDirty;
        }

        /// <internal/>
        /// <private/>
        internal virtual void Init(SlotData slotData, Armature armatureValue, object rawDisplay, object meshDisplay)
        {
            if (this._slotData != null)
            {
                return;
            }

            this._slotData = slotData;
            //
            this._visibleDirty = true;
            this._blendModeDirty = true;
            this._colorDirty = true;
            this._blendMode = this._slotData.blendMode;
            this._zOrder = this._slotData.zOrder;
            this._colorTransform.CopyFrom(this._slotData.color);
            this._rawDisplay = rawDisplay;
            this._meshDisplay = meshDisplay;

            this._armature = armatureValue;

            var slotParent = this._armature.GetBone(this._slotData.parent.name);
            if (slotParent != null)
            {
                this._parent = slotParent;
            }
            else
            {
                // Never;
            }

            this._armature._AddSlot(this);

            //
            this._InitDisplay(this._rawDisplay, false);
            if (this._rawDisplay != this._meshDisplay)
            {
                this._InitDisplay(this._meshDisplay, false);
            }

            this._OnUpdateDisplay();
            this._AddDisplay();

            //
            // this.rawDisplayDatas = displayDatas; // TODO
        }

        /// <internal/>
        /// <private/>
        internal void Update(int cacheFrameIndex)
        {
            if (this._displayDirty)
            {
                this._displayDirty = false;
                this._UpdateDisplay();

                // TODO remove slot
                if (this._transformDirty)
                {
                    // Update local matrix. (Only updated when both display and transform are dirty.)
                    if (this.origin != null)
                    {
                        this.global.CopyFrom(this.origin).Add(this.offset).ToMatrix(this._localMatrix);
                    }
                    else
                    {
                        this.global.CopyFrom(this.offset).ToMatrix(this._localMatrix);
                    }
                }
            }

            if (this._zOrderDirty)
            {
                this._zOrderDirty = false;
                this._UpdateZOrder();
            }

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
                else if (this._transformDirty || this._parent._childrenTransformDirty)
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
            else if (this._transformDirty || this._parent._childrenTransformDirty)
            {
                // Dirty.
                cacheFrameIndex = -1;
                this._transformDirty = true;
                this._cachedFrameIndex = -1;
            }

            if (this._display == null)
            {
                return;
            }

            if (this._visibleDirty)
            {
                this._visibleDirty = false;
                this._UpdateVisible();
            }

            if (this._blendModeDirty)
            {
                this._blendModeDirty = false;
                this._UpdateBlendMode();
            }

            if (this._colorDirty)
            {
                this._colorDirty = false;
                this._UpdateColor();
            }

            if (this._deformVertices != null && this._deformVertices.verticesData != null && this._display == this._meshDisplay)
            {
                var isSkinned = this._deformVertices.verticesData.weight != null;

                if (this._deformVertices.verticesDirty ||
                    (isSkinned && this._deformVertices.isBonesUpdate()))
                {
                    this._deformVertices.verticesDirty = false;
                    this._UpdateMesh();
                }

                if (isSkinned)
                {
                    // Compatible.
                    return;
                }
            }

            if (this._transformDirty)
            {
                this._transformDirty = false;

                if (this._cachedFrameIndex < 0)
                {
                    var isCache = cacheFrameIndex >= 0;
                    this._UpdateGlobalTransformMatrix(isCache);

                    if (isCache && this._cachedFrameIndices != null)
                    {
                        this._cachedFrameIndex = this._cachedFrameIndices[cacheFrameIndex] = this._armature._armatureData.SetCacheFrame(this.globalTransformMatrix, this.global);
                    }
                }
                else
                {
                    this._armature._armatureData.GetCacheFrame(this.globalTransformMatrix, this.global, this._cachedFrameIndex);
                }

                this._UpdateTransform();
            }
        }

        /// <private/>
        public void UpdateTransformAndMatrix()
        {
            if (this._transformDirty)
            {
                this._transformDirty = false;
                this._UpdateGlobalTransformMatrix(false);
            }
        }

        /// <private/>
        internal void ReplaceDisplayData(DisplayData value, int displayIndex = -1)
        {
            if (displayIndex < 0)
            {
                if (this._displayIndex < 0)
                {
                    displayIndex = 0;
                }
                else
                {
                    displayIndex = this._displayIndex;
                }
            }

            if (this._displayDatas.Count <= displayIndex)
            {
                this._displayDatas.ResizeList(displayIndex + 1);

                for (int i = 0, l = this._displayDatas.Count; i < l; ++i)
                {
                    // Clean undefined.
                    this._displayDatas[i] = null;
                }
            }

            this._displayDatas[displayIndex] = value;
        }

        /// <summary>
        /// - Check whether a specific point is inside a custom bounding box in the slot.
        /// The coordinate system of the point is the inner coordinate system of the armature.
        /// Custom bounding boxes need to be customized in Dragonbones Pro.
        /// </summary>
        /// <param name="x">- The horizontal coordinate of the point.</param>
        /// <param name="y">- The vertical coordinate of the point.</param>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查特定点是否在插槽的自定义边界框内。
        /// 点的坐标系为骨架内坐标系。
        /// 自定义边界框需要在 DragonBones Pro 中自定义。
        /// </summary>
        /// <param name="x">- 点的水平坐标。</param>
        /// <param name="y">- 点的垂直坐标。</param>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public bool ContainsPoint(float x, float y)
        {
            if (this._boundingBoxData == null)
            {
                return false;
            }

            this.UpdateTransformAndMatrix();

            Slot._helpMatrix.CopyFrom(this.globalTransformMatrix);
            Slot._helpMatrix.Invert();
            Slot._helpMatrix.TransformPoint(x, y, Slot._helpPoint);

            return this._boundingBoxData.ContainsPoint(Slot._helpPoint.x, Slot._helpPoint.y);
        }

        /// <summary>
        /// - Check whether a specific segment intersects a custom bounding box for the slot.
        /// The coordinate system of the segment and intersection is the inner coordinate system of the armature.
        /// Custom bounding boxes need to be customized in Dragonbones Pro.
        /// </summary>
        /// <param name="xA">- The horizontal coordinate of the beginning of the segment.</param>
        /// <param name="yA">- The vertical coordinate of the beginning of the segment.</param>
        /// <param name="xB">- The horizontal coordinate of the end point of the segment.</param>
        /// <param name="yB">- The vertical coordinate of the end point of the segment.</param>
        /// <param name="intersectionPointA">- The first intersection at which a line segment intersects the bounding box from the beginning to the end. (If not set, the intersection point will not calculated)</param>
        /// <param name="intersectionPointB">- The first intersection at which a line segment intersects the bounding box from the end to the beginning. (If not set, the intersection point will not calculated)</param>
        /// <param name="normalRadians">- The normal radians of the tangent of the intersection boundary box. [x: Normal radian of the first intersection tangent, y: Normal radian of the second intersection tangent] (If not set, the normal will not calculated)</param>
        /// <returns>Intersection situation. [1: Disjoint and segments within the bounding box, 0: Disjoint, 1: Intersecting and having a nodal point and ending in the bounding box, 2: Intersecting and having a nodal point and starting at the bounding box, 3: Intersecting and having two intersections, N: Intersecting and having N intersections]</returns>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查特定线段是否与插槽的自定义边界框相交。
        /// 线段和交点的坐标系均为骨架内坐标系。
        /// 自定义边界框需要在 DragonBones Pro 中自定义。
        /// </summary>
        /// <param name="xA">- 线段起点的水平坐标。</param>
        /// <param name="yA">- 线段起点的垂直坐标。</param>
        /// <param name="xB">- 线段终点的水平坐标。</param>
        /// <param name="yB">- 线段终点的垂直坐标。</param>
        /// <param name="intersectionPointA">- 线段从起点到终点与边界框相交的第一个交点。 （如果未设置，则不计算交点）</param>
        /// <param name="intersectionPointB">- 线段从终点到起点与边界框相交的第一个交点。 （如果未设置，则不计算交点）</param>
        /// <param name="normalRadians">- 交点边界框切线的法线弧度。 [x: 第一个交点切线的法线弧度, y: 第二个交点切线的法线弧度] （如果未设置，则不计算法线）</param>
        /// <returns>相交的情况。 [-1: 不相交且线段在包围盒内, 0: 不相交, 1: 相交且有一个交点且终点在包围盒内, 2: 相交且有一个交点且起点在包围盒内, 3: 相交且有两个交点, N: 相交且有 N 个交点]</returns>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public int IntersectsSegment(float xA, float yA, float xB, float yB,
                                    Point intersectionPointA = null,
                                    Point intersectionPointB = null,
                                    Point normalRadians = null)
        {
            if (this._boundingBoxData == null)
            {
                return 0;
            }

            this.UpdateTransformAndMatrix();
            Slot._helpMatrix.CopyFrom(this.globalTransformMatrix);
            Slot._helpMatrix.Invert();
            Slot._helpMatrix.TransformPoint(xA, yA, Slot._helpPoint);
            xA = Slot._helpPoint.x;
            yA = Slot._helpPoint.y;
            Slot._helpMatrix.TransformPoint(xB, yB, Slot._helpPoint);
            xB = Slot._helpPoint.x;
            yB = Slot._helpPoint.y;

            var intersectionCount = this._boundingBoxData.IntersectsSegment(xA, yA, xB, yB, intersectionPointA, intersectionPointB, normalRadians);
            if (intersectionCount > 0)
            {
                if (intersectionCount == 1 || intersectionCount == 2)
                {
                    if (intersectionPointA != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointA.x, intersectionPointA.y, intersectionPointA);

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = intersectionPointA.x;
                            intersectionPointB.y = intersectionPointA.y;
                        }
                    }
                    else if (intersectionPointB != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointB.x, intersectionPointB.y, intersectionPointB);
                    }
                }
                else
                {
                    if (intersectionPointA != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointA.x, intersectionPointA.y, intersectionPointA);
                    }

                    if (intersectionPointB != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointB.x, intersectionPointB.y, intersectionPointB);
                    }
                }

                if (normalRadians != null)
                {
                    this.globalTransformMatrix.TransformPoint((float)Math.Cos(normalRadians.x), (float)Math.Sin(normalRadians.x), Slot._helpPoint, true);
                    normalRadians.x = (float)Math.Atan2(Slot._helpPoint.y, Slot._helpPoint.x);

                    this.globalTransformMatrix.TransformPoint((float)Math.Cos(normalRadians.y), (float)Math.Sin(normalRadians.y), Slot._helpPoint, true);
                    normalRadians.y = (float)Math.Atan2(Slot._helpPoint.y, Slot._helpPoint.x);
                }
            }

            return intersectionCount;
        }

        /// <summary>
        /// - Forces the slot to update the state of the display object in the next frame.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 强制插槽在下一帧更新显示对象的状态。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public void InvalidUpdate()
        {
            this._displayDirty = true;
            this._transformDirty = true;
        }
        /// <summary>
        /// - The visible of slot's display object.
        /// </summary>
        /// <default>true</default>
        /// <version>DragonBones 5.6</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽的显示对象的可见。
        /// </summary>
        /// <default>true</default>
        /// <version>DragonBones 5.6</version>
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
                this._UpdateVisible();
            }
        }
        /// <summary>
        /// - The index of the display object displayed in the display list.
        /// </summary>
        /// <example>
        /// TypeScript style, for reference only.
        /// <pre>
        ///     let slot = armature.getSlot("weapon");
        ///     slot.displayIndex = 3;
        ///     slot.displayController = "none";
        /// </pre>
        /// </example>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 此时显示的显示对象在显示列表中的索引。
        /// </summary>
        /// <example>
        /// TypeScript 风格，仅供参考。
        /// <pre>
        ///     let slot = armature.getSlot("weapon");
        ///     slot.displayIndex = 3;
        ///     slot.displayController = "none";
        /// </pre>
        /// </example>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public int displayIndex
        {
            get { return this._displayIndex; }
            set
            {
                if (this._SetDisplayIndex(value))
                {
                    this.Update(-1);
                }
            }
        }

        /// <summary>
        /// - The slot name.
        /// </summary>
        /// <see cref="DragonBones.SlotData.name"/>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽名称。
        /// </summary>
        /// <see cref="DragonBones.SlotData.name"/>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name
        {
            get { return this._slotData.name; }
        }

        /// <summary>
        /// - Contains a display list of display objects or child armatures.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 包含显示对象或子骨架的显示列表。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public List<object> displayList
        {
            get { return new List<object>(_displayList.ToArray()); }
            set
            {
                var backupDisplayList = _displayList.ToArray(); // Copy.
                var disposeDisplayList = new List<object>();

                if (this._SetDisplayList(value))
                {
                    this.Update(-1);
                }

                // Release replaced displays.
                foreach (var eachDisplay in backupDisplayList)
                {
                    if (eachDisplay != null &&
                        eachDisplay != this._rawDisplay &&
                        eachDisplay != this._meshDisplay &&
                        this._displayList.IndexOf(eachDisplay) < 0 &&
                        disposeDisplayList.IndexOf(eachDisplay) < 0)
                    {
                        disposeDisplayList.Add(eachDisplay);
                    }
                }

                foreach (var eachDisplay in disposeDisplayList)
                {
                    if (eachDisplay is Armature)
                    {
                        // (eachDisplay as Armature).Dispose();
                    }
                    else
                    {
                        this._DisposeDisplay(eachDisplay, true);
                    }
                }
            }
        }
        /// <summary>
        /// - The slot data.
        /// </summary>
        /// <see cref="DragonBones.SlotData"/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽数据。
        /// </summary>
        /// <see cref="DragonBones.SlotData"/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public SlotData slotData
        {
            get { return this._slotData; }
        }

        /// <private/>
        public List<DisplayData> rawDisplayDatas
        {
            get { return this._rawDisplayDatas; }
            set
            {
                if (this._rawDisplayDatas == value)
                {
                    return;
                }

                this._displayDirty = true;
                this._rawDisplayDatas = value;

                if (this._rawDisplayDatas != null)
                {
                    this._displayDatas.ResizeList(this._rawDisplayDatas.Count);
                    for (int i = 0, l = this._displayDatas.Count; i < l; ++i)
                    {
                        var rawDisplayData = this._rawDisplayDatas[i];

                        if (rawDisplayData == null)
                        {
                            rawDisplayData = this._GetDefaultRawDisplayData(i);
                        }

                        this._displayDatas[i] = rawDisplayData;
                    }
                }
                else
                {
                    this._displayDatas.Clear();
                }
            }
        }
        /// <summary>
        /// - The custom bounding box data for the slot at current time.
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽此时的自定义包围盒数据。
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public BoundingBoxData boundingBoxData
        {
            get { return this._boundingBoxData; }
        }
        /// <private/>
        public object rawDisplay
        {
            get { return this._rawDisplay; }
        }

        /// <private/>
        public object meshDisplay
        {
            get { return this._meshDisplay; }
        }
        /// <summary>
        /// - The display object that the slot displays at this time.
        /// </summary>
        /// <example>
        /// TypeScript style, for reference only.
        /// <pre>
        ///     let slot = armature.getSlot("text");
        ///     slot.display = new yourEngine.TextField();
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽此时显示的显示对象。
        /// </summary>
        /// <example>
        /// TypeScript 风格，仅供参考。
        /// <pre>
        ///     let slot = armature.getSlot("text");
        ///     slot.display = new yourEngine.TextField();
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public object display
        {
            get { return this._display; }
            set
            {
                if (this._display == value)
                {
                    return;
                }

                var displayListLength = this._displayList.Count;
                if (this._displayIndex < 0 && displayListLength == 0)
                {
                    // Emprty.
                    this._displayIndex = 0;
                }

                if (this._displayIndex < 0)
                {
                    return;
                }
                else
                {
                    var replaceDisplayList = this.displayList; // Copy.
                    if (displayListLength <= this._displayIndex)
                    {
                        replaceDisplayList.ResizeList(this._displayIndex + 1);
                    }

                    replaceDisplayList[this._displayIndex] = value;
                    this.displayList = replaceDisplayList;
                }
            }
        }
        /// <summary>
        /// - The child armature that the slot displayed at current time.
        /// </summary>
        /// <example>
        /// TypeScript style, for reference only.
        /// <pre>
        ///     let slot = armature.getSlot("weapon");
        /// let prevChildArmature = slot.childArmature;
        /// if (prevChildArmature) {
        /// prevChildArmature.dispose();
        ///     }
        ///     slot.childArmature = factory.buildArmature("weapon_blabla", "weapon_blabla_project");
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 插槽此时显示的子骨架。
        /// 注意，被替换的对象并不会被回收，根据语言和引擎的不同，需要额外处理。
        /// </summary>
        /// <example>
        /// TypeScript 风格，仅供参考。
        /// <pre>
        ///     let slot = armature.getSlot("weapon");
        /// let prevChildArmature = slot.childArmature;
        /// if (prevChildArmature) {
        /// prevChildArmature.dispose();
        ///     }
        ///     slot.childArmature = factory.buildArmature("weapon_blabla", "weapon_blabla_project");
        /// </pre>
        /// </example>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public Armature childArmature
        {
            get { return this._childArmature; }
            set
            {
                if (this._childArmature == value)
                {
                    return;
                }

                this.display = value;
            }
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
    }
}
