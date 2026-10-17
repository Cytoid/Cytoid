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
        }

        private sealed class Endpoint
        {
            public Vector3 Position;
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
            endpoints.Remove(id);
        }

        public void Stop(NoteController controller, float time)
        {
            if (!live.TryGetValue(controller, out var track)) return;
            track.StoppedAt = time;
            live.Remove(controller);
            endpoints.Remove(controller.States[0].Note.Value);
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
            endpoint = new Endpoint();
            var result = new ChartModel.Note.NoteOverride();
            if (tracks.TryGetValue(note.id, out var list))
            {
                var writtenAt = new float[NotePositionSampler.ChannelCount];
                for (var i = 0; i < writtenAt.Length; i++) writtenAt[i] = float.NegativeInfinity;
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
            var states = track.Controller.States;
            var until = Mathf.Min(time, track.StoppedAt);
            for (var i = 0; i < states.Count; i++)
            {
                var from = states[i];
                if (from.Time > until || from.Destroy == true) break;
                var to = i + 1 < states.Count ? states[i + 1] : from;
                var sampleAt = i + 1 < states.Count ? Mathf.Min(until, to.Time) : until;
                if (sampleAt < track.AvailableFrom) continue;
                NotePositionSampler.Apply(result, from, to, sampleAt, writtenAt, endpoint.ConvertUnit);
            }
        }
    }
}
