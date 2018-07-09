using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Cytus2.Models
{

    public class Chart : BaseChart
    {
        
        public ChartRoot Root;
        public int CurrentPageId;
        public float MusicOffset;
        private float baseSize;
        private float offset;
        
        public Chart(string text) : base(text)
        {
        }

        public override string Parse(string text)
        {
            Root = new ChartRoot();

            try
            {
                // C2/Cytoid format
                Root = JsonConvert.DeserializeObject<ChartRoot>(text);
            } catch (JsonReaderException)
            {
                // C1 format
                Root = FromCytus1Chart(text);
            }

            baseSize = Camera.main.orthographicSize;
            offset = -Camera.main.orthographicSize * 0.04f;

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
            }

            for (var i = 0; i < Root.note_list.Count; i++)
            {
                var note = Root.note_list[i];
                var page = Root.page_list[note.page_index];

                note.direction = page.scan_line_direction;
                note.speed = note.page_index == 0 ? 1.0f : CalculateNoteSpeed(i);

                note.start_time = ConvertToTime(note.tick);
                note.end_time = ConvertToTime(note.tick + note.hold_tick);

                note.position = new Vector3(

                    (note.x * 1.7f - 0.85f) * baseSize * Screen.width /
                    Screen.height,

                    7.0f / 9.0f * page.scan_line_direction *
                    (-baseSize + 2.0f *
                     baseSize *
                     (note.tick - page.start_tick) * 1.0f /
                     (page.end_tick - page.start_tick))
                    
                    + offset

                );

                note.end_position = new Vector3(
                    (note.x * 1.7f - 0.85f) * baseSize * Screen.width /
                    Screen.height, 
                    
                    GetNotePosition(note.tick + note.hold_tick)
                );

                note.holdlength = 7.0f / 9.0f * 2.0f * baseSize *
                                  note.hold_tick /
                                  (page.end_tick -
                                   page.start_tick);

                if (note.type == 3 || note.type == 4)
                    note.intro_time = note.start_time - 1.175f / note.speed;
                else
                    note.intro_time = note.start_time - 1.367f / note.speed;

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
            
            return string.Empty;
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
                return -7.0f / 9.0f * Root.page_list[targetPageId - 1].scan_line_direction *
                       (-baseSize + 2.0f *
                        baseSize *
                        (tick - Root.page_list[targetPageId - 1].end_tick) *
                        1.0f / (Root.page_list[targetPageId - 1].end_tick -
                                Root.page_list[targetPageId - 1].start_tick))
                    
                        + offset;
            }

            return 7.0f / 9.0f * Root.page_list[targetPageId].scan_line_direction *
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
                return -7.0f / 9.0f * Root.page_list[CurrentPageId - 1].scan_line_direction *
                       (-baseSize + 2.0f *
                        baseSize *
                        (time - Root.page_list[CurrentPageId - 1].end_time) *
                        1.0f / (Root.page_list[CurrentPageId - 1].end_time -
                                Root.page_list[CurrentPageId - 1].start_time))
                       + offset;
            }

            return 7.0f / 9.0f * Root.page_list[CurrentPageId].scan_line_direction *
                   (-baseSize + 2.0f *
                    baseSize *
                    (time - Root.page_list[CurrentPageId].start_time) *
                    1.0f / (Root.page_list[CurrentPageId].end_time - Root.page_list[CurrentPageId].start_time))
                
                    + offset;
        }

        public ChartRoot FromCytus1Chart(string text)
        {
            var chart = new Cytus.Models.Chart(text);
            
            const int timeBase = 480;

            var root = new ChartRoot();
            root.format_version = 0;
            root.time_base = 480;
            root.start_offset_time = 0;

            var tempo = new ChartTempo();
            tempo.tick = 0;
            var tempoValue = chart.PageDuration * 1000000;
            tempo.value = tempoValue;
            root.tempo_list = new List<ChartTempo> { tempo };

            if (chart.PageShift < 0) chart.PageShift = chart.PageShift + 2 * chart.PageDuration;
            
            var pageShiftTickOffset = chart.PageShift / chart.PageDuration * timeBase;

            var noteList = new List<ChartNote>();
            var page = 0;
            foreach (var note in chart.Notes.Values)
            {
                var obj = new ChartNote();
                obj.id = note.id;
                switch (note.type)
                {
                    case OldNoteType.Single:
                        obj.type = NoteType.Click;
                        break;
                    case OldNoteType.Chain:
                        obj.type = note.isChainHead ? NoteType.DragHead : NoteType.DragChild;
                        break;
                    case OldNoteType.Hold:
                        obj.type = NoteType.Hold;
                        break;
                }
                obj.x = note.x;
                var ti = note.time * timeBase * 1000000 / tempoValue + pageShiftTickOffset;
                obj.tick = ti;
                obj.hold_tick = note.duration * timeBase * 1000000 / tempoValue;
                page = Mathf.FloorToInt(ti / timeBase);
                obj.page_index = page;
                if (note.type == OldNoteType.Chain)
                {
                    obj.next_id = note.connectedNote != null ? note.connectedNote.id : -1;
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

    }

}