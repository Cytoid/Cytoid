using System;
using System.Collections.Generic;

public enum ChartEventType
{
    SpeedUp = 0,
    SpeedDown = 1,
    ShowUi = 2,
    HideUi = 3,
    FadeInUi = 4,
    FadeOutUi = 5,
    AnimationInUi = 6,
    AnimationOutUi = 7,
    Message = 8
}

public enum ChartUiTarget
{
    Combo = 0,
    Score = 1,
    LeftUi = 2,
    RightUi = 3,
    Scanline = 4,
    BoundaryLine = 5,
    Spectrum = 6,
    ProgressBar = 7
}

public enum ChartUiAnimationKind
{
    None,
    In,
    Out
}

public readonly struct ChartUiTargetState
{
    public float Alpha { get; }
    public float AnimationAlpha { get; }
    public float AnimationProgress { get; }
    public ChartUiAnimationKind AnimationKind { get; }

    public ChartUiTargetState(
        float alpha,
        float animationAlpha,
        float animationProgress,
        ChartUiAnimationKind animationKind)
    {
        Alpha = alpha;
        AnimationAlpha = animationAlpha;
        AnimationProgress = animationProgress;
        AnimationKind = animationKind;
    }
}

public static class ChartEventCursor
{
    public static int FindFirstAfter(IReadOnlyList<ChartModel.EventOrder> events, float time)
    {
        var low = 0;
        var high = events.Count;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (events[middle].time <= time) low = middle + 1;
            else high = middle;
        }
        return low;
    }
}

/// <summary>
/// Precomputed, seek-safe C2 UI event tracks. Conflict behavior intentionally mirrors Cylheim:
/// every new alpha event starts at its authored time using the value of the previous snapshot at
/// that time, while animation-out contributes a delayed hide at the end of its animation.
/// </summary>
public sealed class ChartUiEventTimeline
{
    public const float FadeDuration = 1.3f;
    private const float BlinkStepDuration = 0.033f;

    private static readonly ChartUiTarget[] SupportedTargets =
    {
        ChartUiTarget.Combo,
        ChartUiTarget.Score,
        ChartUiTarget.LeftUi,
        ChartUiTarget.RightUi,
        ChartUiTarget.Scanline,
        ChartUiTarget.BoundaryLine,
        ChartUiTarget.ProgressBar
    };

    private readonly Dictionary<ChartUiTarget, float> initialAlpha = new Dictionary<ChartUiTarget, float>();
    private readonly Dictionary<ChartUiTarget, List<AlphaSnapshot>> alphaSnapshots =
        new Dictionary<ChartUiTarget, List<AlphaSnapshot>>();
    private readonly Dictionary<ChartUiTarget, List<AnimationSnapshot>> animationSnapshots =
        new Dictionary<ChartUiTarget, List<AnimationSnapshot>>();

