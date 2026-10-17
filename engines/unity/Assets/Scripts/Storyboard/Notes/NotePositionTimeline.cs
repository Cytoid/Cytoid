using System.Collections.Generic;
using UnityEngine;

namespace Cytoid.Storyboard.Notes
{
    /// <summary>
    /// Samples drag endpoints at the note's chart time, independently of its spawned
    /// Transform. Only controllers already introduced by the runtime are recorded.
    /// </summary>
    public sealed class NotePositionTimeline
    {
        private sealed class Track
        {
            public NoteController Controller;
            public float AvailableFrom;
            public float StoppedAt = float.PositiveInfinity;
            public readonly ChartModel.Note.NoteOverride LastApplied = new ChartModel.Note.NoteOverride();
            public readonly float[] WrittenAt = NewWriteTimes();
            public NoteControllerState LastState;
            public float LastAppliedAt = float.NegativeInfinity;
        }

        private sealed class Endpoint
        {
            public Vector3 Position;
            public float Time;
            private readonly Dictionary<UnitFloat, float> conversions = new Dictionary<UnitFloat, float>();

            public float ConvertUnit(UnitFloat value)
            {
                var converted = value.PeekConvertedValue();
                conversions[value] = converted;
                return converted;
            }

            public bool IsCurrent()
            {
                // Unconverted units can change with the camera, screen or canvas.
                // A later live conversion may also fix their value on a new basis.
                foreach (var pair in conversions)
                    if (!pair.Key.PeekConvertedValue().Equals(pair.Value)) return false;
                return true;
            }
        }

        private readonly Dictionary<int, List<Track>> tracks = new Dictionary<int, List<Track>>();
        private readonly Dictionary<NoteController, Track> live = new Dictionary<NoteController, Track>();
        private readonly Dictionary<int, Endpoint> endpoints = new Dictionary<int, Endpoint>();

        public void Register(NoteController controller, float availableFrom)
        {
            var id = controller.States[0].Note.Value;
            var track = new Track {Controller = controller, AvailableFrom = availableFrom};
            if (!tracks.TryGetValue(id, out var list))
                tracks[id] = list = new List<Track>();
            list.Add(track);
            live.Add(controller, track);
            InvalidateFuture(id, availableFrom);
        }

        public void Stop(NoteController controller, float time)
        {
            if (!live.TryGetValue(controller, out var track)) return;
            track.StoppedAt = time;
            live.Remove(controller);
            InvalidateFuture(controller.States[0].Note.Value, time);
        }

        // Record only this controller's writes, after its live easer has run. Other
        // controllers may have already written unrelated channels on the same note.
        public void RecordApplied(NoteController controller, NoteControllerState state,
            ChartModel.Note.NoteOverride values, float time)
        {
            if (!live.TryGetValue(controller, out var track)) return;
            // Crossing a state boundary can release channels via reset. From then
            // on, project from real writes rather than replaying a missed boundary.
            if (track.LastState != state || time < track.LastAppliedAt)
                InvalidateFuture(controller.States[0].Note.Value, time);
            track.LastState = state;
            track.LastAppliedAt = time;
            var last = track.LastApplied;
            var at = track.WrittenAt;
            if (state.OverrideX != null) { last.X = values.X; at[0] = time; }
            if (state.OverrideY != null) { last.Y = values.Y; at[1] = time; }
            if (state.OverrideZ != null) { last.Z = values.Z; at[2] = time; }
            if (state.XMultiplier != null) { last.XMultiplier = values.XMultiplier; at[3] = time; }
            if (state.YMultiplier != null) { last.YMultiplier = values.YMultiplier; at[4] = time; }
            if (state.XOffset != null) { last.XOffset = values.XOffset; at[5] = time; }
            if (state.YOffset != null) { last.YOffset = values.YOffset; at[6] = time; }
        }

        public void Clear()
        {
            tracks.Clear();
            live.Clear();
            endpoints.Clear();
        }

        public Vector3 GetEndpoint(ChartModel.Note note, Chart chart)
        {
            if (endpoints.TryGetValue(note.id, out var endpoint) && endpoint.IsCurrent()) return endpoint.Position;
            endpoint = new Endpoint {Time = note.start_time};
            var result = new ChartModel.Note.NoteOverride();
            if (tracks.TryGetValue(note.id, out var list))
            {
                var writtenAt = NewWriteTimes();
                foreach (var track in list)
                    Sample(track, note.start_time, result, writtenAt, endpoint);
            }
            endpoint.Position = note.CalculatePosition(chart, result);
            endpoints[note.id] = endpoint;
            return endpoint.Position;
        }

        private static void Sample(Track track, float time, ChartModel.Note.NoteOverride result,
            float[] writtenAt, Endpoint endpoint)
        {
            if (time < track.AvailableFrom) return;
            if (time >= track.StoppedAt)
            {
                MergeApplied(track, result, writtenAt);
                return;
            }
            var states = track.Controller.States;
            var until = Mathf.Min(time, track.StoppedAt);
            var observedUntil = time >= track.LastAppliedAt ? track.LastAppliedAt : float.NegativeInfinity;
            if (!float.IsNegativeInfinity(observedUntil)) MergeApplied(track, result, writtenAt);
            for (var i = 0; i < states.Count; i++)
            {
                var from = states[i];
                // FindStates selects the last state at a timestamp. Earlier states
                // in that group never become the live easing source.
                if (i + 1 < states.Count && states[i + 1].Time == from.Time) continue;
                if (from.Time > until || from.Destroy == true) break;
                var to = i + 1 < states.Count ? states[i + 1] : from;
                var sampleAt = i + 1 < states.Count ? Mathf.Min(until, to.Time) : until;
                if (sampleAt < track.AvailableFrom || sampleAt <= observedUntil) continue;
                NotePositionSampler.Apply(result, from, to, sampleAt, writtenAt, endpoint.ConvertUnit);
            }
        }

        private void InvalidateFuture(int id, float time)
        {
            // A later observation must not rewrite an endpoint already crossed by
            // the head. Frame positions remain frozen separately by Game.
            if (endpoints.TryGetValue(id, out var endpoint) && endpoint.Time >= time)
                endpoints.Remove(id);
        }

        private static void MergeApplied(Track track, ChartModel.Note.NoteOverride result, float[] writtenAt)
        {
            var last = track.LastApplied;
            if (AcceptApplied(track, 0, writtenAt)) result.X = last.X;
            if (AcceptApplied(track, 1, writtenAt)) result.Y = last.Y;
            if (AcceptApplied(track, 2, writtenAt)) result.Z = last.Z;
            if (AcceptApplied(track, 3, writtenAt)) result.XMultiplier = last.XMultiplier;
            if (AcceptApplied(track, 4, writtenAt)) result.YMultiplier = last.YMultiplier;
            if (AcceptApplied(track, 5, writtenAt)) result.XOffset = last.XOffset;
            if (AcceptApplied(track, 6, writtenAt)) result.YOffset = last.YOffset;
        }

        private static bool AcceptApplied(Track track, int channel, float[] writtenAt)
        {
            var time = track.WrittenAt[channel];
            if (float.IsNegativeInfinity(time) || time < writtenAt[channel]) return false;
            writtenAt[channel] = time;
            return true;
        }

        private static float[] NewWriteTimes()
        {
            var result = new float[NotePositionSampler.ChannelCount];
            for (var i = 0; i < result.Length; i++) result[i] = float.NegativeInfinity;
            return result;
        }
    }
}
