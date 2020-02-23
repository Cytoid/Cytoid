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
﻿/**
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
using UnityEngine;
using System.Collections.Generic;

namespace DragonBones
{
    /**
     * @language zh_CN
     * Unity 插槽。
     * @version DragonBones 3.0
     */
    public class UnitySlot : Slot
    {
        internal const float Z_OFFSET = 0.001f;
        private static readonly int[] TRIANGLES = { 0, 1, 2, 0, 2, 3 };
        private static Vector3 _helpVector3 = new Vector3();

        internal GameObject _renderDisplay;
        internal UnityUGUIDisplay _uiDisplay = null;

        internal MeshBuffer _meshBuffer;

        internal MeshRenderer _meshRenderer = null;
        internal MeshFilter _meshFilter = null;

        //combineMesh
        internal bool _isIgnoreCombineMesh;
        internal bool _isCombineMesh;
        internal int _sumMeshIndex = -1;
        internal int _verticeOrder = -1;
        internal int _verticeOffset = -1;
        internal UnityCombineMeshs _combineMesh = null;
        internal bool _isActive = false;

        private bool _skewed;
        private UnityArmatureComponent _proxy;
        private BlendMode _currentBlendMode;

        /**
         * @private
         */
        public UnitySlot()
        {
        }

        /**
         * @private
         */
        protected override void _OnClear()
        {
            base._OnClear();

            if (this._meshBuffer != null)
            {
                this._meshBuffer.Dispose();
            }

            this._skewed = false;
            this._proxy = null;

            this._renderDisplay = null;
            this._uiDisplay = null;

            this._meshBuffer = null;

            this._meshRenderer = null;
            this._meshFilter = null;

            this._isIgnoreCombineMesh = false;
            this._isCombineMesh = false;
            this._sumMeshIndex = -1;
            this._verticeOrder = -1;
            this._verticeOffset = -1;

            this._combineMesh = null;

            this._currentBlendMode = BlendMode.Normal;
            this._isActive = false;
        }

        /**
         * @private
         */
        protected override void _InitDisplay(object value, bool isRetain)
        {

        }
        /**
         * @private
         */
        protected override void _DisposeDisplay(object value, bool isRelease)
        {
            if (!isRelease)
            {
                UnityFactoryHelper.DestroyUnityObject(value as GameObject);
            }
        }
        /**
         * @private
         */
        protected override void _OnUpdateDisplay()
        {
            _renderDisplay = (_display != null ? _display : _rawDisplay) as GameObject;

            //
            _proxy = _armature.proxy as UnityArmatureComponent;
            if (_proxy.isUGUI)
            {
                _uiDisplay = _renderDisplay.GetComponent<UnityUGUIDisplay>();
                if (_uiDisplay == null)
                {
                    _uiDisplay = _renderDisplay.AddComponent<UnityUGUIDisplay>();
                    _uiDisplay.raycastTarget = false;
                }
            }
            else
            {
                _meshRenderer = _renderDisplay.GetComponent<MeshRenderer>();
                if (_meshRenderer == null)
                {
                    _meshRenderer = _renderDisplay.AddComponent<MeshRenderer>();
                }
                //
                _meshFilter = _renderDisplay.GetComponent<MeshFilter>();
                if (_meshFilter == null && _renderDisplay.GetComponent<TextMesh>() == null)
                {
                    _meshFilter = _renderDisplay.AddComponent<MeshFilter>();
                }
            }

            //init mesh
            if (this._meshBuffer == null)
            {
                this._meshBuffer = new MeshBuffer();
                this._meshBuffer.sharedMesh = MeshBuffer.GenerateMesh();
                this._meshBuffer.sharedMesh.name = this.name;
            }
        }
        /**
         * @private
         */
        protected override void _AddDisplay()
        {
            _proxy = _armature.proxy as UnityArmatureComponent;
            var container = _proxy;
            if (_renderDisplay.transform.parent != container.transform)
            {
                _renderDisplay.transform.SetParent(container.transform);

                _helpVector3.Set(0.0f, 0.0f, 0.0f);
                _SetZorder(_helpVector3);
            }
        }
        /**
         * @private
         */
        protected override void _ReplaceDisplay(object value)
        {
            var container = _proxy;
            var prevDisplay = value as GameObject;
            int index = prevDisplay.transform.GetSiblingIndex();
            prevDisplay.SetActive(false);

            _renderDisplay.hideFlags = HideFlags.None;
            _renderDisplay.transform.SetParent(container.transform);
            _renderDisplay.SetActive(true);
            _renderDisplay.transform.SetSiblingIndex(index);

            _SetZorder(prevDisplay.transform.localPosition);
        }
        /**
         * @private
         */
        protected override void _RemoveDisplay()
        {
            _renderDisplay.transform.parent = null;
        }
        /**
         * @private
         */
        protected override void _UpdateZOrder()
        {
            _SetZorder(this._renderDisplay.transform.localPosition);

            //
            if (this._childArmature != null || !this._isActive)
            {
                this._CombineMesh();
            }
        }

        /**
         * @internal
         */
        internal void _SetZorder(Vector3 zorderPos)
        {
            if (this._isCombineMesh)
            {
                var meshBuffer = this._combineMesh.meshBuffers[this._sumMeshIndex];
                meshBuffer.zorderDirty = true;
            }

            {
                zorderPos.z = -this._zOrder * (this._proxy._zSpace + Z_OFFSET);

                if (_renderDisplay != null)
                {
                    _renderDisplay.transform.localPosition = zorderPos;
                    _renderDisplay.transform.SetSiblingIndex(_zOrder);

                    if (_proxy.isUGUI)
                    {
                        return;
                    }

                    if (_childArmature == null)
                    {
                        _meshRenderer.sortingLayerName = _proxy.sortingLayerName;
                        if (_proxy.sortingMode == SortingMode.SortByOrder)
                        {
                            _meshRenderer.sortingOrder = _zOrder * UnityArmatureComponent.ORDER_SPACE;
                        }
                        else
                        {
                            _meshRenderer.sortingOrder = _proxy.sortingOrder;
                        }
                    }
                    else
                    {
                        var childArmatureComp = childArmature.proxy as UnityArmatureComponent;
                        childArmatureComp._sortingMode = _proxy._sortingMode;
                        childArmatureComp._sortingLayerName = _proxy._sortingLayerName;
                        if (_proxy._sortingMode == SortingMode.SortByOrder)
                        {
                            childArmatureComp.sortingOrder = _zOrder * UnityArmatureComponent.ORDER_SPACE;
                        }
                        else
                        {
                            childArmatureComp.sortingOrder = _proxy._sortingOrder;
                        }
                    }
                }
            }
        }

        public void DisallowCombineMesh()
        {
            this.CancelCombineMesh();
            this._isIgnoreCombineMesh = true;
        }

        internal void CancelCombineMesh()
        {
            if (this._isCombineMesh)
            {
                this._isCombineMesh = false;
                if (this._meshFilter != null)
                {
                    this._meshFilter.sharedMesh = this._meshBuffer.sharedMesh;
                    var isSkinnedMesh = this._deformVertices != null && this._deformVertices.verticesData != null && this._deformVertices.verticesData.weight != null;
                    if (!isSkinnedMesh)
                    {
                        this._meshBuffer.rawVertextBuffers.CopyTo(this._meshBuffer.vertexBuffers, 0);
                    }

                    //
                    this._meshBuffer.UpdateVertices();
                    this._meshBuffer.UpdateColors();

                    if (isSkinnedMesh)
                    {
                        this._UpdateMesh();
                        this._IdentityTransform();
                    }
                    else
                    {
                        this._UpdateTransform();
                    }
                }

                this._meshBuffer.enabled = true;
            }

            if (this._renderDisplay != null)
            {
                if (this._childArmature != null)
                {
                    this._renderDisplay.SetActive(true);
                }
                else
                {
                    this._renderDisplay.SetActive(this._isActive);
                }
                //
                this._renderDisplay.hideFlags = HideFlags.None;
            }

            //
            this._isCombineMesh = false;
            this._sumMeshIndex = -1;
            this._verticeOrder = -1;
            this._verticeOffset = -1;
            // this._combineMesh = null;
        }

        //
        private void _CombineMesh()
        {
            //引起合并的条件,Display改变，混合模式改变，Visible改变，Zorder改变
            //已经关闭合并，不再考虑
            if (this._isIgnoreCombineMesh || this._proxy.isUGUI)
            {
                return;
            }

            //已经合并过了，又触发合并，那么打断合并，用自己的网格数据还原
            if (this._isCombineMesh)
            {
                //已经合并过，除非满足一下情况，否则都不能再合并, TODO
                this.CancelCombineMesh();
                this._isIgnoreCombineMesh = true;
            }

            var combineMeshComp = this._proxy.GetComponent<UnityCombineMeshs>();
            //从来没有合并过，触发合并，那么尝试合并
            if (combineMeshComp != null)
            {
                combineMeshComp.dirty = true;
            }
        }

        /**
         * @private
         */
        internal override void _UpdateVisible()
        {
            this._renderDisplay.SetActive(this._parent.visible);

            if (this._isCombineMesh && !this._parent.visible)
            {
                this._CombineMesh();
            }
        }
        /**
         * @private
         */
        internal override void _UpdateBlendMode()
        {
            if (this._currentBlendMode == this._blendMode)
            {
                return;
            }

            if (this._childArmature == null)
            {
                if (this._uiDisplay != null)
                {
                    this._uiDisplay.material = (this._textureData as UnityTextureData).GetMaterial(this._blendMode, true);
                }
                else
                {
                    this._meshRenderer.sharedMaterial = (this._textureData as UnityTextureData).GetMaterial(this._blendMode);
                }

                this._meshBuffer.name = this._uiDisplay != null ? this._uiDisplay.material.name : this._meshRenderer.sharedMaterial.name;
            }
            else
            {
                foreach (var slot in _childArmature.GetSlots())
                {
                    slot._blendMode = this._blendMode;
                    slot._UpdateBlendMode();
                }
            }

            this._currentBlendMode = this._blendMode;
            this._CombineMesh();
        }
        /**
         * @private
         */
        protected override void _UpdateColor()
        {
            if (this._childArmature == null)
            {
                var proxyTrans = _proxy._colorTransform;
                if (this._isCombineMesh)
                {
                    var meshBuffer = this._combineMesh.meshBuffers[this._sumMeshIndex];
                    for (var i = 0; i < this._meshBuffer.vertexBuffers.Length; i++)
                    {
                        var index = this._verticeOffset + i;
                        this._meshBuffer.color32Buffers[i].r = (byte)(_colorTransform.redMultiplier * proxyTrans.redMultiplier * 255);
                        this._meshBuffer.color32Buffers[i].g = (byte)(_colorTransform.greenMultiplier * proxyTrans.greenMultiplier * 255);
                        this._meshBuffer.color32Buffers[i].b = (byte)(_colorTransform.blueMultiplier * proxyTrans.blueMultiplier * 255);
                        this._meshBuffer.color32Buffers[i].a = (byte)(_colorTransform.alphaMultiplier * proxyTrans.alphaMultiplier * 255);
                        //
                        meshBuffer.color32Buffers[index] = this._meshBuffer.color32Buffers[i];
                    }

                    meshBuffer.UpdateColors();
                }
                else if (this._meshBuffer.sharedMesh != null)
                {
                    for (int i = 0, l = this._meshBuffer.sharedMesh.vertexCount; i < l; ++i)
                    {
                        this._meshBuffer.color32Buffers[i].r = (byte)(_colorTransform.redMultiplier * proxyTrans.redMultiplier * 255);
                        this._meshBuffer.color32Buffers[i].g = (byte)(_colorTransform.greenMultiplier * proxyTrans.greenMultiplier * 255);
                        this._meshBuffer.color32Buffers[i].b = (byte)(_colorTransform.blueMultiplier * proxyTrans.blueMultiplier * 255);
                        this._meshBuffer.color32Buffers[i].a = (byte)(_colorTransform.alphaMultiplier * proxyTrans.alphaMultiplier * 255);
                    }
                    //
                    this._meshBuffer.UpdateColors();
                }
            }
            else
            {
                //Set all childArmature color dirty
                (this._childArmature.proxy as UnityArmatureComponent).color = _colorTransform;
            }

        }
        /**
         * @private
         */
        protected override void _UpdateFrame()
        {
            var currentVerticesData = (this._deformVertices != null && this._display == this._meshDisplay) ? this._deformVertices.verticesData : null;
            var currentTextureData = this._textureData as UnityTextureData;

            this._meshBuffer.Clear();
            this._isActive = false;
            if (this._displayIndex >= 0 && this._display != null && currentTextureData != null)
            {
                var currentTextureAtlas = _proxy.isUGUI ? currentTextureAtlasData.uiTexture : currentTextureAtlasData.texture;
                if (currentTextureAtlas != null)
                {
                    this._isActive = true;
                    //
                    var textureAtlasWidth = currentTextureAtlasData.width > 0.0f ? (int)currentTextureAtlasData.width : currentTextureAtlas.mainTexture.width;
                    var textureAtlasHeight = currentTextureAtlasData.height > 0.0f ? (int)currentTextureAtlasData.height : currentTextureAtlas.mainTexture.height;

                    var textureScale = _armature.armatureData.scale * currentTextureData.parent.scale;
                    var sourceX = currentTextureData.region.x;
                    var sourceY = currentTextureData.region.y;
                    var sourceWidth = currentTextureData.region.width;
                    var sourceHeight = currentTextureData.region.height;

                    if (currentVerticesData != null)
                    {
                        var data = currentVerticesData.data;
                        var meshOffset = currentVerticesData.offset;
                        var intArray = data.intArray;
                        var floatArray = data.floatArray;
                        var vertexCount = intArray[meshOffset + (int)BinaryOffset.MeshVertexCount];
                        var triangleCount = intArray[meshOffset + (int)BinaryOffset.MeshTriangleCount];
                        int vertexOffset = intArray[meshOffset + (int)BinaryOffset.MeshFloatOffset];
                        if (vertexOffset < 0)
                        {
                            vertexOffset += 65536; // Fixed out of bouds bug. 
                        }

                        var uvOffset = vertexOffset + vertexCount * 2;
                        if (this._meshBuffer.uvBuffers == null || this._meshBuffer.uvBuffers.Length != vertexCount)
                        {
                            this._meshBuffer.uvBuffers = new Vector2[vertexCount];
                        }

                        if (this._meshBuffer.rawVertextBuffers == null || this._meshBuffer.rawVertextBuffers.Length != vertexCount)
                        {
                            this._meshBuffer.rawVertextBuffers = new Vector3[vertexCount];
                            this._meshBuffer.vertexBuffers = new Vector3[vertexCount];
                        }

                        this._meshBuffer.triangleBuffers = new int[triangleCount * 3];

                        for (int i = 0, iV = vertexOffset, iU = uvOffset, l = vertexCount; i < l; ++i)
                        {
                            this._meshBuffer.uvBuffers[i].x = (sourceX + floatArray[iU++] * sourceWidth) / textureAtlasWidth;
                            this._meshBuffer.uvBuffers[i].y = 1.0f - (sourceY + floatArray[iU++] * sourceHeight) / textureAtlasHeight;

                            this._meshBuffer.rawVertextBuffers[i].x = floatArray[iV++] * textureScale;
                            this._meshBuffer.rawVertextBuffers[i].y = floatArray[iV++] * textureScale;

                            this._meshBuffer.vertexBuffers[i].x = this._meshBuffer.rawVertextBuffers[i].x;
                            this._meshBuffer.vertexBuffers[i].y = this._meshBuffer.rawVertextBuffers[i].y;
                        }

                        for (int i = 0; i < triangleCount * 3; ++i)
                        {
                            this._meshBuffer.triangleBuffers[i] = intArray[meshOffset + (int)BinaryOffset.MeshVertexIndices + i];
                        }

                        var isSkinned = currentVerticesData.weight != null;
                        if (isSkinned)
                        {
                            this._IdentityTransform();
                        }
                    }
                    else
                    {
                        if (this._meshBuffer.rawVertextBuffers == null || this._meshBuffer.rawVertextBuffers.Length != 4)
                        {
                            this._meshBuffer.rawVertextBuffers = new Vector3[4];
                            this._meshBuffer.vertexBuffers = new Vector3[4];
                        }

                        if (this._meshBuffer.uvBuffers == null || this._meshBuffer.uvBuffers.Length != this._meshBuffer.rawVertextBuffers.Length)
                        {
                            this._meshBuffer.uvBuffers = new Vector2[this._meshBuffer.rawVertextBuffers.Length];
                        }

                        // Normal texture.                        
                        for (int i = 0, l = 4; i < l; ++i)
                        {
                            var u = 0.0f;
                            var v = 0.0f;

                            switch (i)
                            {
                                case 0:
                                    break;

                                case 1:
                                    u = 1.0f;
                                    break;

                                case 2:
                                    u = 1.0f;
                                    v = 1.0f;
                                    break;

                                case 3:
                                    v = 1.0f;
                                    break;

                                default:
                                    break;
                            }

                            var scaleWidth = sourceWidth * textureScale;
                            var scaleHeight = sourceHeight * textureScale;
                            var pivotX = _pivotX;
                            var pivotY = _pivotY;

                            if (currentTextureData.rotated)
                            {
                                var temp = scaleWidth;
                                scaleWidth = scaleHeight;
                                scaleHeight = temp;

                                pivotX = scaleWidth - _pivotX;
                                pivotY = scaleHeight - _pivotY;
                                //uv
                                this._meshBuffer.uvBuffers[i].x = (sourceX + (1.0f - v) * sourceWidth) / textureAtlasWidth;
                                this._meshBuffer.uvBuffers[i].y = 1.0f - (sourceY + u * sourceHeight) / textureAtlasHeight;
                            }
                            else
                            {
                                //uv
                                this._meshBuffer.uvBuffers[i].x = (sourceX + u * sourceWidth) / textureAtlasWidth;
                                this._meshBuffer.uvBuffers[i].y = 1.0f - (sourceY + v * sourceHeight) / textureAtlasHeight;
                            }

                            //vertices
                            this._meshBuffer.rawVertextBuffers[i].x = u * scaleWidth - pivotX;
                            this._meshBuffer.rawVertextBuffers[i].y = (1.0f - v) * scaleHeight - pivotY;

                            this._meshBuffer.vertexBuffers[i].x = this._meshBuffer.rawVertextBuffers[i].x;
                            this._meshBuffer.vertexBuffers[i].y = this._meshBuffer.rawVertextBuffers[i].y;
                        }

                        this._meshBuffer.triangleBuffers = TRIANGLES;
                    }

                    if (_proxy.isUGUI)
                    {
                        this._uiDisplay.material = currentTextureAtlas;
                        this._uiDisplay.texture = currentTextureAtlas.mainTexture;
                        this._uiDisplay.sharedMesh = this._meshBuffer.sharedMesh;
                    }
                    else
                    {
                        this._meshFilter.sharedMesh = this._meshBuffer.sharedMesh;
                        this._meshRenderer.sharedMaterial = currentTextureAtlas;
                    }

                    this._meshBuffer.name = currentTextureAtlas.name;
                    this._meshBuffer.InitMesh();
                    this._currentBlendMode = BlendMode.Normal;
                    this._blendModeDirty = true;
                    this._colorDirty = true;// Relpace texture will override blendMode and color.
                    this._visibleDirty = true;

                    this._CombineMesh();
                    return;
                }
            }

            this._renderDisplay.SetActive(this._isActive);
            if (_proxy.isUGUI)
            {
                this._uiDisplay.material = null;
                this._uiDisplay.texture = null;
                this._uiDisplay.sharedMesh = null;
            }
            else
            {
                this._meshFilter.sharedMesh = null;
                this._meshRenderer.sharedMaterial = null;
            }

            _helpVector3.x = 0.0f;
            _helpVector3.y = 0.0f;
            _helpVector3.z = this._renderDisplay.transform.localPosition.z;

            this._renderDisplay.transform.localPosition = _helpVector3;

            if (this._isCombineMesh)
            {
                this._CombineMesh();
            }
        }

        protected override void _IdentityTransform()
        {
            var transform = this._renderDisplay.transform;

            transform.localPosition = new Vector3(0.0f, 0.0f, transform.localPosition.z);
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        protected override void _UpdateMesh()
        {
            if (this._meshBuffer.sharedMesh == null || this._deformVertices == null)
            {
                return;
            }
            
            var scale = this._armature.armatureData.scale;
            var deformVertices = this._deformVertices.vertices;
            var bones = this._deformVertices.bones;
            var hasDeform = deformVertices.Count > 0;
            var verticesData = this._deformVertices.verticesData;
            var weightData = verticesData.weight;

            var data = verticesData.data;
            var intArray = data.intArray;
            var floatArray = data.floatArray;
            var vertextCount = intArray[verticesData.offset + (int)BinaryOffset.MeshVertexCount];

            if (weightData != null)
            {
                int weightFloatOffset = intArray[weightData.offset + 1/*(int)BinaryOffset.MeshWeightOffset*/];
                if (weightFloatOffset < 0)
                {
                    weightFloatOffset += 65536; // Fixed out of bouds bug. 
                }

                MeshBuffer meshBuffer = null;
                if (this._isCombineMesh)
                {
                    meshBuffer = this._combineMesh.meshBuffers[this._sumMeshIndex];
                }
                int iB = weightData.offset + (int)BinaryOffset.WeigthBoneIndices + weightData.bones.Count, iV = weightFloatOffset, iF = 0;
                for (int i = 0; i < vertextCount; ++i)
                {
                    var boneCount = intArray[iB++];
                    float xG = 0.0f, yG = 0.0f;
                    for (var j = 0; j < boneCount; ++j)
                    {
                        var boneIndex = intArray[iB++];
                        var bone = bones[boneIndex];
                        if (bone != null)
                        {
                            var matrix = bone.globalTransformMatrix;
                            var weight = floatArray[iV++];
                            var xL = floatArray[iV++] * scale;
                            var yL = floatArray[iV++] * scale;

                            if (hasDeform)
                            {
                                xL += deformVertices[iF++];
                                yL += deformVertices[iF++];
                            }

                            xG += (matrix.a * xL + matrix.c * yL + matrix.tx) * weight;
                            yG += (matrix.b * xL + matrix.d * yL + matrix.ty) * weight;
                        }
                    }
                    this._meshBuffer.vertexBuffers[i].x = xG;
                    this._meshBuffer.vertexBuffers[i].y = yG;

                    if (meshBuffer != null)
                    {
                        meshBuffer.vertexBuffers[i + this._verticeOffset].x = xG;
                        meshBuffer.vertexBuffers[i + this._verticeOffset].y = yG;
                    }
                }

                if (meshBuffer != null)
                {
                    meshBuffer.vertexDirty = true;
                }
                else
                {
                    // if (this._meshRenderer && this._meshRenderer.enabled)
                    {
                        this._meshBuffer.UpdateVertices();
                    }
                }
            }
            else if (deformVertices.Count > 0)
            {
                int vertexOffset = data.intArray[verticesData.offset + (int)BinaryOffset.MeshFloatOffset];
                if (vertexOffset < 0)
                {
                    vertexOffset += 65536; // Fixed out of bouds bug. 
                }
                //
                var a = globalTransformMatrix.a;
                var b = globalTransformMatrix.b;
                var c = globalTransformMatrix.c;
                var d = globalTransformMatrix.d;
                var tx = globalTransformMatrix.tx;
                var ty = globalTransformMatrix.ty;

                var index = 0;
                var rx = 0.0f;
                var ry = 0.0f;
                var vx = 0.0f;
                var vy = 0.0f;
                MeshBuffer meshBuffer = null;
                if (this._isCombineMesh)
                {
                    meshBuffer = this._combineMesh.meshBuffers[this._sumMeshIndex];
                }

                for (int i = 0, iV = 0, iF = 0, l = vertextCount; i < l; ++i)
                {
                    rx = (data.floatArray[vertexOffset + (iV++)] * scale + deformVertices[iF++]);
                    ry = (data.floatArray[vertexOffset + (iV++)] * scale + deformVertices[iF++]);

                    this._meshBuffer.rawVertextBuffers[i].x = rx;
                    this._meshBuffer.rawVertextBuffers[i].y = -ry;

                    this._meshBuffer.vertexBuffers[i].x = rx;
                    this._meshBuffer.vertexBuffers[i].y = -ry;

                    if (meshBuffer != null)
                    {
                        index = i + this._verticeOffset;
                        vx = (rx * a + ry * c + tx);
                        vy = (rx * b + ry * d + ty);

                        meshBuffer.vertexBuffers[index].x = vx;
                        meshBuffer.vertexBuffers[index].y = vy;
                    }
                }
                if (meshBuffer != null)
                {
                    meshBuffer.vertexDirty = true;
                }
                // else if (this._meshRenderer && this._meshRenderer.enabled)
                else
                {
                    this._meshBuffer.UpdateVertices();
                }
            }
        }

        protected override void _UpdateTransform()
        {
            if (this._isCombineMesh)
            {
                var a = globalTransformMatrix.a;
                var b = globalTransformMatrix.b;
                var c = globalTransformMatrix.c;
                var d = globalTransformMatrix.d;
                var tx = globalTransformMatrix.tx;
                var ty = globalTransformMatrix.ty;

                var index = 0;
                var rx = 0.0f;
                var ry = 0.0f;
                var vx = 0.0f;
                var vy = 0.0f;
                var meshBuffer = this._combineMesh.meshBuffers[this._sumMeshIndex];
                for (int i = 0, l = this._meshBuffer.vertexBuffers.Length; i < l; i++)
                {
                    index = i + this._verticeOffset;
                    //vertices
                    rx = this._meshBuffer.rawVertextBuffers[i].x;
                    ry = -this._meshBuffer.rawVertextBuffers[i].y;

                    vx = rx * a + ry * c + tx;
                    vy = rx * b + ry * d + ty;

                    this._meshBuffer.vertexBuffers[i].x = vx;
                    this._meshBuffer.vertexBuffers[i].y = vy;

                    meshBuffer.vertexBuffers[index].x = vx;
                    meshBuffer.vertexBuffers[index].y = vy;
                }
                //
                meshBuffer.vertexDirty = true;
            }
            else
            {
                this.UpdateGlobalTransform(); // Update transform.

                //localPosition
                var flipX = _armature.flipX;
                var flipY = _armature.flipY;
                var transform = _renderDisplay.transform;

                _helpVector3.x = global.x;
                _helpVector3.y = global.y;
                _helpVector3.z = transform.localPosition.z;

                transform.localPosition = _helpVector3;

                //localEulerAngles
                if (_childArmature == null)
                {
                    _helpVector3.x = flipY ? 180.0f : 0.0f;
                    _helpVector3.y = flipX ? 180.0f : 0.0f;
                    _helpVector3.z = global.rotation * Transform.RAD_DEG;
                }
                else
                {
                    //If the childArmature is not null,
                    //X, Y axis can not flip in the container of the childArmature container,
                    //because after the flip, the Z value of the child slot is reversed,
                    //showing the order is wrong, only in the child slot to deal with X, Y axis flip 
                    _helpVector3.x = 0.0f;
                    _helpVector3.y = 0.0f;
                    _helpVector3.z = global.rotation * Transform.RAD_DEG;

                    //这里这样处理，是因为子骨架的插槽也要处理z值,那就在容器中反一下，子插槽再正过来
                    if (flipX != flipY)
                    {
                        _helpVector3.z = -_helpVector3.z;
                    }
                }

                if (flipX || flipY)
                {
                    if (flipX && flipY)
                    {
                        _helpVector3.z += 180.0f;
                    }
                    else
                    {
                        if (flipX)
                        {
                            _helpVector3.z = 180.0f - _helpVector3.z;
                        }
                        else
                        {
                            _helpVector3.z = -_helpVector3.z;
                        }
                    }
                }

                transform.localEulerAngles = _helpVector3;

                //Modify mesh skew. // TODO child armature skew.
                if ((this._display == this._rawDisplay || this._display == this._meshDisplay) && this._meshBuffer.sharedMesh != null)
                {
                    var skew = global.skew;
                    var dSkew = skew;
                    if (flipX && flipY)
                    {
                        dSkew = -skew + Transform.PI;
                    }
                    else if (!flipX && !flipY)
                    {
                        dSkew = -skew - Transform.PI;
                    }

                    var skewed = dSkew < -0.01f || 0.01f < dSkew;
                    if (_skewed || skewed)
                    {
                        _skewed = skewed;

                        var isPositive = global.scaleX >= 0.0f;
                        var cos = Mathf.Cos(dSkew);
                        var sin = Mathf.Sin(dSkew);

                        var x = 0.0f;
                        var y = 0.0f;
                        for (int i = 0, l = this._meshBuffer.vertexBuffers.Length; i < l; ++i)
                        {
                            x = this._meshBuffer.rawVertextBuffers[i].x;
                            y = this._meshBuffer.rawVertextBuffers[i].y;

                            if (isPositive)
                            {
                                this._meshBuffer.vertexBuffers[i].x = x + y * sin;
                            }
                            else
                            {
                                this._meshBuffer.vertexBuffers[i].x = -x + y * sin;
                            }

                            this._meshBuffer.vertexBuffers[i].y = y * cos;
                        }

                        // if (this._meshRenderer && this._meshRenderer.enabled)
                        {
                            this._meshBuffer.UpdateVertices();
                        }
                    }
                }

                //localScale
                _helpVector3.x = global.scaleX;
                _helpVector3.y = global.scaleY;
                _helpVector3.z = 1.0f;

                transform.localScale = _helpVector3;
            }

            if (_childArmature != null)
            {
                _childArmature.flipX = _armature.flipX;
                _childArmature.flipY = _armature.flipY;
            }
        }

        public Mesh mesh
        {
            get
            {
                if (this._meshBuffer == null)
                {
                    return null;
                }

                return this._meshBuffer.sharedMesh;
            }
        }

        public MeshRenderer meshRenderer
        {
            get { return this._meshRenderer; }
        }

        public UnityTextureAtlasData currentTextureAtlasData
        {
            get
            {
                if (this._textureData == null || this._textureData.parent == null)
                {
                    return null;
                }

                return this._textureData.parent as UnityTextureAtlasData;
            }
        }

        public GameObject renderDisplay
        {
            get { return this._renderDisplay; }
        }

        public UnityArmatureComponent proxy
        {
            get { return this._proxy; }
        }

        public bool isIgnoreCombineMesh
        {
            get { return this._isIgnoreCombineMesh; }
        }
    }
}