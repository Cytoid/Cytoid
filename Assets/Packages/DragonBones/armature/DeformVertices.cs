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
namespace DragonBones
{
    /// <internal/>
    public class DeformVertices : BaseObject
    {
        public bool verticesDirty;
        public readonly List<float> vertices = new List<float>();
        public readonly List<Bone> bones = new List<Bone>();
        public VerticesData verticesData;

        protected override void _OnClear()
        {
            this.verticesDirty = false;
            this.vertices.Clear();
            this.bones.Clear();
            this.verticesData = null;
        }

        public void init(VerticesData verticesDataValue, Armature armature)
        {
            this.verticesData = verticesDataValue;

            if (this.verticesData != null)
            {
                var vertexCount = 0;
                if (this.verticesData.weight != null)
                {
                    vertexCount = this.verticesData.weight.count * 2;
                }
                else
                {
                    vertexCount = (int)this.verticesData.data.intArray[this.verticesData.offset + (int)BinaryOffset.MeshVertexCount] * 2;
                }

                this.verticesDirty = true;
                this.vertices.ResizeList(vertexCount);
                this.bones.Clear();
                //
                for (int i = 0, l = this.vertices.Count; i < l; ++i)
                {
                    this.vertices[i] = 0.0f;
                }

                if (this.verticesData.weight != null)
                {
                    for (int i = 0, l = this.verticesData.weight.bones.Count; i < l; ++i)
                    {
                        var bone = armature.GetBone(this.verticesData.weight.bones[i].name);
                        this.bones.Add(bone);
                    }
                }
            }
            else
            {
                this.verticesDirty = false;
                this.vertices.Clear();
                this.bones.Clear();
                this.verticesData = null;
            }
        }

        public bool isBonesUpdate()
        {
            foreach (var bone in this.bones)
            {
                if (bone != null && bone._childrenTransformDirty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