    public ChartUiEventTimeline(ChartModel model, Action<string> warningLogger = null)
    {
        var initial = model.is_start_without_ui ? 0f : 1f;
        var alphaDrafts = new Dictionary<ChartUiTarget, List<AlphaDraft>>();
        var animationDrafts = new Dictionary<ChartUiTarget, List<AnimationSnapshot>>();
        foreach (var target in SupportedTargets)
        {
            initialAlpha[target] = initial;
            alphaDrafts[target] = new List<AlphaDraft>();
            animationDrafts[target] = new List<AnimationSnapshot>();
        }

        var sequence = 0;
        foreach (var order in model.event_order_list)
        {
            if (order?.event_list == null) continue;
            foreach (var chartEvent in order.event_list)
            {
                var currentSequence = sequence++;
                if (chartEvent == null || chartEvent.type < (int) ChartEventType.ShowUi ||
                    chartEvent.type > (int) ChartEventType.AnimationOutUi)
                    continue;

                var type = (ChartEventType) chartEvent.type;
                foreach (var target in ParseTargets(chartEvent.args, warningLogger))
                {
                    switch (type)
                    {
                        case ChartEventType.ShowUi:
                            alphaDrafts[target].Add(new AlphaDraft(order.time, 0, 1, currentSequence));
                            break;
                        case ChartEventType.HideUi:
                            alphaDrafts[target].Add(new AlphaDraft(order.time, 0, 0, currentSequence));
                            break;
                        case ChartEventType.FadeInUi:
                            alphaDrafts[target].Add(new AlphaDraft(order.time, FadeDuration, 1, currentSequence));
                            break;
                        case ChartEventType.FadeOutUi:
                            alphaDrafts[target].Add(new AlphaDraft(order.time, FadeDuration, 0, currentSequence));
                            break;
                        case ChartEventType.AnimationInUi:
                            alphaDrafts[target].Add(new AlphaDraft(order.time, 0, 1, currentSequence));
                            animationDrafts[target].Add(
                                new AnimationSnapshot(
                                    order.time,
                                    GetAnimationDuration(target, ChartUiAnimationKind.In),
                                    ChartUiAnimationKind.In,
                                    currentSequence));
                            break;
                        case ChartEventType.AnimationOutUi:
                            var duration = GetAnimationDuration(target, ChartUiAnimationKind.Out);
                            alphaDrafts[target].Add(
                                new AlphaDraft(order.time + duration, 0, 0, currentSequence));
                            animationDrafts[target].Add(
                                new AnimationSnapshot(
                                    order.time,
                                    duration,
                                    ChartUiAnimationKind.Out,
                                    currentSequence));
                            break;
                    }
                }
            }
        }

        foreach (var target in SupportedTargets)
        {
            var drafts = alphaDrafts[target];
            drafts.Sort(CompareDrafts);
            var snapshots = new List<AlphaSnapshot>(drafts.Count);
            AlphaSnapshot previous = null;
            foreach (var draft in drafts)
            {
                var startAlpha = previous == null
                    ? initialAlpha[target]
                    : ResolveAlpha(previous, draft.Time);
                previous = new AlphaSnapshot(
                    draft.Time,
                    draft.Duration,
                    startAlpha,
                    draft.TargetAlpha,
                    draft.Sequence);
                snapshots.Add(previous);
            }
            alphaSnapshots[target] = snapshots;

            var animations = animationDrafts[target];
            animations.Sort(CompareAnimations);
            animationSnapshots[target] = animations;
        }
    }

    public ChartUiTargetState Evaluate(ChartUiTarget target, float time)
    {
        if (!initialAlpha.ContainsKey(target)) return new ChartUiTargetState(1, 1, 1, ChartUiAnimationKind.None);

        var alpha = initialAlpha[target];
        var alphaIndex = FindLastAtOrBefore(alphaSnapshots[target], time, snapshot => snapshot.Time);
        if (alphaIndex >= 0) alpha = ResolveAlpha(alphaSnapshots[target][alphaIndex], time);

        var animationAlpha = 1f;
        var animationProgress = 1f;
        var animationKind = ChartUiAnimationKind.None;
        var animationIndex = FindLastAtOrBefore(animationSnapshots[target], time, snapshot => snapshot.Time);
        if (animationIndex >= 0)
        {
            var snapshot = animationSnapshots[target][animationIndex];
            animationKind = snapshot.Kind;
            animationProgress = snapshot.Duration <= 0
                ? 1
                : Clamp01((time - snapshot.Time) / snapshot.Duration);
            animationAlpha = ResolveAnimationAlpha(
                target,
                animationKind,
                Math.Max(0, time - snapshot.Time),
                snapshot.Duration);
        }

        return new ChartUiTargetState(Clamp01(alpha), Clamp01(animationAlpha), animationProgress, animationKind);
    }

    private static IEnumerable<ChartUiTarget> ParseTargets(string args, Action<string> warningLogger)
    {
        if (string.IsNullOrWhiteSpace(args)) return SupportedTargets;

        var result = new List<ChartUiTarget>();
        var seen = new HashSet<ChartUiTarget>();
        foreach (var raw in args.Split(','))
        {
            if (!int.TryParse(raw.Trim(), out var value) || value < 0 || value > 7)
            {
                warningLogger?.Invoke($"Ignoring invalid C2 UI target '{raw}'.");
                continue;
            }

            var target = (ChartUiTarget) value;
            if (target == ChartUiTarget.Spectrum) continue;
            if (seen.Add(target)) result.Add(target);
        }
        return result;
    }

    private static int CompareDrafts(AlphaDraft left, AlphaDraft right)
    {
        var time = left.Time.CompareTo(right.Time);
        return time != 0 ? time : left.Sequence.CompareTo(right.Sequence);
    }

    private static int CompareAnimations(AnimationSnapshot left, AnimationSnapshot right)
    {
        var time = left.Time.CompareTo(right.Time);
        return time != 0 ? time : left.Sequence.CompareTo(right.Sequence);
    }

