using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RankedPlayData
{

    public string user;
    public string password;
    public long start;
    public long end;
    public string id;
    public string type;
    public int version;
    public string mods;
    
    public long score;
    public int accuracy;
    public int max_combo;
    public int perfect;
    public int great;
    public int good;
    public int bad;
    public int miss;

    public List<Note> notes = new List<Note>();
    public List<Pause> pauses = new List<Pause>();
    
    public Device device = new Device();
    
    public string chart_checksum;
    public string checksum;

    [Serializable]
    public class Note
    {
        public int id;
        public long press_time;
        public int press_x;
        public int press_y;
        public long release_time;
        public int release_x;
        public int release_y;
    }

    [Serializable]
    public class Pause
    {
        public long start;
        public long end;
    }
    
    [Serializable]
    public class Device
    {
        public int width;
        public int height;
        public int dpi;
        public string model;
    }
    
}