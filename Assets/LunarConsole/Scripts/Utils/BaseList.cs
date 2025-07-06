//
//  BaseList.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


﻿using System;
using System.Collections.Generic;

namespace LunarConsolePluginInternal
{
    abstract class BaseList<T> where T : class // TODO: thread safety
    {
        protected readonly List<T> list;

        private readonly T nullElement;
        private int removedCount;
        private bool locked;

        protected BaseList(T nullElement)
            : this(nullElement, 0)
        {
        }

        protected BaseList(T nullElement, int capacity)
            : this(new List<T>(capacity), nullElement)
        {   
            if (nullElement == null)
            {
                throw new ArgumentNullException("nullElement");
            }
        }

        protected BaseList(List<T> list, T nullElement)
        {
            this.list = list;
            this.nullElement = nullElement;
        }

        public virtual bool Add(T e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            // Assert.NotContains(e, list);
            list.Add(e);

            return true;
        }

        public virtual bool Remove(T e)
        {
            int index = list.IndexOf(e);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public virtual T Get(int index)
        {
            return list[index];
        }

        public virtual int IndexOf(T e)
        {
            return list.IndexOf(e);
        }

        public virtual void RemoveAt(int index)
        {
            if (locked)
            {
                ++removedCount;
                list[index] = nullElement;
            }
            else
            {
                list.RemoveAt(index);
            }
        }

        public virtual void Clear()
        {
            if (locked)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i] = nullElement;
                }
                removedCount = list.Count;
            }
            else
            {
                list.Clear();
                removedCount = 0;
            }
        }

        public virtual bool Contains(T e)
        {
            return list.Contains(e);
        }

        private void ClearRemoved()
        {
            for (int i = list.Count - 1; removedCount > 0 && i >= 0; --i)
            {
                if (list[i] == nullElement)
                {
                    list.RemoveAt(i);
                    --removedCount;
                }
            }
        }

        public virtual int Count
        {
            get { return list.Count - removedCount; }
        }

        protected void Lock()
        {
            // Assert.IsFalse(locked);
            locked = true;
        }

        protected void Unlock()
        {
            // Assert.IsTrue(locked);
            ClearRemoved();
            locked = false;
        }
    }
}