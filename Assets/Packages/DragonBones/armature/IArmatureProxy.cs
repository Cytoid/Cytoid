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
    /// - The armature proxy interface, the docking engine needs to implement it concretely.
    /// </summary>
    /// <see cref="DragonBones.Armature"/>
    /// <version>DragonBones 5.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 骨架代理接口，对接的引擎需要对其进行具体实现。
    /// </summary>
    /// <see cref="DragonBones.Armature"/>
    /// <version>DragonBones 5.0</version>
    /// <language>zh_CN</language>
    public interface IArmatureProxy : IEventDispatcher<EventObject>
    {
        /// <internal/>
        /// <private/>
        void DBInit(Armature armature);
        /// <internal/>
        /// <private/>
        void DBClear();
        /// <internal/>
        /// <private/>
        void DBUpdate();
        /// <summary>
        /// - Dispose the instance and the Armature instance. (The Armature instance will return to the object pool)
        /// </summary>
        /// <example>
        /// TypeScript style, for reference only.
        /// <pre>
        ///     removeChild(armatureDisplay);
        ///     armatureDisplay.dispose();
        /// </pre>
        /// </example>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 释放该实例和骨架。 （骨架会回收到对象池）
        /// </summary>
        /// <example>
        /// TypeScript 风格，仅供参考。
        /// <pre>
        ///     removeChild(armatureDisplay);
        ///     armatureDisplay.dispose();
        /// </pre>
        /// </example>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        void Dispose(bool disposeProxy);
         /// <summary>
         /// - The armature.
         /// </summary>
         /// <version>DragonBones 4.5</version>
         /// <language>en_US</language>

         /// <summary>
         /// - 骨架。
         /// </summary>
         /// <version>DragonBones 4.5</version>
         /// <language>zh_CN</language>
         Armature armature { get; }
         /// <summary>
         /// - The animation player.
         /// </summary>
         /// <version>DragonBones 3.0</version>
         /// <language>en_US</language>

         /// <summary>
         /// - 动画播放器。
         /// </summary>
         /// <version>DragonBones 3.0</version>
         /// <language>zh_CN</language>
         Animation animation { get; }
    }
}
