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
    public Dictionary<int, Note> note_map = new Dictionary<int, Note>();
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
        public double opacity = double.MinValue;
        public string ring_color;
        public string fill_color;

        public float y;
        public float start_time;
        public float end_time;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 end_position;
        public float holdlength;
        public float speed;
        public float intro_time;
        public int direction;
        public float tint;
        public float nextdraglinestarttime;
        public float nextdraglinestoptime;
        public int style = 1;

        public NoteOverride Override { get; } = new NoteOverride();

        public class NoteOverride
        {
            public float? X;
            public float? Y;
            public float? Z;
            public float? RotX;
            public float? RotY;
            public float? RotZ;
            public float XMultiplier = 1;
            public float YMultiplier = 1;
            public float XOffset = 0;
            public float YOffset = 0;
            public Color? RingColor;
            public Color? FillColor;
            public float OpacityMultiplier = 1;
            public float SizeMultiplier = 1;
        }
        
        public float Duration => end_time - start_time;

        public Note GetDragEndNote(ChartModel parent)
        {
            Assert.IsTrue((NoteType) type == NoteType.DragHead || (NoteType) type == NoteType.DragChild || (NoteType) type == NoteType.CDragHead || (NoteType) type == NoteType.CDragChild, $"Expected (C)DragHead or (C)DragChild, but type is {((NoteType) type).ToString()} for note {id}");
            return next_id <= 0 ? this : parent.note_map[next_id].GetDragEndNote(parent);
        }

        public bool UseAlternativeColor()
        {
            var alt = direction > 0;
            if (is_forward) alt = !alt;
            return alt;
        }

        public void PasteFrom(Note note)
        {
            page_index = note.page_index;
            type = note.type;
            id = note.id;
            tick = note.tick;
            x = note.x;
            has_sibling = note.has_sibling;
            hold_tick = note.hold_tick;
            next_id = note.next_id;
            is_forward = note.is_forward;
            approach_rate = note.approach_rate;
            size = note.size;
            ring_color = note.ring_color;
            fill_color = note.fill_color;
            opacity = note.opacity;
            start_time = note.start_time;
            end_time = note.end_time;
            position = note.position;
            end_position = note.end_position;
            holdlength = note.holdlength;
            speed = note.speed;
            intro_time = note.intro_time;
            direction = note.direction;
            rotation = note.rotation;
            tint = note.tint;
            nextdraglinestarttime = note.nextdraglinestarttime;
            nextdraglinestoptime = note.nextdraglinestoptime;
            style = note.style;
        }
    }
}