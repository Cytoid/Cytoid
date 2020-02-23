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
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DragonBones
{
    /// <summary>
    /// The slots sorting mode
    /// </summary>
    /// <version>DragonBones 4.5</version>
    /// <language>en_US</language>
    /// 
    /// <summary>
    /// 插槽排序模式
    /// </summary>
    /// <version>DragonBones 4.5</version>
    /// <language>zh_CN</language>
    public enum SortingMode
    {
        /// <summary>
        /// Sort by Z values
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 按照插槽显示对象的z值排序
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        SortByZ,
        /// <summary>
        /// Renderer's order within a sorting layer.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 在同一层sorting layer中插槽按照sortingOrder排序
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        SortByOrder
    }

    ///<inheritDoc/>
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class UnityArmatureComponent : DragonBoneEventDispatcher, IArmatureProxy
    {
        public const int ORDER_SPACE = 10;
        /// <private/>
        public UnityDragonBonesData unityData = null;
        /// <private/>
        public string armatureName = null;
        /// <private/>
        public string armatureBaseName = null;
        /// <summary>
        /// Is it the UGUI model?
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 是否是UGUI模式
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public bool isUGUI = false;
        public bool debugDraw = false;
        internal readonly ColorTransform _colorTransform = new ColorTransform();

        /// <private/>
        public string animationName = null;
        /// <private/>
        private bool _disposeProxy = true;
        /// <private/>
        internal Armature _armature = null;
        [Tooltip("0 : Loop")]
        [Range(0, 100)]
        [SerializeField]
        protected int _playTimes = 0;
        [Range(-2f, 2f)]
        [SerializeField]
        protected float _timeScale = 1.0f;

        [SerializeField]
        internal SortingMode _sortingMode = SortingMode.SortByZ;
        [SerializeField]
        internal string _sortingLayerName = "Default";
        [SerializeField]
        internal int _sortingOrder = 0;
        [SerializeField]
        internal float _zSpace = 0.0f;

        [SerializeField]
        protected bool _flipX = false;
        [SerializeField]
        protected bool _flipY = false;
        //default open combineMeshs
        [SerializeField]
        protected bool _closeCombineMeshs;

        private bool _hasSortingGroup = false;
        private Material _debugDrawer;

        //
        internal int _armatureZ;

        /// <private/>
        public void DBClear()
        {
            if (this._armature != null)
            {
                this._armature = null;
                if (this._disposeProxy)
                {
                    try
                    {
                        var go = gameObject;
                        UnityFactoryHelper.DestroyUnityObject(gameObject);
                    }
                    catch (System.Exception e)
                    {

                    }
                }
            }

            this.unityData = null;
            this.armatureName = null;
            this.animationName = null;
            this.isUGUI = false;
            this.debugDraw = false;

            this._disposeProxy = true;
            this._armature = null;
            this._colorTransform.Identity();
            this._sortingMode = SortingMode.SortByZ;
            this._sortingLayerName = "Default";
            this._sortingOrder = 0;
            this._playTimes = 0;
            this._timeScale = 1.0f;
            this._zSpace = 0.0f;
            this._flipX = false;
            this._flipY = false;

            this._hasSortingGroup = false;

            this._debugDrawer = null;

            this._armatureZ = 0;
            this._closeCombineMeshs = false;
        }
        ///
        public void DBInit(Armature armature)
        {
            this._armature = armature;
        }

        public void DBUpdate()
        {

        }

        void CreateLineMaterial()
        {
            if (!_debugDrawer)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _debugDrawer = new Material(shader);
                _debugDrawer.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                _debugDrawer.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _debugDrawer.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                _debugDrawer.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                _debugDrawer.SetInt("_ZWrite", 0);
            }
        }

        void OnRenderObject()
        {
            var drawed = DragonBones.debugDraw || this.debugDraw;
            if (drawed)
            {
                Color boneLineColor = new Color(0.0f, 1.0f, 1.0f, 0.7f);
                Color boundingBoxLineColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);
                CreateLineMaterial();
                // Apply the line material
                _debugDrawer.SetPass(0);

                GL.PushMatrix();
                // Set transformation matrix for drawing to
                // match our transform
                GL.MultMatrix(transform.localToWorldMatrix);
                //
                var bones = this._armature.GetBones();
                var offset = 0.02f;
                // draw bone line
                for (int i = 0; i < bones.Count; i++)
                {
                    var bone = bones[i];
                    var boneLength = System.Math.Max(bone.boneData.length, offset);

                    var startPos = new Vector3(bone.globalTransformMatrix.tx, bone.globalTransformMatrix.ty, 0.0f);
                    var endPos = new Vector3(bone.globalTransformMatrix.a * boneLength, bone.globalTransformMatrix.b * boneLength, 0.0f) + startPos;

                    var torwardDir = (startPos - endPos).normalized;
                    var leftStartPos = Quaternion.AngleAxis(90, Vector3.forward) * torwardDir * offset + startPos;
                    var rightStartPos = Quaternion.AngleAxis(-90, Vector3.forward) * torwardDir * offset + startPos;
                    var newStartPos = startPos + torwardDir * offset;
                    //
                    GL.Begin(GL.LINES);
                    GL.Color(boneLineColor);
                    GL.Vertex(leftStartPos);
                    GL.Vertex(rightStartPos);
                    GL.End();
                    GL.Begin(GL.LINES);
                    GL.Color(boneLineColor);
                    GL.Vertex(newStartPos);
                    GL.Vertex(endPos);
                    GL.End();
                }

                // draw boundingBox
                Point result = new Point();
                var slots = this._armature.GetSlots();
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i] as UnitySlot;
                    var boundingBoxData = slot.boundingBoxData;

                    if (boundingBoxData == null)
                    {
                        continue;
                    }

                    var bone = slot.parent;

                    slot.UpdateTransformAndMatrix();
                    slot.UpdateGlobalTransform();

                    var tx = slot.globalTransformMatrix.tx;
                    var ty = slot.globalTransformMatrix.ty;
                    var boundingBoxWidth = boundingBoxData.width;
                    var boundingBoxHeight = boundingBoxData.height;
                    //
                    switch (boundingBoxData.type)
                    {
                        case BoundingBoxType.Rectangle:
                            {
                                //
#if UNITY_5_6_OR_NEWER
                                GL.Begin(GL.LINE_STRIP);
#else
                                GL.Begin(GL.LINES);
#endif
                                GL.Color(boundingBoxLineColor);

                                var leftTopPos = new Vector3(tx - boundingBoxWidth * 0.5f, ty + boundingBoxHeight * 0.5f, 0.0f);
                                var leftBottomPos = new Vector3(tx - boundingBoxWidth * 0.5f, ty - boundingBoxHeight * 0.5f, 0.0f);
                                var rightTopPos = new Vector3(tx + boundingBoxWidth * 0.5f, ty + boundingBoxHeight * 0.5f, 0.0f);
                                var rightBottomPos = new Vector3(tx + boundingBoxWidth * 0.5f, ty - boundingBoxHeight * 0.5f, 0.0f);

                                GL.Vertex(leftTopPos);
                                GL.Vertex(rightTopPos);
                                GL.Vertex(rightBottomPos);
                                GL.Vertex(leftBottomPos);
                                GL.Vertex(leftTopPos);

                                GL.End();
                            }
                            break;
                        case BoundingBoxType.Ellipse:
                            {

                            }
                            break;
                        case BoundingBoxType.Polygon:
                            {
                                var vertices = (boundingBoxData as PolygonBoundingBoxData).vertices;
#if UNITY_5_6_OR_NEWER
                                GL.Begin(GL.LINE_STRIP);
#else
                                GL.Begin(GL.LINES);
#endif
                                GL.Color(boundingBoxLineColor);
                                for (var j = 0; j < vertices.Count; j += 2)
                                {
                                    slot.globalTransformMatrix.TransformPoint(vertices[j], vertices[j + 1], result);
                                    GL.Vertex3(result.x, result.y, 0.0f);
                                }

                                slot.globalTransformMatrix.TransformPoint(vertices[0], vertices[1], result);
                                GL.Vertex3(result.x, result.y, 0.0f);
                                GL.End();
                            }
                            break;
                        default:
                            break;
                    }
                }

                GL.PopMatrix();
            }

        }

        /// <inheritDoc/>
        public void Dispose(bool disposeProxy = true)
        {
            _disposeProxy = disposeProxy;

            if (_armature != null)
            {
                _armature.Dispose();
            }
        }
        /// <summary>
        /// Get the Armature.
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// 获取骨架。
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public Armature armature
        {
            get { return _armature; }
        }

        /// <summary>
        /// Get the animation player
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// 获取动画播放器。
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public new Animation animation
        {
            get { return _armature != null ? _armature.animation : null; }
        }

        /// <summary>
        /// The slots sorting mode
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 插槽排序模式
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public SortingMode sortingMode
        {
            get { return _sortingMode; }
            set
            {
                if (_sortingMode == value)
                {
                    return;
                }

#if UNITY_5_6_OR_NEWER
                var isWarning = false;
#else
                var isWarning = value == SortingMode.SortByOrder;
#endif

                if (isWarning)
                {
                    LogHelper.LogWarning("SortingMode.SortByOrder is userd by Unity 5.6 or highter only.");
                    return;
                }

                _sortingMode = value;

                //
#if UNITY_5_6_OR_NEWER
                if (_sortingMode == SortingMode.SortByOrder)
                {
                    _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
                    if (_sortingGroup == null)
                    {
                        _sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                    }
                }
                else
                {
                    _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();

                    if (_sortingGroup != null)
                    {
                        DestroyImmediate(_sortingGroup);
                    }
                }
#endif

                _UpdateSlotsSorting();
            }
        }

        /// <summary>
        /// Name of the Renderer's sorting layer.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// sorting layer名称。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public string sortingLayerName
        {
            get { return _sortingLayerName; }
            set
            {
                if (_sortingLayerName == value)
                {
                    //return;
                }

                _sortingLayerName = value;

                _UpdateSlotsSorting();
            }
        }

        /// <summary>
        /// Renderer's order within a sorting layer.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 插槽按照sortingOrder在同一层sorting layer中排序
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public int sortingOrder
        {
            get { return _sortingOrder; }
            set
            {
                if (_sortingOrder == value)
                {
                    //return;
                }

                _sortingOrder = value;

                _UpdateSlotsSorting();
            }
        }
        /// <summary>
        /// The Z axis spacing of slot display objects
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        /// 
        /// <summary>
        /// 插槽显示对象的z轴间隔
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public float zSpace
        {
            get { return _zSpace; }
            set
            {
                if (value < 0.0f || float.IsNaN(value))
                {
                    value = 0.0f;
                }

                if (_zSpace == value)
                {
                    return;
                }

                _zSpace = value;

                _UpdateSlotsSorting();
            }
        }
        /// <summary>
        /// - The armature color.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// - 骨架的颜色。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public ColorTransform color
        {
            get { return this._colorTransform; }
            set
            {
                this._colorTransform.CopyFrom(value);

                foreach (var slot in this._armature.GetSlots())
                {
                    slot._colorDirty = true;
                }
            }
        }


