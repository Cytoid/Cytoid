using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class OnlineLevel
{
    [JsonProperty("uid")] public string Uid { get; set; }
    [JsonProperty("version")] public int Version { get; set; }

    [JsonProperty("title")] public string Title { get; set; }
    [JsonProperty("metadata")] public OnlineLevelMetadata Metadata { get; set; }
    [JsonProperty("bundle")] public OnlineLevelBundle Bundle { get; set; }
    [JsonProperty("owner")] public OnlineUser Owner { get; set; }
    [JsonProperty("charts")] public OnlineLevelChart[] Charts { get; set; }
    [JsonProperty("rating")] public double? Rating { get; set; }
    [JsonProperty("plays")] public int Plays { get; set; }
    [JsonProperty("downloads")] public int Downloads { get; set; }

    // Extra info with /levels/{uid}
    [JsonProperty("duration")] public double Duration { get; set; } // in seconds
    [JsonProperty("size")] public long Size { get; set; } // in bytes
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("tags")] public string[] Tags { get; set; }

    [Serializable]
    public class OnlineLevelMetadata
    {
        [JsonProperty("title_localized")] public string LocalizedTitle { get; set; }
        [JsonProperty("artist")] public ArtistMeta Artist { get; set; }
        [JsonProperty("charter")] public CharterMeta Charter { get; set; }
        [JsonProperty("illustrator")] public IllustratorMeta Illustrator { get; set; }

        [Serializable]
        public class ArtistMeta
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("localized_name")] public string LocalizedName { get; set; }
            [JsonProperty("url")] public string Url { get; set; }
        }

        [Serializable]
        public class CharterMeta
        {
            [JsonProperty("name")] public string Name { get; set; }
        }

        [Serializable]
        public class IllustratorMeta
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("url")] public string Url { get; set; }
        }
    }

    [Serializable]
    public class OnlineLevelBundle
    {
        [JsonProperty("background")] public string BackgroundUrl { get; set; }
        [JsonProperty("music")] public string MusicUrl { get; set; }
        [JsonProperty("music_preview")] public string MusicPreviewUrl { get; set; }
    }

    [Serializable]
    public class OnlineLevelChart
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("difficulty")] public int Difficulty { get; set; }
        [JsonProperty("notesCount")] public int NotesCount { get; set; }
    }

    // TODO: NEO!!!!

    [JsonProperty("music")] public string MusicUrl { get; set; }
    [JsonProperty("music_preview")] public string MusicPreviewUrl { get; set; }
    [JsonProperty("cover")] public OnlineImageAsset Cover { get; set; }

    public Level ToLevel(LevelType type, bool resolveLocalLevel = true)
    {
        if (resolveLocalLevel && Context.LevelManager.LoadedLocalLevels.ContainsKey(Uid))
        {
            var localLevel = Context.LevelManager.LoadedLocalLevels[Uid];
            if (localLevel.Type == type)
            {
                Debug.Log($"Online level {Uid} resolved locally");
                return localLevel;
            }
        }

        return new Level($"{Context.ApiUrl}/levels/{Uid}/resources", type, GenerateLevelMeta())
            .Also(it => it.OnlineLevel = this);
    }

    public LevelMeta GenerateLevelMeta()
    {
        var meta = new LevelMeta();
        meta.schema_version = 2;
        meta.version = Version;
        meta.id = Uid;
        meta.title = Title;
        meta.title_localized = Metadata.LocalizedTitle;
        meta.artist = Metadata.Artist.Name;
        meta.artist_localized = Metadata.Artist.LocalizedName;
        meta.artist_source = Metadata.Artist.Url;
        meta.illustrator = Metadata.Illustrator.Name;
        meta.illustrator_source = Metadata.Illustrator.Url;
        meta.charter = Metadata.Charter.Name;
        meta.charts = Charts.Select(onlineChart => new LevelMeta.ChartSection
        {
            type = onlineChart.Type, name = onlineChart.Name, difficulty = onlineChart.Difficulty
        }).ToList();
        meta.background = new LevelMeta.BackgroundSection {path = Bundle?.BackgroundUrl ?? Cover.CoverUrl};
        meta.music = new LevelMeta.MusicSection {path = Bundle?.MusicUrl ?? MusicUrl};
        meta.music_preview = new LevelMeta.MusicSection {path = Bundle?.MusicPreviewUrl ?? MusicPreviewUrl};
        meta.SortCharts();
        return meta;
    }
}

[Serializable]
public class OnlineImageAsset
{
    [JsonProperty("original")] public string OriginalUrl { get; set; }
    [JsonProperty("thumbnail")] public string ThumbnailUrl { get; set; }
    [JsonProperty("cover")] public string CoverUrl { get; set; }
    [JsonProperty("stripe")] public string StripeUrl { get; set; }
}