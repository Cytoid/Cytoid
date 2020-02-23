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
using UnityEngine;

namespace DragonBones
{
    /**
     * @inheritDoc
     */
    public class UnityEventDispatcher<T> : MonoBehaviour
    {
        private readonly Dictionary<string, ListenerDelegate<T>> _listeners = new Dictionary<string, ListenerDelegate<T>>();
        /**
         * @private
         */
        public UnityEventDispatcher()
        {
        }
        /**
         * @inheritDoc
         */
        public void DispatchEvent(string type, T eventObject)
        {
            if (!_listeners.ContainsKey(type))
            {
                return;
            }
            else
            {
                _listeners[type](type, eventObject);
            }
        }
        /**
         * @inheritDoc
         */
        public bool HasEventListener(string type)
        {
            return _listeners.ContainsKey(type);
        }
        /**
         * @inheritDoc
         */
        public void AddEventListener(string type, ListenerDelegate<T> listener)
        {
            if (_listeners.ContainsKey(type))
            {
                var delegates = _listeners[type].GetInvocationList();
                for (int i = 0, l = delegates.Length; i < l; ++i)
                {
                    if (listener == delegates[i] as ListenerDelegate<T>)
                    {
                        return;
                    }
                }

                _listeners[type] += listener;
            }
            else
            {
                _listeners.Add(type, listener);
            }
        }
        /**
         * @inheritDoc
         */
        public void RemoveEventListener(string type, ListenerDelegate<T> listener)
        {
            if (!_listeners.ContainsKey(type))
            {
                return;
            }

            var delegates = _listeners[type].GetInvocationList();
            for (int i = 0, l = delegates.Length; i < l; ++i)
            {
                if (listener == delegates[i] as ListenerDelegate<T>)
                {
                    _listeners[type] -= listener;
                    break;
                }
            }

            if (_listeners[type] == null)
            {
                _listeners.Remove(type);
            }
        }
    }

    [DisallowMultipleComponent]
    public class DragonBoneEventDispatcher : UnityEventDispatcher<EventObject>, IEventDispatcher<EventObject>
    {
        public void AddDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            AddEventListener(type, listener);
        }

        public void DispatchDBEvent(string type, EventObject eventObject)
        {
            DispatchEvent(type, eventObject);
        }

        public bool HasDBEventListener(string type)
        {
            return HasEventListener(type);
        }

        public void RemoveDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            RemoveEventListener(type, listener);
        }
    }
}
