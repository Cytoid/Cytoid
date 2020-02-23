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
    /// <summary>
    /// - 2D Transform.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 2D 变换。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class Transform
    {
        /// <private/>
        public static readonly float PI = 3.141593f;
        /// <private/>
        public static readonly float PI_D = PI * 2.0f;
        /// <private/>
        public static readonly float PI_H = PI / 2.0f;
        /// <private/>
        public static readonly float PI_Q = PI / 4.0f;
        /// <private/>
        public static readonly float RAD_DEG = 180.0f / PI;
        /// <private/>
        public static readonly float DEG_RAD = PI / 180.0f;

        /// <private/>
        public static float NormalizeRadian(float value)
        {
            value = (value + PI) % (PI * 2.0f);

           
            value += value > 0.0f ? -PI : PI;

            return value;
        }

        /// <summary>
        /// - Horizontal translate.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 水平位移。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float x = 0.0f;
        /// <summary>
        /// - Vertical translate.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 垂直位移。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float y = 0.0f;
        /// <summary>
        /// - Skew. (In radians)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 倾斜。 （以弧度为单位）
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float skew = 0.0f;
        /// <summary>
        /// - rotation. (In radians)
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 旋转。 （以弧度为单位）
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float rotation = 0.0f;
        /// <summary>
        /// - Horizontal Scaling.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 水平缩放。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float scaleX = 1.0f;
        /// <summary>
        /// - Vertical scaling.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 垂直缩放。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float scaleY = 1.0f;

        /// <private/>
        public Transform()
        {
            
        }

        public override string ToString()
        {
            return "[object dragonBones.Transform] x:" + this.x + " y:" + this.y + " skew:" + this.skew* 180.0 / PI + " rotation:" + this.rotation* 180.0 / PI + " scaleX:" + this.scaleX + " scaleY:" + this.scaleY;
        }

        /// <private/>
        public Transform CopyFrom(Transform value)
        {
            this.x = value.x;
            this.y = value.y;
            this.skew = value.skew;
            this.rotation = value.rotation;
            this.scaleX = value.scaleX;
            this.scaleY = value.scaleY;

            return this;
        }

        /// <private/>
        public Transform Identity()
        {
            this.x = this.y = 0.0f;
            this.skew = this.rotation = 0.0f;
            this.scaleX = this.scaleY = 1.0f;

            return this;
        }

        /// <private/>
        public Transform Add(Transform value)
        {
            this.x += value.x;
            this.y += value.y;
            this.skew += value.skew;
            this.rotation += value.rotation;
            this.scaleX *= value.scaleX;
            this.scaleY *= value.scaleY;

            return this;
        }

        /// <private/>
        public Transform Minus(Transform value)
        {
            this.x -= value.x;
            this.y -= value.y;
            this.skew -= value.skew;
            this.rotation -= value.rotation;
            this.scaleX /= value.scaleX;
            this.scaleY /= value.scaleY;

            return this;
        }

        /// <private/>
        public Transform FromMatrix(Matrix matrix)
        {
            var backupScaleX = this.scaleX;
            var backupScaleY = this.scaleY;

            this.x = matrix.tx;
            this.y = matrix.ty;

            var skewX = (float)Math.Atan(-matrix.c / matrix.d);
            this.rotation = (float)Math.Atan(matrix.b / matrix.a);

            if(float.IsNaN(skewX))
            {
                skewX = 0.0f;
            }

            if(float.IsNaN(this.rotation))
            {
                this.rotation = 0.0f; 
            }

            this.scaleX = (float)((this.rotation > -PI_Q && this.rotation < PI_Q) ? matrix.a / Math.Cos(this.rotation) : matrix.b / Math.Sin(this.rotation));
            this.scaleY = (float)((skewX > -PI_Q && skewX < PI_Q) ? matrix.d / Math.Cos(skewX) : -matrix.c / Math.Sin(skewX));

            if (backupScaleX >= 0.0f && this.scaleX < 0.0f)
            {
                this.scaleX = -this.scaleX;
                this.rotation = this.rotation - PI;
            }

            if (backupScaleY >= 0.0f && this.scaleY < 0.0f)
            {
                this.scaleY = -this.scaleY;
                skewX = skewX - PI;
            }

            this.skew = skewX - this.rotation;

            return this;
        }

        /// <private/>
        public Transform ToMatrix(Matrix matrix)
        {
            if(this.rotation == 0.0f)
            {
                matrix.a = 1.0f;
                matrix.b = 0.0f;
            }
            else
            {
                matrix.a = (float)Math.Cos(this.rotation);
                matrix.b = (float)Math.Sin(this.rotation);
            }

            if(this.skew == 0.0f)
            {
                matrix.c = -matrix.b;
                matrix.d = matrix.a;
            }
            else
            {
                matrix.c = -(float)Math.Sin(this.skew + this.rotation);
                matrix.d = (float)Math.Cos(this.skew + this.rotation);
            }

            if(this.scaleX != 1.0f)
            {
                matrix.a *= this.scaleX;
                matrix.b *= this.scaleX;
            }

            if(this.scaleY != 1.0f)
            {
                matrix.c *= this.scaleY;
                matrix.d *= this.scaleY;
            }

            matrix.tx = this.x;
            matrix.ty = this.y;

            return this;
        }
    }
}
