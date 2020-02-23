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
﻿using System.Collections.Generic;

namespace DragonBones
{
    /// <summary>
    /// - The animation config is used to describe all the information needed to play an animation state.
    /// The API is still in the experimental phase and may encounter bugs or stability or compatibility issues when used.
    /// </summary>
    /// <see cref="DragonBones.AnimationState"/>
    /// <beta/>
    /// <version>DragonBones 5.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 动画配置用来描述播放一个动画状态所需要的全部信息。
    /// 该 API 仍在实验阶段，使用时可能遭遇 bug 或稳定性或兼容性问题。
    /// </summary>
    /// <see cref="DragonBones.AnimationState"/>
    /// <beta/>
    /// <version>DragonBones 5.0</version>
    /// <language>zh_CN</language>
    public class AnimationConfig : BaseObject
    {
        /// <private/>
        public bool pauseFadeOut;
        /// <summary>
        /// - Fade out the pattern of other animation states when the animation state is fade in.
        /// This property is typically used to specify the substitution of multiple animation states blend.
        /// </summary>
        /// <default>dragonBones.AnimationFadeOutMode.All</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 淡入动画状态时淡出其他动画状态的模式。
        /// 该属性通常用来指定多个动画状态混合时的相互替换关系。
        /// </summary>
        /// <default>dragonBones.AnimationFadeOutMode.All</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public AnimationFadeOutMode fadeOutMode;
        /// <private/>
        public TweenType fadeOutTweenType;
        /// <private/>
        public float fadeOutTime;
        /// <private/>
        public bool pauseFadeIn;

        /// <private/>
        public bool actionEnabled;
        /// <private/>
        public bool additiveBlending;
        /// <summary>
        /// - Whether the animation state has control over the display property of the slots.
        /// Sometimes blend a animation state does not want it to control the display properties of the slots,
        /// especially if other animation state are controlling the display properties of the slots.
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
        /// <private/>
        public TweenType fadeInTweenType;
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
        /// - The start time of play. (In seconds)
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 播放的开始时间。 （以秒为单位）
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public float position;
        /// <summary>
        /// - The duration of play.
        /// [-1: Use the default value of the animation data, 0: Stop play, (0~N]: The duration] (In seconds)
        /// </summary>
        /// <default>-1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 播放的持续时间。
        /// [-1: 使用动画数据默认值, 0: 动画停止, (0~N]: 持续时间] （以秒为单位）
        /// </summary>
        /// <default>-1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public float duration;
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
        /// - The fade in time.
        /// [-1: Use the default value of the animation data, [0~N]: The fade in time] (In seconds)
        /// </summary>
        /// <default>-1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 淡入时间。
        /// [-1: 使用动画数据默认值, [0~N]: 淡入时间] （以秒为单位）
        /// </summary>
        /// <default>-1.0</default>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public float fadeInTime;
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
        /// <summary>
        /// - The name of the animation state. (Can be different from the name of the animation data)
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画状态名称。 （可以不同于动画数据）
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public string name;
        /// <summary>
        /// - The animation data name.
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 动画数据名称。
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public string animation;
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
        /// <private/>
        public readonly List<string> boneMask = new List<string>();

        /// <private/>
        protected override void _OnClear()
        {
            this.pauseFadeOut = true;
            this.fadeOutMode = AnimationFadeOutMode.All;
            this.fadeOutTweenType = TweenType.Line;
            this.fadeOutTime = -1.0f;

            this.actionEnabled = true;
            this.additiveBlending = false;
            this.displayControl = true;
            this.pauseFadeIn = true;
            this.resetToPose = true;
            this.fadeInTweenType = TweenType.Line;
            this.playTimes = -1;
            this.layer = 0;
            this.position = 0.0f;
            this.duration = -1.0f;
            this.timeScale = -100.0f;
            this.weight = 1.0f;
            this.fadeInTime = -1.0f;
            this.autoFadeOutTime = -1.0f;
            this.name = "";
            this.animation = "";
            this.group = "";
            this.boneMask.Clear();
        }

        /// <private/>
        public void Clear()
        {
            this._OnClear();
        }

        /// <private/>
        public void CopyFrom(AnimationConfig value)
        {
            this.pauseFadeOut = value.pauseFadeOut;
            this.fadeOutMode = value.fadeOutMode;
            this.autoFadeOutTime = value.autoFadeOutTime;
            this.fadeOutTweenType = value.fadeOutTweenType;

            this.actionEnabled = value.actionEnabled;
            this.additiveBlending = value.additiveBlending;
            this.displayControl = value.displayControl;
            this.pauseFadeIn = value.pauseFadeIn;
            this.resetToPose = value.resetToPose;
            this.playTimes = value.playTimes;
            this.layer = value.layer;
            this.position = value.position;
            this.duration = value.duration;
            this.timeScale = value.timeScale;
            this.fadeInTime = value.fadeInTime;
            this.fadeOutTime = value.fadeOutTime;
            this.fadeInTweenType = value.fadeInTweenType;
            this.weight = value.weight;
            this.name = value.name;
            this.animation = value.animation;
            this.group = value.group;

            boneMask.ResizeList(value.boneMask.Count, null);
            for (int i = 0, l = boneMask.Count; i < l; ++i)
            {
                boneMask[i] = value.boneMask[i];
            }
        }

        /// <private/>
        public bool ContainsBoneMask(string boneName)
        {
            return boneMask.Count == 0 || boneMask.Contains(boneName);
        }

        /// <private/>
        public void AddBoneMask(Armature armature, string boneName, bool recursive = false)
        {
            var currentBone = armature.GetBone(boneName);
            if (currentBone == null)
            {
                return;
            }

            if (!boneMask.Contains(boneName)) // Add mixing
            {
                boneMask.Add(boneName);
            }

            if (recursive) // Add recursive mixing.
            {
                var bones = armature.GetBones();
                for (int i = 0, l = bones.Count; i < l; ++i)
                {
                    var bone = bones[i];
                    if (!boneMask.Contains(bone.name) && currentBone.Contains(bone))
                    {
                        boneMask.Add(bone.name);
                    }
                }
            }
        }

        /// <private/>
        public void RemoveBoneMask(Armature armature, string name, bool recursive = true)
        {
            if (boneMask.Contains(name)) // Remove mixing.
            {
                boneMask.Remove(name);
            }

            if (recursive)
            {
                var currentBone = armature.GetBone(name);
                if (currentBone != null)
                {
                    var bones = armature.GetBones();
                    if (boneMask.Count > 0) // Remove recursive mixing.
                    {
                        for (int i = 0, l = bones.Count; i < l; ++i)
                        {
                            var bone = bones[i];
                            if (boneMask.Contains(bone.name) && currentBone.Contains(bone))
                            {
                                boneMask.Remove(bone.name);
                            }
                        }
                    }
                    else // Add unrecursive mixing.
                    {
                        for (int i = 0, l = bones.Count; i < l; ++i)
                        {
                            var bone = bones[i];
                            if (bone == currentBone)
                            {
                                continue;
                            }

                            if (!currentBone.Contains(bone))
                            {
                                boneMask.Add(bone.name);
                            }
                        }
                    }
                }
            }
        }
    }
}