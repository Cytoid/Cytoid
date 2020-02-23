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

namespace DragonBones
{
    /// <internal/>
    /// <private/>
    internal abstract class Constraint : BaseObject
    {
        protected static readonly Matrix _helpMatrix = new Matrix();
        protected static readonly Transform _helpTransform = new Transform();
        protected static readonly Point _helpPoint = new Point();

        /// <summary>
        /// - For timeline state.
        /// </summary>
        /// <internal/>
        internal ConstraintData _constraintData;
        protected Armature _armature;

        /// <summary>
        /// - For sort bones.
        /// </summary>
        /// <internal/>
        internal Bone _target;
        /// <summary>
        /// - For sort bones.
        /// </summary>
        /// <internal/>
        internal Bone _root;
        internal Bone _bone;

        protected override void _OnClear()
        {
            this._armature = null;
            this._target = null; //
            this._root = null; //
            this._bone = null; //
        }

        public abstract void Init(ConstraintData constraintData, Armature armature);
        public abstract void Update();
        public abstract void InvalidUpdate();

        public string name
        {
            get { return this._constraintData.name; }
        }
    }
    /// <internal/>
    /// <private/>
    internal class IKConstraint : Constraint
    {
        internal bool _scaleEnabled; // TODO
        /// <summary>
        /// - For timeline state.
        /// </summary>
        /// <internal/>
        internal bool _bendPositive;
        /// <summary>
        /// - For timeline state.
        /// </summary>
        /// <internal/>
        internal float _weight;

        protected override void _OnClear()
        {
            base._OnClear();

            this._scaleEnabled = false;
            this._bendPositive = false;
            this._weight = 1.0f;
            this._constraintData = null;
        }

        private void _ComputeA()
        {
            var ikGlobal = this._target.global;
            var global = this._root.global;
            var globalTransformMatrix = this._root.globalTransformMatrix;

            var radian = (float)Math.Atan2(ikGlobal.y - global.y, ikGlobal.x - global.x);
            if (global.scaleX < 0.0f)
            {
                radian += (float)Math.PI;
            }

            global.rotation += Transform.NormalizeRadian(radian - global.rotation) * this._weight;
            global.ToMatrix(globalTransformMatrix);
        }

        private void _ComputeB()
        {
            var boneLength = this._bone.boneData.length;
            var parent = this._root as Bone;
            var ikGlobal = this._target.global;
            var parentGlobal = parent.global;
            var global = this._bone.global;
            var globalTransformMatrix = this._bone.globalTransformMatrix;

            var x = globalTransformMatrix.a * boneLength;
            var y = globalTransformMatrix.b * boneLength;

            var lLL = x * x + y * y;
            var lL = (float)Math.Sqrt(lLL);

            var dX = global.x - parentGlobal.x;
            var dY = global.y - parentGlobal.y;
            var lPP = dX * dX + dY * dY;
            var lP = (float)Math.Sqrt(lPP);
            var rawRadian = global.rotation;
            var rawParentRadian = parentGlobal.rotation;
            var rawRadianA = (float)Math.Atan2(dY, dX);

            dX = ikGlobal.x - parentGlobal.x;
            dY = ikGlobal.y - parentGlobal.y;
            var lTT = dX * dX + dY * dY;
            var lT = (float)Math.Sqrt(lTT);

            var radianA = 0.0f;
            if (lL + lP <= lT || lT + lL <= lP || lT + lP <= lL)
            {
                radianA = (float)Math.Atan2(ikGlobal.y - parentGlobal.y, ikGlobal.x - parentGlobal.x);
                if (lL + lP <= lT)
                {
                }
                else if (lP < lL)
                {
                    radianA += (float)Math.PI;
                }
            }
            else
            {
                var h = (lPP - lLL + lTT) / (2.0f * lTT);
                var r = (float)Math.Sqrt(lPP - h * h * lTT) / lT;
                var hX = parentGlobal.x + (dX * h);
                var hY = parentGlobal.y + (dY * h);
                var rX = -dY * r;
                var rY = dX * r;

                var isPPR = false;
                var parentParent = parent.parent;
                if (parentParent != null)
                {
                    var parentParentMatrix = parentParent.globalTransformMatrix;
                    isPPR = parentParentMatrix.a * parentParentMatrix.d - parentParentMatrix.b * parentParentMatrix.c < 0.0f;
                }

                if (isPPR != this._bendPositive)
                {
                    global.x = hX - rX;
                    global.y = hY - rY;
                }
                else
                {
                    global.x = hX + rX;
                    global.y = hY + rY;
                }

                radianA = (float)Math.Atan2(global.y - parentGlobal.y, global.x - parentGlobal.x);
            }

            var dR = Transform.NormalizeRadian(radianA - rawRadianA);
            parentGlobal.rotation = rawParentRadian + dR * this._weight;
            parentGlobal.ToMatrix(parent.globalTransformMatrix);
            //
            var currentRadianA = rawRadianA + dR * this._weight;
            global.x = parentGlobal.x + (float)Math.Cos(currentRadianA) * lP;
            global.y = parentGlobal.y + (float)Math.Sin(currentRadianA) * lP;
            //
            var radianB = (float)Math.Atan2(ikGlobal.y - global.y, ikGlobal.x - global.x);
            if (global.scaleX < 0.0f)
            {
                radianB += (float)Math.PI;
            }

            global.rotation = parentGlobal.rotation + rawRadian - rawParentRadian + Transform.NormalizeRadian(radianB - dR - rawRadian) * this._weight;
            global.ToMatrix(globalTransformMatrix);
        }

        public override void Init(ConstraintData constraintData, Armature armature)
        {
            if (this._constraintData != null)
            {
                return;
            }

            this._constraintData = constraintData;
            this._armature = armature;
            this._target = this._armature.GetBone(this._constraintData.target.name);
            this._root = this._armature.GetBone(this._constraintData.root.name);
            this._bone = this._constraintData.bone != null ? this._armature.GetBone(this._constraintData.bone.name) : null;

            {
                var ikConstraintData = this._constraintData as IKConstraintData;
                //
                this._scaleEnabled = ikConstraintData.scaleEnabled;
                this._bendPositive = ikConstraintData.bendPositive;
                this._weight = ikConstraintData.weight;
            }

            this._root._hasConstraint = true;
        }

        public override void Update()
        {
            this._root.UpdateByConstraint();

            if (this._bone != null)
            {
                this._bone.UpdateByConstraint();
                this._ComputeB();
            }
            else
            {
                this._ComputeA();
            }
        }

        public override void InvalidUpdate()
        {
            this._root.InvalidUpdate();

            if (this._bone != null)
            {
                this._bone.InvalidUpdate();
            }
        }
    }
}
