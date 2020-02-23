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
﻿namespace DragonBones
{
    /// <summary>
    /// - Play animation interface. (Both Armature and Wordclock implement the interface)
    /// Any instance that implements the interface can be added to the Worldclock instance and advance time by Worldclock instance uniformly.
    /// </summary>
    /// <see cref="DragonBones.WorldClock"/>
    /// <see cref="DragonBones.Armature"/>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 播放动画接口。 (Armature 和 WordClock 都实现了该接口)
    /// 任何实现了此接口的实例都可以添加到 WorldClock 实例中，由 WorldClock 实例统一更新时间。
    /// </summary>
    /// <see cref="DragonBones.WorldClock"/>
    /// <see cref="DragonBones.Armature"/>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public interface IAnimatable
    {
        /// <summary>
        /// - Advance time.
        /// </summary>
        /// <param name="passedTime">- Passed time. (In seconds)</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 更新时间。
        /// </summary>
        /// <param name="passedTime">- 前进的时间。 （以秒为单位）</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        void AdvanceTime(float passedTime);
        WorldClock clock
        {
            get;
            set;
        }
    }
}
