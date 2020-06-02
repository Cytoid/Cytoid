using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polyglot;
using UnityEngine;
using JsonReader = Newtonsoft.Json.JsonReader;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using JsonWriter = Newtonsoft.Json.JsonWriter;

[Serializable]
public class LocalPlayerSettings
{

    public void FillDefault()
    {
        var dummy = new LocalPlayerSettings();
        NoteRingColors = RightJoin(NoteRingColors, dummy.NoteRingColors);
        NoteFillColors = RightJoin(NoteFillColors, dummy.NoteFillColors);
        NoteFillColorsAlt = RightJoin(NoteFillColorsAlt, dummy.NoteFillColorsAlt);
    }

    private Dictionary<TK, TV> RightJoin<TK, TV>(Dictionary<TK, TV> from, Dictionary<TK, TV> to)
    {
        var dummy = new Dictionary<TK, TV>(to);
        from.ToList().ForEach(x => dummy[x.Key] = x.Value);
        return dummy;
    }

    public ObjectId Id { get; set; }
    
    [JsonProperty("schema_version")] public int SchemaVersion { get; set; }

    [JsonProperty("cdn_region")] public CdnRegion CdnRegion { get; set; } = CdnRegion.International;

    [JsonProperty("player_id")] public string PlayerId { get; set; }

    [JsonProperty("login_token")] public string LoginToken { get; set; }

    [JsonProperty("character_id")] public string ActiveCharacterId { get; set; }

    [JsonProperty("language")] public int Language { get; set; } = 0;
    [JsonProperty("play_ranked")] public bool PlayRanked { get; set; } = true;

    [JsonProperty("enabled_mods")] public List<Mod> EnabledMods { get; set; } = new List<Mod>();

    [JsonProperty("display_boundaries")] public bool DisplayBoundaries { get; set; } = false;

    [JsonProperty("display_early_late_indicators")]
    public bool DisplayEarlyLateIndicators { get; set; } = true;

    [JsonProperty("hitbox_sizes")]
    [JsonConverter(typeof(NoteTypeDictionaryConverter<int>))]
    public Dictionary<NoteType, int> HitboxSizes { get; set; } = new Dictionary<NoteType, int>
    {
        {NoteType.Click, 2},
        {NoteType.DragChild, 2},
        {NoteType.DragHead, 2},
        {NoteType.Hold, 2},
        {NoteType.LongHold, 2},
        {NoteType.Flick, 1},
    };

    [JsonProperty("note_ring_colors")]
    [JsonConverter(typeof(NoteTypeDictionaryConverter<Color>))]
    public Dictionary<NoteType, Color> NoteRingColors { get; set; } = new Dictionary<NoteType, Color>
    {
        {NoteType.Click, "#FFFFFF".ToColor()},
        {NoteType.DragChild, "#FFFFFF".ToColor()},
        {NoteType.DragHead, "#FFFFFF".ToColor()},
        {NoteType.Hold, "#FFFFFF".ToColor()},
        {NoteType.LongHold, "#FFFFFF".ToColor()},
        {NoteType.Flick, "#FFFFFF".ToColor()},
        {NoteType.CDragChild, "#FFFFFF".ToColor()},
        {NoteType.CDragHead, "#FFFFFF".ToColor()},
    };

    [JsonProperty("note_fill_colors")]
    [JsonConverter(typeof(NoteTypeDictionaryConverter<Color>))]
    public Dictionary<NoteType, Color> NoteFillColors { get; set; } = new Dictionary<NoteType, Color>
    {
        {NoteType.Click, "#35A7FF".ToColor()},
        {NoteType.DragChild, "#39E59E".ToColor()},
        {NoteType.DragHead, "#39E59E".ToColor()},
        {NoteType.Hold, "#35A7FF".ToColor()},
        {NoteType.LongHold, "#F2C85A".ToColor()},
        {NoteType.Flick, "#35A7FF".ToColor()},
        {NoteType.CDragChild, "#39E59E".ToColor()},
        {NoteType.CDragHead, "#39E59E".ToColor()},
    };

