using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class OnlineLevel
{
    public string uid;
    public string title;
    public Metadata metadata;
    public DateTime creationDate;
    public DateTime modificationDate;
    public Bundle bundle;
    public OnlineUser owner;
    public Chart[] charts;
    public double rating;
    public int plays;
    public int downloads;
    
    // Extra info with /levels/{uid}
    public double duration; // in seconds
    public long size; // in bytes
    public string description;
    public string[] tags;

    [Serializable]
    public class Metadata
    {
        public LevelMeta raw;
        public string title_localized;
        public Artist artist;
        public Charter charter;
        public Illustrator illustrator;

        [Serializable]
        public class Artist
        {
            public string name;
            public string localized_name;
            public string url;
        }

        [Serializable]
        public class Charter
        {
            public string name;
        }

        [Serializable]
        public class Illustrator
        {
            public string name;
            public string url;
        }
    }

    [Serializable]
    public class Bundle
    {
        public string background;
        public string music;
        public string music_preview;
    }

    [Serializable]
    public class Chart
    {
        public string type;
        public string name;
        public int difficulty;
        public int notesCount;
    }

    public Level ToLevel(bool resolveLocalLevel = true)
    {
        if (resolveLocalLevel && Context.LevelManager.LoadedLevels.Any(it => it.Meta.id == uid))
        {
            Debug.Log($"Online level {uid} resolved locally");
            return Context.LevelManager.LoadedLevels.Find(it => it.Meta.id == uid);
        }
        var level = new Level($"{Context.ApiBaseUrl}/levels/{uid}/resources",
            metadata.raw.JsonDeepCopy());
        level.Meta.SortCharts();
        level.Meta.background.path = bundle.background;
        level.Meta.music.path = bundle.music;
        level.Meta.music_preview.path = bundle.music_preview;
        return level;
    }
}