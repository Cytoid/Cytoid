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

namespace DragonBones
{
    /// <summary>
    /// - The animation data.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 动画数据。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class AnimationData : BaseObject
    {
        /// <summary>
        /// - FrameIntArray.
        /// </summary>
        /// <internal/>
        /// <private/>
        public uint frameIntOffset;
        /// <summary>
        /// - FrameFloatArray.
        /// </summary>
        /// <internal/>
        /// <private/>
        public uint frameFloatOffset;
        /// <summary>
        /// - FrameArray.
        /// </summary>
        /// <internal/>
        /// <private/>
        public uint frameOffset;
        /// <summary>
        /// - The frame count of the animation.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画的帧数。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public uint frameCount;
        /// <summary>
        /// - The play times of the animation. [0: Loop play, [1~N]: Play N times]
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画的播放次数。 [0: 无限循环播放, [1~N]: 循环播放 N 次]
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public uint playTimes;
        /// <summary>
        /// - The duration of the animation. (In seconds)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画的持续时间。 （以秒为单位）
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float duration;
        /// <private/>
        public float scale;
        /// <summary>
        /// - The fade in time of the animation. (In seconds)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画的淡入时间。 （以秒为单位）
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float fadeInTime;
        /// <private/>
        public float cacheFrameRate;
        /// <summary>
        /// - The animation name.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画名称。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <private/>
        public readonly List<bool> cachedFrames = new List<bool>();
        /// <private/>
        public readonly Dictionary<string, List<TimelineData>> boneTimelines = new Dictionary<string, List<TimelineData>>();
        /// <private/>
        public readonly Dictionary<string, List<TimelineData>> slotTimelines = new Dictionary<string, List<TimelineData>>();
        /// <private/>
        public readonly Dictionary<string, List<TimelineData>> constraintTimelines = new Dictionary<string, List<TimelineData>>();
        /// <private/>
        public readonly Dictionary<string, List<int>> boneCachedFrameIndices = new Dictionary<string, List<int>>();
        /// <private/>
        public readonly Dictionary<string, List<int>> slotCachedFrameIndices = new Dictionary<string, List<int>>();
        /// <private/>
        public TimelineData actionTimeline = null; // Initial value.
        /// <private/>
        public TimelineData zOrderTimeline = null; // Initial value.
        /// <private/>
        public ArmatureData parent;

        public AnimationData()
        {

        }

        /// <inheritDoc/>
        protected override void _OnClear()
        {
            foreach (var pair in boneTimelines)
            {
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    pair.Value[i].ReturnToPool();
                }
            }

            foreach (var pair in slotTimelines)
            {
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    pair.Value[i].ReturnToPool();
                }
            }

