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
    /// <internal/>
    /// <private/>
    public abstract class ConstraintData : BaseObject
    {
        public int order;
        public string name;
        public BoneData target;
        public BoneData root;
        public BoneData bone = null;

        protected override void _OnClear()
        {
            this.order = 0;
            this.name = string.Empty;
            this.target = null; 
            this.bone = null; 
            this.root = null;
        }
    }
    /// <internal/>
    /// <private/>
    public class IKConstraintData : ConstraintData
    {
        public bool scaleEnabled;
        public bool bendPositive;
        public float weight;

        protected override void _OnClear()
        {
            base._OnClear();

            this.scaleEnabled = false;
            this.bendPositive = false;
            this.weight = 1.0f;
        }
    }
}
