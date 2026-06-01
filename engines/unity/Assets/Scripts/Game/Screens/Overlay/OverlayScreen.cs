using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverlayScreen : Screen
{
    public const string Id = "Overlay";
    private const string OffsetAdjustName = "OffsetAdjust";

    private readonly List<RectTransformSnapshot> childSnapshots = new List<RectTransformSnapshot>();
    private CanvasScaler canvasScaler;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastCanvasScale;
    private bool lastSafeAreaEnabled = true;
    private bool hasAppliedSafeArea;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        CacheChildren();
        ApplySafeArea(true);
        base.OnScreenInitialized();
    }

    private void LateUpdate()
    {
        ApplySafeArea(false);
    }

    private void CacheChildren()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        childSnapshots.Clear();
        foreach (Transform child in transform)
        {
            if (child is RectTransform rectTransform)
            {
                childSnapshots.Add(new RectTransformSnapshot(rectTransform));
            }
        }

        var offsetAdjust = GameObject.Find(OffsetAdjustName);
        if (offsetAdjust != null && offsetAdjust.transform is RectTransform offsetAdjustRectTransform)
        {
            childSnapshots.Add(new RectTransformSnapshot(offsetAdjustRectTransform));
        }
    }

    private void ApplySafeArea(bool forceTransitionDefaults)
    {
        if (UnityEngine.Screen.width <= 0 || UnityEngine.Screen.height <= 0)
        {
            return;
        }

        if (childSnapshots.Count == 0)
        {
            CacheChildren();
        }

        var safeArea = UnityEngine.Screen.safeArea;
        var screenSize = new Vector2Int(UnityEngine.Screen.width, UnityEngine.Screen.height);
        var canvasScale = GetCanvasScale();
        var safeAreaEnabled = Context.Player?.Settings?.AdaptOverlayToSafeArea ?? true;
        if (hasAppliedSafeArea && safeArea == lastSafeArea && screenSize == lastScreenSize &&
            Mathf.Approximately(canvasScale, lastCanvasScale) && safeAreaEnabled == lastSafeAreaEnabled)
        {
            return;
        }

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastCanvasScale = canvasScale;
        lastSafeAreaEnabled = safeAreaEnabled;
        hasAppliedSafeArea = true;

        var insets = safeAreaEnabled ? GetSafeAreaInsets(safeArea, canvasScale) : Vector4.zero;

        foreach (var snapshot in childSnapshots)
        {
            snapshot.Apply(insets, canvasScaler != null ? canvasScaler.referenceResolution.x : 0);
        }

        RefreshTransitionDefaults(forceTransitionDefaults);
    }

    private Vector4 GetSafeAreaInsets(Rect safeArea, float canvasScale)
    {
        var left = safeArea.xMin / canvasScale;
        var right = (UnityEngine.Screen.width - safeArea.xMax) / canvasScale;
        var top = (UnityEngine.Screen.height - safeArea.yMax) / canvasScale;
        var bottom = safeArea.yMin / canvasScale;
        var horizontal = Mathf.Min(Mathf.Max(left, right), UnityEngine.Screen.width * 0.5f / canvasScale);
        return new Vector4(horizontal, bottom, horizontal, top);
    }

    private float GetCanvasScale()
    {
        if (canvasScaler == null || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            return canvasScaler != null ? Mathf.Max(canvasScaler.scaleFactor, 0.0001f) : 1f;
        }

        var referenceResolution = canvasScaler.referenceResolution;
        if (referenceResolution.x <= 0 || referenceResolution.y <= 0)
        {
            return 1f;
        }

        var widthScale = UnityEngine.Screen.width / referenceResolution.x;
        var heightScale = UnityEngine.Screen.height / referenceResolution.y;
        switch (canvasScaler.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.Expand:
                return Mathf.Min(widthScale, heightScale);
            case CanvasScaler.ScreenMatchMode.Shrink:
                return Mathf.Max(widthScale, heightScale);
            default:
                var logWidth = Mathf.Log(widthScale, 2);
                var logHeight = Mathf.Log(heightScale, 2);
                return Mathf.Pow(2, Mathf.Lerp(logWidth, logHeight, canvasScaler.matchWidthOrHeight));
        }
    }

    private void RefreshTransitionDefaults(bool force)
    {
        var transitionElements = new HashSet<TransitionElement>();
        foreach (var snapshot in childSnapshots)
        {
            if (snapshot.RectTransform == null)
            {
                continue;
            }

            foreach (var transitionElement in snapshot.RectTransform.GetComponentsInChildren<TransitionElement>(true))
            {
                transitionElements.Add(transitionElement);
            }
        }

        foreach (var transitionElement in transitionElements)
        {
            if (transitionElement != null && !transitionElement.IsInTransition && (force || transitionElement.IsShown))
            {
                transitionElement.UseCurrentStateAsDefault();
            }
        }
    }

    private class RectTransformSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 pivot;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;

        public RectTransform RectTransform => rectTransform;

        public RectTransformSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            pivot = rectTransform.pivot;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
        }

        public void Apply(Vector4 insets, float referenceWidth)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var position = anchoredPosition;
            var size = sizeDelta;
            var stretchX = IsStretching(anchorMin.x, anchorMax.x);
            var stretchY = IsStretching(anchorMin.y, anchorMax.y);
            if (!stretchX)
            {
                if (IsLeftAnchored(anchorMin.x, anchorMax.x))
                {
                    position.x += insets.x;
                    if (SpansReferenceWidth(referenceWidth))
                    {
                        size.x = Mathf.Max(0, sizeDelta.x - insets.x - insets.z);
                    }
                }
                else if (IsRightAnchored(anchorMin.x, anchorMax.x))
                {
                    position.x -= insets.z;
                }
            }

            if (!stretchY)
            {
                if (IsBottomAnchored(anchorMin.y, anchorMax.y))
                {
                    position.y += insets.y;
                }
                else if (IsTopAnchored(anchorMin.y, anchorMax.y))
                {
                    position.y -= insets.w;
                }
            }

            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            if (stretchX)
            {
                rectTransform.offsetMin = new Vector2(offsetMin.x + insets.x, rectTransform.offsetMin.y);
                rectTransform.offsetMax = new Vector2(offsetMax.x - insets.z, rectTransform.offsetMax.y);
            }

            if (stretchY)
            {
                rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, offsetMin.y + insets.y);
                rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, offsetMax.y - insets.w);
            }
        }

        private bool SpansReferenceWidth(float referenceWidth)
        {
            return referenceWidth > 0 && Mathf.Abs(sizeDelta.x - referenceWidth) <= 1f && Mathf.Approximately(pivot.x, 0);
        }

        private static bool IsStretching(float min, float max) => !Mathf.Approximately(min, max);

        private static bool IsLeftAnchored(float min, float max)
        {
            return Mathf.Approximately(min, max) && Mathf.Approximately(min, 0);
        }

        private static bool IsRightAnchored(float min, float max)
        {
            return Mathf.Approximately(min, max) && Mathf.Approximately(min, 1);
        }

        private static bool IsBottomAnchored(float min, float max)
        {
            return Mathf.Approximately(min, max) && Mathf.Approximately(min, 0);
        }

        private static bool IsTopAnchored(float min, float max)
        {
            return Mathf.Approximately(min, max) && Mathf.Approximately(min, 1);
        }
    }
}
