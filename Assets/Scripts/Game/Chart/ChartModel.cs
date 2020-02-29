using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class ChartModel
{
    public double time_base;
    
    public List<Tempo> tempo_list = new List<Tempo>();
    public List<Page> page_list = new List<Page>();
    public List<Note> note_list = new List<Note>();
    [JsonIgnore] public Dictionary<int, Note> note_map = new Dictionary<int, Note>();
    public List<EventOrder> event_order_list = new List<EventOrder>();

    public double music_offset;
    public double size = 1.0;
    public double opacity = 1.0;
    public string ring_color;
    public string[] fill_colors = new string[10];
    
    [Serializable]
    public class Page
    {
        public double start_tick;
        public double end_tick;
        public int scan_line_direction;

        public float start_time;
        public float end_time;
        public float actual_start_tick;
        public float actual_start_time;

        public float Duration
        {
            get { return end_time - start_time; }
        }
    }

    [Serializable]
    public class Tempo
    {
        public double tick;
        public long value;
    }

    [Serializable]
    public class ChartEvent
    {
        public int type;
        public string args;
    }

    [Serializable]
    public class EventOrder
    {
        public int tick;
        public float time;
        public List<ChartEvent> event_list;
    }

    [Serializable]
    public class Animation
    {
        public int tick;
        public float time;
        public int type;
        public string new_text;
        public float transition_time;
    }

    [Serializable]
    public class Note
    {
        public int page_index;
        public int type;
        public int id;
        public double tick;
        public double x;
        public bool has_sibling;
        public double hold_tick;
        public int next_id;
        public bool is_forward;

        public double approach_rate = 1.0;
        public double size = double.MinValue;
        public string ring_color;
        public string fill_color;
        public double opacity = double.MinValue;

        public float start_time;
        public float end_time;
        public Vector3 position;
        public Vector3 end_position;
        public float holdlength;
        public float speed;
        public float intro_time;
        public int direction;
        public float rotation;
        public float tint;
        public float nextdraglinestarttime;
        public float nextdraglinestoptime;

        public float Duration => end_time - start_time;

        public Note GetDragEndNote(ChartModel parent)
        {
            Assert.IsTrue((NoteType) type == NoteType.DragHead || (NoteType) type == NoteType.DragChild, $"Expected DragHead or DragChild, but type is {((NoteType) type).ToString()} for note {id}");
            return next_id <= 0 ? this : parent.note_map[next_id].GetDragEndNote(parent);
        }

        public bool UseAlternativeColor()
        {
            var alt = direction > 0;
            if (is_forward) alt = !alt;
            return alt;
        }
    }
}