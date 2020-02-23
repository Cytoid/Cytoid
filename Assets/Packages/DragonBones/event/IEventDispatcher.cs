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
    public delegate void ListenerDelegate<T>(string type, T eventObject);
    /// <summary>
    /// - The event dispatcher interface.
    /// Dragonbones event dispatch usually relies on docking engine to implement, which defines the event method to be implemented when docking the engine.
    /// </summary>
    /// <version>DragonBones 4.5</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 事件派发接口。
    /// DragonBones 的事件派发通常依赖于对接的引擎来实现，该接口定义了对接引擎时需要实现的事件方法。
    /// </summary>
    /// <version>DragonBones 4.5</version>
    /// <language>zh_CN</language>
    public interface IEventDispatcher<T>
    {
        /// <summary>
        /// - Checks whether the object has any listeners registered for a specific type of event。
        /// </summary>
        /// <param name="type">- Event type.</param>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查是否为特定的事件类型注册了任何侦听器。
        /// </summary>
        /// <param name="type">- 事件类型。</param>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        bool HasDBEventListener(string type);
        /// <summary>
        /// - Dispatches an event into the event flow.
        /// </summary>
        /// <param name="type">- Event type.</param>
        /// <param name="eventObject">- Event object.</param>
        /// <see cref="DragonBones.EventObject"/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 分派特定的事件到事件流中。
        /// </summary>
        /// <param name="type">- 事件类型。</param>
        /// <param name="eventObject">- 事件数据。</param>
        /// <see cref="DragonBones.EventObject"/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        void DispatchDBEvent(string type, T eventObject);
        /// <summary>
        /// - Add an event listener object so that the listener receives notification of an event.
        /// </summary>
        /// <param name="type">- Event type.</param>
        /// <param name="listener">- Event listener.</param>
        /// <param name="thisObject">- The listener function's "this".</param>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 添加特定事件类型的事件侦听器，以使侦听器能够接收事件通知。
        /// </summary>
        /// <param name="type">- 事件类型。</param>
        /// <param name="listener">- 事件侦听器。</param>
        /// <param name="thisObject">- 侦听函数绑定的 this 对象。</param>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        void AddDBEventListener(string type, ListenerDelegate<T> listener);
        /// <summary>
        /// - Removes a listener from the object.
        /// </summary>
        /// <param name="type">- Event type.</param>
        /// <param name="listener">- Event listener.</param>
        /// <param name="thisObject">- The listener function's "this".</param>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 删除特定事件类型的侦听器。
        /// </summary>
        /// <param name="type">- 事件类型。</param>
        /// <param name="listener">- 事件侦听器。</param>
        /// <param name="thisObject">- 侦听函数绑定的 this 对象。</param>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        void RemoveDBEventListener(string type, ListenerDelegate<T> listener);
    }
}
