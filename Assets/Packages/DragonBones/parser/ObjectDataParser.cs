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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DragonBones
{
    /// <internal/>
    /// <private/>
    public class ObjectDataParser : DataParser
    {
        protected static bool _GetBoolean(Dictionary<string, object> rawData, string key, bool defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];
                if (value is bool)
                {
                    return (bool)value;
                }
                else if (value is string)
                {
                    switch (value as string)
                    {
                        case "0":
                        case "NaN":
                        case "":
                        case "false":
                        case "null":
                        case "undefined":
                            return false;

                        default:
                            return true;
                    }
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }

            return defaultValue;
        }

        protected static uint _GetNumber(Dictionary<string, object> rawData, string key, uint defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];

                if (value == null)
                {
                    return defaultValue;
                }

                if (value is uint)
                {
                    return (uint)value;
                }

                return Convert.ToUInt32(value);

            }

            return defaultValue;
        }

        protected static int _GetNumber(Dictionary<string, object> rawData, string key, int defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];

                if (value == null)
                {
                    return defaultValue;
                }

                if (value is int)
                {
                    return (int)value;
                }

                return Convert.ToInt32(value);
            }

            return defaultValue;
        }
        protected static float _GetNumber(Dictionary<string, object> rawData, string key, float defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];

                if (value == null)
                {
                    return defaultValue;
                }

                if (value is float)
                {
                    return (float)value;
                }

                return Convert.ToSingle(value);
            }

            return defaultValue;
        }

        protected static string _GetString(Dictionary<string, object> rawData, string key, string defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];
                var res = Convert.ToString(value);
                if (value is string)
                {
                    res = (string)value;
                }

                if (!string.IsNullOrEmpty(res))
                {
                    return res;
                }
            }

            return defaultValue;
        }

        protected int _rawTextureAtlasIndex = 0;
        protected readonly List<BoneData> _rawBones = new List<BoneData>();
        protected DragonBonesData _data = null; //
        protected ArmatureData _armature = null; //
        protected BoneData _bone = null; //
        protected SlotData _slot = null; //
        protected SkinData _skin = null; //
        protected MeshDisplayData _mesh = null; //
        protected AnimationData _animation = null; //
        protected TimelineData _timeline = null; //
        protected List<object> _rawTextureAtlases = null;

        private int _defaultColorOffset = -1;
        private int _prevClockwise = 0;
        private float _prevRotation = 0.0f;
        private readonly Matrix _helpMatrixA = new Matrix();
        private readonly Matrix _helpMatrixB = new Matrix();
        private readonly Transform _helpTransform = new Transform();
        private readonly ColorTransform _helpColorTransform = new ColorTransform();
        private readonly Point _helpPoint = new Point();
        private readonly List<float> _helpArray = new List<float>();
        private readonly List<short> _intArray = new List<short>();
        private readonly List<float> _floatArray = new List<float>();
        private readonly List<short> _frameIntArray = new List<short>();
        private readonly List<float> _frameFloatArray = new List<float>();
        private readonly List<short> _frameArray = new List<short>();
        private readonly List<ushort> _timelineArray = new List<ushort>();
        private readonly List<object> _cacheRawMeshes = new List<object>();
        private readonly List<MeshDisplayData> _cacheMeshes = new List<MeshDisplayData>();
        private readonly List<ActionFrame> _actionFrames = new List<ActionFrame>();
        private readonly Dictionary<string, List<float>> _weightSlotPose = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, List<float>> _weightBonePoses = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, List<uint>> _weightBoneIndices = new Dictionary<string, List<uint>>();
        private readonly Dictionary<string, List<BoneData>> _cacheBones = new Dictionary<string, List<BoneData>>();
        private readonly Dictionary<string, List<ActionData>> _slotChildActions = new Dictionary<string, List<ActionData>>();

        public ObjectDataParser()
        {

        }

        private void _GetCurvePoint(float x1, float y1,
                                    float x2, float y2,
                                    float x3, float y3,
                                    float x4, float y4,
                                    float t, Point result)
        {
            var l_t = 1.0f - t;
            var powA = l_t * l_t;
            var powB = t * t;
            var kA = l_t * powA;
            var kB = 3.0f * t * powA;
            var kC = 3.0f * l_t * powB;
            var kD = t * powB;

            result.x = kA * x1 + kB * x2 + kC * x3 + kD * x4;
            result.y = kA * y1 + kB * y2 + kC * y3 + kD * y4;
        }

        private void _SamplingEasingCurve(float[] curve, List<float> samples)
        {
            var curveCount = curve.Length;
            var stepIndex = -2;
            for (int i = 0, l = samples.Count; i < l; ++i)
            {
                var t = ((float)i + 1.0f) / ((float)l + 1.0f);
                while ((stepIndex + 6 < curveCount ? curve[stepIndex + 6] : 1) < t)
                {
                    // stepIndex + 3 * 2
                    stepIndex += 6;
                }

                var isInCurve = stepIndex >= 0 && stepIndex + 6 < curveCount;
                var x1 = isInCurve ? curve[stepIndex] : 0.0f;
                var y1 = isInCurve ? curve[stepIndex + 1] : 0.0f;
                var x2 = curve[stepIndex + 2];
                var y2 = curve[stepIndex + 3];
                var x3 = curve[stepIndex + 4];
                var y3 = curve[stepIndex + 5];
                var x4 = isInCurve ? curve[stepIndex + 6] : 1.0f;
                var y4 = isInCurve ? curve[stepIndex + 7] : 1.0f;

                var lower = 0.0f;
                var higher = 1.0f;
                while (higher - lower > 0.0001f)
                {
                    var percentage = (higher + lower) * 0.5f;
                    this._GetCurvePoint(x1, y1, x2, y2, x3, y3, x4, y4, percentage, this._helpPoint);
                    if (t - this._helpPoint.x > 0.0)
                    {
                        lower = percentage;
                    }
                    else
                    {
                        higher = percentage;
                    }
                }

                samples[i] = this._helpPoint.y;
            }
        }
        //private int _SortActionFrame(ActionFrame a, ActionFrame b)
        //{
        //    return a.frameStart > b.frameStart? 1 : -1;
        //}
        private void _ParseActionDataInFrame(object rawData, int frameStart, BoneData bone = null, SlotData slot = null)
        {
            Dictionary<string, object> rawDic = rawData as Dictionary<string, object>;
            if (rawDic == null)
            {
                return;
            }

            if (rawDic.ContainsKey(ObjectDataParser.EVENT))
            {
                this._MergeActionFrame(rawDic[ObjectDataParser.EVENT], frameStart, ActionType.Frame, bone, slot);
            }

            if (rawDic.ContainsKey(ObjectDataParser.SOUND))
            {
                this._MergeActionFrame(rawDic[ObjectDataParser.SOUND], frameStart, ActionType.Sound, bone, slot);
            }

            if (rawDic.ContainsKey(ObjectDataParser.ACTION))
            {
                this._MergeActionFrame(rawDic[ObjectDataParser.ACTION], frameStart, ActionType.Play, bone, slot);
            }

            if (rawDic.ContainsKey(ObjectDataParser.EVENTS))
            {
                this._MergeActionFrame(rawDic[ObjectDataParser.EVENTS], frameStart, ActionType.Frame, bone, slot);
            }

            if (rawDic.ContainsKey(ObjectDataParser.ACTIONS))
            {
                this._MergeActionFrame(rawDic[ObjectDataParser.ACTIONS], frameStart, ActionType.Play, bone, slot);
            }
        }
        private void _MergeActionFrame(object rawData, int frameStart, ActionType type, BoneData bone = null, SlotData slot = null)
        {
            var actionOffset = this._armature.actions.Count;
            var actions = this._ParseActionData(rawData, type, bone, slot);
            var frameIndex = 0;
            ActionFrame frame = null;

            foreach (var action in actions)
            {
                this._armature.AddAction(action, false);
            }

            if (this._actionFrames.Count == 0)
            {
                // First frame.
                frame = new ActionFrame();
                frame.frameStart = 0;
                this._actionFrames.Add(frame);
                frame = null;
            }

            foreach (var eachFrame in this._actionFrames)
            {
                // Get same frame.
                if (eachFrame.frameStart == frameStart)
                {
                    frame = eachFrame;
                    break;
                }
                else if (eachFrame.frameStart > frameStart)
                {
                    break;
                }

                frameIndex++;
            }

            if (frame == null)
            {
                // Create and cache frame.
                frame = new ActionFrame();
                frame.frameStart = frameStart;

                if (frameIndex + 1 < this._actionFrames.Count)
                {
                    this._actionFrames.Insert(frameIndex + 1, frame);
                }
                else
                {
                    this._actionFrames.Add(frame);
                }
            }

            for (var i = 0; i < actions.Count; ++i)
            {
                // Cache action offsets.
                frame.actions.Add(actionOffset + i);
            }
        }

        private int _ParseCacheActionFrame(ActionFrame frame)
        {
            var frameOffset = this._frameArray.Count;
            var actionCount = frame.actions.Count;
            this._frameArray.ResizeList(this._frameArray.Count + 1 + 1 + actionCount, (short)0);
            this._frameArray[frameOffset + (int)BinaryOffset.FramePosition] = (short)frame.frameStart;
            this._frameArray[frameOffset + (int)BinaryOffset.FramePosition + 1] = (short)actionCount; // Action count.

            for (var i = 0; i < actionCount; ++i)
            {
                // Action offsets.
                this._frameArray[frameOffset + (int)BinaryOffset.FramePosition + 2 + i] = (short)frame.actions[i];
            }

            return frameOffset;
        }

        private ArmatureData _ParseArmature(Dictionary<string, object> rawData, float scale)
        {
            var armature = BaseObject.BorrowObject<ArmatureData>();
            armature.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");
            armature.frameRate = ObjectDataParser._GetNumber(rawData, ObjectDataParser.FRAME_RATE, this._data.frameRate);
            armature.scale = scale;

            if (rawData.ContainsKey(ObjectDataParser.TYPE) && rawData[ObjectDataParser.TYPE] is string)
            {
                armature.type = ObjectDataParser._GetArmatureType((string)rawData[ObjectDataParser.TYPE]);
            }
            else
            {
                armature.type = (ArmatureType)ObjectDataParser._GetNumber(rawData, ObjectDataParser.TYPE.ToString(), (int)ArmatureType.Armature);
            }

            if (armature.frameRate == 0)
            {
                // Data error.
                armature.frameRate = 24;
            }

            this._armature = armature;

            //CANVAS功能为完全实现，这里先注释
            if (rawData != null && rawData.ContainsKey(ObjectDataParser.CANVAS))
            {
                var rawCanvas = rawData[ObjectDataParser.CANVAS] as Dictionary<string, object>;
                var canvas = BaseObject.BorrowObject<CanvasData>();

                if (rawData.ContainsKey(ObjectDataParser.COLOR))
                {
                    canvas.hasBackground = true;
                }
                else
                {
                    canvas.hasBackground = false;
                }

                canvas.color = ObjectDataParser._GetNumber(rawCanvas, ObjectDataParser.COLOR, 0);
                canvas.x = ObjectDataParser._GetNumber(rawCanvas, ObjectDataParser.X, 0) * armature.scale;
                canvas.y = ObjectDataParser._GetNumber(rawCanvas, ObjectDataParser.Y, 0) * armature.scale;
                canvas.width = ObjectDataParser._GetNumber(rawCanvas, ObjectDataParser.WIDTH, 0) * armature.scale;
                canvas.height = ObjectDataParser._GetNumber(rawCanvas, ObjectDataParser.HEIGHT, 0) * armature.scale;

                armature.canvas = canvas;
            }

            if (rawData.ContainsKey(ObjectDataParser.AABB))
            {
                var rawAABB = rawData[AABB] as Dictionary<string, object>;
                armature.aabb.x = ObjectDataParser._GetNumber(rawAABB, ObjectDataParser.X, 0.0f) * armature.scale;
                armature.aabb.y = ObjectDataParser._GetNumber(rawAABB, ObjectDataParser.Y, 0.0f) * armature.scale;
                armature.aabb.width = ObjectDataParser._GetNumber(rawAABB, ObjectDataParser.WIDTH, 0.0f) * armature.scale;
                armature.aabb.height = ObjectDataParser._GetNumber(rawAABB, ObjectDataParser.HEIGHT, 0.0f) * armature.scale;
            }

            if (rawData.ContainsKey(ObjectDataParser.BONE))
            {
                var rawBones = rawData[ObjectDataParser.BONE] as List<object>;
                foreach (Dictionary<string, object> rawBone in rawBones)
                {
                    var parentName = ObjectDataParser._GetString(rawBone, ObjectDataParser.PARENT, "");
                    var bone = this._ParseBone(rawBone);

                    if (parentName.Length > 0)
                    {
                        // Get bone parent.
                        var parent = armature.GetBone(parentName);
                        if (parent != null)
                        {
                            bone.parent = parent;
                        }
                        else
                        {
                            // Cache.
                            if (!this._cacheBones.ContainsKey(parentName))
                            {
                                this._cacheBones[parentName] = new List<BoneData>();
                            }

                            this._cacheBones[parentName].Add(bone);
                        }
                    }

                    if (this._cacheBones.ContainsKey(bone.name))
                    {
                        foreach (var child in this._cacheBones[bone.name])
                        {
                            child.parent = bone;
                        }

                        this._cacheBones[bone.name].Clear();
                    }

                    armature.AddBone(bone);
                    this._rawBones.Add(bone); // Cache raw bones sort.
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.IK))
            {
                var rawIKS = rawData[ObjectDataParser.IK] as List<object>;
                foreach (Dictionary<string, object> rawIK in rawIKS)
                {
                    var constraint = this._ParseIKConstraint(rawIK);
                    if (constraint != null)
                    {
                        armature.AddConstraint(constraint);
                    }
                }
            }

            armature.SortBones();

            if (rawData.ContainsKey(ObjectDataParser.SLOT))
            {
                var zOrder = 0;
                var rawSlots = rawData[ObjectDataParser.SLOT] as List<object>;
                foreach (Dictionary<string, object> rawSlot in rawSlots)
                {
                    armature.AddSlot(this._ParseSlot(rawSlot, zOrder++));
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.SKIN))
            {
                var rawSkins = rawData[ObjectDataParser.SKIN] as List<object>;
                foreach (Dictionary<string, object> rawSkin in rawSkins)
                {
                    armature.AddSkin(this._ParseSkin(rawSkin));
                }
            }

            for (int i = 0, l = this._cacheRawMeshes.Count; i < l; i++)
            {
                var shareName = ObjectDataParser._GetString(rawData, DataParser.SHARE, "");
                if (shareName.Length == 0)
                {
                    continue;
                }

                var skinName = ObjectDataParser._GetString(rawData, DataParser.SKIN, DataParser.DEFAULT_NAME);
                if (skinName.Length == 0)
                { // 
                    skinName = DataParser.DEFAULT_NAME;
                }
                
                var shareMesh = armature.GetMesh(skinName, "", shareName) as MeshDisplayData; // TODO slot;
                if (shareMesh == null)
                {
                    continue; // Error.
                }

                var mesh = this._cacheMeshes[i];
                mesh.vertices.ShareFrom(shareMesh.vertices);
            }

            if (rawData.ContainsKey(ObjectDataParser.ANIMATION))
            {
                var rawAnimations = rawData[ObjectDataParser.ANIMATION] as List<object>;
                foreach (Dictionary<string, object> rawAnimation in rawAnimations)
                {
                    var animation = this._ParseAnimation(rawAnimation);
                    armature.AddAnimation(animation);
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.DEFAULT_ACTIONS))
            {
                var actions = this._ParseActionData(rawData[ObjectDataParser.DEFAULT_ACTIONS], ActionType.Play, null, null);
                foreach (var action in actions)
                {
                    armature.AddAction(action, true);

                    if (action.type == ActionType.Play)
                    { // Set default animation from default action.
                        var animation = armature.GetAnimation(action.name);
                        if (animation != null)
                        {
                            armature.defaultAnimation = animation;
                        }
                    }
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.ACTIONS))
            {
                var actions = this._ParseActionData(rawData[ObjectDataParser.ACTIONS], ActionType.Play, null, null);

                foreach (var action in actions)
                {
                    armature.AddAction(action, false);
                }
            }

            // Clear helper.
            this._rawBones.Clear();
            this._cacheRawMeshes.Clear();
            this._cacheMeshes.Clear();
            this._armature = null;

            this._cacheBones.Clear();
            this._slotChildActions.Clear();
            this._weightSlotPose.Clear();
            this._weightBonePoses.Clear();
            this._weightBoneIndices.Clear();

            return armature;
        }
        protected BoneData _ParseBone(Dictionary<string, object> rawData)
        {
            var scale = this._armature.scale;

            var bone = BaseObject.BorrowObject<BoneData>();
            bone.inheritTranslation = ObjectDataParser._GetBoolean(rawData, ObjectDataParser.INHERIT_TRANSLATION, true);
            bone.inheritRotation = ObjectDataParser._GetBoolean(rawData, ObjectDataParser.INHERIT_ROTATION, true);
            bone.inheritScale = ObjectDataParser._GetBoolean(rawData, ObjectDataParser.INHERIT_SCALE, true);
            bone.inheritReflection = ObjectDataParser._GetBoolean(rawData, ObjectDataParser.INHERIT_REFLECTION, true);
            bone.length = ObjectDataParser._GetNumber(rawData, ObjectDataParser.LENGTH, 0) * scale;
            bone.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");

            if (rawData.ContainsKey(ObjectDataParser.TRANSFORM))
            {
                this._ParseTransform(rawData[ObjectDataParser.TRANSFORM] as Dictionary<string, object>, bone.transform, scale);
            }

            return bone;
        }
        protected ConstraintData _ParseIKConstraint(Dictionary<string, object> rawData)
        {
            var bone = _armature.GetBone(_GetString(rawData, BONE, ""));
            if (bone == null)
            {
                return null;
            }

            var target = this._armature.GetBone(ObjectDataParser._GetString(rawData, ObjectDataParser.TARGET, ""));
            if (target == null)
            {
                return null;
            }

            var constraint = BaseObject.BorrowObject<IKConstraintData>();
            constraint.scaleEnabled = ObjectDataParser._GetBoolean(rawData, ObjectDataParser.SCALE, false);
            constraint.bendPositive = ObjectDataParser._GetBoolean(rawData, ObjectDataParser.BEND_POSITIVE, true);
            constraint.weight = ObjectDataParser._GetNumber(rawData, ObjectDataParser.WEIGHT, 1.0f);
            constraint.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");
            constraint.bone = bone;
            constraint.target = target;

            var chain = ObjectDataParser._GetNumber(rawData, ObjectDataParser.CHAIN, 0);
            if (chain > 0 && bone.parent != null)
            {
                constraint.root = bone.parent;
                constraint.bone = bone;
            }
            else
            {
                constraint.root = bone;
                constraint.bone = null;
            }

            return constraint;
        }

        private SlotData _ParseSlot(Dictionary<string, object> rawData, int zOrder)
        {
            var slot = BaseObject.BorrowObject<SlotData>();
            slot.displayIndex = ObjectDataParser._GetNumber(rawData, ObjectDataParser.DISPLAY_INDEX, 0);
            slot.zOrder = zOrder;
            slot.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");
            slot.parent = this._armature.GetBone(ObjectDataParser._GetString(rawData, ObjectDataParser.PARENT, "")); //

            if (rawData.ContainsKey(ObjectDataParser.BLEND_MODE) && rawData[ObjectDataParser.BLEND_MODE] is string)
            {
                slot.blendMode = ObjectDataParser._GetBlendMode((string)rawData[ObjectDataParser.BLEND_MODE]);
            }
            else
            {
                slot.blendMode = (BlendMode)ObjectDataParser._GetNumber(rawData, ObjectDataParser.BLEND_MODE, (int)BlendMode.Normal);
            }

            if (rawData.ContainsKey(ObjectDataParser.COLOR))
            {
                slot.color = SlotData.CreateColor();
                this._ParseColorTransform(rawData[ObjectDataParser.COLOR] as Dictionary<string, object>, slot.color);
            }
            else
            {
                slot.color = SlotData.DEFAULT_COLOR;
            }

            if (rawData.ContainsKey(ObjectDataParser.ACTIONS))
            {
                this._slotChildActions[slot.name] = this._ParseActionData(rawData[ObjectDataParser.ACTIONS], ActionType.Play, null, null);
            }

            return slot;
        }

        protected SkinData _ParseSkin(Dictionary<string, object> rawData)
        {
            var skin = BaseObject.BorrowObject<SkinData>();
            skin.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, ObjectDataParser.DEFAULT_NAME);

            if (rawData.ContainsKey(ObjectDataParser.SLOT))
            {
                var rawSlots = rawData[ObjectDataParser.SLOT] as List<object>;
                this._skin = skin;

                foreach (Dictionary<string, object> rawSlot in rawSlots)
                {
                    var slotName = ObjectDataParser._GetString(rawSlot, ObjectDataParser.NAME, "");
                    var slot = this._armature.GetSlot(slotName);
                    if (slot != null)
                    {
                        this._slot = slot;

                        if (rawSlot.ContainsKey(ObjectDataParser.DISPLAY))
                        {
                            var rawDisplays = rawSlot[ObjectDataParser.DISPLAY] as List<object>;
                            foreach (Dictionary<string, object> rawDisplay in rawDisplays)
                            {
                                skin.AddDisplay(slotName, this._ParseDisplay(rawDisplay));
                            }
                        }

                        this._slot = null; //
                    }
                }

                this._skin = null;
            }

            return skin;
        }

        protected DisplayData _ParseDisplay(Dictionary<string, object> rawData)
        {
            var name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");
            var path = ObjectDataParser._GetString(rawData, ObjectDataParser.PATH, "");
            var type = DisplayType.Image;
            DisplayData display = null;

            if (rawData.ContainsKey(ObjectDataParser.TYPE) && rawData[ObjectDataParser.TYPE] is string)
            {
                type = ObjectDataParser._GetDisplayType((string)rawData[ObjectDataParser.TYPE]);
            }
            else
            {
                type = (DisplayType)ObjectDataParser._GetNumber(rawData, ObjectDataParser.TYPE, (int)type);
            }

            switch (type)
            {
                case DisplayType.Image:
                    var imageDisplay = BaseObject.BorrowObject<ImageDisplayData>();
                    display = imageDisplay;
                    imageDisplay.name = name;
                    imageDisplay.path = path.Length > 0 ? path : name;
                    this._ParsePivot(rawData, imageDisplay);
                    break;
                case DisplayType.Armature:
                    var armatureDisplay = BaseObject.BorrowObject<ArmatureDisplayData>();
                    display = armatureDisplay;
                    armatureDisplay.name = name;
                    armatureDisplay.path = path.Length > 0 ? path : name;
                    armatureDisplay.inheritAnimation = true;

                    if (rawData.ContainsKey(ObjectDataParser.ACTIONS))
                    {
                        var actions = this._ParseActionData(rawData[ObjectDataParser.ACTIONS], ActionType.Play, null, null);

                        foreach (var action in actions)
                        {
                            armatureDisplay.AddAction(action);
                        }
                    }
                    else if (this._slotChildActions.ContainsKey(this._slot.name))
                    {
                        var displays = this._skin.GetDisplays(this._slot.name);
                        if (displays == null ? this._slot.displayIndex == 0 : this._slot.displayIndex == displays.Count)
                        {
                            foreach (var action in this._slotChildActions[this._slot.name])
                            {
                                armatureDisplay.AddAction(action);
                            }

                            this._slotChildActions[this._slot.name].Clear();
                        }
                    }
                    break;

                case DisplayType.Mesh:
                    var meshDisplay = BaseObject.BorrowObject<MeshDisplayData>();
                    display = meshDisplay;
                    meshDisplay.vertices.inheritDeform = ObjectDataParser._GetBoolean(rawData, DataParser.INHERIT_DEFORM, true);
                    meshDisplay.name = name;
                    meshDisplay.path = path.Length > 0 ? path : name;
                    meshDisplay.vertices.data = this._data;

                    if (rawData.ContainsKey(DataParser.SHARE))
                    {
                        this._cacheRawMeshes.Add(rawData);
                        this._cacheMeshes.Add(meshDisplay);
                    }
                    else
                    {
                        this._ParseMesh(rawData, meshDisplay);
                    }
                    break;

                case DisplayType.BoundingBox:
                    var boundingBox = this._ParseBoundingBox(rawData);
                    if (boundingBox != null)
                    {
                        var boundingBoxDisplay = BaseObject.BorrowObject<BoundingBoxDisplayData>();
                        display = boundingBoxDisplay;
                        boundingBoxDisplay.name = name;
                        boundingBoxDisplay.path = path.Length > 0 ? path : name;
                        boundingBoxDisplay.boundingBox = boundingBox;
                    }
                    break;
            }

            if (display != null && rawData.ContainsKey(ObjectDataParser.TRANSFORM))
            {
                this._ParseTransform(rawData[ObjectDataParser.TRANSFORM] as Dictionary<string, object>, display.transform, this._armature.scale);
            }

            return display;
        }

        protected void _ParsePivot(Dictionary<string, object> rawData, ImageDisplayData display)
        {
            if (rawData.ContainsKey(ObjectDataParser.PIVOT))
            {
                var rawPivot = rawData[ObjectDataParser.PIVOT] as Dictionary<string, object>;
                display.pivot.x = ObjectDataParser._GetNumber(rawPivot, ObjectDataParser.X, 0.0f);
                display.pivot.y = ObjectDataParser._GetNumber(rawPivot, ObjectDataParser.Y, 0.0f);
            }
            else
            {
                display.pivot.x = 0.5f;
                display.pivot.y = 0.5f;
            }
        }
        protected virtual void _ParseMesh(Dictionary<string, object> rawData, MeshDisplayData mesh)
        {
            var rawVertices = (rawData[ObjectDataParser.VERTICES] as List<object>).ConvertAll<float>(Convert.ToSingle);//float
            var rawUVs = (rawData[ObjectDataParser.UVS] as List<object>).ConvertAll<float>(Convert.ToSingle);//float
            var rawTriangles = (rawData[ObjectDataParser.TRIANGLES] as List<object>).ConvertAll<short>(Convert.ToInt16);//uint
            var vertexCount = (rawVertices.Count / 2); // uint
            var triangleCount = (rawTriangles.Count / 3); // uint
            var vertexOffset = this._floatArray.Count;
            var uvOffset = vertexOffset + vertexCount * 2;
            var meshOffset = this._intArray.Count;
            var meshName = this._skin.name + "_" + this._slot.name + "_" + mesh.name; // Cache pose data.


            mesh.vertices.offset = meshOffset;
            this._intArray.ResizeList(this._intArray.Count + 1 + 1 + 1 + 1 + triangleCount * 3, (short)0);
            this._intArray[meshOffset + (int)BinaryOffset.MeshVertexCount] = (short)vertexCount;
            this._intArray[meshOffset + (int)BinaryOffset.MeshTriangleCount] = (short)triangleCount;
            this._intArray[meshOffset + (int)BinaryOffset.MeshFloatOffset] = (short)vertexOffset;

            for (int i = 0, l = triangleCount * 3; i < l; ++i)
            {
                this._intArray[meshOffset + (int)BinaryOffset.MeshVertexIndices + i] = rawTriangles[i];
            }

            this._floatArray.ResizeList(this._floatArray.Count + vertexCount * 2 + vertexCount * 2, 0.0f);
            for (int i = 0, l = vertexCount * 2; i < l; ++i)
            {
                this._floatArray[vertexOffset + i] = rawVertices[i];
                this._floatArray[uvOffset + i] = rawUVs[i];
            }

            if (rawData.ContainsKey(ObjectDataParser.WEIGHTS))
            {
                var rawWeights = (rawData[ObjectDataParser.WEIGHTS] as List<object>).ConvertAll<float>(Convert.ToSingle); // float;
                var rawSlotPose = (rawData[ObjectDataParser.SLOT_POSE] as List<object>).ConvertAll<float>(Convert.ToSingle); // float;
                var rawBonePoses = (rawData[ObjectDataParser.BONE_POSE] as List<object>).ConvertAll<float>(Convert.ToSingle); //float ;
                //var sortedBones = this._armature.sortedBones;
                var weightBoneIndices = new List<uint>();
                var weightBoneCount = rawBonePoses.Count / 7; // uint
                var floatOffset = this._floatArray.Count;
                var weightCount = (int)Math.Floor((double)rawWeights.Count - (double)vertexCount) / 2; // uint
                var weightOffset = this._intArray.Count;
                var weight = BaseObject.BorrowObject<WeightData>();

                weight.count = weightCount;
                weight.offset = weightOffset;

                weightBoneIndices.ResizeList(weightBoneCount, uint.MinValue);
                this._intArray.ResizeList(this._intArray.Count + 1 + 1 + weightBoneCount + vertexCount + weight.count, (short)0);
                this._intArray[weightOffset + (int)BinaryOffset.WeigthFloatOffset] = (short)floatOffset;

                for (var i = 0; i < weightBoneCount; ++i)
                {
                    var rawBoneIndex = (int)rawBonePoses[i * 7]; // uint
                    var bone = this._rawBones[(int)rawBoneIndex];
                    weight.AddBone(bone);
                    weightBoneIndices[i] = (uint)rawBoneIndex;

                    this._intArray[weightOffset + (int)BinaryOffset.WeigthBoneIndices + i] = (short)this._armature.sortedBones.IndexOf(bone);
                }

                this._floatArray.ResizeList(this._floatArray.Count + (weightCount * 3), 0.0f);
                this._helpMatrixA.CopyFromArray(rawSlotPose, 0);
                for (int i = 0, iW = 0, iB = weightOffset + (int)BinaryOffset.WeigthBoneIndices + weightBoneCount, iV = floatOffset; i < vertexCount; ++i)
                {
                    var iD = i * 2;
                    var vertexBoneCount = this._intArray[iB++] = short.Parse(rawWeights[iW++].ToString()); // uint

                    var x = this._floatArray[vertexOffset + iD];
                    var y = this._floatArray[vertexOffset + iD + 1];
                    this._helpMatrixA.TransformPoint(x, y, this._helpPoint);
                    x = this._helpPoint.x;
                    y = this._helpPoint.y;

                    for (var j = 0; j < vertexBoneCount; ++j)
                    {
                        var rawBoneIndex = (uint)rawWeights[iW++]; // uint
                        var boneIndex = weightBoneIndices.IndexOf(rawBoneIndex);
                        this._helpMatrixB.CopyFromArray(rawBonePoses, weightBoneIndices.IndexOf(rawBoneIndex) * 7 + 1);
                        this._helpMatrixB.Invert();
                        this._helpMatrixB.TransformPoint(x, y, this._helpPoint);
                        this._intArray[iB++] = (short)boneIndex;
                        this._floatArray[iV++] = rawWeights[iW++];
                        this._floatArray[iV++] = this._helpPoint.x;
                        this._floatArray[iV++] = this._helpPoint.y;
                    }
                }

                mesh.vertices.weight = weight;

                this._weightSlotPose[meshName] = rawSlotPose;
                this._weightBonePoses[meshName] = rawBonePoses;
            }
        }
        protected BoundingBoxData _ParseBoundingBox(Dictionary<string, object> rawData)
        {
            BoundingBoxData boundingBox = null;
            var type = BoundingBoxType.Rectangle;

            if (rawData.ContainsKey(ObjectDataParser.SUB_TYPE) && rawData[ObjectDataParser.SUB_TYPE] is string)
            {
                type = ObjectDataParser._GetBoundingBoxType((string)rawData[ObjectDataParser.SUB_TYPE]);
            }
            else
            {
                type = (BoundingBoxType)ObjectDataParser._GetNumber(rawData, ObjectDataParser.SUB_TYPE, (uint)type);
            }

            switch (type)
            {
                case BoundingBoxType.Rectangle:
                    boundingBox = BaseObject.BorrowObject<RectangleBoundingBoxData>();
                    break;

                case BoundingBoxType.Ellipse:
                    boundingBox = BaseObject.BorrowObject<EllipseBoundingBoxData>();
                    break;

                case BoundingBoxType.Polygon:
                    boundingBox = this._ParsePolygonBoundingBox(rawData);
                    break;
            }

            if (boundingBox != null)
            {
                boundingBox.color = ObjectDataParser._GetNumber(rawData, ObjectDataParser.COLOR, (uint)0x000000);
                if (boundingBox.type == BoundingBoxType.Rectangle || boundingBox.type == BoundingBoxType.Ellipse)
                {
                    boundingBox.width = ObjectDataParser._GetNumber(rawData, ObjectDataParser.WIDTH, 0.0f);
                    boundingBox.height = ObjectDataParser._GetNumber(rawData, ObjectDataParser.HEIGHT, 0.0f);
                }
            }

            return boundingBox;
        }
        protected PolygonBoundingBoxData _ParsePolygonBoundingBox(Dictionary<string, object> rawData)
        {
            var polygonBoundingBox = BaseObject.BorrowObject<PolygonBoundingBoxData>();

            if (rawData.ContainsKey(ObjectDataParser.VERTICES))
            {
                float scale = this._armature.scale;
                var rawVertices = (rawData[ObjectDataParser.VERTICES] as List<object>).ConvertAll<float>(Convert.ToSingle);
                var vertices = polygonBoundingBox.vertices;

                vertices.ResizeList(rawVertices.Count, 0.0f);
                for (int i = 0, l = rawVertices.Count; i < l; i += 2)
                {
                    var x = rawVertices[i] * scale;
                    var y = rawVertices[i + 1] * scale;

                    vertices[i] = x;
                    vertices[i + 1] = y;

                    if (i == 0)
                    {
                        polygonBoundingBox.x = x;
                        polygonBoundingBox.y = y;
                        polygonBoundingBox.width = x;
                        polygonBoundingBox.height = y;
                    }
                    else
                    {
                        if (x < polygonBoundingBox.x)
                        {
                            polygonBoundingBox.x = x;
                        }
                        else if (x > polygonBoundingBox.width)
                        {
                            polygonBoundingBox.width = x;
                        }

                        if (y < polygonBoundingBox.y)
                        {
                            polygonBoundingBox.y = y;
                        }
                        else if (y > polygonBoundingBox.height)
                        {
                            polygonBoundingBox.height = y;
                        }
                    }
                }

                polygonBoundingBox.width -= polygonBoundingBox.x;
                polygonBoundingBox.height -= polygonBoundingBox.y;
            }
            else
            {
                Helper.Assert(false, "Data error.\n Please reexport DragonBones Data to fixed the bug.");
            }

            return polygonBoundingBox;

        }
        protected virtual AnimationData _ParseAnimation(Dictionary<string, object> rawData)
        {
            var animation = BaseObject.BorrowObject<AnimationData>();

            animation.frameCount = (uint)Math.Max(ObjectDataParser._GetNumber(rawData, ObjectDataParser.DURATION, 1), 1);
            animation.playTimes = (uint)ObjectDataParser._GetNumber(rawData, ObjectDataParser.PLAY_TIMES, 1);
            animation.duration = (float)animation.frameCount / (float)this._armature.frameRate;// float
            animation.fadeInTime = ObjectDataParser._GetNumber(rawData, ObjectDataParser.FADE_IN_TIME, 0.0f);
            animation.scale = ObjectDataParser._GetNumber(rawData, ObjectDataParser.SCALE, 1.0f);
            animation.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, ObjectDataParser.DEFAULT_NAME);

            if (animation.name.Length == 0)
            {
                animation.name = ObjectDataParser.DEFAULT_NAME;
            }

            animation.frameIntOffset = (uint)this._frameIntArray.Count;
            animation.frameFloatOffset = (uint)this._frameFloatArray.Count;
            animation.frameOffset = (uint)this._frameArray.Count;

            this._animation = animation;

            if (rawData.ContainsKey(ObjectDataParser.FRAME))
            {
                var rawFrames = rawData[ObjectDataParser.FRAME] as List<object>;
                var keyFrameCount = rawFrames.Count;
                if (keyFrameCount > 0)
                {
                    for (int i = 0, frameStart = 0; i < keyFrameCount; ++i)
                    {
                        var rawFrame = rawFrames[i] as Dictionary<string, object>;
                        this._ParseActionDataInFrame(rawFrame, frameStart, null, null);
                        frameStart += ObjectDataParser._GetNumber(rawFrame, ObjectDataParser.DURATION, 1);
                    }
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.Z_ORDER))
            {
                this._animation.zOrderTimeline = this._ParseTimeline(
                    rawData[ObjectDataParser.Z_ORDER] as Dictionary<string, object>, null, ObjectDataParser.FRAME, TimelineType.ZOrder,
                    false, false, 0,
                    this._ParseZOrderFrame
                );
            }

            if (rawData.ContainsKey(ObjectDataParser.BONE))
            {
                var rawTimelines = rawData[ObjectDataParser.BONE] as List<object>;
                foreach (Dictionary<string, object> rawTimeline in rawTimelines)
                {
                    this._ParseBoneTimeline(rawTimeline);
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.SLOT))
            {
                var rawTimelines = rawData[ObjectDataParser.SLOT] as List<object>;
                foreach (Dictionary<string, object> rawTimeline in rawTimelines)
                {
                    this._ParseSlotTimeline(rawTimeline);
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.FFD))
            {
                var rawTimelines = rawData[ObjectDataParser.FFD] as List<object>;
                foreach (Dictionary<string, object> rawTimeline in rawTimelines)
                {
                    var skinName = ObjectDataParser._GetString(rawTimeline, DataParser.SKIN, DataParser.DEFAULT_NAME);
                    var slotName = ObjectDataParser._GetString(rawTimeline, DataParser.SLOT, "");
                    var displayName = ObjectDataParser._GetString(rawTimeline, DataParser.NAME, "");

                    if (skinName.Length == 0)
                    {
                        skinName = ObjectDataParser.DEFAULT_NAME;
                    }
                    
                    this._slot = this._armature.GetSlot(slotName);
                    this._mesh = this._armature.GetMesh(skinName, slotName, displayName) as MeshDisplayData;
                    if (this._slot == null || this._mesh == null)
                    {
                        continue;
                    }

                    var timeline = this._ParseTimeline(
                        rawTimeline, null, ObjectDataParser.FRAME, TimelineType.SlotDeform,
                        false, true, 0,
                        this._ParseSlotFFDFrame
                    );

                    if (timeline != null)
                    {
                        this._animation.AddSlotTimeline(this._slot, timeline);
                    }

                    this._slot = null; //
                    this._mesh = null; //
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.IK))
            {
                var rawTimelines = rawData[ObjectDataParser.IK] as List<object>;
                foreach (Dictionary<string, object> rawTimeline in rawTimelines)
                {
                    var constraintName = ObjectDataParser._GetString(rawTimeline, ObjectDataParser.NAME, "");
                    var constraint = this._armature.GetConstraint(constraintName);
                    if (constraint == null)
                    {
                        continue;
                    }

                    var timeline = this._ParseTimeline(
                        rawTimeline, null, ObjectDataParser.FRAME, TimelineType.IKConstraint,
                        true, false, 2,
                        this._ParseIKConstraintFrame
                    );

                    if (timeline != null)
                    {
                        this._animation.AddConstraintTimeline(constraint, timeline);
                    }
                }
            }

            if (this._actionFrames.Count > 0)
            {
                var timeline = this._animation.actionTimeline = BaseObject.BorrowObject<TimelineData>();
                var keyFrameCount = this._actionFrames.Count;
                timeline.type = TimelineType.Action;
                timeline.offset = (uint)this._timelineArray.Count;

                this._timelineArray.ResizeList(this._timelineArray.Count + 1 + 1 + 1 + 1 + 1 + keyFrameCount, (ushort)0);
                this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineScale] = 100;
                this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineOffset] = 0;
                this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineKeyFrameCount] = (ushort)keyFrameCount;
                this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineFrameValueCount] = 0;
                this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineFrameValueOffset] = 0;

                this._timeline = timeline;
                if (keyFrameCount == 1)
                {
                    timeline.frameIndicesOffset = -1;
                    this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineFrameOffset + 0] = (ushort)(this._ParseCacheActionFrame(this._actionFrames[0]) - this._animation.frameOffset);
                }
                else
                {
                    var totalFrameCount = this._animation.frameCount + 1; // One more frame than animation.
                    var frameIndices = this._data.frameIndices;

                    timeline.frameIndicesOffset = frameIndices.Count;
                    frameIndices.ResizeList(frameIndices.Count + (int)totalFrameCount, uint.MinValue);

                    for (
                        int i = 0, iK = 0, frameStart = 0, frameCount = 0;
                        i < totalFrameCount;
                        ++i
                        )
                    {
                        if (frameStart + frameCount <= i && iK < keyFrameCount)
                        {
                            var frame = this._actionFrames[iK];
                            frameStart = frame.frameStart;
                            if (iK == keyFrameCount - 1)
                            {
                                frameCount = (int)this._animation.frameCount - frameStart;
                            }
                            else
                            {
                                frameCount = this._actionFrames[iK + 1].frameStart - frameStart;
                            }

                            this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineFrameOffset + iK] = (ushort)(this._ParseCacheActionFrame(frame) - (int)this._animation.frameOffset);
                            iK++;
                        }

                        frameIndices[timeline.frameIndicesOffset + i] = (uint)iK - 1;
                    }
                }

                this._timeline = null; //
                this._actionFrames.Clear();
            }

            this._animation = null; //

            return animation;
        }
        protected TimelineData _ParseTimeline(
                                                Dictionary<string, object> rawData, List<object> rawFrames, string framesKey, TimelineType type,
                                                bool addIntOffset, bool addFloatOffset, uint frameValueCount,
                                                Func<Dictionary<string, object>, int, int, int> frameParser)
        {
            if (rawData != null && framesKey.Length > 0 && rawData.ContainsKey(framesKey))
            {
                rawFrames = rawData[framesKey] as List<object>;
            }

            if (rawFrames == null)
            {
                return null;
            }

            var keyFrameCount = rawFrames.Count;
            if (keyFrameCount == 0)
            {
                return null;
            }

            var frameIntArrayLength = this._frameIntArray.Count;
            var frameFloatArrayLength = this._frameFloatArray.Count;
            var timeline = BaseObject.BorrowObject<TimelineData>();
            var timelineOffset = this._timelineArray.Count;

            this._timelineArray.ResizeList(this._timelineArray.Count + 1 + 1 + 1 + 1 + 1 + keyFrameCount, (ushort)0);
            if (rawData != null)
            {
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineScale] = (ushort)Math.Round(ObjectDataParser._GetNumber(rawData, ObjectDataParser.SCALE, 1.0f) * 100);
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineOffset] = (ushort)Math.Round(ObjectDataParser._GetNumber(rawData, ObjectDataParser.OFFSET, 0.0f) * 100);
            }
            else
            {
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineScale] = 100;
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineOffset] = 0;
            }

            this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineKeyFrameCount] = (ushort)keyFrameCount;
            this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineFrameValueCount] = (ushort)frameValueCount;

            if (addIntOffset)
            {
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineFrameValueOffset] = (ushort)(frameIntArrayLength - this._animation.frameIntOffset);
            }
            else if (addFloatOffset)
            {
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineFrameValueOffset] = (ushort)(frameFloatArrayLength - (int)this._animation.frameFloatOffset);
            }
            else
            {
                this._timelineArray[timelineOffset + (int)BinaryOffset.TimelineFrameValueOffset] = 0;
            }

            this._timeline = timeline;
            this._timeline.type = type;
            this._timeline.offset = (uint)timelineOffset;

            if (keyFrameCount == 1)
            {
                // Only one frame.
                timeline.frameIndicesOffset = -1;
                int frameParserResult = frameParser(rawFrames[0] as Dictionary<string, object>, 0, 0);
                this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineFrameOffset + 0] = (ushort)(frameParserResult - this._animation.frameOffset);
            }
            else
            {
                var frameIndices = this._data.frameIndices;
                var totalFrameCount = this._animation.frameCount + 1; // One more frame than animation.

                timeline.frameIndicesOffset = frameIndices.Count;
                frameIndices.ResizeList(frameIndices.Count + (int)totalFrameCount, uint.MinValue);

                for (
                    int i = 0, iK = 0, frameStart = 0, frameCount = 0;
                    i < totalFrameCount;
                    ++i
                    )
                {
                    if (frameStart + frameCount <= i && iK < keyFrameCount)
                    {
                        var rawFrame = rawFrames[iK] as Dictionary<string, object>;
                        frameStart = i;
                        frameCount = ObjectDataParser._GetNumber(rawFrame, ObjectDataParser.DURATION, 1);
                        if (iK == keyFrameCount - 1)
                        {
                            frameCount = (int)this._animation.frameCount - frameStart;
                        }

                        int frameParserResult = frameParser(rawFrame, frameStart, frameCount);
                        this._timelineArray[(int)timeline.offset + (int)BinaryOffset.TimelineFrameOffset + iK] = (ushort)(frameParserResult - this._animation.frameOffset);
                        iK++;
                    }

                    frameIndices[timeline.frameIndicesOffset + i] = (uint)iK - 1;
                }
            }

            this._timeline = null; //

            return timeline;
        }

        protected void _ParseBoneTimeline(Dictionary<string, object> rawData)
        {
            var bone = this._armature.GetBone(ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, ""));
            if (bone == null)
            {
                return;
            }

            this._bone = bone;
            this._slot = this._armature.GetSlot(this._bone.name);

            if (rawData.ContainsKey(ObjectDataParser.TRANSLATE_FRAME))
            {
                var timeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.TRANSLATE_FRAME, TimelineType.BoneTranslate,
                    false, true, 2,
                    this._ParseBoneTranslateFrame
                );

                if (timeline != null)
                {
                    this._animation.AddBoneTimeline(bone, timeline);
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.ROTATE_FRAME))
            {
                var timeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.ROTATE_FRAME, TimelineType.BoneRotate,
                    false, true, 2,
                    this._ParseBoneRotateFrame
                );

                if (timeline != null)
                {
                    this._animation.AddBoneTimeline(bone, timeline);
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.SCALE_FRAME))
            {
                var timeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.SCALE_FRAME, TimelineType.BoneScale,
                    false, true, 2,
                    this._ParseBoneScaleFrame
                );

                if (timeline != null)
                {
                    this._animation.AddBoneTimeline(bone, timeline);
                }
            }

            if (rawData.ContainsKey(ObjectDataParser.FRAME))
            {
                var timeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.FRAME, TimelineType.BoneAll,
                    false, true, 6,
                    this._ParseBoneAllFrame
                );

                if (timeline != null)
                {
                    this._animation.AddBoneTimeline(bone, timeline);
                }
            }

            this._bone = null; //
            this._slot = null; //
        }
        protected void _ParseSlotTimeline(Dictionary<string, object> rawData)
        {
            var slot = this._armature.GetSlot(ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, ""));
            if (slot == null)
            {
                return;
            }

            this._slot = slot;
            // Display timeline.
            TimelineData displayTimeline = null;
            if (rawData.ContainsKey(ObjectDataParser.DISPLAY_FRAME))
            {
                displayTimeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.DISPLAY_FRAME, TimelineType.SlotDisplay,
                    false, false, 0,
                    this._ParseSlotDisplayFrame
                );
            }
            else
            {
                displayTimeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.FRAME, TimelineType.SlotDisplay,
                    false, false, 0,
                    this._ParseSlotDisplayFrame
                );
            }

            if (displayTimeline != null)
            {
                this._animation.AddSlotTimeline(slot, displayTimeline);
            }

            TimelineData colorTimeline = null;
            if (rawData.ContainsKey(ObjectDataParser.COLOR_FRAME))
            {
                colorTimeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.COLOR_FRAME, TimelineType.SlotColor,
                    true, false, 1,
                    this._ParseSlotColorFrame
                );
            }
            else
            {
                colorTimeline = this._ParseTimeline(
                    rawData, null, ObjectDataParser.FRAME, TimelineType.SlotColor,
                    true, false, 1,
                    this._ParseSlotColorFrame
                );
            }

            if (colorTimeline != null)
            {
                this._animation.AddSlotTimeline(slot, colorTimeline);
            }

            this._slot = null; //
        }

        protected int _ParseFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            //rawData没用到
            var frameOffset = this._frameArray.Count;
            this._frameArray.ResizeList(this._frameArray.Count + 1, (short)0);
            this._frameArray[(int)frameOffset + (int)BinaryOffset.FramePosition] = (short)frameStart;

            return frameOffset;
        }
        protected int _ParseTweenFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseFrame(rawData, frameStart, frameCount);
            if (frameCount > 0)
            {
                if (rawData.ContainsKey(ObjectDataParser.CURVE))
                {
                    var sampleCount = frameCount + 1;
                    this._helpArray.ResizeList(sampleCount, 0.0f);
                    var rawCurve = rawData[ObjectDataParser.CURVE] as List<object>;
                    var curve = new float[rawCurve.Count];
                    for (int i = 0, l = rawCurve.Count; i < l; ++i)
                    {
                        curve[i] = Convert.ToSingle(rawCurve[i]);
                    }
                    this._SamplingEasingCurve(curve, this._helpArray);

                    this._frameArray.ResizeList(this._frameArray.Count + 1 + 1 + this._helpArray.Count, (short)0);
                    this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.Curve;
                    this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenEasingOrCurveSampleCount] = (short)sampleCount;
                    for (var i = 0; i < sampleCount; ++i)
                    {
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameCurveSamples + i] = (short)Math.Round(this._helpArray[i] * 10000.0f);
                    }
                }
                else
                {
                    var noTween = -2.0f;
                    var tweenEasing = noTween;
                    if (rawData.ContainsKey(ObjectDataParser.TWEEN_EASING))
                    {
                        tweenEasing = ObjectDataParser._GetNumber(rawData, ObjectDataParser.TWEEN_EASING, noTween);
                    }

                    if (tweenEasing == noTween)
                    {
                        this._frameArray.ResizeList(this._frameArray.Count + 1, (short)0);
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.None;
                    }
                    else if (tweenEasing == 0.0f)
                    {
                        this._frameArray.ResizeList(this._frameArray.Count + 1, (short)0);
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.Line;
                    }
                    else if (tweenEasing < 0.0f)
                    {
                        this._frameArray.ResizeList(this._frameArray.Count + 1 + 1, (short)0);
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.QuadIn;
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenEasingOrCurveSampleCount] = (short)Math.Round(-tweenEasing * 100.0f);
                    }
                    else if (tweenEasing <= 1.0f)
                    {
                        this._frameArray.ResizeList(this._frameArray.Count + 1 + 1, (short)0);
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.QuadOut;
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenEasingOrCurveSampleCount] = (short)Math.Round(tweenEasing * 100.0f);
                    }
                    else
                    {
                        this._frameArray.ResizeList(this._frameArray.Count + 1 + 1, (short)0);
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.QuadInOut;
                        this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenEasingOrCurveSampleCount] = (short)Math.Round(tweenEasing * 100.0f - 100.0f);
                    }
                }
            }
            else
            {
                this._frameArray.ResizeList(this._frameArray.Count + 1);
                this._frameArray[frameOffset + (int)BinaryOffset.FrameTweenType] = (int)TweenType.None;
            }

            return frameOffset;
        }
        private int _ParseZOrderFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseFrame(rawData, frameStart, frameCount);

            if (rawData.ContainsKey(ObjectDataParser.Z_ORDER))
            {
                var rawZOrder = (rawData[ObjectDataParser.Z_ORDER] as List<object>).ConvertAll<int>(Convert.ToInt32);

                if (rawZOrder.Count > 0)
                {
                    int slotCount = this._armature.sortedSlots.Count;
                    int[] unchanged = new int[slotCount - rawZOrder.Count / 2];
                    int[] zOrders = new int[slotCount];

                    for (var i = 0; i < unchanged.Length; ++i)
                    {
                        unchanged[i] = 0;
                    }

                    for (var i = 0; i < slotCount; ++i)
                    {
                        zOrders[i] = -1;
                    }

                    var originalIndex = 0;
                    var unchangedIndex = 0;

                    for (int i = 0, l = rawZOrder.Count; i < l; i += 2)
                    {
                        var slotIndex = rawZOrder[i];
                        var zOrderOffset = rawZOrder[i + 1];

                        while (originalIndex != slotIndex)
                        {
                            unchanged[unchangedIndex++] = originalIndex++;
                        }

                        //兼容错误格式zorder索引负值
                        if (originalIndex + zOrderOffset >= 0)
                        {
                            var tempIndex = originalIndex + zOrderOffset;
                            zOrders[tempIndex] = originalIndex++;
                        }
                        else
                        {
                            originalIndex++;
                        }
                    }

                    while (originalIndex < slotCount)
                    {
                        unchanged[unchangedIndex++] = originalIndex++;
                    }

                    this._frameArray.ResizeList(this._frameArray.Count + 1 + slotCount);
                    this._frameArray[frameOffset + 1] = (short)slotCount;

                    var index = slotCount;
                    while (index-- > 0)
                    {
                        var value = 0;
                        if (zOrders[index] == -1)
                        {
                            if (unchangedIndex > 0)
                            {
                                value = unchanged[--unchangedIndex];
                            }

                            this._frameArray[frameOffset + 2 + index] = (short)(value > 0 ? value : 0);
                        }
                        else
                        {
                            value = zOrders[index];
                            this._frameArray[frameOffset + 2 + index] = (short)(value > 0 ? value : 0);
                        }
                    }

                    return frameOffset;
                }
            }

            this._frameArray.ResizeList(this._frameArray.Count + 1);
            this._frameArray[frameOffset + 1] = 0;

            return frameOffset;
        }
        protected int _ParseBoneAllFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {

            this._helpTransform.Identity();
            if (rawData.ContainsKey(ObjectDataParser.TRANSFORM))
            {
                this._ParseTransform(rawData[ObjectDataParser.TRANSFORM] as Dictionary<string, object>, this._helpTransform, 1.0f);
            }

            // Modify rotation.
            var rotation = this._helpTransform.rotation;
            if (frameStart != 0)
            {
                if (this._prevClockwise == 0)
                {
                    rotation = this._prevRotation + Transform.NormalizeRadian(rotation - this._prevRotation);
                }
                else
                {
                    if (this._prevClockwise > 0 ? rotation >= this._prevRotation : rotation <= this._prevRotation)
                    {
                        this._prevClockwise = this._prevClockwise > 0 ? this._prevClockwise - 1 : this._prevClockwise + 1;
                    }

                    rotation = this._prevRotation + rotation - this._prevRotation + Transform.PI_D * this._prevClockwise;
                }
            }

            this._prevClockwise = ObjectDataParser._GetNumber(rawData, ObjectDataParser.TWEEN_ROTATE, 0);
            this._prevRotation = rotation;

            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);
            var frameFloatOffset = this._frameFloatArray.Count;
            this._frameFloatArray.ResizeList(this._frameFloatArray.Count + 6);
            this._frameFloatArray[frameFloatOffset++] = this._helpTransform.x;
            this._frameFloatArray[frameFloatOffset++] = this._helpTransform.y;
            this._frameFloatArray[frameFloatOffset++] = rotation;
            this._frameFloatArray[frameFloatOffset++] = this._helpTransform.skew;
            this._frameFloatArray[frameFloatOffset++] = this._helpTransform.scaleX;
            this._frameFloatArray[frameFloatOffset++] = this._helpTransform.scaleY;

            this._ParseActionDataInFrame(rawData, frameStart, this._bone, this._slot);

            return frameOffset;
        }
        protected int _ParseBoneTranslateFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);

            var frameFloatOffset = this._frameFloatArray.Count;
            this._frameFloatArray.ResizeList(this._frameFloatArray.Count + 2);
            this._frameFloatArray[frameFloatOffset++] = ObjectDataParser._GetNumber(rawData, ObjectDataParser.X, 0.0f);
            this._frameFloatArray[frameFloatOffset++] = ObjectDataParser._GetNumber(rawData, ObjectDataParser.Y, 0.0f);

            return frameOffset;
        }
        protected int _ParseBoneRotateFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            // Modify rotation.
            var rotation = ObjectDataParser._GetNumber(rawData, ObjectDataParser.ROTATE, 0.0f) * Transform.DEG_RAD;
            if (frameStart != 0)
            {
                if (this._prevClockwise == 0)
                {
                    rotation = this._prevRotation + Transform.NormalizeRadian(rotation - this._prevRotation);
                }
                else
                {
                    if (this._prevClockwise > 0 ? rotation >= this._prevRotation : rotation <= this._prevRotation)
                    {
                        this._prevClockwise = this._prevClockwise > 0 ? this._prevClockwise - 1 : this._prevClockwise + 1;
                    }

                    rotation = this._prevRotation + rotation - this._prevRotation + Transform.PI_D * this._prevClockwise;
                }
            }

            this._prevClockwise = ObjectDataParser._GetNumber(rawData, ObjectDataParser.CLOCK_WISE, 0);
            this._prevRotation = rotation;

            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);
            var frameFloatOffset = this._frameFloatArray.Count;
            this._frameFloatArray.ResizeList(this._frameFloatArray.Count + 2);
            this._frameFloatArray[frameFloatOffset++] = rotation;
            this._frameFloatArray[frameFloatOffset++] = ObjectDataParser._GetNumber(rawData, ObjectDataParser.SKEW, 0.0f) * Transform.DEG_RAD;

            return frameOffset;
        }
        protected int _ParseBoneScaleFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);

            var frameFloatOffset = this._frameFloatArray.Count;
            this._frameFloatArray.ResizeList(this._frameFloatArray.Count + 2);
            this._frameFloatArray[frameFloatOffset++] = ObjectDataParser._GetNumber(rawData, ObjectDataParser.X, 1.0f);
            this._frameFloatArray[frameFloatOffset++] = ObjectDataParser._GetNumber(rawData, ObjectDataParser.Y, 1.0f);

            return frameOffset;
        }
        protected int _ParseSlotDisplayFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseFrame(rawData, frameStart, frameCount);

            this._frameArray.ResizeList(this._frameArray.Count + 1);

            if (rawData.ContainsKey(ObjectDataParser.VALUE))
            {
                this._frameArray[frameOffset + 1] = (short)ObjectDataParser._GetNumber(rawData, ObjectDataParser.VALUE, 0);
            }
            else
            {
                this._frameArray[frameOffset + 1] = (short)ObjectDataParser._GetNumber(rawData, ObjectDataParser.DISPLAY_INDEX, 0);
            }

            this._ParseActionDataInFrame(rawData, frameStart, this._slot.parent, this._slot);

            return frameOffset;
        }
        protected int _ParseSlotColorFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);
            var colorOffset = -1;
            if (rawData.ContainsKey(ObjectDataParser.VALUE) || rawData.ContainsKey(ObjectDataParser.COLOR))
            {
                var rawColor = rawData.ContainsKey(ObjectDataParser.VALUE) ? rawData[ObjectDataParser.VALUE] as Dictionary<string, object> : rawData[ObjectDataParser.COLOR] as Dictionary<string, object>;

                foreach (var k in rawColor)
                {
                    // Detects the presence of color.
                    this._ParseColorTransform(rawColor, this._helpColorTransform);
                    colorOffset = this._intArray.Count;
                    this._intArray.ResizeList(this._intArray.Count + 8);
                    this._intArray[colorOffset++] = (short)Math.Round(this._helpColorTransform.alphaMultiplier * 100);
                    this._intArray[colorOffset++] = (short)Math.Round(this._helpColorTransform.redMultiplier * 100);
                    this._intArray[colorOffset++] = (short)Math.Round(this._helpColorTransform.greenMultiplier * 100);
                    this._intArray[colorOffset++] = (short)Math.Round(this._helpColorTransform.blueMultiplier * 100);
                    this._intArray[colorOffset++] = (short)Math.Round((float)this._helpColorTransform.alphaOffset);
                    this._intArray[colorOffset++] = (short)Math.Round((float)this._helpColorTransform.redOffset);
                    this._intArray[colorOffset++] = (short)Math.Round((float)this._helpColorTransform.greenOffset);
                    this._intArray[colorOffset++] = (short)Math.Round((float)this._helpColorTransform.blueOffset);
                    colorOffset -= 8;
                    break;
                }
            }

            if (colorOffset < 0)
            {
                if (this._defaultColorOffset < 0)
                {
                    this._defaultColorOffset = colorOffset = this._intArray.Count;
                    this._intArray.ResizeList(this._intArray.Count + 8);
                    this._intArray[colorOffset++] = 100;
                    this._intArray[colorOffset++] = 100;
                    this._intArray[colorOffset++] = 100;
                    this._intArray[colorOffset++] = 100;
                    this._intArray[colorOffset++] = 0;
                    this._intArray[colorOffset++] = 0;
                    this._intArray[colorOffset++] = 0;
                    this._intArray[colorOffset++] = 0;
                }

                colorOffset = this._defaultColorOffset;
            }

            var frameIntOffset = this._frameIntArray.Count;
            this._frameIntArray.ResizeList(this._frameIntArray.Count + 1);
            this._frameIntArray[frameIntOffset] = (short)colorOffset;

            return frameOffset;
        }

        protected int _ParseSlotFFDFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameFloatOffset = this._frameFloatArray.Count;
            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);
            var rawVertices = rawData.ContainsKey(ObjectDataParser.VERTICES) ? (rawData[ObjectDataParser.VERTICES] as List<object>).ConvertAll<float>(Convert.ToSingle) : null;
            var offset = ObjectDataParser._GetNumber(rawData, ObjectDataParser.OFFSET, 0); // uint
            var vertexCount = this._intArray[this._mesh.vertices.offset + (int)BinaryOffset.MeshVertexCount];
            var meshName = this._mesh.parent.name + "_" + this._slot.name + "_" + this._mesh.name;
            var weight = this._mesh.vertices.weight;

            var x = 0.0f;
            var y = 0.0f;
            var iB = 0;
            var iV = 0;

            if (weight != null)
            {
                var rawSlotPose = this._weightSlotPose[meshName];
                this._helpMatrixA.CopyFromArray(rawSlotPose, 0);
                this._frameFloatArray.ResizeList(this._frameFloatArray.Count + (weight.count * 2));
                iB = weight.offset + (int)BinaryOffset.WeigthBoneIndices + weight.bones.Count;
            }
            else
            {
                this._frameFloatArray.ResizeList(this._frameFloatArray.Count + (vertexCount * 2));
            }

            for (var i = 0; i < vertexCount * 2; i += 2)
            {
                if (rawVertices == null)
                { // Fill 0.
                    x = 0.0f;
                    y = 0.0f;
                }
                else
                {
                    if (i < offset || i - offset >= rawVertices.Count)
                    {
                        x = 0.0f;
                    }
                    else
                    {
                        x = rawVertices[i - offset];
                    }

                    if (i + 1 < offset || i + 1 - offset >= rawVertices.Count)
                    {
                        y = 0.0f;
                    }
                    else
                    {
                        y = rawVertices[i + 1 - offset];
                    }
                }

                if (weight != null)
                {
                    // If mesh is skinned, transform point by bone bind pose.
                    var rawBonePoses = this._weightBonePoses[meshName];
                    var vertexBoneCount = this._intArray[iB++];

                    this._helpMatrixA.TransformPoint(x, y, this._helpPoint, true);
                    x = this._helpPoint.x;
                    y = this._helpPoint.y;

                    for (var j = 0; j < vertexBoneCount; ++j)
                    {
                        var boneIndex = this._intArray[iB++];
                        this._helpMatrixB.CopyFromArray(rawBonePoses, boneIndex * 7 + 1);
                        this._helpMatrixB.Invert();
                        this._helpMatrixB.TransformPoint(x, y, this._helpPoint, true);

                        this._frameFloatArray[frameFloatOffset + iV++] = this._helpPoint.x;
                        this._frameFloatArray[frameFloatOffset + iV++] = this._helpPoint.y;
                    }
                }
                else
                {
                    this._frameFloatArray[frameFloatOffset + i] = x;
                    this._frameFloatArray[frameFloatOffset + i + 1] = y;
                }
            }

            if (frameStart == 0)
            {
                var frameIntOffset = this._frameIntArray.Count;
                this._frameIntArray.ResizeList(this._frameIntArray.Count + 1 + 1 + 1 + 1 + 1);
                this._frameIntArray[frameIntOffset + (int)BinaryOffset.DeformVertexOffset] = (short)this._mesh.vertices.offset;
                this._frameIntArray[frameIntOffset + (int)BinaryOffset.DeformCount] = (short)(this._frameFloatArray.Count - frameFloatOffset);
                this._frameIntArray[frameIntOffset + (int)BinaryOffset.DeformValueCount] = (short)(this._frameFloatArray.Count - frameFloatOffset);
                this._frameIntArray[frameIntOffset + (int)BinaryOffset.DeformValueOffset] = 0;
                this._frameIntArray[frameIntOffset + (int)BinaryOffset.DeformFloatOffset] = (short)(frameFloatOffset - this._animation.frameFloatOffset);// fixed ffd timeline mesh bound
                this._timelineArray[(int)this._timeline.offset + (int)BinaryOffset.TimelineFrameValueCount] = (ushort)(frameIntOffset - this._animation.frameIntOffset);
            }

            return frameOffset;
        }

        protected int _ParseIKConstraintFrame(Dictionary<string, object> rawData, int frameStart, int frameCount)
        {
            var frameOffset = this._ParseTweenFrame(rawData, frameStart, frameCount);

            var frameIntOffset = this._frameIntArray.Count;
            this._frameIntArray.ResizeList(this._frameIntArray.Count + 2);
            this._frameIntArray[frameIntOffset++] = (short)(ObjectDataParser._GetBoolean(rawData, ObjectDataParser.BEND_POSITIVE, true) ? 1 : 0);
            this._frameIntArray[frameIntOffset++] = (short)Math.Round(ObjectDataParser._GetNumber(rawData, ObjectDataParser.WEIGHT, 1.0f) * 100.0);

            return frameOffset;
        }

        protected List<ActionData> _ParseActionData(object rawData, ActionType type, BoneData bone = null, SlotData slot = null)
        {
            var actions = new List<ActionData>();
            if (rawData is string)
            {
                var action = BaseObject.BorrowObject<ActionData>();
                action.type = type;
                action.name = (string)rawData;
                action.bone = bone;
                action.slot = slot;
                actions.Add(action);
            }
            else if (rawData is IList)
            {
                var actionsObject = rawData as List<object>;
                foreach (Dictionary<string, object> rawAction in actionsObject)
                {
                    var action = BaseObject.BorrowObject<ActionData>();
                    if (rawAction.ContainsKey(ObjectDataParser.GOTO_AND_PLAY))
                    {
                        action.type = ActionType.Play;
                        action.name = ObjectDataParser._GetString(rawAction, ObjectDataParser.GOTO_AND_PLAY, "");
                    }
                    else
                    {
                        if (rawAction.ContainsKey(ObjectDataParser.TYPE) && rawAction[ObjectDataParser.TYPE] is string)
                        {
                            action.type = (ActionType)ObjectDataParser._GetActionType((string)rawAction[ObjectDataParser.TYPE]);
                        }
                        else
                        {
                            action.type = (ActionType)ObjectDataParser._GetNumber(rawAction, ObjectDataParser.TYPE, (uint)type);
                        }

                        action.name = ObjectDataParser._GetString(rawAction, ObjectDataParser.NAME, "");
                    }

                    if (rawAction.ContainsKey(ObjectDataParser.BONE))
                    {
                        var boneName = ObjectDataParser._GetString(rawAction, ObjectDataParser.BONE, "");
                        action.bone = this._armature.GetBone(boneName);
                    }
                    else
                    {
                        action.bone = bone;
                    }

                    if (rawAction.ContainsKey(ObjectDataParser.SLOT))
                    {
                        var slotName = ObjectDataParser._GetString(rawAction, ObjectDataParser.SLOT, "");
                        action.slot = this._armature.GetSlot(slotName);
                    }
                    else
                    {
                        action.slot = slot;
                    }

                    UserData userData = null;

                    if (rawAction.ContainsKey(ObjectDataParser.INTS))
                    {
                        if (userData == null)
                        {
                            userData = BaseObject.BorrowObject<UserData>();
                        }

                        var rawInts = (rawAction[ObjectDataParser.INTS] as List<object>).ConvertAll<int>(Convert.ToInt32);
                        foreach (var rawValue in rawInts)
                        {
                            userData.AddInt(rawValue);
                        }
                    }

                    if (rawAction.ContainsKey(ObjectDataParser.FLOATS))
                    {
                        if (userData == null)
                        {
                            userData = BaseObject.BorrowObject<UserData>();
                        }

                        var rawFloats = (rawAction[ObjectDataParser.FLOATS] as List<object>).ConvertAll<float>(Convert.ToSingle);
                        foreach (var rawValue in rawFloats)
                        {
                            userData.AddFloat(rawValue);
                        }
                    }

                    if (rawAction.ContainsKey(ObjectDataParser.STRINGS))
                    {
                        if (userData == null)
                        {
                            userData = BaseObject.BorrowObject<UserData>();
                        }

                        var rawStrings = (rawAction[ObjectDataParser.STRINGS] as List<object>).ConvertAll<string>(Convert.ToString);
                        foreach (var rawValue in rawStrings)
                        {
                            userData.AddString(rawValue);
                        }
                    }

                    action.data = userData;
                    actions.Add(action);
                }
            }

            return actions;
        }

        protected void _ParseTransform(Dictionary<string, object> rawData, Transform transform, float scale)
        {
            transform.x = ObjectDataParser._GetNumber(rawData, ObjectDataParser.X, 0.0f) * scale;
            transform.y = ObjectDataParser._GetNumber(rawData, ObjectDataParser.Y, 0.0f) * scale;

            if (rawData.ContainsKey(ObjectDataParser.ROTATE) || rawData.ContainsKey(ObjectDataParser.SKEW))
            {
                transform.rotation = Transform.NormalizeRadian(ObjectDataParser._GetNumber(rawData, ObjectDataParser.ROTATE, 0.0f) * Transform.DEG_RAD);
                transform.skew = Transform.NormalizeRadian(ObjectDataParser._GetNumber(rawData, ObjectDataParser.SKEW, 0.0f) * Transform.DEG_RAD);
            }
            else if (rawData.ContainsKey(ObjectDataParser.SKEW_X) || rawData.ContainsKey(ObjectDataParser.SKEW_Y))
            {
                transform.rotation = Transform.NormalizeRadian(ObjectDataParser._GetNumber(rawData, ObjectDataParser.SKEW_Y, 0.0f) * Transform.DEG_RAD);
                transform.skew = Transform.NormalizeRadian(ObjectDataParser._GetNumber(rawData, ObjectDataParser.SKEW_X, 0.0f) * Transform.DEG_RAD) - transform.rotation;
            }

            transform.scaleX = ObjectDataParser._GetNumber(rawData, ObjectDataParser.SCALE_X, 1.0f);
            transform.scaleY = ObjectDataParser._GetNumber(rawData, ObjectDataParser.SCALE_Y, 1.0f);
        }

        protected void _ParseColorTransform(Dictionary<string, object> rawData, ColorTransform color)
        {
            color.alphaMultiplier = ObjectDataParser._GetNumber(rawData, ObjectDataParser.ALPHA_MULTIPLIER, 100) * 0.01f;
            color.redMultiplier = ObjectDataParser._GetNumber(rawData, ObjectDataParser.RED_MULTIPLIER, 100) * 0.01f;
            color.greenMultiplier = ObjectDataParser._GetNumber(rawData, ObjectDataParser.GREEN_MULTIPLIER, 100) * 0.01f;
            color.blueMultiplier = ObjectDataParser._GetNumber(rawData, ObjectDataParser.BLUE_MULTIPLIER, 100) * 0.01f;
            color.alphaOffset = ObjectDataParser._GetNumber(rawData, ObjectDataParser.ALPHA_OFFSET, 0);
            color.redOffset = ObjectDataParser._GetNumber(rawData, ObjectDataParser.RED_OFFSET, 0);
            color.greenOffset = ObjectDataParser._GetNumber(rawData, ObjectDataParser.GREEN_OFFSET, 0);
            color.blueOffset = ObjectDataParser._GetNumber(rawData, ObjectDataParser.BLUE_OFFSET, 0);
        }

        protected virtual void _ParseArray(Dictionary<string, object> rawData)
        {
            this._intArray.Clear();
            this._floatArray.Clear();
            this._frameIntArray.Clear();
            this._frameFloatArray.Clear();
            this._frameArray.Clear();
            this._timelineArray.Clear();
        }

        protected void _ModifyArray()
        {
            // Align.
            if ((this._intArray.Count % Helper.INT16_SIZE) != 0)
            {
                this._intArray.Add(0);
            }

            if ((this._frameIntArray.Count % Helper.INT16_SIZE) != 0)
            {
                this._frameIntArray.Add(0);
            }

            if ((this._frameArray.Count % Helper.INT16_SIZE) != 0)
            {
                this._frameArray.Add(0);
            }

            if ((this._timelineArray.Count % Helper.UINT16_SIZE) != 0)
            {
                this._timelineArray.Add(0);
            }

            var l1 = this._intArray.Count * Helper.INT16_SIZE;
            var l2 = this._floatArray.Count * Helper.FLOAT_SIZE;
            var l3 = this._frameIntArray.Count * Helper.INT16_SIZE;
            var l4 = this._frameFloatArray.Count * Helper.FLOAT_SIZE;
            var l5 = this._frameArray.Count * Helper.INT16_SIZE;
            var l6 = this._timelineArray.Count * Helper.UINT16_SIZE;
            var lTotal = l1 + l2 + l3 + l4 + l5 + l6;

            using (MemoryStream ms = new MemoryStream(lTotal))
            using (BinaryDataWriter writer = new BinaryDataWriter(ms))
            using (BinaryDataReader reader = new BinaryDataReader(ms))
            {

                //ToWrite
                writer.Write(_intArray.ToArray());
                writer.Write(_floatArray.ToArray());
                writer.Write(_frameIntArray.ToArray());
                writer.Write(_frameFloatArray.ToArray());
                writer.Write(_frameArray.ToArray());
                writer.Write(_timelineArray.ToArray());

                ms.Position = 0;

                //ToRead
                this._data.binary = ms.GetBuffer();
                this._data.intArray = reader.ReadInt16s(0, this._intArray.Count);
                this._data.floatArray = reader.ReadSingles(0, this._floatArray.Count);
                this._data.frameIntArray = reader.ReadInt16s(0, this._frameIntArray.Count);
                this._data.frameFloatArray = reader.ReadSingles(0, this._frameFloatArray.Count);
                this._data.frameArray = reader.ReadInt16s(0, this._frameArray.Count);
                this._data.timelineArray = reader.ReadUInt16s(0, this._timelineArray.Count);

                ms.Close();
            }

            this._defaultColorOffset = -1;
        }
        public override DragonBonesData ParseDragonBonesData(object rawObj, float scale = 1.0f)
        {
            var rawData = rawObj as Dictionary<string, object>;
            Helper.Assert(rawData != null, "Data error.");

            var version = ObjectDataParser._GetString(rawData, ObjectDataParser.VERSION, "");
            var compatibleVersion = ObjectDataParser._GetString(rawData, ObjectDataParser.COMPATIBLE_VERSION, "");

            if (ObjectDataParser.DATA_VERSIONS.IndexOf(version) >= 0 ||
                ObjectDataParser.DATA_VERSIONS.IndexOf(compatibleVersion) >= 0)
            {
                var data = BaseObject.BorrowObject<DragonBonesData>();
                data.version = version;
                data.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");
                data.frameRate = ObjectDataParser._GetNumber(rawData, ObjectDataParser.FRAME_RATE, (uint)24);

                if (data.frameRate == 0)
                {
                    // Data error.
                    data.frameRate = 24;
                }

                if (rawData.ContainsKey(ObjectDataParser.ARMATURE))
                {
                    this._data = data;

                    this._ParseArray(rawData);

                    var rawArmatures = rawData[ObjectDataParser.ARMATURE] as List<object>;
                    foreach (Dictionary<string, object> rawArmature in rawArmatures)
                    {
                        data.AddArmature(this._ParseArmature(rawArmature, scale));
                    }

                    if (this._data.binary == null)
                    {
                        this._ModifyArray();
                    }

                    if (rawData.ContainsKey(ObjectDataParser.STAGE))
                    {
                        data.stage = data.GetArmature(ObjectDataParser._GetString(rawData, ObjectDataParser.STAGE, ""));
                    }
                    else if (data.armatureNames.Count > 0)
                    {
                        data.stage = data.GetArmature(data.armatureNames[0]);
                    }

                    this._data = null;
                }

                if (rawData.ContainsKey(ObjectDataParser.TEXTURE_ATLAS))
                {
                    this._rawTextureAtlases = rawData[ObjectDataParser.TEXTURE_ATLAS] as List<object>;
                }

                return data;
            }
            else
            {
                Helper.Assert(
                    false,
                    "Nonsupport data version: " + version + "\n" +
                    "Please convert DragonBones data to support version.\n" +
                    "Read more: https://github.com/DragonBones/Tools/"
                );
            }

            return null;
        }

        public override bool ParseTextureAtlasData(object rawObj, TextureAtlasData textureAtlasData, float scale = 1.0f)
        {
            var rawData = rawObj as Dictionary<string, object>;
            if (rawData == null)
            {
                if (this._rawTextureAtlases == null || this._rawTextureAtlases.Count == 0)
                {
                    return false;
                }

                var rawTextureAtlas = this._rawTextureAtlases[this._rawTextureAtlasIndex++];
                this.ParseTextureAtlasData(rawTextureAtlas, textureAtlasData, scale);
                if (this._rawTextureAtlasIndex >= this._rawTextureAtlases.Count)
                {
                    this._rawTextureAtlasIndex = 0;
                    this._rawTextureAtlases = null;
                }

                return true;
            }

            // Texture format.
            textureAtlasData.width = ObjectDataParser._GetNumber(rawData, ObjectDataParser.WIDTH, uint.MinValue);
            textureAtlasData.height = ObjectDataParser._GetNumber(rawData, ObjectDataParser.HEIGHT, uint.MinValue);
            textureAtlasData.scale = scale == 1.0f ? (1.0f / ObjectDataParser._GetNumber(rawData, ObjectDataParser.SCALE, 1.0f)) : scale;
            textureAtlasData.name = ObjectDataParser._GetString(rawData, ObjectDataParser.NAME, "");
            textureAtlasData.imagePath = ObjectDataParser._GetString(rawData, ObjectDataParser.IMAGE_PATH, "");

            if (rawData.ContainsKey(ObjectDataParser.SUB_TEXTURE))
            {
                var rawTextures = rawData[ObjectDataParser.SUB_TEXTURE] as List<object>;

                for (int i = 0, l = rawTextures.Count; i < l; ++i)
                {
                    var rawTexture = rawTextures[i] as Dictionary<string, object>;
                    var textureData = textureAtlasData.CreateTexture();
                    textureData.rotated = ObjectDataParser._GetBoolean(rawTexture, ObjectDataParser.ROTATED, false);
                    textureData.name = ObjectDataParser._GetString(rawTexture, ObjectDataParser.NAME, "");
                    textureData.region.x = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.X, 0.0f);
                    textureData.region.y = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.Y, 0.0f);
                    textureData.region.width = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.WIDTH, 0.0f);
                    textureData.region.height = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.HEIGHT, 0.0f);

                    var frameWidth = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.FRAME_WIDTH, -1.0f);
                    var frameHeight = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.FRAME_HEIGHT, -1.0f);
                    if (frameWidth > 0.0 && frameHeight > 0.0)
                    {
                        textureData.frame = TextureData.CreateRectangle();
                        textureData.frame.x = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.FRAME_X, 0.0f);
                        textureData.frame.y = ObjectDataParser._GetNumber(rawTexture, ObjectDataParser.FRAME_Y, 0.0f);
                        textureData.frame.width = frameWidth;
                        textureData.frame.height = frameHeight;
                    }

                    textureAtlasData.AddTexture(textureData);
                }
            }

            return true;
        }
    }

    /// <internal/>
    /// <private/>
    class ActionFrame
    {
        public int frameStart = 0;
        public readonly List<int> actions = new List<int>();
    }
}