#if UNITY_5_6_OR_NEWER
        internal UnityEngine.Rendering.SortingGroup _sortingGroup;
        public UnityEngine.Rendering.SortingGroup sortingGroup
        {
            get { return _sortingGroup; }
        }

        private void _UpdateSortingGroup()
        {
            //发现骨架有SortingGroup，那么子骨架也都加上，反之删除
            _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (_sortingGroup != null)
            {
                _sortingMode = SortingMode.SortByOrder;
                _sortingLayerName = _sortingGroup.sortingLayerName;
                _sortingOrder = _sortingGroup.sortingOrder;

                foreach (UnitySlot slot in _armature.GetSlots())
                {
                    if (slot.childArmature != null)
                    {
                        var childArmatureProxy = slot.childArmature.proxy as UnityArmatureComponent;
                        childArmatureProxy._sortingGroup = childArmatureProxy.GetComponent<UnityEngine.Rendering.SortingGroup>();
                        if (childArmatureProxy._sortingGroup == null)
                        {
                            childArmatureProxy._sortingGroup = childArmatureProxy.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                        }

                        childArmatureProxy._sortingGroup.sortingLayerName = _sortingLayerName;
                        childArmatureProxy._sortingGroup.sortingOrder = _sortingOrder;
                    }
                }
            }
            else
            {
                _sortingMode = SortingMode.SortByZ;
                foreach (UnitySlot slot in _armature.GetSlots())
                {
                    if (slot.childArmature != null)
                    {
                        var childArmatureProxy = slot.childArmature.proxy as UnityArmatureComponent;
                        childArmatureProxy._sortingGroup = childArmatureProxy.GetComponent<UnityEngine.Rendering.SortingGroup>();
                        if (childArmatureProxy._sortingGroup != null)
                        {
                            DestroyImmediate(childArmatureProxy._sortingGroup);
                        }
                    }
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            _UpdateSlotsSorting();
        }
#endif
        private void _UpdateSlotsSorting()
        {
            if (_armature == null)
            {
                return;
            }

            if (!isUGUI)
            {
#if UNITY_5_6_OR_NEWER
                if (_sortingGroup)
                {
                    _sortingMode = SortingMode.SortByOrder;
                    _sortingGroup.sortingLayerName = _sortingLayerName;
                    _sortingGroup.sortingOrder = _sortingOrder;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(_sortingGroup);
                    }
#endif
                }
#endif
            }

            //
            foreach (UnitySlot slot in _armature.GetSlots())
            {
                var display = slot._renderDisplay;
                if (display == null)
                {
                    continue;
                }

                slot._SetZorder(new Vector3(display.transform.localPosition.x, display.transform.localPosition.y, -slot._zOrder * (_zSpace + 0.001f)));

                if (slot.childArmature != null)
                {
                    (slot.childArmature.proxy as UnityArmatureComponent)._UpdateSlotsSorting();
                }

#if UNITY_EDITOR
                if (!Application.isPlaying && slot.meshRenderer != null)
                {
                    EditorUtility.SetDirty(slot.meshRenderer);
                }
#endif
            }
        }

#if UNITY_EDITOR
        private bool _IsPrefab()
        {
            return PrefabUtility.GetPrefabParent(gameObject) == null
                && PrefabUtility.GetPrefabObject(gameObject) != null;
        }
#endif

        /// <private/>
        void Awake()
        {
#if UNITY_EDITOR
            if (_IsPrefab())
            {
                return;
            }
#endif
            if (unityData != null && unityData.dragonBonesJSON != null && unityData.textureAtlas != null)
            {
                var dragonBonesData = UnityFactory.factory.LoadData(unityData, isUGUI);
                if (dragonBonesData != null && !string.IsNullOrEmpty(armatureName))
                {
                    UnityFactory.factory.BuildArmatureComponent(armatureName, unityData.dataName, null, null, gameObject, isUGUI);
                    if (!string.IsNullOrEmpty(armatureBaseName))
                    {
                        ArmatureData baseData = UnityFactory.factory.GetArmatureData(armatureBaseName, unityData.dataName);
                        UnityFactory.factory.ReplaceAnimation(armature, baseData);
                    }
                }
            }

            if (_armature != null)
            {
#if UNITY_5_6_OR_NEWER
                if (!isUGUI)
                {
                    _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
                }
#endif
                _UpdateSlotsSorting();

                _armature.flipX = _flipX;
                _armature.flipY = _flipY;

                _armature.animation.timeScale = _timeScale;

                if (!string.IsNullOrEmpty(animationName))
                {
                    _armature.animation.Play(animationName, _playTimes);
                }
            }


        }

        void Start()
        {
            // this._closeCombineMeshs = true;
            //默认开启合并
            if (this._closeCombineMeshs)
            {
                this.CloseCombineMeshs();
            }
            else
            {
                this.OpenCombineMeshs();
            }
        }

        void LateUpdate()
        {
            if (_armature == null)
            {
                return;
            }

            _flipX = _armature.flipX;
            _flipY = _armature.flipY;

#if UNITY_5_6_OR_NEWER
            var hasSortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>() != null;
            if (hasSortingGroup != _hasSortingGroup)
            {
                _hasSortingGroup = hasSortingGroup;

                _UpdateSortingGroup();
            }
#endif
        }

        /// <private/>
        void OnDestroy()
        {
            if (_armature != null)
            {
                var armature = _armature;
                _armature = null;

                armature.Dispose();

                if (!Application.isPlaying)
                {
                    UnityFactory.factory._dragonBones.AdvanceTime(0.0f);
                }
            }

            _disposeProxy = true;
            _armature = null;
        }

        private void OpenCombineMeshs()
        {
            if (this.isUGUI)
            {
                return;
            }

            //
            var cm = gameObject.GetComponent<UnityCombineMeshs>();
            if (cm == null)
            {
                cm = gameObject.AddComponent<UnityCombineMeshs>();
            }
            //

            if (this._armature == null)
            {
                return;
            }
            var slots = this._armature.GetSlots();
            foreach (var slot in slots)
            {
                if (slot.childArmature != null)
                {
                    (slot.childArmature.proxy as UnityArmatureComponent).OpenCombineMeshs();
                }
            }
        }

        public void CloseCombineMeshs()
        {
            this._closeCombineMeshs = true;
            //
            var cm = gameObject.GetComponent<UnityCombineMeshs>();
            if (cm != null)
            {
                DestroyImmediate(cm);
            }

            if (this._armature == null)
            {
                return;
            }
            //
            var slots = this._armature.GetSlots();
            foreach (var slot in slots)
            {
                if (slot.childArmature != null)
                {
                    (slot.childArmature.proxy as UnityArmatureComponent).CloseCombineMeshs();
                }
            }
        }
    }
}