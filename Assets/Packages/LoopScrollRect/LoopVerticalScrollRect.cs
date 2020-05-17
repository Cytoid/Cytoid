using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEditor;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Loop Vertical Scroll Rect", 51)]
    [DisallowMultipleComponent]
    public class LoopVerticalScrollRect : LoopScrollRect
    {
        public LayoutGroup contentLayoutGroup; // EDIT: Cytoid
        public Bounds viewBounds;
        public Bounds contentBounds;

        private RectTransform rectTransform;
        
        protected override float GetSize(RectTransform item)
        {
            float size = contentSpacing;
            if (m_GridLayout != null)
            {
                size += m_GridLayout.cellSize.y;
            }
            else
            {
                size += LayoutUtility.GetPreferredHeight(item);
            }
            return size;
        }

        protected override float GetDimension(Vector2 vector)
        {
            return vector.y;
        }

        protected override Vector2 GetVector(float value)
        {
            return new Vector2(0, value);
        }

        protected override void Awake()
        {
            base.Awake();
            directionSign = -1;

            GridLayoutGroup layout = content.GetComponent<GridLayoutGroup>();
            if (layout != null && layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
            {
                Debug.LogError("[LoopHorizontalScrollRect] unsupported GridLayoutGroup constraint");
            }
            
            // EDIT: Cytoid
            contentLayoutGroup = content.GetComponent<LayoutGroup>();
            rectTransform = GetComponent<RectTransform>();
            // End of EDIT
        }
        
        // EDIT: Cytoid
        public override float GetTopPadding()
        {
            return contentLayoutGroup?.padding.top ?? 0;
        }

        public override float GetBottomPadding()
        {
            return contentLayoutGroup?.padding.bottom ?? 0;
        }
        // End of EDIT

        protected override bool UpdateItems(Bounds viewBounds, Bounds contentBounds)
        {
            bool changed = false;

            // EDIT: Cytoid
            this.viewBounds = viewBounds;
            this.contentBounds = contentBounds;
            float dBottom = GetBottomPadding(), dTop = GetTopPadding();
            float selfBottom = rectTransform.offsetMin.y, selfTop = -rectTransform.offsetMax.y;
            // End of EDIT

            if (viewBounds.min.y < contentBounds.min.y + (dBottom + selfBottom) * thresholdMultiplier) // EDIT: Cytoid
            {
                float size = NewItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.min.y < contentBounds.min.y + (dBottom + selfBottom) * thresholdMultiplier - totalSize)
                {
                    size = NewItemAtEnd();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (viewBounds.max.y > contentBounds.max.y - (dTop + selfTop) * thresholdMultiplier) // EDIT: Cytoid
            {
                float size = NewItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.max.y > contentBounds.max.y - (dTop + selfTop) * thresholdMultiplier + totalSize)
                {
                    size = NewItemAtStart();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (viewBounds.min.y > contentBounds.min.y + (dBottom + selfBottom) * thresholdMultiplier + threshold)
            {
                float size = DeleteItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.min.y > contentBounds.min.y + (dBottom + selfBottom) + threshold + totalSize)
                {
                    size = DeleteItemAtEnd();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            if (viewBounds.max.y < contentBounds.max.y - (dTop + selfTop) * thresholdMultiplier - threshold)
            {
                float size = DeleteItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.max.y < contentBounds.max.y - (dTop + selfTop) - threshold - totalSize)
                {
                    size = DeleteItemAtStart();
                    totalSize += size;
                }
                if (totalSize > 0)
                    changed = true;
            }

            return changed;
        }
    }
    
    /* Cytoid: start */
#if UNITY_EDITOR
    [CustomEditor(typeof(LoopVerticalScrollRect))]
    public class LoopVerticalScrollRectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                var t = (LoopVerticalScrollRect) target;
                GUILayout.Label($"ViewBounds: ({t.viewBounds.min.y}, {t.viewBounds.max.y})");
                GUILayout.Label($"ContentBounds: ({t.contentBounds.min.y}, {t.contentBounds.max.y})");
                GUILayout.Label($"Top/Bottom padding: ({t.GetTopPadding()}, {t.GetBottomPadding()})");
                GUILayout.Label($"contentBounds.max.y - dTop * thresholdMultiplier: {(t.contentBounds.max.y - t.GetTopPadding() * t.thresholdMultiplier)}");
                GUILayout.Label("VerticalNormalizedPosition: " + t.verticalNormalizedPosition);
                if (GUILayout.Button("Test"))
                {
                    t.verticalNormalizedPosition = t.verticalNormalizedPosition;
                }
                EditorUtility.SetDirty(target);
            }
        }
    }
#endif
    /* Cytoid: end */
}