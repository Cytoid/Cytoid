using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChartEventPresentationKind
{
    None,
    SpeedUp,
    SpeedDown,
    Message
}

public readonly struct ChartEventPresentationState
{
    public static readonly ChartEventPresentationState Empty = new ChartEventPresentationState(
        false,
        ChartEventPresentationKind.None,
        string.Empty,
        Color.white,
        Color.white,
        0,
        0);

    public bool IsActive { get; }
    public ChartEventPresentationKind Kind { get; }
    public string Content { get; }
    public Color TextColor { get; }
    public Color ScanlineColor { get; }
    public float TextAlpha { get; }
    public bool IsTextVisible => TextAlpha > 0;
    public float LetterSpacing { get; }

    public ChartEventPresentationState(
        bool isActive,
        ChartEventPresentationKind kind,
        string content,
        Color textColor,
        Color scanlineColor,
        float textAlpha,
        float letterSpacing)
    {
        IsActive = isActive;
        Kind = kind;
        Content = content;
        TextColor = textColor;
        ScanlineColor = scanlineColor;
        TextAlpha = textAlpha;
        LetterSpacing = letterSpacing;
    }
}

/// <summary>
/// Seek-safe presentation track shared by C2 speed-change and custom-message events.
/// Later events interrupt the previous text and continue the scanline transition from the
/// previously sampled color. Overlapping non-empty message lifetimes share one opacity run,
/// while their content, color, and letter-spacing animations remain event-local. Events at
/// the same time use their authored source order.
/// </summary>
public sealed class ChartEventPresentationTimeline
{
    public const float FadeInDuration = 1f;
    public const float HoldDuration = 3f;
    public const float FadeOutDuration = 1f;
    public const float TotalDuration = FadeInDuration + HoldDuration + FadeOutDuration;
    public const float TextDuration = 1.5f;
    public const float TextFadeDuration = 0.4f;
    public const float TextHoldDuration = TextDuration - TextFadeDuration * 2;
    public const float MaxLetterSpacing = 192f;

    private readonly List<Snapshot> snapshots = new List<Snapshot>();

    public ChartEventPresentationTimeline(ChartModel model, Action<string> warningLogger = null)
    {
        var drafts = new List<Draft>();
        var sequence = 0;
        if (model?.event_order_list == null) return;

        foreach (var order in model.event_order_list)
        {
            if (order?.event_list == null) continue;
            foreach (var chartEvent in order.event_list)
            {
                var currentSequence = sequence++;
                if (chartEvent == null) continue;

                switch ((ChartEventType) chartEvent.type)
                {
                    case ChartEventType.SpeedUp:
                        drafts.Add(new Draft(
                            order.time,
                            ChartEventPresentationKind.SpeedUp,
                            string.Empty,
                            Scanner.SpeedUpColor,
                            currentSequence));
                        break;
                    case ChartEventType.SpeedDown:
                        drafts.Add(new Draft(
                            order.time,
                            ChartEventPresentationKind.SpeedDown,
                            string.Empty,
                            Scanner.SpeedDownColor,
                            currentSequence));
                        break;
                    case ChartEventType.Message:
                        ParseMessage(
                            chartEvent.args,
                            warningLogger,
                            out var content,
                            out var color);
                        drafts.Add(new Draft(
                            order.time,
                            ChartEventPresentationKind.Message,
                            content,
                            color,
                            currentSequence));
                        break;
                }
            }
        }

        drafts.Sort((left, right) =>
        {
            var timeComparison = left.Time.CompareTo(right.Time);
            return timeComparison != 0 ? timeComparison : left.Sequence.CompareTo(right.Sequence);
        });

        foreach (var draft in drafts)
        {
            if (snapshots.Count > 0 && snapshots[snapshots.Count - 1].Time == draft.Time)
            {
                var previous = snapshots[snapshots.Count - 1];
                snapshots[snapshots.Count - 1] = new Snapshot(
                    draft.Time,
                    draft.Kind,
                    draft.Content,
                    previous.StartColor,
                    draft.TargetColor);
                continue;
            }

            var startColor = snapshots.Count == 0
                ? Color.white
                : ResolveScanlineColor(snapshots[snapshots.Count - 1], draft.Time);
            snapshots.Add(new Snapshot(
                draft.Time,
                draft.Kind,
                draft.Content,
                startColor,
                draft.TargetColor));
        }

        BuildMessageOpacityRuns();
    }

