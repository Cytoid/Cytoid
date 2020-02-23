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
    /// - Worldclock provides clock support for animations, advance time for each IAnimatable object added to the instance.
    /// </summary>
    /// <see cref="DragonBones.IAnimateble"/>
    /// <see cref="DragonBones.Armature"/>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - WorldClock 对动画提供时钟支持，为每个加入到该实例的 IAnimatable 对象更新时间。
    /// </summary>
    /// <see cref="DragonBones.IAnimateble"/>
    /// <see cref="DragonBones.Armature"/>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class WorldClock : IAnimatable
    {
        /// <summary>
        /// - Current time. (In seconds)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 当前的时间。 (以秒为单位)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float time = 0.0f;

        /// <summary>
        /// - The play speed, used to control animation speed-shift play.
        /// [0: Stop play, (0~1): Slow play, 1: Normal play, (1~N): Fast play]
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 播放速度，用于控制动画变速播放。
        /// [0: 停止播放, (0~1): 慢速播放, 1: 正常播放, (1~N): 快速播放]
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float timeScale = 1.0f;
        private float _systemTime = 0.0f;
        private readonly List<IAnimatable> _animatebles = new List<IAnimatable>();
        private WorldClock _clock = null;
        /// <summary>
        /// - Creating a Worldclock instance. Typically, you do not need to create Worldclock instance.
        /// When multiple Worldclock instances are running at different speeds, can achieving some specific animation effects, such as bullet time.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 创建一个 WorldClock 实例。通常并不需要创建 WorldClock 实例。
        /// 当多个 WorldClock 实例使用不同的速度运行时，可以实现一些特殊的动画效果，比如子弹时间等。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public WorldClock(float time = -1.0f)
        {
            this.time = time;
            this._systemTime = DateTime.Now.Ticks * 0.01f * 0.001f;
        }

        /// <summary>
        /// - Advance time for all IAnimatable instances.
        /// </summary>
        /// <param name="passedTime">- Passed time. [-1: Automatically calculates the time difference between the current frame and the previous frame, [0~N): Passed time] (In seconds)</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 为所有的 IAnimatable 实例更新时间。
        /// </summary>
        /// <param name="passedTime">- 前进的时间。 [-1: 自动计算当前帧与上一帧的时间差, [0~N): 前进的时间] (以秒为单位)</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void AdvanceTime(float passedTime)
        {
            if (float.IsNaN(passedTime))
            {
                passedTime = 0.0f;
            }

            var currentTime = DateTime.Now.Ticks * 0.01f * 0.001f;
            if (passedTime < 0.0f)
            {
                passedTime = currentTime - this._systemTime;
            }

            this._systemTime = currentTime;

            if (this.timeScale != 1.0f)
            {
                passedTime *= this.timeScale;
            }

            if (passedTime == 0.0f)
            {
                return;
            }

            if (passedTime < 0.0f)
            {
                this.time -= passedTime;
            }
            else
            {
                this.time += passedTime;
            }

            int i = 0, r = 0, l = _animatebles.Count;
            for (; i < l; ++i)
            {
                var animateble = _animatebles[i];
                if (animateble != null)
                {
                    if (r > 0)
                    {
                        _animatebles[i - r] = animateble;
                        _animatebles[i] = null;
                    }

                    animateble.AdvanceTime(passedTime);
                }
                else
                {
                    r++;
                }
            }

            if (r > 0)
            {
                l = _animatebles.Count;
                for (; i < l; ++i)
                {
                    var animateble = _animatebles[i];
                    if (animateble != null)
                    {
                        _animatebles[i - r] = animateble;
                    }
                    else
                    {
                        r++;
                    }
                }

                _animatebles.ResizeList(l - r, null);
            }
        }
        /// <summary>
        /// - Check whether contains a specific instance of IAnimatable.
        /// </summary>
        /// <param name="value">- The IAnimatable instance.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查是否包含特定的 IAnimatable 实例。
        /// </summary>
        /// <param name="value">- IAnimatable 实例。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public bool Contains(IAnimatable value)
        {
            if (value == this)
            {
                return false;
            }

            IAnimatable ancestor = value;
            while (ancestor != this && ancestor != null)
            {
                ancestor = ancestor.clock;
            }

            return ancestor == this;
        }
        /// <summary>
        /// - Add IAnimatable instance.
        /// </summary>
        /// <param name="value">- The IAnimatable instance.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 添加 IAnimatable 实例。
        /// </summary>
        /// <param name="value">- IAnimatable 实例。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void Add(IAnimatable value)
        {
            if (value != null && !_animatebles.Contains(value))
            {
                _animatebles.Add(value);
                value.clock = this;
            }
        }
        /// <summary>
        /// - Removes a specified IAnimatable instance.
        /// </summary>
        /// <param name="value">- The IAnimatable instance.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 移除特定的 IAnimatable 实例。
        /// </summary>
        /// <param name="value">- IAnimatable 实例。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void Remove(IAnimatable value)
        {
            var index = _animatebles.IndexOf(value);
            if (index >= 0)
            {
                _animatebles[index] = null;
                value.clock = null;
            }
        }
        /// <summary>
        /// - Clear all IAnimatable instances.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 清除所有的 IAnimatable 实例。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void Clear()
        {
            for (int i = 0, l = _animatebles.Count; i < l; ++i)
            {
                var animateble = _animatebles[i];
                _animatebles[i] = null;
                if (animateble != null)
                {
                    animateble.clock = null;
                }
            }
        }
        /// <summary>
        /// - Deprecated, please refer to {@link dragonBones.BaseFactory#clock}.
        /// </summary>
        /// <language>en_US</language>

        /// <summary>
        /// - 已废弃，请参考 {@link dragonBones.BaseFactory#clock}。
        /// </summary>
        /// <language>zh_CN</language>
        [System.Obsolete("")]
        /// <inheritDoc/>
        public WorldClock clock
        {
            get { return _clock; }
            set
            {
                if (_clock == value)
                {
                    return;
                }

                if (_clock != null)
                {
                    _clock.Remove(this);
                }

                _clock = value;

                if (_clock != null)
                {
                    _clock.Add(this);
                }
            }
        }
    }
}