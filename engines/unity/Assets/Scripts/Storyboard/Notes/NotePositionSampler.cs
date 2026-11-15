using System;

namespace Cytoid.Storyboard.Notes
{
    /// <summary>Position channels shared by live note controllers and drag endpoint sampling.</summary>
    public static class NotePositionSampler
    {
        public const int ChannelCount = 7;

        internal readonly struct WriteTime
        {
            private readonly float time;
            private readonly bool beforeTransition;
            private readonly bool hasValue;

            public WriteTime(float time, bool beforeTransition = false)
            {
                this.time = time;
                this.beforeTransition = beforeTransition;
                hasValue = true;
            }

            public bool TryWrite(WriteTime[] writtenAt, int channel)
            {
                if (writtenAt == null) return true;
                var previous = writtenAt[channel];
                if (previous.hasValue && (time < previous.time ||
                    (time == previous.time && beforeTransition && !previous.beforeTransition))) return false;
                writtenAt[channel] = this;
                return true;
            }
        }

        public static void Apply(ChartModel.Note.NoteOverride target, NoteControllerState from,
            NoteControllerState to, float time)
            => Apply(target, from, to, time, default, null, null);

        internal static void Apply(ChartModel.Note.NoteOverride target, NoteControllerState from,
            NoteControllerState to, float time, WriteTime writeTime, WriteTime[] writtenAt,
            Func<UnitFloat, float> convertUnit)
        {
            if (from.OverrideX != null && writeTime.TryWrite(writtenAt, 0))
                target.X = from.OverrideX.Value ? Sample(from.X, to.X, .5f, from, to, time, convertUnit) : (float?) null;
            if (from.OverrideY != null && writeTime.TryWrite(writtenAt, 1))
                target.Y = from.OverrideY.Value ? Sample(from.Y, to.Y, .5f, from, to, time, convertUnit) : (float?) null;
            if (from.OverrideZ != null && writeTime.TryWrite(writtenAt, 2))
                target.Z = from.OverrideZ.Value ? Sample(from.Z, to.Z, 0f, from, to, time, convertUnit) : (float?) null;
            if (from.XMultiplier != null && writeTime.TryWrite(writtenAt, 3))
                target.XMultiplier = Sample(from.XMultiplier.Value, to.XMultiplier, from, to, time);
            if (from.YMultiplier != null && writeTime.TryWrite(writtenAt, 4))
                target.YMultiplier = Sample(from.YMultiplier.Value, to.YMultiplier, from, to, time);
            if (from.XOffset != null && writeTime.TryWrite(writtenAt, 5))
                target.XOffset = Sample(from.XOffset.Value, to.XOffset, from, to, time);
            if (from.YOffset != null && writeTime.TryWrite(writtenAt, 6))
                target.YOffset = Sample(from.YOffset.Value, to.YOffset, from, to, time);
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
