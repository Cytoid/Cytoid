using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies seek-safe C2 UI event state after storyboard has updated for the frame.
/// Canvas targets are deliberately below the storyboard UI canvas, so their alpha values multiply.
/// </summary>
public sealed class GameUiEventController
{
    private readonly Game game;
    private readonly ChartUiEventTimeline timeline;
    private readonly Action<string> warningLogger;
    private readonly Dictionary<ChartUiTarget, CanvasTarget> canvasTargets =
        new Dictionary<ChartUiTarget, CanvasTarget>();

    private bool active = true;

    public GameUiEventController(Game game, Action<string> warningLogger = null)
    {
        this.game = game;
        this.warningLogger = warningLogger ?? Debug.LogWarning;
        timeline = new ChartUiEventTimeline(game.Chart.Model, this.warningLogger);
        ResolveTargets();

        // Establish only the initial state before the overlay enters. Events authored at tick zero
        // must not become visible during the pre-song countdown.
        ApplyCanvasTargets(float.NegativeInfinity, false);

        var boundary = timeline.Evaluate(ChartUiTarget.BoundaryLine, float.NegativeInfinity);
        game.Renderer.SetUiEventOpacity(boundary.Alpha, boundary.AnimationAlpha);

        var scanner = Scanner.Instance;
        if (scanner != null)
        {
            var scanline = timeline.Evaluate(ChartUiTarget.Scanline, float.NegativeInfinity);
            scanner.SetUiEventState(
                scanline.Alpha,
                scanline.AnimationAlpha,
                scanline.AnimationKind,
                EaseScanlineProgress(scanline.AnimationProgress),
                false);
        }

        game.onGameCompleted.AddListener(_ => Release());
        game.onGameFailed.AddListener(_ => Release());
        game.onGameBeforeExit.AddListener(_ => Release());
        game.onGameDisposed.AddListener(_ => Release());
    }

    public void Apply(float time)
    {
        if (!active) return;

        ApplyCanvasTargets(time, game.State != null && game.State.IsStarted);

        var boundary = timeline.Evaluate(ChartUiTarget.BoundaryLine, time);
        game.Renderer.SetUiEventOpacity(boundary.Alpha, boundary.AnimationAlpha);

        var scanner = Scanner.Instance;
        if (scanner != null)
        {
            var scanline = timeline.Evaluate(ChartUiTarget.Scanline, time);
            scanner.SetUiEventState(
                scanline.Alpha,
                scanline.AnimationAlpha,
                scanline.AnimationKind,
                EaseScanlineProgress(scanline.AnimationProgress));
        }
    }

    public void ApplyBeforeGameUpdate(float time)
    {
        if (!active) return;
        var scanner = Scanner.Instance;
        if (scanner == null) return;
        var scanline = timeline.Evaluate(ChartUiTarget.Scanline, time);
        scanner.SetUiEventOpacity(scanline.Alpha, scanline.AnimationAlpha);
    }

    public void Release()
    {
        if (!active) return;
        active = false;
    }

    private void ResolveTargets()
    {
        var overlay = game.levelInfoParent != null ? game.levelInfoParent.transform.parent : null;

        AddCanvasTarget(ChartUiTarget.Combo, FindDescendant(overlay, "Combo"), Vector2.zero);
        AddCanvasTarget(ChartUiTarget.Score, FindPerformanceTarget(overlay), Vector2.zero);

        var left = game.levelInfoParent != null
            ? FindDescendant(game.levelInfoParent.transform, "LevelInfo") ?? game.levelInfoParent.transform
            : null;
        AddCanvasTarget(ChartUiTarget.LeftUi, left, Vector2.left);

        var right = game.modHolderParent != null
            ? FindDescendant(game.modHolderParent.transform, "Mods") ?? game.modHolderParent.transform
            : null;
        AddCanvasTarget(ChartUiTarget.RightUi, right, Vector2.right);

        AddCanvasTarget(ChartUiTarget.ProgressBar, FindDescendant(overlay, "ProgressIndicator"), Vector2.zero);
    }

    private static Transform FindPerformanceTarget(Transform overlay)
    {
        var score = FindDescendant(overlay, "Score");
        if (score?.parent != null && FindDescendant(score.parent, "Accuracy") != null)
            return score.parent;
        return score;
    }

    private void AddCanvasTarget(ChartUiTarget target, Transform transform, Vector2 animationDirection)
    {
        if (!(transform is RectTransform rectTransform))
        {
            warningLogger?.Invoke($"Could not resolve C2 UI canvas target '{target}'.");
            return;
        }
        var canvasGroup = transform.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = transform.gameObject.AddComponent<CanvasGroup>();
        canvasTargets[target] = new CanvasTarget(canvasGroup, rectTransform, animationDirection);
    }

    private void ApplyCanvasTargets(float time, bool capturePosition)
    {
        foreach (var pair in canvasTargets)
        {
            var state = timeline.Evaluate(pair.Key, time);
            pair.Value.Apply(state, EaseSlideProgress(state), capturePosition);
        }
    }

    private static float EaseSlideProgress(ChartUiTargetState state)
    {
        var progress = Mathf.Clamp01(state.AnimationProgress);
        if (state.AnimationKind == ChartUiAnimationKind.In)
        {
            var inverse = 1 - progress;
            return 1 - inverse * inverse * inverse; // OutCubic
        }
        if (state.AnimationKind == ChartUiAnimationKind.Out) return progress * progress * progress; // InCubic
        return 1;
    }

    private static float EaseScanlineProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        return progress * progress * progress * (progress * (progress * 6 - 15) + 10);
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        if (parent == null) return null;
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var nested = FindDescendant(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private sealed class CanvasTarget
    {
        private const float SlideDistance = 128f;

        private readonly CanvasGroup canvasGroup;
        private readonly RectTransform rectTransform;
        private readonly float baseAlpha;
        private readonly Vector2 animationDirection;

        private Vector2 basePosition;
        private bool positionCaptured;

        public CanvasTarget(CanvasGroup canvasGroup, RectTransform rectTransform, Vector2 animationDirection)
        {
            this.canvasGroup = canvasGroup;
            this.rectTransform = rectTransform;
            this.animationDirection = animationDirection;
            baseAlpha = canvasGroup.alpha;
        }

        public void Apply(ChartUiTargetState state, float easedProgress, bool capturePosition)
        {
            canvasGroup.alpha = baseAlpha * state.Alpha * state.AnimationAlpha;

            if (!positionCaptured && capturePosition)
            {
                basePosition = rectTransform.anchoredPosition;
                positionCaptured = true;
            }
            if (!positionCaptured) return;

            var offsetProgress = 0f;
            if (state.AnimationKind == ChartUiAnimationKind.In) offsetProgress = 1 - easedProgress;
            else if (state.AnimationKind == ChartUiAnimationKind.Out) offsetProgress = easedProgress;
            rectTransform.anchoredPosition = basePosition + animationDirection * (SlideDistance * offsetProgress);
        }
    }
}
