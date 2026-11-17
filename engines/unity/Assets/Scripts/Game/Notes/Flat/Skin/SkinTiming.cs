// Port of cytoid-design packages/notes/src/tokens.ts (engine clocks) and
// packages/notes/src/skin/slots.ts (slot registry) — the fixed contract every
// skin compiles against. PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;

public enum SkinGrade
{
    Perfect,
    Great,
    Good,
    Bad,
    Miss,
}

public enum SkinTiming
{
    Fast,
    Slow,
}

public enum SkinFeedbackView
{
    Judgement,
    Timing,
    Combined,
}

public enum SkinClipMode
{
    Normalized,
    Once,
    Loop,
    Progress,
}

public readonly struct SkinExitCurve
{
    public readonly double Duration, Drop, Fall, Power;

    public SkinExitCurve(double duration, double drop, double fall, double power)
    {
        Duration = duration;
        Drop = drop;
        Fall = fall;
        Power = power;
    }
}

/// <summary>One engine slot a skin can bind: `${kind}:${path}` or `${kind}:feedback:...`.</summary>
public sealed class SkinSlotContract
{
    public readonly NoteKind? Kind;
    public readonly SkinGrade? Grade;
    public readonly SkinTiming? Timing;
    public readonly SkinFeedbackView? View;
    public readonly string Path;
    public readonly string Id;
    public readonly SkinClipMode Mode;
    public readonly double Duration;

    internal SkinSlotContract(NoteKind? kind, SkinGrade? grade, SkinTiming? timing, SkinFeedbackView? view,
        string path, string id, SkinClipMode mode, double duration)
    {
        Kind = kind;
        Grade = grade;
        Timing = timing;
        View = view;
        Path = path;
        Id = id;
        Mode = mode;
        Duration = duration;
    }
}

public static class SkinTimingTokens
{
    /// <summary>tokens.noteTiming — engine clip lifetimes, seconds.</summary>
    public static class Note
    {
        public const double Enter = 1.2;
        public const double ExitRewind = 1;
        public static readonly SkinExitCurve ExitTap = new SkinExitCurve(0.3, 0.8, 1, 3);
        public static readonly SkinExitCurve ExitHoldHead = new SkinExitCurve(0.3, 1, 1, 3);
        public static readonly SkinExitCurve ExitDragChild = new SkinExitCurve(0.15, 1, 1, 1);
        // Drops: the exit fade IS the miss — no separate miss clip. The note rides
        // past the line at its approach speed while the opacity runs 1 → 0 linearly,
        // always inside the 0.2 s drop miss window (cytoid-design tokens.ts
        // noteTiming.exit.drop).
        public static readonly SkinExitCurve ExitDrop = new SkinExitCurve(0.15, 1, 1, 1);

        /// <summary>noteTiming.exit.drop.travel — how far past the line (fraction of the
        /// board height) the ride may sweep before the note is fully gone. When the
        /// approach speed would outrun it, the exit clock compresses so the fade
        /// completes exactly at the cap (the position clamps there too).</summary>
        public const double ExitDropTravel = 0.2;
        public const double HoldPress = 0.2;
        public const double HoldLoop = 1;

        /// <summary>Triggered drag-head loop period (the head following its chain).</summary>
        public const double ActiveLoop = 1;

        /// <summary>Chain-link loop period (marching chains); the flat chain is a frozen pose.</summary>
        public const double ChainLinkLoop = 1;
        public static double Clear(SkinGrade grade)
        {
            switch (grade)
            {
                case SkinGrade.Perfect: return 0.42;
                case SkinGrade.Great: return 0.46;
                case SkinGrade.Good: return 0.52;
                case SkinGrade.Bad: return 0.56;
                case SkinGrade.Miss: return 0.5;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }
        }
    }

    public static readonly NoteKind[] NoteKinds =
    {
        NoteKind.Click,
        NoteKind.Hold,
        NoteKind.LongHold,
        NoteKind.DragHead,
        NoteKind.DragChild,
        NoteKind.Flick,
        NoteKind.ClickDragHead,
        NoteKind.ClickDragChild,
        NoteKind.DropClick,
        NoteKind.DropDrag,
    };

