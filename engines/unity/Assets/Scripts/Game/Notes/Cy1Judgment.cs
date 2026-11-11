using System;

internal static class Cy1Judgment
{
    internal const long ClickClose = 300_000;
    internal const long DragClose = 150_000;

    internal static bool Supports(NoteType type, float duration)
    {
        return type == NoteType.Click || type == NoteType.DragHead ||
               type == NoteType.DragChild ||
               (type == NoteType.Hold && duration > 0 && !float.IsInfinity(duration));
    }

    internal static long ToMicroseconds(double seconds) =>
        (long)Math.Round(seconds * 1_000_000, MidpointRounding.AwayFromZero);

    internal static NoteGrade Click(long delta)
    {
        if (delta <= -800_000 || delta >= ClickClose) return NoteGrade.None;
        if (delta <= -400_000 || delta >= 200_000) return NoteGrade.Bad;
        if (delta <= -200_000 || delta >= 150_000) return NoteGrade.Good;
        if (delta <= -70_000 || delta >= 70_000) return NoteGrade.Great;
        return NoteGrade.Perfect;
    }

    internal static NoteGrade Drag(long delta)
    {
        if (delta <= -400_000 || delta >= DragClose) return NoteGrade.None;
        return delta <= -200_000 || delta >= 100_000 ? NoteGrade.Great : NoteGrade.Perfect;
    }

    internal static long HoldClose(long start, long end) => Math.Min(start + ClickClose, end);

    internal static bool CanStartHold(long now, long start, long end) =>
        now > start - 800_000 && now < HoldClose(start, end);

    internal static long HeldTime(long start, long end, long boundAt, long now) =>
        Math.Max(0, Math.Min(now, end) - Math.Max(start, boundAt));

    internal static NoteGrade Hold(long duration, long held)
    {
        if (duration <= 0 || held * 10 < duration * 3) return NoteGrade.Miss;
        if (held * 10 < duration * 5) return NoteGrade.Bad;
        if (held * 10 < duration * 7) return NoteGrade.Good;
        return duration - held <= 50_000 ? NoteGrade.Perfect : NoteGrade.Great;
    }
}
