using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalPlayer
{
    
    public int Language
    {
        get => PlayerPrefs.GetInt("Language", 0);
        set => PlayerPrefs.SetInt("Language", value);
    }
    
    public bool PlayRanked
    {
        get => PlayerPrefsExtensions.GetBool("ranked");
        set => PlayerPrefsExtensions.SetBool("ranked", value);
    }
    
    public HashSet<Mod> EnabledMods
    {
        get => new HashSet<Mod>(PlayerPrefsExtensions.GetStringArray("mods").Select(it => (Mod) Enum.Parse(typeof(Mod), it)).ToList());
        set => PlayerPrefsExtensions.SetStringArray("mods", value.Select(it => it.ToString()).ToArray());
    }
    
    public bool ShowBoundaries
    {
        get => PlayerPrefsExtensions.GetBool("boundaries", false);
        set => PlayerPrefsExtensions.SetBool("boundaries", value);
    }
    
    public bool DisplayEarlyLateIndicators
    {
        get => PlayerPrefsExtensions.GetBool("early_late_indicator", true);
        set => PlayerPrefsExtensions.SetBool("early_late_indicator", value);
    }

    public int ClickHitboxSize
    {
        get => PlayerPrefs.GetInt("click hitbox size", 2);
        set => PlayerPrefs.SetInt("click hitbox size", value);
    }
    
    public int DragHitboxSize
    {
        get => PlayerPrefs.GetInt("drag hitbox size", 2);
        set => PlayerPrefs.SetInt("drag hitbox size", value);
    }

    public int HoldHitboxSize
    {
        get => PlayerPrefs.GetInt("hold hitbox size", 2);
        set => PlayerPrefs.SetInt("hold hitbox size", value);
    }
    
    public int FlickHitboxSize
    {
        get => PlayerPrefs.GetInt("flick hitbox size", 1);
        set => PlayerPrefs.SetInt("flick hitbox size", value);
    }

    public int HoldHitSoundTiming
    {
        get => PlayerPrefs.GetInt("HoldHitSoundTiming", (int) global::HoldHitSoundTiming.Both);
        set => PlayerPrefs.SetInt("HoldHitSoundTiming", value);
    }
    
    // Bounded by -0.5~0.5.
    public float NoteSize
    {
        get => PlayerPrefs.GetFloat("NoteSize", 0);
        set => PlayerPrefs.SetFloat("NoteSize", value);
    }
    
    // Bounded by 1~5.
    public int HorizontalMargin
    {
        get => (int) PlayerPrefs.GetFloat("HorizontalMargin", 3);
        set => PlayerPrefs.SetFloat("HorizontalMargin", value);
    }
    
    // Bounded by 1~5.
    public int VerticalMargin
    {
        get => (int) PlayerPrefs.GetFloat("VerticalMargin", 3);
        set => PlayerPrefs.SetFloat("VerticalMargin", value);
    }
    
    // Bounded by 0~1.
    public float CoverOpacity
    {
        get => PlayerPrefs.GetFloat("CoverOpacity", 0.15f);
        set => PlayerPrefs.SetFloat("CoverOpacity", value);
    }

    // Bounded by 0~1.
    public float MusicVolume
    {
        get => PlayerPrefs.GetFloat("MusicVolume", 0.85f);
        set => PlayerPrefs.SetFloat("MusicVolume", value);
    }
    
    // Bounded by 0~1.
    public float SoundEffectsVolume
    {
        get => PlayerPrefs.GetFloat("SoundEffectsVolume", 1f);
        set => PlayerPrefs.SetFloat("SoundEffectsVolume", value);
    }

    public string HitSound
    {
        get => PlayerPrefs.GetString("HitSound", "none").ToLower();
        set => PlayerPrefs.SetString("HitSound", value.ToLower());
    }

    public bool HitTapticFeedback
    {
        get => PlayerPrefsExtensions.GetBool("HitTapticFeedback", true);
        set => PlayerPrefsExtensions.SetBool("HitTapticFeedback", value);
    }

    public bool UseStoryboardEffects
    {
        get => PlayerPrefsExtensions.GetBool("StoryboardEffects", true);
        set => PlayerPrefsExtensions.SetBool("StoryboardEffects", value);
    }

    public string GraphicsQuality
    {
        get => PlayerPrefs.GetString("GraphicsQuality",
            Application.platform == RuntimePlatform.Android ? "medium" : "high");
        set => PlayerPrefs.SetString("GraphicsQuality", value.ToLower());
    }

    public float BaseNoteOffset
    {
        get => PlayerPrefs.GetFloat("main chart offset", 
            Application.platform == RuntimePlatform.Android ? 0.2f : 0.1f);
        set => PlayerPrefs.SetFloat("main chart offset", value);
    }

    public float HeadsetNoteOffset
    {
        get => PlayerPrefs.GetFloat("headset chart offset", -0.05f);
        set => PlayerPrefs.SetFloat("headset chart offset", value);
    }
    
    public float ClearFXSize
    {
        get => PlayerPrefs.GetFloat("ClearFXSize", 0);
        set => PlayerPrefs.SetFloat("ClearFXSize", value);
    }
    
    public bool DisplayProfiler
    {
        get => PlayerPrefsExtensions.GetBool("profiler", true);
        set
        {
            PlayerPrefsExtensions.SetBool("profiler", value);
            Context.UpdateProfilerDisplay();
        }
    }

    public bool DisplayNoteIds
    {
        get => PlayerPrefsExtensions.GetBool("note ids");
        set => PlayerPrefsExtensions.SetBool("note ids", value);
    }

    public string LocalLevelsSortBy
    {
        get => PlayerPrefs.GetString("local levels sort by", LevelSort.AddedDate.ToString());
        set => PlayerPrefs.SetString("local levels sort by", value);
    }
    
    public int DspBufferSize
    {
        get => PlayerPrefs.GetInt("AndroidDspBufferSize", -1);
        set => PlayerPrefs.SetInt("AndroidDspBufferSize", value);
    }

    public bool LocalLevelsSortInAscendingOrder
    {
        get => PlayerPrefsExtensions.GetBool("local levels sort in ascending order", false);
        set => PlayerPrefsExtensions.SetBool("local levels sort in ascending order", value);
    }

    public float GetLevelNoteOffset(string levelId)
    {
        return PlayerPrefs.GetFloat($"level {levelId} chart offset", 0);
    }
    
    public void SetLevelNoteOffset(string levelId, float offset)
    {
        PlayerPrefs.SetFloat($"level {levelId} chart offset", offset);
    }

    public Color GetRingColor(NoteType type, bool alt)
    {
        return PlayerPrefsExtensions.GetColor("ring color", "#FFFFFF");
    }

    public void SetRingColor(NoteType type, bool alt, Color color)
    {
        PlayerPrefsExtensions.SetColor("ring color", color);
    }

    private static Dictionary<NoteType, string> NoteTypeConfigKeyMapping = new Dictionary<NoteType, string>
    {
        {NoteType.Click, "click"}, {NoteType.DragHead, "drag"}, {NoteType.DragChild, "drag"}, 
        {NoteType.Hold, "hold"}, {NoteType.LongHold, "long hold"}, {NoteType.Flick, "flick"}
    };

    private static Dictionary<NoteType, string[]> NoteTypeDefaultFillColors = new Dictionary<NoteType, string[]>
    {
        {NoteType.Click, new[] {"#35A7FF", "#FF5964"}},
        {NoteType.DragHead, new[] {"#39E59E", "#39E59E"}},
        {NoteType.DragChild, new[] {"#39E59E", "#39E59E"}},
        {NoteType.Hold, new[] {"#35A7FF", "#FF5964"}},
        {NoteType.LongHold, new[] {"#F2C85A", "#F2C85A"}},
        {NoteType.Flick, new[] {"#35A7FF", "#FF5964"}}
    };

    public Color GetFillColor(NoteType type, bool alt)
    {
        return PlayerPrefsExtensions.GetColor($"fill color ({NoteTypeConfigKeyMapping[type]} {(alt ? 2 : 1)})", NoteTypeDefaultFillColors[type][alt ? 1 : 0]);
    }
    
    public void SetFillColor(NoteType type, bool alt, Color color)
    {
        PlayerPrefsExtensions.SetColor($"fill color ({NoteTypeConfigKeyMapping[type]} {(alt ? 2 : 1)})", color);
    }

    public class Performance
    {
        public int Score;
        public float Accuracy; // 0~100
        public string ClearType;
    }
    
    public bool HasPerformance(string levelId, string chartType, bool ranked)
    {
        return GetBestPerformance(levelId, chartType, ranked).Score >= 0;
    }

    public Performance GetBestPerformance(string levelId, string chartType, bool ranked)
    {
        return new Performance
        {
            Score = (int) SecuredPlayerPrefs.GetFloat(BestScoreKey(levelId, chartType, ranked), -1),
            Accuracy = SecuredPlayerPrefs.GetFloat(BestAccuracyKey(levelId, chartType, ranked), -1),
            ClearType = SecuredPlayerPrefs.GetString(BestClearTypeKey(levelId, chartType, ranked), "")
        };
    }

    public void SetBestPerformance(string levelId, string chartType, bool ranked, Performance performance)
    {
        SecuredPlayerPrefs.SetFloat(BestScoreKey(levelId, chartType, ranked), performance.Score);
        SecuredPlayerPrefs.SetFloat(BestAccuracyKey(levelId, chartType, ranked), performance.Accuracy);
        SecuredPlayerPrefs.SetString(BestClearTypeKey(levelId, chartType, ranked), performance.ClearType);
    }
    
    public DateTime GetAddedDate(string levelId)
    {
        return SecuredPlayerPrefs.HasKey(AddedKey(levelId)) ? 
            DateTime.Parse(SecuredPlayerPrefs.GetString(AddedKey(levelId), null)) : 
            default;
    }
    
    public void SetAddedDate(string levelId, DateTime dateTime)
    {
        SecuredPlayerPrefs.SetString(AddedKey(levelId), dateTime.ToString("s"));
    }
    
    public DateTime GetLastPlayedDate(string levelId)
    {
        return SecuredPlayerPrefs.HasKey(LastPlayedKey(levelId)) ? 
            DateTime.Parse(SecuredPlayerPrefs.GetString(LastPlayedKey(levelId), null)) : 
            DateTime.MinValue;
    }

    public void SetLastPlayedDate(string levelId, DateTime dateTime)
    {
        SecuredPlayerPrefs.SetString(LastPlayedKey(levelId), dateTime.ToString("s"));
    }

    public int GetPlayCount(string levelId, string chartType)
    {
        return SecuredPlayerPrefs.GetInt(PlayCountKey(levelId, chartType), 0);
    }
    
    public void SetPlayCount(string levelId, string chartType, int playCount)
    {
        SecuredPlayerPrefs.SetInt(PlayCountKey(levelId, chartType), playCount);
    }

    private static string AddedKey(string level) => level + " : " + "added";
    
    private static string LastPlayedKey(string level) => level + " : " + "last played";
    
    private static string BestScoreKey(string level, string type, bool ranked) => level + " : " + type + " : " + "best score" + (ranked ? " ranked" : "");

    private static string BestAccuracyKey(string level, string type, bool ranked) => level + " : " + type + " : " + "best accuracy" + (ranked ? " ranked" : "");

    private static string BestClearTypeKey(string level, string type, bool ranked) => level + " : " + type + " : " + "best clear type" + (ranked ? " ranked" : "");

    private static string PlayCountKey(string level, string type) => level + " : " + type + " : " + "play count";
    
}