    public ChartEventPresentationState Evaluate(float time)
    {
        var index = FindLastAtOrBefore(time);
        if (index < 0) return ChartEventPresentationState.Empty;

        var snapshot = snapshots[index];
        var elapsed = time - snapshot.Time;
        if (elapsed < 0 || elapsed >= TotalDuration) return ChartEventPresentationState.Empty;

        var hasText = snapshot.Kind != ChartEventPresentationKind.Message ||
                      !string.IsNullOrEmpty(snapshot.Content);
        return new ChartEventPresentationState(
            true,
            snapshot.Kind,
            snapshot.Content,
            snapshot.TargetColor,
            ResolveScanlineColor(snapshot, time),
            hasText ? ResolveTextAlpha(snapshot, time) : 0,
            ResolveLetterSpacing(snapshot.Kind, elapsed));
    }

    private void BuildMessageOpacityRuns()
    {
        for (var startIndex = 0; startIndex < snapshots.Count;)
        {
            if (!HasMessageText(snapshots[startIndex]))
            {
                startIndex++;
                continue;
            }

            var endIndex = startIndex;
            while (endIndex + 1 < snapshots.Count &&
                   HasMessageText(snapshots[endIndex + 1]) &&
                   snapshots[endIndex + 1].Time < snapshots[endIndex].Time + TextDuration)
                endIndex++;

            var runStart = snapshots[startIndex].Time;
            var holdStart = Mathf.Max(runStart + TextFadeDuration, snapshots[endIndex].Time);
            var runEnd = holdStart + TextHoldDuration + TextFadeDuration;
            for (var index = startIndex; index <= endIndex; index++)
                snapshots[index] = snapshots[index].WithTextOpacityRun(runStart, runEnd);

            startIndex = endIndex + 1;
        }
    }

    private static bool HasMessageText(Snapshot snapshot) =>
        snapshot.Kind == ChartEventPresentationKind.Message &&
        !string.IsNullOrEmpty(snapshot.Content);

