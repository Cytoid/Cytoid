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
    /// <internal/>
    /// <private/>
    internal enum TweenState
    {
        None,
        Once,
        Always
    }
    /// <internal/>
    /// <private/>
    internal abstract class TimelineState : BaseObject
    {
        public int playState; // -1: start, 0: play, 1: complete;
        public int currentPlayTimes;
        public float currentTime;

        protected TweenState _tweenState;
        protected uint _frameRate;
        protected int _frameValueOffset;
        protected uint _frameCount;
        protected uint _frameOffset;
        protected int _frameIndex;
        protected float _frameRateR;
        protected float _position;
        protected float _duration;
        protected float _timeScale;
        protected float _timeOffset;
        protected DragonBonesData _dragonBonesData;
        protected AnimationData _animationData;
        protected TimelineData _timelineData;
        protected Armature _armature;
        protected AnimationState _animationState;
        protected TimelineState _actionTimeline;

        protected short[] _frameArray;
        protected short[] _frameIntArray;
        protected float[] _frameFloatArray;
        protected ushort[] _timelineArray;
        protected List<uint> _frameIndices;

        protected override void _OnClear()
        {
            this.playState = -1;
            this.currentPlayTimes = -1;
            this.currentTime = -1.0f;

            this._tweenState = TweenState.None;
            this._frameRate = 0;
            this._frameValueOffset = 0;
            this._frameCount = 0;
            this._frameOffset = 0;
            this._frameIndex = -1;
            this._frameRateR = 0.0f;
            this._position = 0.0f;
            this._duration = 0.0f;
            this._timeScale = 1.0f;
            this._timeOffset = 0.0f;
            this._dragonBonesData = null; //
            this._animationData = null; //
            this._timelineData = null; //
            this._armature = null; //
            this._animationState = null; //
            this._actionTimeline = null; //
            this._frameArray = null; //
            this._frameIntArray = null; //
            this._frameFloatArray = null; //
            this._timelineArray = null; //
            this._frameIndices = null; //
        }

        protected abstract void _OnArriveAtFrame();
        protected abstract void _OnUpdateFrame();

        protected bool _SetCurrentTime(float passedTime)
        {
            var prevState = this.playState;
            var prevPlayTimes = this.currentPlayTimes;
            var prevTime = this.currentTime;

            if (this._actionTimeline != null && this._frameCount <= 1)
            {
                // No frame or only one frame.
                this.playState = this._actionTimeline.playState >= 0 ? 1 : -1;
                this.currentPlayTimes = 1;
                this.currentTime = this._actionTimeline.currentTime;
            }
            else if (this._actionTimeline == null || this._timeScale != 1.0f || this._timeOffset != 0.0f)
            {
                var playTimes = this._animationState.playTimes;
                var totalTime = playTimes * this._duration;

                passedTime *= this._timeScale;
                if (this._timeOffset != 0.0f)
                {
                    passedTime += this._timeOffset * this._animationData.duration;
                }

                if (playTimes > 0 && (passedTime >= totalTime || passedTime <= -totalTime))
                {
                    if (this.playState <= 0 && this._animationState._playheadState == 3)
                    {
                        this.playState = 1;
                    }

                    this.currentPlayTimes = playTimes;
                    if (passedTime < 0.0f)
                    {
                        this.currentTime = 0.0f;
                    }
                    else
                    {
                        this.currentTime = this._duration + 0.000001f; // Precision problem
                    }
                }
                else
                {
                    if (this.playState != 0 && this._animationState._playheadState == 3)
                    {
                        this.playState = 0;
                    }

                    if (passedTime < 0.0f)
                    {
                        passedTime = -passedTime;
                        this.currentPlayTimes = (int)(passedTime / this._duration);
                        this.currentTime = this._duration - (passedTime % this._duration);
                    }
                    else
                    {
                        this.currentPlayTimes = (int)(passedTime / this._duration);
                        this.currentTime = passedTime % this._duration;
                    }
                }

                this.currentTime += this._position;
            }
            else
            {
                // Multi frames.
                this.playState = this._actionTimeline.playState;
                this.currentPlayTimes = this._actionTimeline.currentPlayTimes;
                this.currentTime = this._actionTimeline.currentTime;
            }

            if (this.currentPlayTimes == prevPlayTimes && this.currentTime == prevTime)
            {
                return false;
            }

            // Clear frame flag when timeline start or loopComplete.
            if ((prevState < 0 && this.playState != prevState) || (this.playState <= 0 && this.currentPlayTimes != prevPlayTimes))
            {
                this._frameIndex = -1;
            }

            return true;
        }

        public virtual void Init(Armature armature, AnimationState animationState, TimelineData timelineData)
        {
            this._armature = armature;
            this._animationState = animationState;
            this._timelineData = timelineData;
            this._actionTimeline = this._animationState._actionTimeline;

            if (this == this._actionTimeline)
            {
                this._actionTimeline = null; //
            }

            this._frameRate = this._armature.armatureData.frameRate;
            this._frameRateR = 1.0f / this._frameRate;
            this._position = this._animationState._position;
            this._duration = this._animationState._duration;
            this._dragonBonesData = this._armature.armatureData.parent;
            this._animationData = this._animationState._animationData;

            if (this._timelineData != null)
            {
                this._frameIntArray = this._dragonBonesData.frameIntArray;
                this._frameFloatArray = this._dragonBonesData.frameFloatArray;
                this._frameArray = this._dragonBonesData.frameArray;
                this._timelineArray = this._dragonBonesData.timelineArray;
                this._frameIndices = this._dragonBonesData.frameIndices;

                this._frameCount = this._timelineArray[this._timelineData.offset + (int)BinaryOffset.TimelineKeyFrameCount];
                this._frameValueOffset = this._timelineArray[this._timelineData.offset + (int)BinaryOffset.TimelineFrameValueOffset];
                var timelineScale = this._timelineArray[this._timelineData.offset + (int)BinaryOffset.TimelineScale];
                this._timeScale = 100.0f / (timelineScale == 0 ? 100.0f : timelineScale);
                this._timeOffset = this._timelineArray[this._timelineData.offset + (int)BinaryOffset.TimelineOffset] * 0.01f;
            }
        }

        public virtual void FadeOut()
        {

        }

        public virtual void Update(float passedTime)
        {
            if (this._SetCurrentTime(passedTime))
            {
                if (this._frameCount > 1)
                {
                    int timelineFrameIndex = (int)Math.Floor(this.currentTime * this._frameRate); // uint
                    var frameIndex = this._frameIndices[(int)(this._timelineData as TimelineData).frameIndicesOffset + timelineFrameIndex];
                    if (this._frameIndex != frameIndex)
                    {
                        this._frameIndex = (int)frameIndex;
                        this._frameOffset = this._animationData.frameOffset + this._timelineArray[(this._timelineData as TimelineData).offset + (int)BinaryOffset.TimelineFrameOffset + this._frameIndex];

                        this._OnArriveAtFrame();
                    }
                }
                else if (this._frameIndex < 0)
                {
                    this._frameIndex = 0;
                    if (this._timelineData != null)
                    {
                        // May be pose timeline.
                        this._frameOffset = this._animationData.frameOffset + this._timelineArray[this._timelineData.offset + (int)BinaryOffset.TimelineFrameOffset];
                    }

                    this._OnArriveAtFrame();
                }

                if (this._tweenState != TweenState.None)
                {
                    this._OnUpdateFrame();
                }
            }
        }
    }

    /// <internal/>
    /// <private/>
    internal abstract class TweenTimelineState : TimelineState
    {
        private static float _GetEasingValue(TweenType tweenType, float progress, float easing)
        {
            var value = progress;

            switch (tweenType)
            {
                case TweenType.QuadIn:
                    value = (float)Math.Pow(progress, 2.0f);
                    break;

                case TweenType.QuadOut:
                    value = 1.0f - (float)Math.Pow(1.0f - progress, 2.0f);
                    break;

                case TweenType.QuadInOut:
                    value = 0.5f * (1.0f - (float)Math.Cos(progress * Math.PI));
                    break;
            }

            return (value - progress) * easing + progress;
        }

        private static float _GetEasingCurveValue(float progress, short[] samples, int count, int offset)
        {
            if (progress <= 0.0f)
            {
                return 0.0f;
            }
            else if (progress >= 1.0f)
            {
                return 1.0f;
            }

            var segmentCount = count + 1; // + 2 - 1
            var valueIndex = (int)Math.Floor(progress * segmentCount);
            var fromValue = valueIndex == 0 ? 0.0f : samples[offset + valueIndex - 1];

            var toValue = (valueIndex == segmentCount - 1) ? 10000.0f : samples[offset + valueIndex];

            return (fromValue + (toValue - fromValue) * (progress * segmentCount - valueIndex)) * 0.0001f;
        }

        protected TweenType _tweenType;
        protected int _curveCount;
        protected float _framePosition;
        protected float _frameDurationR;
        protected float _tweenProgress;
        protected float _tweenEasing;

        protected override void _OnClear()
        {
            base._OnClear();

            this._tweenType = TweenType.None;
            this._curveCount = 0;
            this._framePosition = 0.0f;
            this._frameDurationR = 0.0f;
            this._tweenProgress = 0.0f;
            this._tweenEasing = 0.0f;
        }

        protected override void _OnArriveAtFrame()
        {
            if (this._frameCount > 1 &&
                (this._frameIndex != this._frameCount - 1 ||
                this._animationState.playTimes == 0 ||
                this._animationState.currentPlayTimes < this._animationState.playTimes - 1))
            {
                this._tweenType = (TweenType)this._frameArray[this._frameOffset + (int)BinaryOffset.FrameTweenType]; // TODO recode ture tween type.
                this._tweenState = this._tweenType == TweenType.None ? TweenState.Once : TweenState.Always;

                if (this._tweenType == TweenType.Curve)
                {
                    this._curveCount = this._frameArray[this._frameOffset + (int)BinaryOffset.FrameTweenEasingOrCurveSampleCount];
                }
                else if (this._tweenType != TweenType.None && this._tweenType != TweenType.Line)
                {
                    this._tweenEasing = this._frameArray[this._frameOffset + (int)BinaryOffset.FrameTweenEasingOrCurveSampleCount] * 0.01f;
                }

                this._framePosition = this._frameArray[this._frameOffset] * this._frameRateR;
                if (this._frameIndex == this._frameCount - 1)
                {
                    this._frameDurationR = 1.0f / (this._animationData.duration - this._framePosition);
                }
                else
                {
                    var nextFrameOffset = this._animationData.frameOffset + this._timelineArray[(this._timelineData as TimelineData).offset + (int)BinaryOffset.TimelineFrameOffset + this._frameIndex + 1];
                    var frameDuration = this._frameArray[nextFrameOffset] * this._frameRateR - this._framePosition;

                    if (frameDuration > 0.0f)
                    {
                        this._frameDurationR = 1.0f / frameDuration;
                    }
                    else
                    {
                        this._frameDurationR = 0.0f;
                    }
                }
            }
            else
            {
                this._tweenState = TweenState.Once;
            }
        }

        protected override void _OnUpdateFrame()
        {
            if (this._tweenState == TweenState.Always)
            {
                this._tweenProgress = (this.currentTime - this._framePosition) * this._frameDurationR;
                if (this._tweenType == TweenType.Curve)
                {
                    this._tweenProgress = TweenTimelineState._GetEasingCurveValue(this._tweenProgress, this._frameArray, this._curveCount, (int)this._frameOffset + (int)BinaryOffset.FrameCurveSamples);
                }
                else if (this._tweenType != TweenType.Line)
                {
                    this._tweenProgress = TweenTimelineState._GetEasingValue(this._tweenType, this._tweenProgress, this._tweenEasing);
                }
            }
            else
            {
                this._tweenProgress = 0.0f;
            }
        }
    }
    /// <internal/>
    /// <private/>
    internal abstract class BoneTimelineState : TweenTimelineState
    {
        public Bone bone;
        public BonePose bonePose;

        protected override void _OnClear()
        {
            base._OnClear();

            this.bone = null; //
            this.bonePose = null; //
        }

        public void Blend(int state)
        {
            var blendWeight = this.bone._blendState.blendWeight;
            var animationPose = this.bone.animationPose;
            var result = this.bonePose.result;

            if (state == 2)
            {
                animationPose.x += result.x * blendWeight;
                animationPose.y += result.y * blendWeight;
                animationPose.rotation += result.rotation * blendWeight;
                animationPose.skew += result.skew * blendWeight;
                animationPose.scaleX += (result.scaleX - 1.0f) * blendWeight;
                animationPose.scaleY += (result.scaleY - 1.0f) * blendWeight;
            }
            else if (blendWeight != 1.0f)
            {
                animationPose.x = result.x * blendWeight;
                animationPose.y = result.y * blendWeight;
                animationPose.rotation = result.rotation * blendWeight;
                animationPose.skew = result.skew * blendWeight;
                animationPose.scaleX = (result.scaleX - 1.0f) * blendWeight + 1.0f;
                animationPose.scaleY = (result.scaleY - 1.0f) * blendWeight + 1.0f;
            }
            else
            {
                animationPose.x = result.x;
                animationPose.y = result.y;
                animationPose.rotation = result.rotation;
                animationPose.skew = result.skew;
                animationPose.scaleX = result.scaleX;
                animationPose.scaleY = result.scaleY;
            }

            if (this._animationState._fadeState != 0 || this._animationState._subFadeState != 0)
            {
                this.bone._transformDirty = true;
            }
        }
    }
    /// <internal/>
    /// <private/>
    internal abstract class SlotTimelineState : TweenTimelineState
    {
        public Slot slot;

        protected override void _OnClear()
        {
            base._OnClear();

            this.slot = null; //
        }
    }

    /// <internal/>
    /// <private/>
    internal abstract class ConstraintTimelineState : TweenTimelineState
    {
        public Constraint constraint;

        protected override void _OnClear()
        {
            base._OnClear();

            this.constraint = null; //
        }
    }
}
