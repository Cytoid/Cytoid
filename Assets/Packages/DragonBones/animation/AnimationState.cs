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
    /// - The animation state is generated when the animation data is played.
    /// </summary>
    /// <see cref="DragonBones.Animation"/>
    /// <see cref="DragonBones.AnimationData"/>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 动画状态由播放动画数据时产生。
    /// </summary>
    /// <see cref="DragonBones.Animation"/>
    /// <see cref="DragonBones.AnimationData"/>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class AnimationState : BaseObject
    {
        /// <private/>
        public bool actionEnabled;
        /// <private/>
        public bool additiveBlending;
        /// <summary>
        /// - Whether the animation state has control over the display object properties of the slots.
        /// Sometimes blend a animation state does not want it to control the display object properties of the slots,
        /// especially if other animation state are controlling the display object properties of the slots.
        /// </summary>
        /// <default>true</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画状态是否对插槽的显示对象属性有控制权。
        /// 有时混合一个动画状态并不希望其控制插槽的显示对象属性，
        /// 尤其是其他动画状态正在控制这些插槽的显示对象属性时。
        /// </summary>
        /// <default>true</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public bool displayControl;
        /// <summary>
        /// - Whether to reset the objects without animation to the armature pose when the animation state is start to play.
        /// This property should usually be set to false when blend multiple animation states.
        /// </summary>
        /// <default>true</default>
        /// <version>DragonBones 5.1</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 开始播放动画状态时是否将没有动画的对象重置为骨架初始值。
        /// 通常在混合多个动画状态时应该将该属性设置为 false。
        /// </summary>
        /// <default>true</default>
        /// <version>DragonBones 5.1</version>
        /// <language>zh_CN</language>
        public bool resetToPose;
        /// <summary>
        /// - The play times. [0: Loop play, [1~N]: Play N times]
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 播放次数。 [0: 无限循环播放, [1~N]: 循环播放 N 次]
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public int playTimes;
        /// <summary>
        /// - The blend layer.
        /// High layer animation state will get the blend weight first.
        /// When the blend weight is assigned more than 1, the remaining animation states will no longer get the weight assigned.
        /// </summary>
        /// <readonly/>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 混合图层。
        /// 图层高的动画状态会优先获取混合权重。
        /// 当混合权重分配超过 1 时，剩余的动画状态将不再获得权重分配。
        /// </summary>
        /// <readonly/>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public int layer;
        /// <summary>
        /// - The play speed.
        /// The value is an overlay relationship with {@link dragonBones.Animation#timeScale}.
        /// [(-N~0): Reverse play, 0: Stop play, (0~1): Slow play, 1: Normal play, (1~N): Fast play]
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 播放速度。
        /// 该值与 {@link dragonBones.Animation#timeScale} 是叠加关系。
        /// [(-N~0): 倒转播放, 0: 停止播放, (0~1): 慢速播放, 1: 正常播放, (1~N): 快速播放]
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float timeScale;
        /// <summary>
        /// - The blend weight.
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 混合权重。
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public float weight;
        /// <summary>
        /// - The auto fade out time when the animation state play completed.
        /// [-1: Do not fade out automatically, [0~N]: The fade out time] (In seconds)
        /// </summary>
        /// <default>-1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画状态播放完成后的自动淡出时间。
        /// [-1: 不自动淡出, [0~N]: 淡出时间] （以秒为单位）
        /// </summary>
        /// <default>-1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public float autoFadeOutTime;
        /// <private/>
        public float fadeTotalTime;
        /// <summary>
        /// - The name of the animation state. (Can be different from the name of the animation data)
        /// </summary>
        /// <readonly/>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画状态名称。 （可以不同于动画数据）
        /// </summary>
        /// <readonly/>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <summary>
        /// - The blend group name of the animation state.
        /// This property is typically used to specify the substitution of multiple animation states blend.
        /// </summary>
        /// <readonly/>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 混合组名称。
        /// 该属性通常用来指定多个动画状态混合时的相互替换关系。
        /// </summary>
        /// <readonly/>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public string group;

        private int _timelineDirty;
        /// <summary>
        /// - xx: Play Enabled, Fade Play Enabled
        /// </summary>
        /// <internal/>
        /// <private/>
        internal int _playheadState;
        /// <summary>
        /// -1: Fade in, 0: Fade complete, 1: Fade out;
        /// </summary>
        /// <internal/>
        /// <private/>
        internal int _fadeState;
        /// <summary>
        /// -1: Fade start, 0: Fading, 1: Fade complete;
        /// </summary>
        /// <internal/>
        /// <private/>
        internal int _subFadeState;
        /// <internal/>
        /// <private/>
        internal float _position;
        /// <internal/>
        /// <private/>
        internal float _duration;
        private float _fadeTime;
        private float _time;
        /// <internal/>
        /// <private/>
        internal float _fadeProgress;
        /// <internal/>
        /// <private/>
        private float _weightResult;
        /// <internal/>
        /// <private/>
        internal readonly BlendState _blendState = new BlendState();
        private readonly List<string> _boneMask = new List<string>();
        private readonly List<BoneTimelineState> _boneTimelines = new List<BoneTimelineState>();
        private readonly List<SlotTimelineState> _slotTimelines = new List<SlotTimelineState>();
        private readonly List<ConstraintTimelineState> _constraintTimelines = new List<ConstraintTimelineState>();
        private readonly List<TimelineState> _poseTimelines = new List<TimelineState>();
        private readonly Dictionary<string, BonePose> _bonePoses = new Dictionary<string, BonePose>();
        /// <internal/>
        /// <private/>
        public AnimationData _animationData;
        private Armature _armature;
        /// <internal/>
        /// <private/>
        internal ActionTimelineState _actionTimeline = null; // Initial value.
        private ZOrderTimelineState _zOrderTimeline = null; // Initial value.
        /// <internal/>
        /// <private/>
        public AnimationState _parent = null; // Initial value.
        /// <private/>
        protected override void _OnClear()
        {
            foreach (var timeline in this._boneTimelines)
            {
                timeline.ReturnToPool();
            }

            foreach (var timeline in this._slotTimelines)
            {
                timeline.ReturnToPool();
            }

            foreach (var timeline in this._constraintTimelines)
            {
                timeline.ReturnToPool();
            }

            foreach (var bonePose in this._bonePoses.Values)
            {
                bonePose.ReturnToPool();
            }

            if (this._actionTimeline != null)
            {
                this._actionTimeline.ReturnToPool();
            }

            if (this._zOrderTimeline != null)
            {
                this._zOrderTimeline.ReturnToPool();
            }

            this.actionEnabled = false;
            this.additiveBlending = false;
            this.displayControl = false;
            this.resetToPose = false;
            this.playTimes = 1;
            this.layer = 0;

            this.timeScale = 1.0f;
            this.weight = 1.0f;
            this.autoFadeOutTime = 0.0f;
            this.fadeTotalTime = 0.0f;
            this.name = string.Empty;
            this.group = string.Empty;

            this._timelineDirty = 2;
            this._playheadState = 0;
            this._fadeState = -1;
            this._subFadeState = -1;
            this._position = 0.0f;
            this._duration = 0.0f;
            this._fadeTime = 0.0f;
            this._time = 0.0f;
            this._fadeProgress = 0.0f;
            this._weightResult = 0.0f;
            this._blendState.Clear();
            this._boneMask.Clear();
            this._boneTimelines.Clear();
            this._slotTimelines.Clear();
            this._constraintTimelines.Clear();
            this._bonePoses.Clear();
            this._animationData = null; //
            this._armature = null; //
            this._actionTimeline = null; //
            this._zOrderTimeline = null;
            this._parent = null;
        }

        private void _UpdateTimelines()
        {
            { // Update constraint timelines.
                foreach (var constraint in this._armature._constraints)
                {
                    var timelineDatas = this._animationData.GetConstraintTimelines(constraint.name);

                    if (timelineDatas != null)
                    {
                        foreach (var timelineData in timelineDatas)
                        {
                            switch (timelineData.type)
                            {
                                case TimelineType.IKConstraint:
                                    {
                                        var timeline = BaseObject.BorrowObject<IKConstraintTimelineState>();
                                        timeline.constraint = constraint;
                                        timeline.Init(this._armature, this, timelineData);
                                        this._constraintTimelines.Add(timeline);
                                        break;
                                    }

                                default:
                                    break;
                            }
                        }
                    }
                    else if (this.resetToPose)
                    { // Pose timeline.
                        var timeline = BaseObject.BorrowObject<IKConstraintTimelineState>();
                        timeline.constraint = constraint;
                        timeline.Init(this._armature, this, null);
                        this._constraintTimelines.Add(timeline);
                        this._poseTimelines.Add(timeline);
                    }
                }
            }
        }

        private void _UpdateBoneAndSlotTimelines()
        {
            { // Update bone timelines.
                Dictionary<string, List<BoneTimelineState>> boneTimelines = new Dictionary<string, List<BoneTimelineState>>();

                foreach (var timeline in this._boneTimelines)
                {
                    // Create bone timelines map.
                    var timelineName = timeline.bone.name;
                    if (!(boneTimelines.ContainsKey(timelineName)))
                    {
                        boneTimelines[timelineName] = new List<BoneTimelineState>();
                    }

                    boneTimelines[timelineName].Add(timeline);
                }

                foreach (var bone in this._armature.GetBones())
                {
                    var timelineName = bone.name;
                    if (!this.ContainsBoneMask(timelineName))
                    {
                        continue;
                    }

                    var timelineDatas = this._animationData.GetBoneTimelines(timelineName);
                    if (boneTimelines.ContainsKey(timelineName))
                    {
                        // Remove bone timeline from map.
                        boneTimelines.Remove(timelineName);
                    }
                    else
                    {
                        // Create new bone timeline.
                        var bonePose = this._bonePoses.ContainsKey(timelineName) ? this._bonePoses[timelineName] : (this._bonePoses[timelineName] = BaseObject.BorrowObject<BonePose>());
                        if (timelineDatas != null)
                        {
                            foreach (var timelineData in timelineDatas)
                            {
                                switch (timelineData.type)
                                {
                                    case TimelineType.BoneAll:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneAllTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }
                                    case TimelineType.BoneTranslate:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneTranslateTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }
                                    case TimelineType.BoneRotate:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneRotateTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }
                                    case TimelineType.BoneScale:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneScaleTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }

                                    default:
                                        break;
                                }
                            }
                        }
                        else if (this.resetToPose)
                        { // Pose timeline.
                            var timeline = BaseObject.BorrowObject<BoneAllTimelineState>();
                            timeline.bone = bone;
                            timeline.bonePose = bonePose;
                            timeline.Init(this._armature, this, null);
                            this._boneTimelines.Add(timeline);
                            this._poseTimelines.Add(timeline);
                        }
                    }
                }

                foreach (var timelines in boneTimelines.Values)
                {
                    // Remove bone timelines.
                    foreach (var timeline in timelines)
                    {
                        this._boneTimelines.Remove(timeline);
                        timeline.ReturnToPool();
                    }
                }
            }

            { // Update slot timelines.
                Dictionary<string, List<SlotTimelineState>> slotTimelines = new Dictionary<string, List<SlotTimelineState>>();
                List<int> ffdFlags = new List<int>();

                foreach (var timeline in this._slotTimelines)
                {
                    // Create slot timelines map.
                    var timelineName = timeline.slot.name;
                    if (!(slotTimelines.ContainsKey(timelineName)))
                    {
                        slotTimelines[timelineName] = new List<SlotTimelineState>();
                    }

                    slotTimelines[timelineName].Add(timeline);
                }

                foreach (var slot in this._armature.GetSlots())
                {
                    var boneName = slot.parent.name;
                    if (!this.ContainsBoneMask(boneName))
                    {
                        continue;
                    }

                    var timelineName = slot.name;
                    var timelineDatas = this._animationData.GetSlotTimelines(timelineName);

                    if (slotTimelines.ContainsKey(timelineName))
                    {
                        // Remove slot timeline from map.
                        slotTimelines.Remove(timelineName);
                    }
                    else
                    {
                        // Create new slot timeline.
                        var displayIndexFlag = false;
                        var colorFlag = false;
                        ffdFlags.Clear();

                        if (timelineDatas != null)
                        {
                            foreach (var timelineData in timelineDatas)
                            {
                                switch (timelineData.type)
                                {
                                    case TimelineType.SlotDisplay:
                                        {
                                            var timeline = BaseObject.BorrowObject<SlotDislayTimelineState>();
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._slotTimelines.Add(timeline);
                                            displayIndexFlag = true;
                                            break;
                                        }
                                    case TimelineType.SlotColor:
                                        {
                                            var timeline = BaseObject.BorrowObject<SlotColorTimelineState>();
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._slotTimelines.Add(timeline);
                                            colorFlag = true;
                                            break;
                                        }
                                    case TimelineType.SlotDeform:
                                        {
                                            var timeline = BaseObject.BorrowObject<DeformTimelineState>();
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._slotTimelines.Add(timeline);
                                            ffdFlags.Add((int)timeline.vertexOffset);
                                            break;
                                        }

                                    default:
                                        break;
                                }
                            }
                        }

                        if (this.resetToPose)
                        {
                            // Pose timeline.
                            if (!displayIndexFlag)
                            {
                                var timeline = BaseObject.BorrowObject<SlotDislayTimelineState>();
                                timeline.slot = slot;
                                timeline.Init(this._armature, this, null);
                                this._slotTimelines.Add(timeline);
                                this._poseTimelines.Add(timeline);
                            }

                            if (!colorFlag)
                            {
                                var timeline = BaseObject.BorrowObject<SlotColorTimelineState>();
                                timeline.slot = slot;
                                timeline.Init(this._armature, this, null);
                                this._slotTimelines.Add(timeline);
                                this._poseTimelines.Add(timeline);
                            }

                            if (slot.rawDisplayDatas != null)
                            {
                                foreach (var displayData in slot.rawDisplayDatas)
                                {
                                    if (displayData != null && displayData.type == DisplayType.Mesh)
                                    {
                                        var meshOffset = (displayData as MeshDisplayData).vertices.offset;
                                        if (!ffdFlags.Contains(meshOffset))
                                        {
                                            var timeline = BaseObject.BorrowObject<DeformTimelineState>();
                                            timeline.vertexOffset = meshOffset; //
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, null);
                                            this._slotTimelines.Add(timeline);
                                            this._poseTimelines.Add(timeline);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var timelines in slotTimelines.Values)
                {
                    // Remove slot timelines.
                    foreach (var timeline in timelines)
                    {
                        this._slotTimelines.Remove(timeline);
                        timeline.ReturnToPool();
                    }
                }
            }

            // {
            //     // Update constraint timelines.
            //     Dictionary<string, List<ConstraintTimelineState>> constraintTimelines = new Dictionary<string, List<ConstraintTimelineState>>();
            //     foreach (var timeline in this._constraintTimelines)
            //     { // Create constraint timelines map.
            //         var timelineName = timeline.constraint.name;
            //         if (!(constraintTimelines.ContainsKey(timelineName)))
            //         {
            //             constraintTimelines[timelineName] = new List<ConstraintTimelineState>();
            //         }

            //         constraintTimelines[timelineName].Add(timeline);
            //     }

            //     foreach (var constraint in this._armature._constraints)
            //     {
            //         var timelineName = constraint.name;
            //         var timelineDatas = this._animationData.GetConstraintTimelines(timelineName);

            //         if (constraintTimelines.ContainsKey(timelineName))
            //         {
            //             // Remove constraint timeline from map.
            //             constraintTimelines.Remove(timelineName);
            //         }
            //         else
            //         {
            //             // Create new constraint timeline.
            //             if (timelineDatas != null)
            //             {
            //                 foreach (var timelineData in timelineDatas)
            //                 {
            //                     switch (timelineData.type)
            //                     {
            //                         case TimelineType.IKConstraint:
            //                             {
            //                                 var timeline = BaseObject.BorrowObject<IKConstraintTimelineState>();
            //                                 timeline.constraint = constraint;
            //                                 timeline.Init(this._armature, this, timelineData);
            //                                 this._constraintTimelines.Add(timeline);
            //                                 break;
            //                             }

            //                         default:
            //                             break;
            //                     }
            //                 }
            //             }
            //             else if (this.resetToPose)
            //             {
            //                 // Pose timeline.
            //                 var timeline = BaseObject.BorrowObject<IKConstraintTimelineState>();
            //                 timeline.constraint = constraint;
            //                 timeline.Init(this._armature, this, null);
            //                 this._constraintTimelines.Add(timeline);
            //                 this._poseTimelines.Add(timeline);
            //             }
            //         }
            //     }

            //     foreach (var timelines in constraintTimelines.Values)
            //     { // Remove constraint timelines.
            //         foreach (var timeline in timelines)
            //         {
            //             this._constraintTimelines.Remove(timeline);
            //             timeline.ReturnToPool();
            //         }
            //     }
            // }
        }

        private void _AdvanceFadeTime(float passedTime)
        {
            var isFadeOut = this._fadeState > 0;

            if (this._subFadeState < 0)
            {
                // Fade start event.
                this._subFadeState = 0;

                var eventType = isFadeOut ? EventObject.FADE_OUT : EventObject.FADE_IN;
                if (this._armature.eventDispatcher.HasDBEventListener(eventType))
                {
                    var eventObject = BaseObject.BorrowObject<EventObject>();
                    eventObject.type = eventType;
                    eventObject.armature = this._armature;
                    eventObject.animationState = this;
                    this._armature._dragonBones.BufferEvent(eventObject);
                }
            }

            if (passedTime < 0.0f)
            {
                passedTime = -passedTime;
            }

            this._fadeTime += passedTime;

            if (this._fadeTime >= this.fadeTotalTime)
            {
                // Fade complete.
                this._subFadeState = 1;
                this._fadeProgress = isFadeOut ? 0.0f : 1.0f;
            }
            else if (this._fadeTime > 0.0f)
            {
                // Fading.
                this._fadeProgress = isFadeOut ? (1.0f - this._fadeTime / this.fadeTotalTime) : (this._fadeTime / this.fadeTotalTime);
            }
            else
            {
                // Before fade.
                this._fadeProgress = isFadeOut ? 1.0f : 0.0f;
            }

            if (this._subFadeState > 0)
            {
                // Fade complete event.
                if (!isFadeOut)
                {
                    this._playheadState |= 1; // x1
                    this._fadeState = 0;
                }

                var eventType = isFadeOut ? EventObject.FADE_OUT_COMPLETE : EventObject.FADE_IN_COMPLETE;
                if (this._armature.eventDispatcher.HasDBEventListener(eventType))
                {
                    var eventObject = BaseObject.BorrowObject<EventObject>();
                    eventObject.type = eventType;
                    eventObject.armature = this._armature;
                    eventObject.animationState = this;
                    this._armature._dragonBones.BufferEvent(eventObject);
                }
            }
        }

        /// <internal/>
        /// <private/>
        internal void Init(Armature armature, AnimationData animationData, AnimationConfig animationConfig)
        {
            if (this._armature != null)
            {
                return;
            }

            this._armature = armature;

            this._animationData = animationData;
            this.resetToPose = animationConfig.resetToPose;
            this.additiveBlending = animationConfig.additiveBlending;
            this.displayControl = animationConfig.displayControl;
            this.actionEnabled = animationConfig.actionEnabled;
            this.layer = animationConfig.layer;
            this.playTimes = animationConfig.playTimes;
            this.timeScale = animationConfig.timeScale;
            this.fadeTotalTime = animationConfig.fadeInTime;
            this.autoFadeOutTime = animationConfig.autoFadeOutTime;
            this.weight = animationConfig.weight;
            this.name = animationConfig.name.Length > 0 ? animationConfig.name : animationConfig.animation;
            this.group = animationConfig.group;

            if (animationConfig.pauseFadeIn)
            {
                this._playheadState = 2; // 10
            }
            else
            {
                this._playheadState = 3; // 11
            }

            if (animationConfig.duration < 0.0f)
            {
                this._position = 0.0f;
                this._duration = this._animationData.duration;
                if (animationConfig.position != 0.0f)
                {
                    if (this.timeScale >= 0.0f)
                    {
                        this._time = animationConfig.position;
                    }
                    else
                    {
                        this._time = animationConfig.position - this._duration;
                    }
                }
                else
                {
                    this._time = 0.0f;
                }
            }
            else
            {
                this._position = animationConfig.position;
                this._duration = animationConfig.duration;
                this._time = 0.0f;
            }

            if (this.timeScale < 0.0f && this._time == 0.0f)
            {
                this._time = -0.000001f; // Turn to end.
            }

            if (this.fadeTotalTime <= 0.0f)
            {
                this._fadeProgress = 0.999999f; // Make different.
            }

            if (animationConfig.boneMask.Count > 0)
            {
                this._boneMask.ResizeList(animationConfig.boneMask.Count);
                for (int i = 0, l = this._boneMask.Count; i < l; ++i)
                {
                    this._boneMask[i] = animationConfig.boneMask[i];
                }
            }

            this._actionTimeline = BaseObject.BorrowObject<ActionTimelineState>();
            this._actionTimeline.Init(this._armature, this, this._animationData.actionTimeline);
            this._actionTimeline.currentTime = this._time;
            if (this._actionTimeline.currentTime < 0.0f)
            {
                this._actionTimeline.currentTime = this._duration - this._actionTimeline.currentTime;
            }

            if (this._animationData.zOrderTimeline != null)
            {
                this._zOrderTimeline = BaseObject.BorrowObject<ZOrderTimelineState>();
                this._zOrderTimeline.Init(this._armature, this, this._animationData.zOrderTimeline);
            }
        }
        /// <internal/>
        /// <private/>
        internal void AdvanceTime(float passedTime, float cacheFrameRate)
        {
            this._blendState.dirty = true;

            // Update fade time.
            if (this._fadeState != 0 || this._subFadeState != 0)
            {
                this._AdvanceFadeTime(passedTime);
            }

            // Update time.
            if (this._playheadState == 3)
            {
                // 11
                if (this.timeScale != 1.0f)
                {
                    passedTime *= this.timeScale;
                }

                this._time += passedTime;
            }

            // Update timeline.
            if (this._timelineDirty != 0)
            {
                if (this._timelineDirty == 2)
                {
                    this._UpdateTimelines();
                }

                this._timelineDirty = 0;
                this._UpdateBoneAndSlotTimelines();
            }

            if (this.weight == 0.0f)
            {
                return;
            }

            var isCacheEnabled = this._fadeState == 0 && cacheFrameRate > 0.0f;
            var isUpdateTimeline = true;
            var isUpdateBoneTimeline = true;
            var time = this._time;
            this._weightResult = this.weight * this._fadeProgress;

            if (this._parent != null)
            {
                this._weightResult *= this._parent._weightResult / this._parent._fadeProgress;
            }

            if (this._actionTimeline.playState <= 0)
            {
                // Update main timeline.
                this._actionTimeline.Update(time);
            }

            if (isCacheEnabled)
            {
                // Cache time internval.
                var internval = cacheFrameRate * 2.0f;
                this._actionTimeline.currentTime = (float)Math.Floor(this._actionTimeline.currentTime * internval) / internval;
            }

            if (this._zOrderTimeline != null && this._zOrderTimeline.playState <= 0)
            {
                // Update zOrder timeline.
                this._zOrderTimeline.Update(time);
            }

            if (isCacheEnabled)
            {
                // Update cache.
                var cacheFrameIndex = (int)Math.Floor(this._actionTimeline.currentTime * cacheFrameRate); // uint
                if (this._armature._cacheFrameIndex == cacheFrameIndex)
                {
                    // Same cache.
                    isUpdateTimeline = false;
                    isUpdateBoneTimeline = false;
                }
                else
                {
                    this._armature._cacheFrameIndex = cacheFrameIndex;
                    if (this._animationData.cachedFrames[cacheFrameIndex])
                    {
                        // Cached.
                        isUpdateBoneTimeline = false;
                    }
                    else
                    {
                        // Cache.
                        this._animationData.cachedFrames[cacheFrameIndex] = true;
                    }
                }
            }

            if (isUpdateTimeline)
            {
                if (isUpdateBoneTimeline)
                {
                    for (int i = 0, l = this._boneTimelines.Count; i < l; ++i)
                    {
                        var timeline = this._boneTimelines[i];

                        if (timeline.playState <= 0)
                        {
                            timeline.Update(time);
                        }

                        if (i == l - 1 || timeline.bone != this._boneTimelines[i + 1].bone)
                        {
                            var state = timeline.bone._blendState.Update(this._weightResult, this.layer);
                            if (state != 0)
                            {
                                timeline.Blend(state);
                            }
                        }
                    }
                }

                if (this.displayControl)
                {
                    for (int i = 0, l = this._slotTimelines.Count; i < l; ++i)
                    {
                        var timeline = this._slotTimelines[i];
                        if (timeline.slot != null)
                        {
                            var displayController = timeline.slot.displayController;

                            if (
                                displayController == null ||
                                displayController == this.name ||
                                displayController == this.group
                            )
                            {
                                if (timeline.playState <= 0)
                                {
                                    timeline.Update(time);
                                }
                            }
                        }
                    }
                }

                for (int i = 0, l = this._constraintTimelines.Count; i < l; ++i)
                {
                    var timeline = this._constraintTimelines[i];
                    if (timeline.playState <= 0)
                    {
                        timeline.Update(time);
                    }
                }
            }

            if (this._fadeState == 0)
            {
                if (this._subFadeState > 0)
                {
                    this._subFadeState = 0;

                    if (this._poseTimelines.Count > 0)
                    {
                        foreach (var timeline in this._poseTimelines)
                        {
                            if (timeline is BoneTimelineState)
                            {
                                this._boneTimelines.Remove(timeline as BoneTimelineState);
                            }
                            else if (timeline is SlotTimelineState)
                            {
                                this._slotTimelines.Remove(timeline as SlotTimelineState);
                            }
                            else if (timeline is ConstraintTimelineState)
                            {
                                this._constraintTimelines.Remove(timeline as ConstraintTimelineState);
                            }

                            timeline.ReturnToPool();
                        }

                        this._poseTimelines.Clear();
                    }
                }

                if (this._actionTimeline.playState > 0)
                {
                    if (this.autoFadeOutTime >= 0.0f)
                    {
                        // Auto fade out.
                        this.FadeOut(this.autoFadeOutTime);
                    }
                }
            }
        }

        /// <summary>
        /// - Continue play.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 继续播放。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void Play()
        {
            this._playheadState = 3; // 11
        }
        /// <summary>
        /// - Stop play.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 暂停播放。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void Stop()
        {
            this._playheadState &= 1; // 0x
        }
        /// <summary>
        /// - Fade out the animation state.
        /// </summary>
        /// <param name="fadeOutTime">- The fade out time. (In seconds)</param>
        /// <param name="pausePlayhead">- Whether to pause the animation playing when fade out.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 淡出动画状态。
        /// </summary>
        /// <param name="fadeOutTime">- 淡出时间。 （以秒为单位）</param>
        /// <param name="pausePlayhead">- 淡出时是否暂停播放。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void FadeOut(float fadeOutTime, bool pausePlayhead = true)
        {
            if (fadeOutTime < 0.0f)
            {
                fadeOutTime = 0.0f;
            }

            if (pausePlayhead)
            {
                this._playheadState &= 2; // x0
            }

            if (this._fadeState > 0)
            {
                if (fadeOutTime > this.fadeTotalTime - this._fadeTime)
                {
                    // If the animation is already in fade out, the new fade out will be ignored.
                    return;
                }
            }
            else
            {
                this._fadeState = 1;
                this._subFadeState = -1;

                if (fadeOutTime <= 0.0f || this._fadeProgress <= 0.0f)
                {
                    this._fadeProgress = 0.000001f; // Modify fade progress to different value.
                }

                foreach (var timeline in this._boneTimelines)
                {
                    timeline.FadeOut();
                }

                foreach (var timeline in this._slotTimelines)
                {
                    timeline.FadeOut();
                }

                foreach (var timeline in this._constraintTimelines)
                {
                    timeline.FadeOut();
                }
            }

            this.displayControl = false; //
            this.fadeTotalTime = this._fadeProgress > 0.000001f ? fadeOutTime / this._fadeProgress : 0.0f;
            this._fadeTime = this.fadeTotalTime * (1.0f - this._fadeProgress);
        }

        /// <summary>
        /// - Check if a specific bone mask is included.
        /// </summary>
        /// <param name="boneName">- The bone name.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查是否包含特定骨骼遮罩。
        /// </summary>
        /// <param name="boneName">- 骨骼名称。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public bool ContainsBoneMask(string boneName)
        {
            return this._boneMask.Count == 0 || this._boneMask.IndexOf(boneName) >= 0;
        }
        /// <summary>
        /// - Add a specific bone mask.
        /// </summary>
        /// <param name="boneName">- The bone name.</param>
        /// <param name="recursive">- Whether or not to add a mask to the bone's sub-bone.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 添加特定的骨骼遮罩。
        /// </summary>
        /// <param name="boneName">- 骨骼名称。</param>
        /// <param name="recursive">- 是否为该骨骼的子骨骼添加遮罩。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void AddBoneMask(string boneName, bool recursive = true)
        {
            var currentBone = this._armature.GetBone(boneName);
            if (currentBone == null)
            {
                return;
            }

            if (this._boneMask.IndexOf(boneName) < 0)
            {
                // Add mixing
                this._boneMask.Add(boneName);
            }

            if (recursive)
            {
                // Add recursive mixing.
                foreach (var bone in this._armature.GetBones())
                {
                    if (this._boneMask.IndexOf(bone.name) < 0 && currentBone.Contains(bone))
                    {
                        this._boneMask.Add(bone.name);
                    }
                }
            }

            this._timelineDirty = 1;
        }
        /// <summary>
        /// - Remove the mask of a specific bone.
        /// </summary>
        /// <param name="boneName">- The bone name.</param>
        /// <param name="recursive">- Whether to remove the bone's sub-bone mask.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 删除特定骨骼的遮罩。
        /// </summary>
        /// <param name="boneName">- 骨骼名称。</param>
        /// <param name="recursive">- 是否删除该骨骼的子骨骼遮罩。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void RemoveBoneMask(string boneName, bool recursive = true)
        {
            if (this._boneMask.Contains(boneName))
            {
                this._boneMask.Remove(boneName);
            }

            if (recursive)
            {
                var currentBone = this._armature.GetBone(boneName);
                if (currentBone != null)
                {
                    var bones = this._armature.GetBones();
                    if (this._boneMask.Count > 0)
                    {
                        // Remove recursive mixing.
                        foreach (var bone in bones)
                        {
                            if (this._boneMask.Contains(bone.name) && currentBone.Contains(bone))
                            {
                                this._boneMask.Remove(bone.name);
                            }
                        }
                    }
                    else
                    {
                        // Add unrecursive mixing.
                        foreach (var bone in bones)
                        {
                            if (bone == currentBone)
                            {
                                continue;
                            }

                            if (!currentBone.Contains(bone))
                            {
                                this._boneMask.Add(bone.name);
                            }
                        }
                    }
                }
            }

            this._timelineDirty = 1;
        }
        /// <summary>
        /// - Remove all bone masks.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 删除所有骨骼遮罩。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void RemoveAllBoneMask()
        {
            this._boneMask.Clear();
            this._timelineDirty = 1;
        }
        /// <summary>
        /// - Whether the animation state is fading in.
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 是否正在淡入。
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>zh_CN</language>
        public bool isFadeIn
        {
            get { return this._fadeState < 0; }
        }
        /// <summary>
        /// - Whether the animation state is fading out.
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 是否正在淡出。
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>zh_CN</language>
        public bool isFadeOut
        {
            get { return this._fadeState > 0; }
        }
        /// <summary>
        /// - Whether the animation state is fade completed.
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 是否淡入或淡出完毕。
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>zh_CN</language>
        public bool isFadeComplete
        {
            get { return this._fadeState == 0; }
        }
        /// <summary>
        /// - Whether the animation state is playing.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 是否正在播放。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public bool isPlaying
        {
            get { return (this._playheadState & 2) != 0 && this._actionTimeline.playState <= 0; }
        }
        /// <summary>
        /// - Whether the animation state is play completed.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 是否播放完毕。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public bool isCompleted
        {
            get { return this._actionTimeline.playState > 0; }
        }
        /// <summary>
        /// - The times has been played.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 已经循环播放的次数。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public int currentPlayTimes
        {
            get { return this._actionTimeline.currentPlayTimes; }
        }

        /// <summary>
        /// - The total time. (In seconds)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 总播放时间。 （以秒为单位）
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float totalTime
        {
            get { return this._duration; }
        }
        /// <summary>
        /// - The time is currently playing. (In seconds)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 当前播放的时间。 （以秒为单位）
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float currentTime
        {
            get { return this._actionTimeline.currentTime; }
            set
            {
                var currentPlayTimes = this._actionTimeline.currentPlayTimes - (this._actionTimeline.playState > 0 ? 1 : 0);
                if (value < 0.0f || this._duration < value)
                {
                    value = (value % this._duration) + currentPlayTimes * this._duration;
                    if (value < 0.0f)
                    {
                        value += this._duration;
                    }
                }

                if (this.playTimes > 0 && currentPlayTimes == this.playTimes - 1 && value == this._duration)
                {
                    value = this._duration - 0.000001f;
                }

                if (this._time == value)
                {
                    return;
                }

                this._time = value;
                this._actionTimeline.SetCurrentTime(this._time);

                if (this._zOrderTimeline != null)
                {
                    this._zOrderTimeline.playState = -1;
                }

                foreach (var timeline in this._boneTimelines)
                {
                    timeline.playState = -1;
                }

                foreach (var timeline in this._slotTimelines)
                {
                    timeline.playState = -1;
                }
            }
        }
    }

    /// <internal/>
    /// <private/>
    internal class BonePose : BaseObject
    {
        public readonly Transform current = new Transform();
        public readonly Transform delta = new Transform();
        public readonly Transform result = new Transform();

        protected override void _OnClear()
        {
            this.current.Identity();
            this.delta.Identity();
            this.result.Identity();
        }
    }

    /// <internal/>
    /// <private/>
    internal class BlendState
    {
        public bool dirty;
        public int layer;
        public float leftWeight;
        public float layerWeight;
        public float blendWeight;

        /// <summary>
        /// -1: First blending, 0: No blending, 1: Blending.
        /// </summary>
        public int Update(float weight, int p_layer)
        {
            if (this.dirty)
            {
                if (this.leftWeight > 0.0f)
                {
                    if (this.layer != p_layer)
                    {
                        if (this.layerWeight >= this.leftWeight)
                        {
                            this.leftWeight = 0.0f;

                            return 0;
                        }
                        else
                        {
                            this.layer = p_layer;
                            this.leftWeight -= this.layerWeight;
                            this.layerWeight = 0.0f;
                        }
                    }
                }
                else
                {
                    return 0;
                }

                weight *= this.leftWeight;
                this.layerWeight += weight;
                this.blendWeight = weight;

                return 2;
            }

            this.dirty = true;
            this.layer = p_layer;
            this.layerWeight = weight;
            this.leftWeight = 1.0f;
            this.blendWeight = weight;

            return 1;
        }

        public void Clear()
        {
            this.dirty = false;
            this.layer = 0;
            this.leftWeight = 0.0f;
            this.layerWeight = 0.0f;
            this.blendWeight = 0.0f;
        }
    }
}
