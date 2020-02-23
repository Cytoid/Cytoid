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
﻿using UnityEngine;
using UnityEngine.UI;

namespace DragonBones
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode,RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    public class UnityUGUIDisplay : MaskableGraphic
    {
        [HideInInspector]
        public Mesh sharedMesh;

        private Texture _texture;
        public override Texture mainTexture
        {
            get { return _texture == null ? material.mainTexture : _texture; }
        }

        /// <summary>
        /// Texture to be used.
        /// </summary>
        public Texture texture
        {
            get    { return _texture; }
            set
            {
                if (_texture == value)
                {
                    return;
                }
                
                _texture = value;
                SetMaterialDirty();
            }
        }

        protected override void OnPopulateMesh (VertexHelper vh)
        {
            vh.Clear();
        }

        public override void Rebuild (CanvasUpdate update)
        {
            base.Rebuild(update);
            if (canvasRenderer.cull)
            {
                return;
            }

            if (update == CanvasUpdate.PreRender)
            {
                canvasRenderer.SetMesh(sharedMesh);
            }
        }

        void Update()
        {
            canvasRenderer.SetMesh(sharedMesh);
        }
    }
}