    private static float GetAnimationDuration(ChartUiTarget target, ChartUiAnimationKind kind)
    {
        switch (target)
        {
            case ChartUiTarget.Combo:
                return 0.198f;
            case ChartUiTarget.Score:
                return 1.198f;
            case ChartUiTarget.LeftUi:
                return 1f;
            case ChartUiTarget.RightUi:
                return kind == ChartUiAnimationKind.In ? 0.5f : 2f / 3f;
            case ChartUiTarget.Scanline:
                return 1f;
            case ChartUiTarget.BoundaryLine:
                return 0.1333f;
            case ChartUiTarget.ProgressBar:
                return 0.5f;
            default:
                return 0;
        }
    }

    private static float ResolveAnimationAlpha(
        ChartUiTarget target,
        ChartUiAnimationKind kind,
        float elapsed,
        float duration)
    {
        switch (target)
        {
            case ChartUiTarget.Combo:
                return SampleBlink(elapsed, kind == ChartUiAnimationKind.In, false);
            case ChartUiTarget.Score:
                if (kind == ChartUiAnimationKind.In)
                    return SampleBlink(elapsed, true, true);
                if (elapsed < 1f) return 1f;
                return SampleBlink(elapsed - 1f, false, false);
            case ChartUiTarget.LeftUi:
            case ChartUiTarget.RightUi:
            case ChartUiTarget.ProgressBar:
            case ChartUiTarget.BoundaryLine:
                var progress = duration <= 0 ? 1f : Clamp01(elapsed / duration);
                return kind == ChartUiAnimationKind.In ? progress : 1f - progress;
            case ChartUiTarget.Scanline:
                // Cytus animates scanline geometry without fading it. The delayed alpha snapshot
                // still hides it once animation-out has completed.
                return 1f;
            default:
                return 1f;
        }
    }

    private static float SampleBlink(
        float elapsed,
        bool terminalVisible,
        bool startsVisible)
    {
        var step = (int) Math.Floor(elapsed / BlinkStepDuration);
        if (step >= 6) return terminalVisible ? 1f : 0f;
        var visible = startsVisible ? step % 2 == 0 : step % 2 == 1;
        return visible ? 1f : 0f;
    }

    private static float ResolveAlpha(AlphaSnapshot snapshot, float time)
    {
        if (time < snapshot.Time) return snapshot.StartAlpha;
        if (snapshot.Duration <= 0 || time >= snapshot.Time + snapshot.Duration) return snapshot.TargetAlpha;
        var progress = Clamp01((time - snapshot.Time) / snapshot.Duration);
        return snapshot.StartAlpha + (snapshot.TargetAlpha - snapshot.StartAlpha) * progress;
    }

    private static int FindLastAtOrBefore<T>(IReadOnlyList<T> snapshots, float time, Func<T, float> getTime)
    {
        var low = 0;
        var high = snapshots.Count - 1;
        var match = -1;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            if (getTime(snapshots[middle]) <= time)
            {
                match = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }
        return match;
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0, Math.Min(1, value));
    }

    private sealed class AlphaDraft
    {
        public float Time { get; }
        public float Duration { get; }
        public float TargetAlpha { get; }
        public int Sequence { get; }

        public AlphaDraft(float time, float duration, float targetAlpha, int sequence)
        {
            Time = time;
            Duration = duration;
            TargetAlpha = targetAlpha;
            Sequence = sequence;
        }
    }

    private sealed class AlphaSnapshot
    {
        public float Time { get; }
        public float Duration { get; }
        public float StartAlpha { get; }
        public float TargetAlpha { get; }
        public int Sequence { get; }

        public AlphaSnapshot(float time, float duration, float startAlpha, float targetAlpha, int sequence)
        {
            Time = time;
            Duration = duration;
            StartAlpha = startAlpha;
            TargetAlpha = targetAlpha;
            Sequence = sequence;
        }
    }

    private sealed class AnimationSnapshot
    {
        public float Time { get; }
        public float Duration { get; }
        public ChartUiAnimationKind Kind { get; }
        public int Sequence { get; }

        public AnimationSnapshot(float time, float duration, ChartUiAnimationKind kind, int sequence)
        {
            Time = time;
            Duration = duration;
            Kind = kind;
            Sequence = sequence;
        }
    }
}
