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
using System.Collections.Generic;

namespace DragonBones
{
    /// <summary>
    /// - 2D Transform matrix.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 2D 转换矩阵。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public class Matrix
    {
        /// <summary>
        /// - The value that affects the positioning of pixels along the x axis when scaling or rotating an image.
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 缩放或旋转图像时影响像素沿 x 轴定位的值。
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float a = 1.0f;
        /// <summary>
        /// - The value that affects the positioning of pixels along the y axis when rotating or skewing an image.
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 旋转或倾斜图像时影响像素沿 y 轴定位的值。
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float b = 0.0f;
        /// <summary>
        /// - The value that affects the positioning of pixels along the x axis when rotating or skewing an image.
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 旋转或倾斜图像时影响像素沿 x 轴定位的值。
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float c = 0.0f;
        /// <summary>
        /// - The value that affects the positioning of pixels along the y axis when scaling or rotating an image.
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 缩放或旋转图像时影响像素沿 y 轴定位的值。
        /// </summary>
        /// <default>1.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float d = 1.0f;
        /// <summary>
        /// - The distance by which to translate each point along the x axis.
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 沿 x 轴平移每个点的距离。
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float tx = 0.0f;
        /// <summary>
        /// - The distance by which to translate each point along the y axis.
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 沿 y 轴平移每个点的距离。
        /// </summary>
        /// <default>0.0</default>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public float ty = 0.0f;

        /// <private/>
        public Matrix()
        {
        }
        public override string ToString()
        {
            return "[object DragonBones.Matrix] a:" + a + " b:" + b + " c:" + c + " d:" + d + " tx:" + tx + " ty:" + ty;
        }
        /// <private/>
        public Matrix CopyFrom(Matrix value)
        {
            this.a = value.a;
            this.b = value.b;
            this.c = value.c;
            this.d = value.d;
            this.tx = value.tx;
            this.ty = value.ty;

            return this;
        }

        /// <private/>
        public Matrix CopyFromArray(List<float> value, int offset = 0)
        {
            this.a = value[offset];
            this.b = value[offset + 1];
            this.c = value[offset + 2];
            this.d = value[offset + 3];
            this.tx = value[offset + 4];
            this.ty = value[offset + 5];

            return this;
        }

    /// <summary>
    /// - Convert to unit matrix.
    /// The resulting matrix has the following properties: a=1, b=0, c=0, d=1, tx=0, ty=0.
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 转换为单位矩阵。
    /// 该矩阵具有以下属性：a=1、b=0、c=0、d=1、tx=0、ty=0。
    /// </summary>
    /// <version>DragonBones 3.0</version>
    /// <language>zh_CN</language>
    public Matrix Identity()
        {
            this.a = this.d = 1.0f;
            this.b = this.c = 0.0f;
            this.tx = this.ty = 0.0f;

            return this;
        }

        /// <summary>
        /// - Multiplies the current matrix with another matrix.
        /// </summary>
        /// <param name="value">- The matrix that needs to be multiplied.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 将当前矩阵与另一个矩阵相乘。
        /// </summary>
        /// <param name="value">- 需要相乘的矩阵。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public Matrix Concat(Matrix value)
        {
            var aA = this.a * value.a;
            var bA = 0.0f;
            var cA = 0.0f;
            var dA = this.d * value.d;
            var txA = this.tx * value.a + value.tx;
            var tyA = this.ty * value.d + value.ty;
            if (this.b != 0.0f || this.c != 0.0f)
            {
                aA += this.b * value.c;
                bA += this.b * value.d;
                cA += this.c * value.a;
                dA += this.c * value.b;
            }
            if (value.b != 0.0f || value.c != 0.0f)
            {
                bA += this.a * value.b;
                cA += this.d * value.c;
                txA += this.ty * value.c;
                tyA += this.tx * value.b;
            }
            this.a = aA;
            this.b = bA;
            this.c = cA;
            this.d = dA;
            this.tx = txA;
            this.ty = tyA;
            return this;
        }
        /// <summary>
        /// - Convert to inverse matrix.
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 转换为逆矩阵。
        /// </summary>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public Matrix Invert()
        {
            var aA = this.a;
            var bA = this.b;
            var cA = this.c;
            var dA = this.d;
            var txA = this.tx;
            var tyA = this.ty;

            if (bA == 0.0f && cA == 0.0f)
            {
                this.b = this.c = 0.0f;
                if (aA == 0.0f || dA == 0.0f)
                {
                    this.a = this.b = this.tx = this.ty = 0.0f;
                }
                else
                {
                    aA = this.a = 1.0f / aA;
                    dA = this.d = 1.0f / dA;
                    this.tx = -aA * txA;
                    this.ty = -dA * tyA;
                }

                return this;
            }

            var determinant = aA * dA - bA * cA;
            if (determinant == 0.0f)
            {
                this.a = this.d = 1.0f;
                this.b = this.c = 0.0f;
                this.tx = this.ty = 0.0f;

                return this;
            }

            determinant = 1.0f / determinant;
            var k = this.a = dA * determinant;
            bA = this.b = -bA * determinant;
            cA = this.c = -cA * determinant;
            dA = this.d = aA * determinant;
            this.tx = -(k * txA + cA * tyA);
            this.ty = -(bA * txA + dA * tyA);

            return this;
        }
        /// <summary>
        /// - Apply a matrix transformation to a specific point.
        /// </summary>
        /// <param name="x">- X coordinate.</param>
        /// <param name="y">- Y coordinate.</param>
        /// <param name="result">- The point after the transformation is applied.</param>
        /// <param name="delta">- Whether to ignore tx, ty's conversion to point.</param>
        /// <version>DragonBones 3.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 将矩阵转换应用于特定点。
        /// </summary>
        /// <param name="x">- 横坐标。</param>
        /// <param name="y">- 纵坐标。</param>
        /// <param name="result">- 应用转换之后的点。</param>
        /// <param name="delta">- 是否忽略 tx，ty 对点的转换。</param>
        /// <version>DragonBones 3.0</version>
        /// <language>zh_CN</language>
        public void TransformPoint(float x, float y, Point result, bool delta = false)
        {
            result.x = this.a * x + this.c * y;
            result.y = this.b * x + this.d * y;

            if (!delta)
            {
                result.x += this.tx;
                result.y += this.ty;
            }
        }

        /// <private/>
        public void TransformRectangle(Rectangle rectangle, bool delta = false)
        {
            var a = this.a;
            var b = this.b;
            var c = this.c;
            var d = this.d;
            var tx = delta ? 0.0f : this.tx;
            var ty = delta ? 0.0f : this.ty;

            var x = rectangle.x;
            var y = rectangle.y;
            var xMax = x + rectangle.width;
            var yMax = y + rectangle.height;

            var x0 = a * x + c * y + tx;
            var y0 = b * x + d * y + ty;
            var x1 = a * xMax + c * y + tx;
            var y1 = b * xMax + d * y + ty;
            var x2 = a * xMax + c * yMax + tx;
            var y2 = b * xMax + d * yMax + ty;
            var x3 = a * x + c * yMax + tx;
            var y3 = b * x + d * yMax + ty;

            var tmp = 0.0f;

            if (x0 > x1)
            {
                tmp = x0;
                x0 = x1;
                x1 = tmp;
            }
            if (x2 > x3)
            {
                tmp = x2;
                x2 = x3;
                x3 = tmp;
            }
            
            rectangle.x = (float)Math.Floor(x0 < x2 ? x0 : x2);
            rectangle.width = (float)Math.Ceiling((x1 > x3 ? x1 : x3) - rectangle.x);

            if (y0 > y1)
            {
                tmp = y0;
                y0 = y1;
                y1 = tmp;
            }
            if (y2 > y3)
            {
                tmp = y2;
                y2 = y3;
                y3 = tmp;
            }

            rectangle.y = (float)Math.Floor(y0 < y2 ? y0 : y2);
            rectangle.height = (float)Math.Ceiling((y1 > y3 ? y1 : y3) - rectangle.y);
        }
    }
}
