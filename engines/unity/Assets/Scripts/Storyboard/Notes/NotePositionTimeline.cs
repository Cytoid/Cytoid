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

        private readonly Dictionary<int, List<Track>> tracks = new Dictionary<int, List<Track>>();
        private readonly Dictionary<NoteController, Track> live = new Dictionary<NoteController, Track>();
        private readonly Dictionary<int, Vector3> endpoints = new Dictionary<int, Vector3>();

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
            if (endpoints.TryGetValue(note.id, out var position)) return position;
            var result = new ChartModel.Note.NoteOverride();
            if (tracks.TryGetValue(note.id, out var list))
            {
                var writtenAt = new float[NotePositionSampler.ChannelCount];
                for (var i = 0; i < writtenAt.Length; i++) writtenAt[i] = float.NegativeInfinity;
                foreach (var track in list)
                    Sample(track, note.start_time, result, writtenAt);
            }
            position = note.CalculatePosition(chart, result);
            endpoints[note.id] = position;
            return position;
        }

        private static void Sample(Track track, float time, ChartModel.Note.NoteOverride result,
            float[] writtenAt)
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
                NotePositionSampler.Apply(result, from, to, sampleAt, writtenAt);
            }
        }
    }
}
