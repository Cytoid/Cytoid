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
    /// - The base class of bounding box data.
    /// </summary>
    /// <see cref="DragonBones.RectangleData"/>
    /// <see cref="DragonBones.EllipseData"/>
    /// <see cref="DragonBones.PolygonData"/>
    /// <version>DragonBones 5.0</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 边界框数据基类。
    /// </summary>
    /// <see cref="DragonBones.RectangleData"/>
    /// <see cref="DragonBones.EllipseData"/>
    /// <see cref="DragonBones.PolygonData"/>
    /// <version>DragonBones 5.0</version>
    /// <language>zh_CN</language>
    public abstract class BoundingBoxData : BaseObject
    {
        /// <summary>
        /// - The bounding box type.
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 边界框类型。
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public BoundingBoxType type;
        /// <private/>
        public uint color;
        /// <private/>
        public float width;
        /// <private/>
        public float height;

        /// <private/>
        protected override void _OnClear()
        {
            this.color = 0x000000;
            this.width = 0.0f;
            this.height = 0.0f;
        }
        /// <summary>
        /// - Check whether the bounding box contains a specific point. (Local coordinate system)
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查边界框是否包含特定点。（本地坐标系）
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public abstract bool ContainsPoint(float pX, float pY);

        /// <summary>
        /// - Check whether the bounding box intersects a specific segment. (Local coordinate system)
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 检查边界框是否与特定线段相交。（本地坐标系）
        /// </summary>
        /// <version>DragonBones 5.0</version>
        /// <language>zh_CN</language>
        public abstract int IntersectsSegment(float xA, float yA, float xB, float yB,
                                                Point intersectionPointA = null,
                                                Point intersectionPointB = null,
                                                Point normalRadians = null);
    }

    /// <summary>
    /// - Cohen–Sutherland algorithm https://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
    /// ----------------------
    /// | 0101 | 0100 | 0110 |
    /// ----------------------
    /// | 0001 | 0000 | 0010 |
    /// ----------------------
    /// | 1001 | 1000 | 1010 |
    /// ----------------------
    /// </summary>
    enum OutCode
    {
        InSide = 0, // 0000
        Left = 1,   // 0001
        Right = 2,  // 0010
        Top = 4,    // 0100
        Bottom = 8  // 1000
    }

    /// <summary>
    /// - The rectangle bounding box data.
    /// </summary>
    /// <version>DragonBones 5.1</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 矩形边界框数据。
    /// </summary>
    /// <version>DragonBones 5.1</version>
    /// <language>zh_CN</language>
    public class RectangleBoundingBoxData : BoundingBoxData
    {
        /// <summary>
        /// - Compute the bit code for a point (x, y) using the clip rectangle
        /// </summary>
        private static int _ComputeOutCode(float x, float y, float xMin, float yMin, float xMax, float yMax)
        {
            var code = OutCode.InSide;  // initialised as being inside of [[clip window]]

            if (x < xMin)
            {             // to the left of clip window
                code |= OutCode.Left;
            }
            else if (x > xMax)
            {        // to the right of clip window
                code |= OutCode.Right;
            }

            if (y < yMin)
            {             // below the clip window
                code |= OutCode.Top;
            }
            else if (y > yMax)
            {        // above the clip window
                code |= OutCode.Bottom;
            }

            return (int)code;
        }
        /// <private/>
        public static int RectangleIntersectsSegment(float xA, float yA, float xB, float yB,
                                                        float xMin, float yMin, float xMax, float yMax,
                                                        Point intersectionPointA = null,
                                                        Point intersectionPointB = null,
                                                        Point normalRadians = null)
        {
            var inSideA = xA > xMin && xA < xMax && yA > yMin && yA < yMax;
            var inSideB = xB > xMin && xB < xMax && yB > yMin && yB < yMax;

            if (inSideA && inSideB)
            {
                return -1;
            }

            var intersectionCount = 0;
            var outcode0 = RectangleBoundingBoxData._ComputeOutCode(xA, yA, xMin, yMin, xMax, yMax);
            var outcode1 = RectangleBoundingBoxData._ComputeOutCode(xB, yB, xMin, yMin, xMax, yMax);

            while (true)
            {
                if ((outcode0 | outcode1) == 0)
                { // Bitwise OR is 0. Trivially accept and get out of loop
                    intersectionCount = 2;
                    break;
                }
                else if ((outcode0 & outcode1) != 0)
                { // Bitwise AND is not 0. Trivially reject and get out of loop
                    break;
                }

                // failed both tests, so calculate the line segment to clip
                // from an outside point to an intersection with clip edge
                var x = 0.0f;
                var y = 0.0f;
                var normalRadian = 0.0f;

                // At least one endpoint is outside the clip rectangle; pick it.
                var outcodeOut = outcode0 != 0 ? outcode0 : outcode1;

                // Now find the intersection point;
                if ((outcodeOut & (int)OutCode.Top) != 0)
                {             // point is above the clip rectangle
                    x = xA + (xB - xA) * (yMin - yA) / (yB - yA);
                    y = yMin;

                    if (normalRadians != null)
                    {
                        normalRadian = -(float)Math.PI * 0.5f;
                    }
                }
                else if ((outcodeOut & (int)OutCode.Bottom) != 0)
                {     // point is below the clip rectangle
                    x = xA + (xB - xA) * (yMax - yA) / (yB - yA);
                    y = yMax;

                    if (normalRadians != null)
                    {
                        normalRadian = (float)Math.PI * 0.5f;
                    }
                }
                else if ((outcodeOut & (int)OutCode.Right) != 0)
                {      // point is to the right of clip rectangle
                    y = yA + (yB - yA) * (xMax - xA) / (xB - xA);
                    x = xMax;

                    if (normalRadians != null)
                    {
                        normalRadian = 0;
                    }
                }
                else if ((outcodeOut & (int)OutCode.Left) != 0)
                {       // point is to the left of clip rectangle
                    y = yA + (yB - yA) * (xMin - xA) / (xB - xA);
                    x = xMin;

                    if (normalRadians != null)
                    {
                        normalRadian = (float)Math.PI;
                    }
                }

                // Now we move outside point to intersection point to clip
                // and get ready for next pass.
                if (outcodeOut == outcode0)
                {
                    xA = x;
                    yA = y;
                    outcode0 = RectangleBoundingBoxData._ComputeOutCode(xA, yA, xMin, yMin, xMax, yMax);

                    if (normalRadians != null)
                    {
                        normalRadians.x = normalRadian;
                    }
                }
                else
                {
                    xB = x;
                    yB = y;
                    outcode1 = RectangleBoundingBoxData._ComputeOutCode(xB, yB, xMin, yMin, xMax, yMax);

                    if (normalRadians != null)
                    {
                        normalRadians.y = normalRadian;
                    }
                }
            }

            if (intersectionCount > 0)
            {
                if (inSideA)
                {
                    intersectionCount = 2; // 10

                    if (intersectionPointA != null)
                    {
                        intersectionPointA.x = xB;
                        intersectionPointA.y = yB;
                    }

                    if (intersectionPointB != null)
                    {
                        intersectionPointB.x = xB;
                        intersectionPointB.y = xB;
                    }

                    if (normalRadians != null)
                    {
                        normalRadians.x = normalRadians.y + (float)Math.PI;
                    }
                }
                else if (inSideB)
                {
                    intersectionCount = 1; // 01

                    if (intersectionPointA != null)
                    {
                        intersectionPointA.x = xA;
                        intersectionPointA.y = yA;
                    }

                    if (intersectionPointB != null)
                    {
                        intersectionPointB.x = xA;
                        intersectionPointB.y = yA;
                    }

                    if (normalRadians != null)
                    {
                        normalRadians.y = normalRadians.x + (float)Math.PI;
                    }
                }
                else
                {
                    intersectionCount = 3; // 11
                    if (intersectionPointA != null)
                    {
                        intersectionPointA.x = xA;
                        intersectionPointA.y = yA;
                    }

                    if (intersectionPointB != null)
                    {
                        intersectionPointB.x = xB;
                        intersectionPointB.y = yB;
                    }
                }
            }

            return intersectionCount;
        }
        /// <inheritDoc/>
        /// <private/>
        protected override void _OnClear()
        {
            base._OnClear();

            this.type = BoundingBoxType.Rectangle;
        }

        /// <inheritDoc/>
        public override bool ContainsPoint(float pX, float pY)
        {
            var widthH = this.width * 0.5f;
            if (pX >= -widthH && pX <= widthH)
            {
                var heightH = this.height * 0.5f;
                if (pY >= -heightH && pY <= heightH)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritDoc/>
        public override int IntersectsSegment(float xA, float yA, float xB, float yB,
                                             Point intersectionPointA = null,
                                             Point intersectionPointB = null,
                                             Point normalRadians = null)
        {
            var widthH = this.width * 0.5f;
            var heightH = this.height * 0.5f;
            var intersectionCount = RectangleBoundingBoxData.RectangleIntersectsSegment
            (
                xA, yA, xB, yB,
                -widthH, -heightH, widthH, heightH,
                intersectionPointA, intersectionPointB, normalRadians
            );

            return intersectionCount;
        }
    }

    /// <summary>
    /// - The ellipse bounding box data.
    /// </summary>
    /// <version>DragonBones 5.1</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 椭圆边界框数据。
    /// </summary>
    /// <version>DragonBones 5.1</version>
    /// <language>zh_CN</language>
    public class EllipseBoundingBoxData : BoundingBoxData
    {
        /// <private/>
        public static int EllipseIntersectsSegment(float xA, float yA, float xB, float yB,
                                                    float xC, float yC, float widthH, float heightH,
                                                    Point intersectionPointA = null,
                                                    Point intersectionPointB = null,
                                                    Point normalRadians = null)
        {
            var d = widthH / heightH;
            var dd = d * d;

            yA *= d;
            yB *= d;

            var dX = xB - xA;
            var dY = yB - yA;
            var lAB = (float)Math.Sqrt(dX * dX + dY * dY);
            var xD = dX / lAB;
            var yD = dY / lAB;
            var a = (xC - xA) * xD + (yC - yA) * yD;
            var aa = a * a;
            var ee = xA * xA + yA * yA;
            var rr = widthH * widthH;
            var dR = rr - ee + aa;
            var intersectionCount = 0;

            if (dR >= 0.0f)
            {
                var dT = (float)Math.Sqrt(dR);
                var sA = a - dT;
                var sB = a + dT;
                var inSideA = sA < 0.0 ? -1 : (sA <= lAB ? 0 : 1);
                var inSideB = sB < 0.0 ? -1 : (sB <= lAB ? 0 : 1);
                var sideAB = inSideA * inSideB;

                if (sideAB < 0)
                {
                    return -1;
                }
                else if (sideAB == 0)
                {
                    if (inSideA == -1)
                    {
                        intersectionCount = 2; // 10
                        xB = xA + sB * xD;
                        yB = (yA + sB * yD) / d;

                        if (intersectionPointA != null)
                        {
                            intersectionPointA.x = xB;
                            intersectionPointA.y = yB;
                        }

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = xB;
                            intersectionPointB.y = yB;
                        }

                        if (normalRadians != null)
                        {
                            normalRadians.x = (float)Math.Atan2(yB / rr * dd, xB / rr);
                            normalRadians.y = normalRadians.x + (float)Math.PI;
                        }
                    }
                    else if (inSideB == 1)
                    {
                        intersectionCount = 1; // 01
                        xA = xA + sA * xD;
                        yA = (yA + sA * yD) / d;

                        if (intersectionPointA != null)
                        {
                            intersectionPointA.x = xA;
                            intersectionPointA.y = yA;
                        }

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = xA;
                            intersectionPointB.y = yA;
                        }

                        if (normalRadians != null)
                        {
                            normalRadians.x = (float)Math.Atan2(yA / rr * dd, xA / rr);
                            normalRadians.y = normalRadians.x + (float)Math.PI;
                        }
                    }
                    else
                    {
                        intersectionCount = 3; // 11

                        if (intersectionPointA != null)
                        {
                            intersectionPointA.x = xA + sA * xD;
                            intersectionPointA.y = (yA + sA * yD) / d;

                            if (normalRadians != null)
                            {
                                normalRadians.x = (float)Math.Atan2(intersectionPointA.y / rr * dd, intersectionPointA.x / rr);
                            }
                        }

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = xA + sB * xD;
                            intersectionPointB.y = (yA + sB * yD) / d;

                            if (normalRadians != null)
                            {
                                normalRadians.y = (float)Math.Atan2(intersectionPointB.y / rr * dd, intersectionPointB.x / rr);
                            }
                        }
                    }
                }
            }

            return intersectionCount;
        }
        /// <inheritDoc/>
        /// <private/>
        protected override void _OnClear()
        {
            base._OnClear();

            this.type = BoundingBoxType.Ellipse;
        }

        /// <inheritDoc/>
        public override bool ContainsPoint(float pX, float pY)
        {
            var widthH = this.width * 0.5f;
            if (pX >= -widthH && pX <= widthH)
            {
                var heightH = this.height * 0.5f;
                if (pY >= -heightH && pY <= heightH)
                {
                    pY *= widthH / heightH;
                    return Math.Sqrt(pX * pX + pY * pY) <= widthH;
                }
            }

            return false;
        }

        /// <inheritDoc/>
        public override int IntersectsSegment(float xA, float yA, float xB, float yB,
                                                Point intersectionPointA,
                                                Point intersectionPointB,
                                                Point normalRadians)
        {
            var intersectionCount = EllipseBoundingBoxData.EllipseIntersectsSegment(xA, yA, xB, yB,
                                                                                    0.0f, 0.0f, this.width * 0.5f, this.height * 0.5f,
                                                                                    intersectionPointA, intersectionPointB, normalRadians);

            return intersectionCount;
        }
    }

    /// <summary>
    /// - The polygon bounding box data.
    /// </summary>
    /// <version>DragonBones 5.1</version>
    /// <language>en_US</language>

    /// <summary>
    /// - 多边形边界框数据。
    /// </summary>
    /// <version>DragonBones 5.1</version>
    /// <language>zh_CN</language>
    public class PolygonBoundingBoxData : BoundingBoxData
    {
        /// <private/>
        public static int PolygonIntersectsSegment(float xA, float yA, float xB, float yB,
                                                    List<float> vertices,
                                                    Point intersectionPointA = null,
                                                    Point intersectionPointB = null,
                                                    Point normalRadians = null)
        {
            if (xA == xB)
            {
                xA = xB + 0.01f;
            }

            if (yA == yB)
            {
                yA = yB + 0.01f;
            }

            var l = vertices.Count;
            var dXAB = xA - xB;
            var dYAB = yA - yB;
            var llAB = xA * yB - yA * xB;
            int intersectionCount = 0;
            var xC = vertices[l - 2];
            var yC = vertices[l - 1];
            var dMin = 0.0f;
            var dMax = 0.0f;
            var xMin = 0.0f;
            var yMin = 0.0f;
            var xMax = 0.0f;
            var yMax = 0.0f;

            for (int i = 0; i < l; i += 2)
            {
                var xD = vertices[i];
                var yD = vertices[i + 1];

                if (xC == xD)
                {
                    xC = xD + 0.01f;
                }

                if (yC == yD)
                {
                    yC = yD + 0.01f;
                }

                var dXCD = xC - xD;
                var dYCD = yC - yD;
                var llCD = xC * yD - yC * xD;
                var ll = dXAB * dYCD - dYAB * dXCD;
                var x = (llAB * dXCD - dXAB * llCD) / ll;

                if (((x >= xC && x <= xD) || (x >= xD && x <= xC)) && (dXAB == 0 || (x >= xA && x <= xB) || (x >= xB && x <= xA)))
                {
                    var y = (llAB * dYCD - dYAB * llCD) / ll;
                    if (((y >= yC && y <= yD) || (y >= yD && y <= yC)) && (dYAB == 0 || (y >= yA && y <= yB) || (y >= yB && y <= yA)))
                    {
                        if (intersectionPointB != null)
                        {
                            var d = x - xA;
                            if (d < 0.0f)
                            {
                                d = -d;
                            }

                            if (intersectionCount == 0)
                            {
                                dMin = d;
                                dMax = d;
                                xMin = x;
                                yMin = y;
                                xMax = x;
                                yMax = y;

                                if (normalRadians != null)
                                {
                                    normalRadians.x = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                    normalRadians.y = normalRadians.x;
                                }
                            }
                            else
                            {
                                if (d < dMin)
                                {
                                    dMin = d;
                                    xMin = x;
                                    yMin = y;

                                    if (normalRadians != null)
                                    {
                                        normalRadians.x = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                    }
                                }

                                if (d > dMax)
                                {
                                    dMax = d;
                                    xMax = x;
                                    yMax = y;

                                    if (normalRadians != null)
                                    {
                                        normalRadians.y = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                    }
                                }
                            }

                            intersectionCount++;
                        }
                        else
                        {
                            xMin = x;
                            yMin = y;
                            xMax = x;
                            yMax = y;
                            intersectionCount++;

                            if (normalRadians != null)
                            {
                                normalRadians.x = (float)Math.Atan2(yD - yC, xD - xC) - (float)Math.PI * 0.5f;
                                normalRadians.y = normalRadians.x;
                            }
                            break;
                        }
                    }
                }

                xC = xD;
                yC = yD;
            }

            if (intersectionCount == 1)
            {
                if (intersectionPointA != null)
                {
                    intersectionPointA.x = xMin;
                    intersectionPointA.y = yMin;
                }

                if (intersectionPointB != null)
                {
                    intersectionPointB.x = xMin;
                    intersectionPointB.y = yMin;
                }

                if (normalRadians != null)
                {
                    normalRadians.y = normalRadians.x + (float)Math.PI;
                }
            }
            else if (intersectionCount > 1)
            {
                intersectionCount++;

                if (intersectionPointA != null)
                {
                    intersectionPointA.x = xMin;
                    intersectionPointA.y = yMin;
                }

                if (intersectionPointB != null)
                {
                    intersectionPointB.x = xMax;
                    intersectionPointB.y = yMax;
                }
            }

            return intersectionCount;
        }

        /// <private/>
        public float x;
        /// <private/>
        public float y;
        /// <summary>
        /// - The polygon vertices.
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>en_US</language>

        /// <summary>
        /// - 多边形顶点。
        /// </summary>
        /// <version>DragonBones 5.1</version>
        /// <language>zh_CN</language>
        public readonly List<float> vertices = new List<float>();

        /// <inheritDoc/>
        /// <private/>
        protected override void _OnClear()
        {
            base._OnClear();

            this.type = BoundingBoxType.Polygon;
            this.x = 0.0f;
            this.y = 0.0f;
            this.vertices.Clear();
        }

        /// <inheritDoc/>
        public override bool ContainsPoint(float pX, float pY)
        {
            var isInSide = false;
            if (pX >= this.x && pX <= this.width && pY >= this.y && pY <= this.height)
            {
                for (int i = 0, l = this.vertices.Count, iP = l - 2; i < l; i += 2)
                {
                    var yA = this.vertices[iP + 1];
                    var yB = this.vertices[i + 1];
                    if ((yB < pY && yA >= pY) || (yA < pY && yB >= pY))
                    {
                        var xA = this.vertices[iP];
                        var xB = this.vertices[i];
                        if ((pY - yB) * (xA - xB) / (yA - yB) + xB < pX)
                        {
                            isInSide = !isInSide;
                        }
                    }

                    iP = i;
                }
            }

            return isInSide;
        }

        /// <inheritDoc/>
        public override int IntersectsSegment(float xA, float yA, float xB, float yB,
                                                Point intersectionPointA = null,
                                                Point intersectionPointB = null,
                                                Point normalRadians = null)
        {
            var intersectionCount = 0;
            if (RectangleBoundingBoxData.RectangleIntersectsSegment(xA, yA, xB, yB, this.x, this.y, this.x + this.width, this.y + this.height, null, null, null) != 0)
            {
                intersectionCount = PolygonBoundingBoxData.PolygonIntersectsSegment
                                                            (
                                                             xA, yA, xB, yB,
                                                             this.vertices,
                                                             intersectionPointA, intersectionPointB, normalRadians
                                                            );
            }

            return intersectionCount;
        }
    }
}