    [JsonProperty("note_fill_colors_alt")]
    [JsonConverter(typeof(NoteTypeDictionaryConverter<Color>))]
    public Dictionary<NoteType, Color> NoteFillColorsAlt { get; set; } = new Dictionary<NoteType, Color>
    {
        {NoteType.Click, "#FF5964".ToColor()},
        {NoteType.DragChild, "#39E59E".ToColor()},
        {NoteType.DragHead, "#39E59E".ToColor()},
        {NoteType.Hold, "#FF5964".ToColor()},
        {NoteType.LongHold, "#F2C85A".ToColor()},
        {NoteType.Flick, "#FF5964".ToColor()},
        {NoteType.CDragChild, "#39E59E".ToColor()},
        {NoteType.CDragHead, "#39E59E".ToColor()},
    };

    [JsonProperty("use_fill_color_for_drag_child_nodes")]
    public bool UseFillColorForDragChildNodes { get; set; } = true;

[JsonProperty("hold_hit_sound_timing")]
    public HoldHitSoundTiming HoldHitSoundTiming { get; set; } = HoldHitSoundTiming.Both;

    [JsonProperty("note_size")] public float NoteSize { get; set; } = 0; // -0.5~0.5
    [JsonProperty("horizontal_margin")] public int HorizontalMargin { get; set; } = 3; // 1~5
    [JsonProperty("vertical_margin")] public int VerticalMargin { get; set; } = 3; // 1~5
    [JsonProperty("cover_opacity")] public float CoverOpacity { get; set; } = 0.15f; // 0~1
    [JsonProperty("music_volume")] public float MusicVolume { get; set; } = 0.85f; // 0~1
    [JsonProperty("sound_effects_volume")] public float SoundEffectsVolume { get; set; } = 1f; // 0~1
    [JsonProperty("hit_sound")] public string HitSound { get; set; } = "click1";
    [JsonProperty("hit_taptic_feedback")] public bool HitTapticFeedback { get; set; } = true;
    [JsonProperty("menu_taptic_feedback")] public bool MenuTapticFeedback { get; set; } = true;

    [JsonProperty("display_storyboard_effects")]
    public bool DisplayStoryboardEffects { get; set; } = true;

    [JsonProperty("graphics_quality")]
    public GraphicsQuality GraphicsQuality { get; set; } =
        Application.platform == RuntimePlatform.Android ? GraphicsQuality.Medium : GraphicsQuality.High;
    [JsonProperty("base_note_offset")] public float BaseNoteOffset { get; set; } =
        Application.platform == RuntimePlatform.Android ? 0.2f : 0.1f;

    [JsonProperty("headset_note_offset")] public float HeadsetNoteOffset { get; set; } = -0.05f;
    [JsonProperty("judgment_offset")] public float JudgmentOffset { get; set; } = 0;
    [JsonProperty("clear_effects_size")] public float ClearEffectsSize { get; set; } = 0; // -0.5~0.5
    [JsonProperty("display_profiler")] public bool DisplayProfiler { get; set; } = false;
    [JsonProperty("display_note_ids")] public bool DisplayNoteIds { get; set; } = false;
    [JsonProperty("local_level_sort")] public LevelSort LocalLevelSort { get; set; } = LevelSort.AddedDate;

    [JsonProperty("use_native_audio")] public bool UseNativeAudio { get; set; } = true;

    [JsonProperty("android_dsp_buffer_size")]
    public int AndroidDspBufferSize { get; set; } = -1;

    [JsonProperty("use_experimental_note_ar")] public bool UseExperimentalNoteAr { get; set; } = true;

    [JsonProperty("use_developer_console")] public bool UseDeveloperConsole { get; set; } = true;

    [JsonProperty("local_level_sort_is_ascending")]
    public bool LocalLevelSortIsAscending { get; set; } = false;
    
    [JsonProperty("performed_one_shots")]
    public HashSet<string> PerformedOneShots { get; set; } = new HashSet<string>();

}

public enum CdnRegion
{
    International,
    MainlandChina
}

