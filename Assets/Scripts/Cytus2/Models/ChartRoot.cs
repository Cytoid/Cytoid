
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Cytus2.Models
{

    [System.Serializable]
    public class ChartRoot
    {

        public int format_version;
        public int time_base;

        public float start_offset_time;
        public List<ChartPage> page_list;
        public List<ChartTempo> tempo_list;
        public List<ChartEventOrder> event_order_list;
        public List<ChartAnimation> animation_list = new List<ChartAnimation>();
        public List<ChartNote> note_list;
        
    }

    [System.Serializable]
    public class ChartPage
    {
        public int start_tick;
        public int end_tick;
        public int scan_line_direction;
        public float start_time;
        public float end_time;

        public float Duration
        {
            get { return end_time - start_time; }
        }
    }

    [System.Serializable]
    public class ChartTempo
    {
        public int tick;
        public int value;
    }

    [System.Serializable]
    public class ChartEvent
    {
        public int type;
        public string args;
    }

    [System.Serializable]
    public class ChartEventOrder
    {
        public int tick;
        public float time;
        public List<ChartEvent> event_list;
    }

    [System.Serializable]
    public class ChartAnimation
    {
        public int tick;
        public float time;
        public int type;
        public string new_text;
        public float transition_time;
    }

    [System.Serializable]
    public class ChartNote
    {
        public int page_index;
        public int type;
        public int id;
        public int tick;
        public float x;
        public bool has_sibling;
        public int hold_tick;
        public int next_id;
        public bool is_forward;
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

        public float Duration
        {
            get { return end_time - start_time; }
        }
        
    }

}