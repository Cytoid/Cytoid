using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class OnlineLevel
{
    public string uid;
    public int version;

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
    
    // TODO: NEO!!!!
    
    public string music;
    public string music_preview;
    public OnlineImageAsset cover;

    public Level ToLevel(bool resolveLocalLevel = true)
    {
        if (resolveLocalLevel && Context.LevelManager.LoadedLocalLevels.ContainsKey(uid))
        {
            Debug.Log($"Online level {uid} resolved locally");
            return Context.LevelManager.LoadedLocalLevels[uid];
        }

        return new Level($"{Context.ApiUrl}/levels/{uid}/resources", GenerateLevelMeta())
            .Also(it => it.OnlineLevel = this);
    }

    public LevelMeta GenerateLevelMeta()
    {
        var meta = new LevelMeta();
        meta.schema_version = 2;
        meta.version = version;
        meta.id = uid;
        meta.title = title;
        meta.title_localized = metadata.title_localized;
        meta.artist = metadata.artist.name;
        meta.artist_localized = metadata.artist.localized_name;
        meta.artist_source = metadata.artist.url;
        meta.illustrator = metadata.illustrator.name;
        meta.illustrator_source = metadata.illustrator.url;
        meta.charter = metadata.charter.name;
        meta.charts = charts.Select(onlineChart => new LevelMeta.ChartSection
        {
            type = onlineChart.type, name = onlineChart.name, difficulty = onlineChart.difficulty
        }).ToList();
        meta.background = new LevelMeta.BackgroundSection {path = bundle?.background ?? cover.cover};
        meta.music = new LevelMeta.MusicSection {path = bundle?.music ?? music};
        meta.music_preview = new LevelMeta.MusicSection {path = bundle?.music_preview ?? music_preview};
        meta.SortCharts();
        return meta;
    }
}

[Serializable]
public class OnlineImageAsset
{
    public string original;
    public string thumbnail;
    public string cover;
    public string stripe;
}