[Serializable]
public class RegionInfo
{
    public string countryCode;
    public string ip;
}

public static class CdnRegionExtensions
{
    
    public static string GetApiUrl(this CdnRegion region)
    {
        switch (region)
        {
            case CdnRegion.International:
                return "https://services.cytoid.io";
            case CdnRegion.MainlandChina:
                return "https://api.cytoid.cn";
            default:
                throw new ArgumentOutOfRangeException(nameof(region), region, null);
        }
    }
    
    public static string GetWebsiteUrl(this CdnRegion region)
    {
        switch (region)
        {
            case CdnRegion.International:
                return "https://cytoid.io";
            case CdnRegion.MainlandChina:
                return "https://cytoid.cn";
            default:
                throw new ArgumentOutOfRangeException(nameof(region), region, null);
        }
    }
    
    public static string GetAddressableRemoteBaseUrl(this CdnRegion region)
    {
        switch (region)
        {
            case CdnRegion.International:
                return "https://artifacts.cytoid.io";
            case CdnRegion.MainlandChina:
                return "https://artifacts.cytoid.cn";
            default:
                throw new ArgumentOutOfRangeException(nameof(region), region, null);
        }
    }
    
    public static string GetStoreUrl(this CdnRegion region)
    {
        switch (region)
        {
            case CdnRegion.International:
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    return "https://apps.apple.com/us/app/cytoid/id1266582726";
                }
                else
                {
                    return "https://play.google.com/store/apps/details?id=me.tigerhix.cytoid";
                }
            case CdnRegion.MainlandChina:
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    return "https://apps.apple.com/us/app/cytoid/id1266582726";
                }
                else
                {
                    return "https://www.taptap.com/app/158749";
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(region), region, null);
        }
    }
}

public enum GraphicsQuality
{
    Low,
    Medium,
    High,
    Ultra
}

public class HashSetConverter<TEnum> : JsonConverter where TEnum : Enum
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value.GetType() != typeof(HashSet<TEnum>)) throw new InvalidCastException();
        var type = (HashSet<TEnum>) value;
        JToken t = new JValue(string.Join(",", type.Select(it => Enum.GetName(typeof(TEnum), it)).ToList()));
        t.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var data = reader.ReadAsString();
        try
        {
            return data.Split(',').Select(it => (TEnum) Enum.Parse(typeof(TEnum), it)).ToHashSet();
        }
        catch
        {
            Debug.LogError($"Incorrect data: {data}");
            return new HashSet<TEnum>();
        }
    }

    public override bool CanConvert(Type objectType) => true;

    public override bool CanRead { get; } = true;
    public override bool CanWrite { get; } = true;
}

public class NoteTypeDictionaryConverter<T> : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value.GetType() != typeof(Dictionary<NoteType, T>)) throw new InvalidCastException();
        var dictionary = (Dictionary<NoteType, T>) value;
        var jObject = new JObject();
        foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType)))
        {
            jObject[((int) type).ToString()] = JsonConvert.SerializeObject(dictionary[type]);
        }

        jObject.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Parse(reader.ReadAsString());
        try
        {
            var res = new Dictionary<NoteType, T>();
            foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType)))
            {
                res[type] = serializer.Deserialize<T>(jObject.GetValue(((int) type).ToString()).CreateReader());
            }

            return res;
        }
        catch
        {
            Debug.LogError($"Incorrect data: {jObject}");
            return new Dictionary<NoteType, T>();
        }
    }

    public override bool CanConvert(Type objectType) => true;

    public override bool CanRead { get; } = true;
    public override bool CanWrite { get; } = true;
}

public class UnityColorConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value.GetType() != typeof(Color)) throw new InvalidCastException();
        var color = (Color) value;
        var jToken = new JValue("#" + ColorUtility.ToHtmlStringRGB(color));
        jToken.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var data = reader.ReadAsString();
        return data.ToColor();
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(Color);

    public override bool CanRead { get; } = true;
    public override bool CanWrite { get; } = true;
}