            foreach (var pair in constraintTimelines)
            {
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    pair.Value[i].ReturnToPool();
                }
            }

            if (this.actionTimeline != null)
            {
                this.actionTimeline.ReturnToPool();
            }

            if (this.zOrderTimeline != null)
            {
                this.zOrderTimeline.ReturnToPool();
            }

            this.frameIntOffset = 0;
            this.frameFloatOffset = 0;
            this.frameOffset = 0;
            this.frameCount = 0;
            this.playTimes = 0;
            this.duration = 0.0f;
            this.scale = 1.0f;
            this.fadeInTime = 0.0f;
            this.cacheFrameRate = 0.0f;
            this.name = "";
            this.boneTimelines.Clear();
            this.slotTimelines.Clear();
            this.constraintTimelines.Clear();
            this.boneCachedFrameIndices.Clear();
            this.slotCachedFrameIndices.Clear();
            this.cachedFrames.Clear();

            this.actionTimeline = null;
            this.zOrderTimeline = null;
            this.parent = null;
        }

        /// <internal/>
        /// <private/>
        public void CacheFrames(float frameRate)
        {
            if (this.cacheFrameRate > 0.0f)
            {
                // TODO clear cache.
                return;
            }

            this.cacheFrameRate = Math.Max((float)Math.Ceiling(frameRate * scale), 1.0f);
            var cacheFrameCount = (int)Math.Ceiling(this.cacheFrameRate * duration) + 1; // Cache one more frame.

            cachedFrames.ResizeList(0, false);
            cachedFrames.ResizeList(cacheFrameCount, false);

            foreach (var bone in this.parent.sortedBones)
            {
                var indices = new List<int>(cacheFrameCount);
                for (int i = 0, l = indices.Capacity; i < l; ++i)
                {
                    indices.Add(-1);
                }

                this.boneCachedFrameIndices[bone.name] = indices;
            }

            foreach (var slot in this.parent.sortedSlots)
            {
                var indices = new List<int>(cacheFrameCount);
                for (int i = 0, l = indices.Capacity; i < l; ++i)
                {
                    indices.Add(-1);
                }

                this.slotCachedFrameIndices[slot.name] = indices;
            }
        }

        /// <private/>
        public void AddBoneTimeline(BoneData bone, TimelineData tiemline)
        {
            if (bone == null || tiemline == null)
            {
                return;
            }

            if (!this.boneTimelines.ContainsKey(bone.name))
            {
                this.boneTimelines[bone.name] = new List<TimelineData>();
            }

            var timelines = this.boneTimelines[bone.name];
            if (!timelines.Contains(tiemline))
            {
                timelines.Add(tiemline);
            }
        }
        /// <private/>
        public void AddSlotTimeline(SlotData slot, TimelineData timeline)
        {
            if (slot == null || timeline == null)
            {
                return;
            }

            if (!this.slotTimelines.ContainsKey(slot.name))
            {
                this.slotTimelines[slot.name] = new List<TimelineData>();
            }

            var timelines = this.slotTimelines[slot.name];
            if (!timelines.Contains(timeline))
            {
                timelines.Add(timeline);
            }
        }

        /// <private/>
        public void AddConstraintTimeline(ConstraintData constraint, TimelineData timeline)
        {
            if (constraint == null || timeline == null)
            {
                return;
            }

            if (!this.constraintTimelines.ContainsKey(constraint.name))
            {
                this.constraintTimelines[constraint.name] = new List<TimelineData>();
            }

            var timelines = this.constraintTimelines[constraint.name];
            if (!timelines.Contains(timeline))
            {
                timelines.Add(timeline);
            }
        }

        /// <private/>
        public List<TimelineData> GetBoneTimelines(string timelineName)
        {
            return this.boneTimelines.ContainsKey(timelineName) ? this.boneTimelines[timelineName] : null;
        }
        /// <private/>
        public List<TimelineData> GetSlotTimelines(string timelineName)
        {
            return slotTimelines.ContainsKey(timelineName) ? slotTimelines[timelineName] : null;
        }

        /// <private/>
        public List<TimelineData> GetConstraintTimelines(string timelineName)
        {
            return constraintTimelines.ContainsKey(timelineName) ? constraintTimelines[timelineName] : null;
        }
        /// <private/>
        public List<int> GetBoneCachedFrameIndices(string boneName)
        {
            return this.boneCachedFrameIndices.ContainsKey(boneName) ? this.boneCachedFrameIndices[boneName] : null;
        }

        /// <private/>
        public List<int> GetSlotCachedFrameIndices(string slotName)
        {
            return this.slotCachedFrameIndices.ContainsKey(slotName) ? this.slotCachedFrameIndices[slotName] : null;
        }
    }

    /// <internal/>
    /// <private/>
    public class TimelineData : BaseObject
    {
        public TimelineType type;
        public uint offset; // TimelineArray.
        public int frameIndicesOffset; // FrameIndices.

        protected override void _OnClear()
        {
            this.type = TimelineType.BoneAll;
            this.offset = 0;
            this.frameIndicesOffset = -1;
        }
    }
}
