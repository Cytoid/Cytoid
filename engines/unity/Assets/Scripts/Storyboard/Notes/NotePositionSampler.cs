using System;

namespace Cytoid.Storyboard.Notes
{
    /// <summary>Position channels shared by live note controllers and drag endpoint sampling.</summary>
    public static class NotePositionSampler
    {
        public const int ChannelCount = 7;

        // writtenAt is only used when merging controller histories. A controller which
        // stopped writing must not obscure a more recent write from another controller.
        public static void Apply(ChartModel.Note.NoteOverride target, NoteControllerState from,
            NoteControllerState to, float time, float[] writtenAt = null,
            Func<UnitFloat, float> convertUnit = null)
        {
            if (from.OverrideX != null && Accept(0, time, writtenAt))
                target.X = from.OverrideX.Value ? Sample(from.X, to.X, .5f, from, to, time, convertUnit) : (float?) null;
            if (from.OverrideY != null && Accept(1, time, writtenAt))
                target.Y = from.OverrideY.Value ? Sample(from.Y, to.Y, .5f, from, to, time, convertUnit) : (float?) null;
            if (from.OverrideZ != null && Accept(2, time, writtenAt))
                target.Z = from.OverrideZ.Value ? Sample(from.Z, to.Z, 0f, from, to, time, convertUnit) : (float?) null;
            if (from.XMultiplier != null && Accept(3, time, writtenAt))
                target.XMultiplier = Sample(from.XMultiplier.Value, to.XMultiplier, from, to, time);
            if (from.YMultiplier != null && Accept(4, time, writtenAt))
                target.YMultiplier = Sample(from.YMultiplier.Value, to.YMultiplier, from, to, time);
            if (from.XOffset != null && Accept(5, time, writtenAt))
                target.XOffset = Sample(from.XOffset.Value, to.XOffset, from, to, time);
            if (from.YOffset != null && Accept(6, time, writtenAt))
                target.YOffset = Sample(from.YOffset.Value, to.YOffset, from, to, time);
        }

        private static bool Accept(int channel, float time, float[] writtenAt)
        {
            if (writtenAt == null) return true;
            if (time < writtenAt[channel]) return false;
            writtenAt[channel] = time;
            return true;
        }

        private static float Sample(UnitFloat from, UnitFloat to, float fallback,
            NoteControllerState a, NoteControllerState b, float time, Func<UnitFloat, float> convertUnit)
        {
            if (from == null) return fallback;
            // Match the live easer's lazy conversion at segment boundaries.
            if (to == null || time <= a.Time) return Convert(from, convertUnit);
            if (time >= b.Time) return Convert(to, convertUnit);
            return Sample(Convert(from, convertUnit), Convert(to, convertUnit), a, b, time);
        }

        private static float Convert(UnitFloat value, Func<UnitFloat, float> convertUnit)
            => convertUnit == null ? value.ConvertedValue : convertUnit(value);

        private static float Sample(float from, float? to, NoteControllerState a,
            NoteControllerState b, float time)
        {
            if (to == null || time <= a.Time) return from;
            if (time >= b.Time) return to.Value;
            return EasingFunction.GetEasingFunction(a.Easing ?? EasingFunction.Ease.Linear)
                (from, to.Value, (time - a.Time) / (b.Time - a.Time));
        }
    }
}