    private int FindLastAtOrBefore(float time)
    {
        var low = 0;
        var high = snapshots.Count - 1;
        var result = -1;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            if (snapshots[middle].Time <= time)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }
        return result;
    }

    private static Color ResolveScanlineColor(Snapshot snapshot, float time)
    {
        var elapsed = time - snapshot.Time;
        if (elapsed < 0 || elapsed >= TotalDuration) return Color.white;
        if (elapsed < FadeInDuration)
            return Color.Lerp(snapshot.StartColor, snapshot.TargetColor, elapsed / FadeInDuration);
        if (elapsed < FadeInDuration + HoldDuration) return snapshot.TargetColor;
        return Color.Lerp(
            snapshot.TargetColor,
            Color.white,
            (elapsed - FadeInDuration - HoldDuration) / FadeOutDuration);
    }

    private static float ResolveTextAlpha(Snapshot snapshot, float time)
    {
        if (snapshot.Kind != ChartEventPresentationKind.Message)
            return ResolveEventTextAlpha(time - snapshot.Time);

        var elapsed = time - snapshot.TextOpacityRunStart;
        var duration = snapshot.TextOpacityRunEnd - snapshot.TextOpacityRunStart;
        return ResolveEventTextAlpha(elapsed, duration);
    }

    private static float ResolveEventTextAlpha(float elapsed, float duration = TextDuration)
    {
        if (elapsed <= 0 || elapsed >= duration) return 0;
        if (elapsed < TextFadeDuration)
            return EaseOutCubic(elapsed / TextFadeDuration);

        var fadeOutStart = duration - TextFadeDuration;
        if (elapsed < fadeOutStart) return 1;
        return 1 - EaseOutCubic((elapsed - fadeOutStart) / TextFadeDuration);
    }

    private static float ResolveLetterSpacing(ChartEventPresentationKind kind, float elapsed)
    {
        var progress = Mathf.Clamp01(elapsed / TextDuration);
        if (kind == ChartEventPresentationKind.Message)
            return MaxLetterSpacing * progress * progress * progress * progress * progress; // InQuint

        var eased = Mathf.Sqrt(1 - (progress - 1) * (progress - 1)); // OutCirc
        return kind == ChartEventPresentationKind.SpeedDown
            ? Mathf.Lerp(MaxLetterSpacing, 0, eased)
            : Mathf.Lerp(0, MaxLetterSpacing, eased);
    }

    private static float EaseOutCubic(float progress)
    {
        progress = Mathf.Clamp01(progress);
        var inverse = 1 - progress;
        return 1 - inverse * inverse * inverse;
    }

    private static void ParseMessage(
        string args,
        Action<string> warningLogger,
        out string content,
        out Color color)
    {
        args = args ?? string.Empty;
        var separator = args.IndexOf(',');
        content = TmpRichTextSanitizer.Sanitize(
            separator < 0 ? args : args.Substring(0, separator));
        var colorText = separator < 0 ? string.Empty : args.Substring(separator + 1).Trim();

        if (IsRgbHexColor(colorText) && ColorUtility.TryParseHtmlString(colorText, out color)) return;

        color = Color.white;
        if (colorText.Length == 0) return;
        warningLogger?.Invoke(
            $"Invalid C2 message color '{colorText}'. Expected #RRGGBB; using white.");
    }

    private static bool IsRgbHexColor(string value)
    {
        if (value == null || value.Length != 7 || value[0] != '#') return false;
        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!((c >= '0' && c <= '9') ||
                  (c >= 'a' && c <= 'f') ||
                  (c >= 'A' && c <= 'F'))) return false;
        }
        return true;
    }

    private readonly struct Draft
    {
        public float Time { get; }
        public ChartEventPresentationKind Kind { get; }
        public string Content { get; }
        public Color TargetColor { get; }
        public int Sequence { get; }

        public Draft(
            float time,
            ChartEventPresentationKind kind,
            string content,
            Color targetColor,
            int sequence)
        {
            Time = time;
            Kind = kind;
            Content = content;
            TargetColor = targetColor;
            Sequence = sequence;
        }
    }

    private readonly struct Snapshot
    {
        public float Time { get; }
        public ChartEventPresentationKind Kind { get; }
        public string Content { get; }
        public Color StartColor { get; }
        public Color TargetColor { get; }
        public float TextOpacityRunStart { get; }
        public float TextOpacityRunEnd { get; }

        public Snapshot(
            float time,
            ChartEventPresentationKind kind,
            string content,
            Color startColor,
            Color targetColor)
        {
            Time = time;
            Kind = kind;
            Content = content;
            StartColor = startColor;
            TargetColor = targetColor;
            TextOpacityRunStart = time;
            TextOpacityRunEnd = time + TextDuration;
        }

        private Snapshot(
            float time,
            ChartEventPresentationKind kind,
            string content,
            Color startColor,
            Color targetColor,
            float textOpacityRunStart,
            float textOpacityRunEnd)
        {
            Time = time;
            Kind = kind;
            Content = content;
            StartColor = startColor;
            TargetColor = targetColor;
            TextOpacityRunStart = textOpacityRunStart;
            TextOpacityRunEnd = textOpacityRunEnd;
        }

        public Snapshot WithTextOpacityRun(float start, float end) => new Snapshot(
            Time,
            Kind,
            Content,
            StartColor,
            TargetColor,
            start,
            end);
    }
}