    public static readonly SkinGrade[] Grades =
    {
        SkinGrade.Perfect,
        SkinGrade.Great,
        SkinGrade.Good,
        SkinGrade.Bad,
        SkinGrade.Miss,
    };

    /// <summary>Grades that carry a FAST/SLOW timing label.</summary>
    public static readonly SkinGrade[] TimedGrades = { SkinGrade.Great, SkinGrade.Good, SkinGrade.Bad };
    public static readonly SkinTiming[] Timings = { SkinTiming.Fast, SkinTiming.Slow };

    private static readonly SkinGrade[] ClearGrades = { SkinGrade.Perfect, SkinGrade.Great, SkinGrade.Good, SkinGrade.Bad };

    public static string KindKey(NoteKind kind)
    {
        switch (kind)
        {
            case NoteKind.Click: return "click";
            case NoteKind.Hold: return "hold";
            case NoteKind.LongHold: return "long-hold";
            case NoteKind.DragHead: return "drag-head";
            case NoteKind.DragChild: return "drag-child";
            case NoteKind.Flick: return "flick";
            case NoteKind.ClickDragHead: return "click-drag-head";
            case NoteKind.ClickDragChild: return "click-drag-child";
            case NoteKind.DropClick: return "drop-click";
            case NoteKind.DropDrag: return "drop-drag";
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public static string GradeKey(SkinGrade grade)
    {
        switch (grade)
        {
            case SkinGrade.Perfect: return "perfect";
            case SkinGrade.Great: return "great";
            case SkinGrade.Good: return "good";
            case SkinGrade.Bad: return "bad";
            case SkinGrade.Miss: return "miss";
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }

    public static string TimingKey(SkinTiming timing) => timing == SkinTiming.Fast ? "fast" : "slow";

    public static string ViewKey(SkinFeedbackView view)
    {
        switch (view)
        {
            case SkinFeedbackView.Judgement: return "judgement";
            case SkinFeedbackView.Timing: return "timing";
            case SkinFeedbackView.Combined: return "combined";
            default: throw new ArgumentOutOfRangeException(nameof(view));
        }
    }

    /// <summary>slots.ts feedbackBindings — `<kind>:feedback:<view>:<grade>[:<timing>]`.</summary>
    public static string FeedbackSlotKey(NoteKind kind, SkinFeedbackView view, SkinGrade grade, SkinTiming? timing)
    {
        var key = $"{KindKey(kind)}:feedback:{ViewKey(view)}:{GradeKey(grade)}";
        return timing.HasValue ? $"{key}:{TimingKey(timing.Value)}" : key;
    }

    public static SkinExitCurve ExitCurve(NoteKind kind)
    {
        if (kind == NoteKind.DropClick || kind == NoteKind.DropDrag) return Note.ExitDrop;
        if (kind == NoteKind.Hold || kind == NoteKind.LongHold) return Note.ExitHoldHead;
        return kind == NoteKind.DragChild || kind == NoteKind.ClickDragChild ? Note.ExitDragChild : Note.ExitTap;
    }
}

public static class SkinSlots
{
    private static readonly Dictionary<string, SkinSlotContract> Registry = BuildRegistry();

    private static Dictionary<string, SkinSlotContract> BuildRegistry()
    {
        var registry = new Dictionary<string, SkinSlotContract>();
        foreach (var kind in SkinTimingTokens.NoteKinds)
        {
            void Add(string path, string id, SkinClipMode mode, double duration) =>
                registry.Add($"{SkinTimingTokens.KindKey(kind)}:{path}",
                    new SkinSlotContract(kind, null, null, null, path, id, mode, duration));
            var kindKey = SkinTimingTokens.KindKey(kind);
            var drop = kind == NoteKind.DropClick || kind == NoteKind.DropDrag;
            Add("enter", "enter", drop ? SkinClipMode.Loop : SkinClipMode.Normalized, SkinTimingTokens.Note.Enter);
            foreach (var grade in new[] { SkinGrade.Perfect, SkinGrade.Great, SkinGrade.Good, SkinGrade.Bad })
                Add($"clear:{SkinTimingTokens.GradeKey(grade)}", $"clear-{SkinTimingTokens.GradeKey(grade)}",
                    SkinClipMode.Once, SkinTimingTokens.Note.Clear(grade));
            Add("miss", "miss", SkinClipMode.Once, SkinTimingTokens.Note.Clear(SkinGrade.Miss));
            // Every kind exits now — drops ride their fade past the line (the fade
            // IS the miss), so no `if (!drop)` carve-out anymore.
            Add("exit", "exit", SkinClipMode.Once, SkinTimingTokens.ExitCurve(kind).Duration);
            if (kind == NoteKind.DragHead || kind == NoteKind.ClickDragHead)
            {
                Add("active", "active", SkinClipMode.Loop, SkinTimingTokens.Note.ActiveLoop);
                Add("chainLink", "chain-link", SkinClipMode.Loop, SkinTimingTokens.Note.ChainLinkLoop);
            }
            if (kind == NoteKind.Hold || kind == NoteKind.LongHold)
            {
                Add("body", "hold-body", SkinClipMode.Progress, 1);
                Add("press", "hold-press", SkinClipMode.Once, SkinTimingTokens.Note.HoldPress);
                Add("loopUnder", "hold-loop-under", SkinClipMode.Loop, SkinTimingTokens.Note.HoldLoop);
                Add("loopOver", "hold-loop-over", SkinClipMode.Loop, SkinTimingTokens.Note.HoldLoop);
                Add("progress", "hold-progress", SkinClipMode.Progress, 1);
            }
        }
        // Feedback bindings — one slot per (kind, view, grade[, timing]). The slot
        // name carries the kind so a skin can lay out feedback per note kind; the
        // contract id/path stay kind-free, exactly like slots.ts feedbackBindings.
        // Duration = the grade's clear duration.
        foreach (var kind in SkinTimingTokens.NoteKinds)
        {
            foreach (var grade in SkinTimingTokens.Grades)
            {
                var id = $"feedback:judgement:{SkinTimingTokens.GradeKey(grade)}";
                registry.Add($"{SkinTimingTokens.KindKey(kind)}:{id}",
                    new SkinSlotContract(kind, grade, null, SkinFeedbackView.Judgement, "feedback:judgement", id,
                        SkinClipMode.Once, SkinTimingTokens.Note.Clear(grade)));
            }
            foreach (var view in new[] { SkinFeedbackView.Timing, SkinFeedbackView.Combined })
                foreach (var grade in SkinTimingTokens.TimedGrades)
                    foreach (var timing in SkinTimingTokens.Timings)
                    {
                        var path = $"feedback:{SkinTimingTokens.ViewKey(view)}";
                        var id = $"{path}:{SkinTimingTokens.GradeKey(grade)}:{SkinTimingTokens.TimingKey(timing)}";
                        registry.Add($"{SkinTimingTokens.KindKey(kind)}:{id}",
                            new SkinSlotContract(kind, grade, timing, view, path, id,
                                SkinClipMode.Once, SkinTimingTokens.Note.Clear(grade)));
                    }
        }
        return registry;
    }

    public static SkinSlotContract Contract(string name) =>
        Registry.TryGetValue(name, out var contract) ? contract : null;

    public static IEnumerable<KeyValuePair<string, SkinSlotContract>> All => Registry;

    /// <summary>slots.ts slotProgress — how a clip's driver value maps to animation progress.
    /// `period` overrides a loop slot's contract duration; `<= 0` freezes on the first frame.</summary>
    public static double SlotProgress(SkinSlotContract slot, double x, double? period = null)
    {
        var p = period ?? slot.Duration;
        switch (slot.Mode)
        {
            case SkinClipMode.Normalized:
            case SkinClipMode.Progress:
                return x;
            case SkinClipMode.Once:
                return x / slot.Duration;
            case SkinClipMode.Loop:
                return p <= 0 ? 0 : (((x % p) + p) % p) / p;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
