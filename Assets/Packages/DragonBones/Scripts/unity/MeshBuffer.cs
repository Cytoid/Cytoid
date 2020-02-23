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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragonBones;

namespace DragonBones
{
    //[Serializable]
    public class MeshBuffer : IDisposable
    {
        public readonly List<UnitySlot> combineSlots = new List<UnitySlot>();
        public string name;
        public Mesh sharedMesh;
        public int vertexCount;
        public Vector3[] rawVertextBuffers;
        public Vector2[] uvBuffers;
        public Vector3[] vertexBuffers;
        public Color32[] color32Buffers;
        public int[] triangleBuffers;

        public bool vertexDirty;
        public bool zorderDirty;
        public bool enabled;

        public static Mesh GenerateMesh()
        {
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            mesh.MarkDynamic();

            return mesh;
        }

        private static int _OnSortSlots(Slot a, Slot b)
        {
            if(a._zOrder > b._zOrder)
            {
                return 1;
            }
            else if(a._zOrder < b._zOrder)
            {
                return -1;
            }

            return 0;
        }

        public void Dispose()
        {
            if (this.sharedMesh != null)
            {
                UnityFactoryHelper.DestroyUnityObject(this.sharedMesh);
            }

            this.combineSlots.Clear();
            this.name = string.Empty;
            this.sharedMesh = null;
            this.vertexCount = 0;
            this.rawVertextBuffers = null;
            this.uvBuffers = null;
            this.vertexBuffers = null;
            this.color32Buffers = null;
            this.vertexDirty = false;
            this.enabled = false;
        }

        public void Clear()
        {
            if (this.sharedMesh != null)
            {
                this.sharedMesh.Clear();
                this.sharedMesh.uv = null;
                this.sharedMesh.vertices = null;
                this.sharedMesh.normals = null;
                this.sharedMesh.triangles = null;
                this.sharedMesh.colors32 = null;
            }

            this.name = string.Empty;
        }

        public void CombineMeshes(CombineInstance[] combines)
        {
            if (this.sharedMesh == null)
            {
                this.sharedMesh = GenerateMesh();
            }

            this.sharedMesh.CombineMeshes(combines);

            //
            this.uvBuffers = this.sharedMesh.uv;
            this.rawVertextBuffers = this.sharedMesh.vertices;
            this.vertexBuffers = this.sharedMesh.vertices;
            this.color32Buffers = this.sharedMesh.colors32;
            this.triangleBuffers = this.sharedMesh.triangles;

            this.vertexCount = this.vertexBuffers.Length;
            //
            if (this.color32Buffers == null || this.color32Buffers.Length != this.vertexCount)
            {
                this.color32Buffers = new Color32[vertexCount];
            }
        }

        public void InitMesh()
        {
            if (this.vertexBuffers != null)
            {
                this.vertexCount = this.vertexBuffers.Length;
            }
            else
            {
                this.vertexCount = 0;
            }

            if (this.color32Buffers == null || this.color32Buffers.Length != this.vertexCount)
            {
                this.color32Buffers = new Color32[this.vertexCount];
            }

            this.sharedMesh.vertices = this.vertexBuffers;// Must set vertices before uvs.
            this.sharedMesh.uv = this.uvBuffers;
            this.sharedMesh.colors32 = this.color32Buffers;
            this.sharedMesh.triangles = this.triangleBuffers;
            this.sharedMesh.RecalculateBounds();

            this.enabled = true;
        }

        public void UpdateVertices()
        {
            this.sharedMesh.vertices = this.vertexBuffers;
            this.sharedMesh.RecalculateBounds();
        }

        public void UpdateColors()
        {
            this.sharedMesh.colors32 = this.color32Buffers;
        }

        public void UpdateOrder()
        {
            this.combineSlots.Sort(_OnSortSlots);

            var index = 0;
            var newVerticeIndex = 0;
            var oldVerticeOffset = 0;

            var newUVs = new Vector2[this.vertexCount];
            var newVertices = new Vector3[this.vertexCount];
            var newColors = new Color32[this.vertexCount];
            CombineInstance[] combines = new CombineInstance[this.combineSlots.Count];
            for (int i = 0; i < combineSlots.Count; i++)
            {
                var slot = combineSlots[i] as UnitySlot;
                oldVerticeOffset = slot._verticeOffset;

                //重新赋值
                slot._verticeOrder = i;
                slot._verticeOffset = newVerticeIndex;
                //
                CombineInstance com = new CombineInstance();
                slot._meshBuffer.InitMesh();
                com.mesh = slot._meshBuffer.sharedMesh;

                combines[i] = com;

                //
                var zspace = (slot._armature.proxy as UnityArmatureComponent).zSpace;
                for (int j = 0; j < slot._meshBuffer.vertexCount; j++)
                {
                    index = oldVerticeOffset + j;
                    newUVs[newVerticeIndex] = this.uvBuffers[index];
                    newVertices[newVerticeIndex] = this.vertexBuffers[index];
                    newColors[newVerticeIndex] = this.color32Buffers[index];

                    newVertices[newVerticeIndex].z = -slot._verticeOrder * (zspace + UnitySlot.Z_OFFSET);

                    newVerticeIndex++;
                }
            }

            //
            this.sharedMesh.Clear();
            this.sharedMesh.CombineMeshes(combines);
            //
            this.uvBuffers = newUVs;
            this.vertexBuffers = newVertices;
            this.color32Buffers = newColors;

            this.triangleBuffers = this.sharedMesh.triangles;

            this.InitMesh();
        }
    }
}