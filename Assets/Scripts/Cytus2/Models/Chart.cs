using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Cytus2.Models
{
    public class Chart
    {
        public ChartRoot Root;
        public int CurrentPageId;
        public float MusicOffset;

        private float baseSize;
        private float offset;
        private float horizontalRatio;
        private float verticalRatio;

        public string Checksum
        {
            get { return global::Checksum.From(checksumSource); }
        }

        private string checksumSource = string.Empty;

        public Chart(string text, float horizontalRatio = 0.85f, float verticalRatio = 7.0f / 9.0f)
        {
            Root = new ChartRoot();

            try
            {
                // C2/Cytoid format
                Root = JsonConvert.DeserializeObject<ChartRoot>(text);
            }
            catch (JsonReaderException)
            {
                // C1 format
                Root = FromCytus1Chart(text);
            }

            baseSize = Camera.main.orthographicSize;
            offset = -Camera.main.orthographicSize * 0.04f;
            this.horizontalRatio = horizontalRatio;
            this.verticalRatio = verticalRatio;

            var thisChecksumSource = string.Empty;

            foreach (var tempo in Root.tempo_list)
            {
                thisChecksumSource += "tempo " + ((int) tempo.tick).ToString() + " " + ((int) (tempo.value)).ToString();
            }

            // Convert tick to absolute time

            foreach (var eventOrder in Root.event_order_list)
            {
                eventOrder.time = ConvertToTime(eventOrder.tick);
            }

            foreach (var anim in Root.animation_list)
            {
                anim.time = ConvertToTime(anim.tick);
            }

            foreach (var page in Root.page_list)
            {
                page.start_time = ConvertToTime(page.start_tick);
                page.end_time = ConvertToTime(page.end_tick);
                thisChecksumSource += "page " + ((int) page.start_tick).ToString() + " " +
                                      ((int) page.end_tick).ToString();
                if (Mod.FlipY.IsEnabled() || Mod.FlipAll.IsEnabled())
                {
                    page.scan_line_direction = page.scan_line_direction == 1 ? -1 : 1;
                }
            }

            for (var i = 0; i < Root.note_list.Count; i++)
            {
                var note = Root.note_list[i];
                var page = Root.page_list[note.page_index];

                note.direction = page.scan_line_direction;
                note.speed = note.page_index == 0 ? 1.0f : CalculateNoteSpeed(i);
                note.speed *= (float) note.approach_rate;

                var modSpeed = 1f;
                if (Mod.Fast.IsEnabled()) modSpeed = 1.5f;
                if (Mod.Slow.IsEnabled()) modSpeed = 0.75f;
                note.speed *= modSpeed;

                note.start_time = ConvertToTime(note.tick);
                note.end_time = ConvertToTime(note.tick + note.hold_tick);

                var flip = (Mod.FlipX.IsEnabled() || Mod.FlipAll.IsEnabled()) ? -1 : 1;

                note.position = new Vector3(
                    ((float) note.x * 2 * horizontalRatio - horizontalRatio) * baseSize * Screen.width /
                    Screen.height
                    * flip,
                    verticalRatio * page.scan_line_direction *
                    (-baseSize + 2.0f *
                     baseSize *
                     (note.tick - page.start_tick) * 1.0f /
                     (page.end_tick - page.start_tick))
                    + offset
                );

                note.end_position = new Vector3(
                    ((float) note.x * 2 * horizontalRatio - horizontalRatio) * baseSize * Screen.width /
                    Screen.height
                    * flip,
                    GetNotePosition(note.tick + note.hold_tick)
                );

                note.holdlength = verticalRatio * 2.0f * baseSize *
                                  note.hold_tick /
                                  (page.end_tick -
                                   page.start_tick);

                if (note.type == 3 || note.type == 4)
                    note.intro_time = note.start_time - (1.175f / note.speed);
                else
                    note.intro_time = note.start_time - (1.367f / note.speed);

                switch (note.type)
                {
                    case 0:
                        note.tint = note.direction == 1 ? 0.94f : 1.06f;
                        break;
                    case 1:
                        note.tint = note.direction == 1 ? 0.94f : 1.06f;
                        break;
                    case 2:
                        note.tint = note.direction == 1 ? 0.94f : 1.06f;
                        break;
                    case 3:
                        note.tint = note.direction == 1 ? 0.94f : 1.06f;
                        note.nextdraglinestarttime =
                            note.start_time - 1.175f / note.speed - 0.133f;
                        break;
                    case 4:
                        note.tint = note.direction == 1 ? 0.94f : 1.06f;
                        if (note.next_id > 0)
                            note.nextdraglinestarttime = note.intro_time - 0.2f;
                        break;
                    case 5:
                        note.tint = note.direction == 1 ? 1.00f : 1.30f;
                        break;
                }

                var lx = note.x;
                if (lx != 0f)
                {
                    while (lx < 10000)
                    {
                        lx *= 10;
                    }
                }

                thisChecksumSource += "note " + note.id.ToString() + " "
                                      + note.page_index.ToString() + " "
                                      + note.type.ToString() + " "
                                      + ((int) note.tick).ToString() + " "
                                      + ((int) lx).ToString() + " "
                                      + ((int) note.hold_tick).ToString() + " "
                                      + note.next_id.ToString() + " "
                                      + ((int) (note.approach_rate * 100)).ToString();
            }

            foreach (var note in Root.note_list)
            {
                if (note.next_id <= 0) continue;

                var noteThis = note;
                var noteNext = Root.note_list[note.next_id];

                if (noteThis.position == noteNext.position)
                    noteThis.rotation = 0;
                else if (Math.Abs(noteThis.position.y - noteNext.position.y) < 0.000001)
                    noteThis.rotation = noteThis.position.x > noteNext.position.x ? -90 : 90;
                else if (Math.Abs(noteThis.position.x - noteNext.position.x) < 0.000001)
                    noteThis.rotation = noteThis.position.y > noteNext.position.y ? 180 : 0;
                else
                    noteThis.rotation =
                        Mathf.Atan((noteNext.position.x - noteThis.position.x) /
                                   (noteNext.position.y - noteThis.position.y)) / Mathf.PI * 180f +
                        (noteNext.position.y > noteThis.position.y ? 0 : 180);
            }

            MusicOffset = Root.music_offset;

            // Set checksum if not a converted chart
            if (checksumSource == string.Empty)
            {
                checksumSource = thisChecksumSource;
            }

            Debug.Log(checksumSource);
        }

        private float ConvertToTime(float tick)
        {
            float result = 0;

            var currentTick = 0f;
            var currentTimeZone = 0;

            for (var i = 1; i < Root.tempo_list.Count; i++)
            {
                if (Root.tempo_list[i].tick >= tick) break;
                result += (Root.tempo_list[i].tick - currentTick) * 1e-6f * Root.tempo_list[i - 1].value /
                          Root.time_base;
                currentTick = Root.tempo_list[i].tick;
                currentTimeZone++;
            }

            result += (tick - currentTick) * 1e-6f * Root.tempo_list[currentTimeZone].value / Root.time_base;
            return result;
        }

        public float CalculateNoteSpeed(int id)
        {
            var pageRatio =
                1.0f * (Root.note_list[id].tick - Root.page_list[Root.note_list[id].page_index].start_tick) /
                (Root.page_list[Root.note_list[id].page_index].end_tick -
                 Root.page_list[Root.note_list[id].page_index].start_tick);
            var tempo =
                (Root.page_list[Root.note_list[id].page_index].end_time -
                 Root.page_list[Root.note_list[id].page_index].start_time) * pageRatio +
                (Root.page_list[Root.note_list[id].page_index - 1].end_time -
                 Root.page_list[Root.note_list[id].page_index - 1].start_time) * (1.0f - pageRatio);
            return tempo >= 1 ? 1.0f : 1.0f / tempo;
        }

        public float GetNotePosition(float tick)
        {
            var targetPageId = 0;
            while (targetPageId < Root.page_list.Count && tick > Root.page_list[targetPageId].end_tick)
                targetPageId++;

            if (targetPageId == Root.page_list.Count)
            {
                return -verticalRatio * Root.page_list[targetPageId - 1].scan_line_direction *
                       (-baseSize + 2.0f *
                        baseSize *
                        (tick - Root.page_list[targetPageId - 1].end_tick) *
                        1.0f / (Root.page_list[targetPageId - 1].end_tick -
                                Root.page_list[targetPageId - 1].start_tick))
                       + offset;
            }

            return verticalRatio * Root.page_list[targetPageId].scan_line_direction *
                   (-baseSize + 2.0f *
                    baseSize * (tick - Root.page_list[targetPageId].start_tick) *
                    1.0f / (Root.page_list[targetPageId].end_tick - Root.page_list[targetPageId].start_tick))
                   + offset;
        }

        public float GetScannerPosition(float time)
        {
            CurrentPageId = 0;
            while (CurrentPageId < Root.page_list.Count && time > Root.page_list[CurrentPageId].end_time)
                CurrentPageId++;
            if (CurrentPageId == Root.page_list.Count)
            {
                return -verticalRatio * Root.page_list[CurrentPageId - 1].scan_line_direction *
                       (-baseSize + 2.0f *
                        baseSize *
                        (time - Root.page_list[CurrentPageId - 1].end_time) *
                        1.0f / (Root.page_list[CurrentPageId - 1].end_time -
                                Root.page_list[CurrentPageId - 1].start_time))
                       + offset;
            }

            return verticalRatio * Root.page_list[CurrentPageId].scan_line_direction *
                   (-baseSize + 2.0f *
                    baseSize *
                    (time - Root.page_list[CurrentPageId].start_time) *
                    1.0f / (Root.page_list[CurrentPageId].end_time - Root.page_list[CurrentPageId].start_time))
                   + offset;
        }

        public ChartRoot FromCytus1Chart(string text)
        {
            // Parse

            var pageDuration = 0f;
            var pageShift = 0f;
            var tmpNotes = new Dictionary<int, C1Note>();

            foreach (var line in text.Split('\n'))
            {
                var data = line.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length == 0) continue;
                var type = data[0];
                switch (type)
                {
                    case "PAGE_SIZE":
                        pageDuration = float.Parse(data[1]);
                        checksumSource += data[1];
                        break;
                    case "PAGE_SHIFT":
                        pageShift = float.Parse(data[1]);
                        checksumSource += data[1];
                        break;
                    case "NOTE":
                        checksumSource += data[1] + data[2] + data[3] + data[4];
                        var note = new C1Note(int.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]),
                            float.Parse(data[4]), false);
                        tmpNotes.Add(int.Parse(data[1]), note);
                        if (note.Duration > 0) note.Type = C1NoteType.Hold;
                        break;
                    case "LINK":
                        var notesInChain = new List<C1Note>();
                        for (var i = 1; i < data.Length; i++)
                        {
                            if (data[i] != "LINK") checksumSource += data[i];
                            int id;
                            if (!int.TryParse(data[i], out id)) continue;
                            note = tmpNotes[id];
                            note.Type = C1NoteType.Chain;
                            notesInChain.Add(note);
                        }

                        for (var i = 0; i < notesInChain.Count - 1; i++)
                        {
                            notesInChain[i].ConnectedNote = notesInChain[i + 1];
                        }

                        notesInChain[0].IsChainHead = true;
                        break;
                }
            }

            pageShift += pageDuration;

            // Calculate chronological note ids
            var sortedNotes = tmpNotes.Values.ToList();
            sortedNotes.Sort((a, b) => a.Time.CompareTo(b.Time));
            var chronologicalIds = sortedNotes.Select(note => note.OriginalId).ToList();

            var notes = new Dictionary<int, C1Note>();

            // Recalculate note ids from original ids
            var newId = 0;
            foreach (var noteId in chronologicalIds)
            {
                tmpNotes[noteId].Id = newId;
                notes[newId] = tmpNotes[noteId];
                newId++;
            }

            // Reset chronological ids
            chronologicalIds.Clear();
            for (var i = 0; i < tmpNotes.Count; i++)
            {
                chronologicalIds.Add(i);
            }

            // Convert

            const int timeBase = 480;

            var root = new ChartRoot();
            root.format_version = 0;
            root.time_base = 480;
            root.start_offset_time = 0;

            var tempo = new ChartTempo();
            tempo.tick = 0;
            var tempoValue = pageDuration * 1000000;
            tempo.value = tempoValue;
            root.tempo_list = new List<ChartTempo> {tempo};

            if (pageShift < 0) pageShift = pageShift + 2 * pageDuration;

            var pageShiftTickOffset = pageShift / pageDuration * timeBase;

            var noteList = new List<ChartNote>();
            var page = 0;
            foreach (var note in notes.Values)
            {
                var obj = new ChartNote();
                obj.id = note.Id;
                switch (note.Type)
                {
                    case C1NoteType.Single:
                        obj.type = NoteType.Click;
                        break;
                    case C1NoteType.Chain:
                        obj.type = note.IsChainHead ? NoteType.DragHead : NoteType.DragChild;
                        break;
                    case C1NoteType.Hold:
                        obj.type = NoteType.Hold;
                        break;
                }

                obj.x = note.X;
                var ti = note.Time * timeBase * 1000000 / tempoValue + pageShiftTickOffset;
                obj.tick = ti;
                obj.hold_tick = note.Duration * timeBase * 1000000 / tempoValue;
                page = Mathf.FloorToInt(ti / timeBase);
                obj.page_index = page;
                if (note.Type == C1NoteType.Chain)
                {
                    obj.next_id = note.ConnectedNote != null ? note.ConnectedNote.Id : -1;
                }
                else
                {
                    obj.next_id = 0;
                }

                noteList.Add(obj);
            }

            root.note_list = noteList;

            var pageList = new List<ChartPage>();
            var direction = false;
            var t = 0;
            for (var i = 0; i <= page; i++)
            {
                var obj = new ChartPage();
                obj.scan_line_direction = direction ? 1 : -1;
                direction = !direction;
                obj.start_tick = t;
                t += timeBase;
                obj.end_tick = t;
                pageList.Add(obj);
            }

            root.page_list = pageList;

            root.music_offset = pageShiftTickOffset / timeBase / 1000000 * tempoValue;
            return root;
        }

        [Serializable]
        public class C1Note
        {
            public int Id;
            public int OriginalId;
            public float Time;
            public float X;
            public float Duration;

            public C1NoteType Type = C1NoteType.Single;

            [NonSerialized] public C1Note ConnectedNote;
            public bool IsChainHead;

            public C1Note(int originalId, float time, float x, float duration, bool isChainHead)
            {
                OriginalId = originalId;
                Time = time;
                X = x;
                Duration = duration;
                IsChainHead = isChainHead;
            }
        }

        public enum C1NoteType
        {
            Single,
            Chain,
            Hold
        }
    }
}