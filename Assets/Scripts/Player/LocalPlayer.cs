using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalPlayer
{
    
    public bool PlayRanked
    {
        get => PlayerPrefsExtensions.GetBool("ranked");
        set => PlayerPrefsExtensions.SetBool("ranked", value);
    }
    
    public List<Mod> EnabledMods
    {
        get => PlayerPrefsExtensions.GetStringArray("mods").Select(it => (Mod) Enum.Parse(typeof(Mod), it)).ToList();
        set => PlayerPrefsExtensions.SetStringArray("mods", value.Select(it => it.ToString()).ToArray());
    }
    
    public bool ShowBoundaries
    {
        get => PlayerPrefsExtensions.GetBool("boundaries", true);
        set => PlayerPrefsExtensions.SetBool("boundaries", value);
    }
    
    public bool DisplayEarlyLateIndicators
    {
        get => PlayerPrefsExtensions.GetBool("early_late_indicator");
        set => PlayerPrefsExtensions.SetBool("early_late_indicator", value);
    }

    public bool UseLargerHitboxes
    {
        get => PlayerPrefsExtensions.GetBool("larger_hitboxes");
        set => PlayerPrefsExtensions.SetBool("larger_hitboxes", value);
    }

    public bool PlayHitSoundsEarly
    {
        get => PlayerPrefsExtensions.GetBool("early hit sounds");
        set => PlayerPrefsExtensions.SetBool("early hit sounds", value);
    }
    
    public float NoteSize
    {
        get => PlayerPrefs.GetFloat("note size", 4);
        set => PlayerPrefs.SetFloat("note size", value);
    }
    
    public float HorizontalMargin
    {
        get => PlayerPrefs.GetFloat("horizontal margin", 3);
        set => PlayerPrefs.SetFloat("horizontal margin", value);
    }
    
    public float VerticalMargin
    {
        get => PlayerPrefs.GetFloat("vertical margin", 3);
        set => PlayerPrefs.SetFloat("vertical margin", value);
    }
    
    public float CoverOpacity
    {
        get => 1 - PlayerPrefs.GetFloat("background dim", 17) / 20.0f;
        set => PlayerPrefs.SetFloat("background dim", 20 - Mathf.Clamp01(value) * 20.0f);
    }

    public string HitSound
    {
        get => PlayerPrefs.GetString("hit sound", "none").ToLower();
        set => PlayerPrefs.SetString("hit sound", value.ToLower());
    }
    
    public string GraphicsLevel
    {
        get => PlayerPrefs.GetString("storyboard effects", "high").ToLower();
        set => PlayerPrefs.SetString("storyboard effects", value.ToLower());
    }
    
    public bool LowerResolution
    {
        get => PlayerPrefsExtensions.GetBool("low res", false);
        set => PlayerPrefsExtensions.SetBool("low res", value);
    }

    public float BaseLevelOffset
    {
        get => PlayerPrefs.GetFloat("main chart offset", 0);
        set => PlayerPrefs.SetFloat("main chart offset", value);
    }

    public float HeadsetLevelOffset
    {
        get => PlayerPrefs.GetFloat("headset chart offset", 0);
        set => PlayerPrefs.SetFloat("headset chart offset", value);
    }
    
    public bool DisplayProfiler
    {
        get => PlayerPrefsExtensions.GetBool("profiler");
        set => PlayerPrefsExtensions.SetBool("profiler", value);
    }

    public bool DisplayNoteIds
    {
        get => PlayerPrefsExtensions.GetBool("note ids");
        set => PlayerPrefsExtensions.SetBool("note ids", value);
    }

    public float GetLevelNoteOffset(string levelId)
    {
        return PlayerPrefs.GetFloat($"level {levelId} chart offset", 0);
    }
    
    public void SetLevelChartOffset(string levelId, float offset)
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
        public float Score;
        public float Accuracy;
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
            Score = SecuredPlayerPrefs.GetFloat(BestScoreKey(levelId, chartType, ranked), -1),
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

    public int GetPlayCount(string levelId, string chartType)
    {
        return SecuredPlayerPrefs.GetInt(PlayCountKey(levelId, chartType), 0);
    }
    
    public void SetPlayCount(string levelId, string chartType, int playCount)
    {
        SecuredPlayerPrefs.SetInt(PlayCountKey(levelId, chartType), playCount);
    }
    
    private static string BestScoreKey(string level, string type, bool ranked) => level + " : " + type + " : " + "best score" + (ranked ? " ranked" : "");

    private static string BestAccuracyKey(string level, string type, bool ranked) => level + " : " + type + " : " + "best accuracy" + (ranked ? " ranked" : "");

    private static string BestClearTypeKey(string level, string type, bool ranked) => level + " : " + type + " : " + "best clear type" + (ranked ? " ranked" : "");

    private static string PlayCountKey(string level, string type) => level + " : " + type + " : " + "play count";
    
}