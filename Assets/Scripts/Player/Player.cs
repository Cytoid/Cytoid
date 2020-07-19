using System;
using System.Collections.Generic;
using System.Linq;
using Polyglot;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Player
{

    public string Id => Settings.PlayerId;

    public LocalPlayerSettings Settings { get; private set; }

    public bool ShouldMigrate { get; private set; }

    private readonly LocalPlayerLegacy legacy = new LocalPlayerLegacy();

    public void Initialize()
    {
        LoadSettings();
        ValidateData();
    }

    public bool ShouldEnableDebug()
    {
        return Id == "tigerhix" || Id == "neo";
    }

    public void ValidateData()
    {
        var col = Context.Database.GetCollection<LevelRecord>("level_records");
        col.DeleteMany(it => it.LevelId == null);

        if (!Localization.Instance.SupportedLanguages.Contains((Language) Settings.Language))
        {
            Settings.Language = (int) Language.English;
        }
    }
    
    public void LoadSettings()
    {
        Context.Database.Let(it =>
        {
            if (!it.CollectionExists("settings"))
            {
                Debug.LogWarning("Cannot find 'settings' collections");
            }
            var col = it.GetCollection<LocalPlayerSettings>("settings");
            var result = col.FindOne(x => true);

            if (result == null)
            {
                Debug.LogWarning("First time startup. Initializing settings...");
                // TODO: Remove migration... one day
                ShouldMigrate = true;
                Debug.LogWarning("Inserted settings");
                result = new LocalPlayerSettings(); //InitializeSettings();
                Debug.LogWarning("Inserted22");
                col.Insert(result);
                Debug.LogWarning("Inserted 33");
            }
            
            Settings = result;
            Debug.LogWarning("Ready to fill default");
            FillDefault();
            Debug.LogWarning("Ready To save");
            SaveSettings();
            Debug.LogWarning("Saved");
        });
    }

    public void SaveSettings()
    {
        if (Settings == null) throw new InvalidOperationException();
        Context.Database.Let(it =>
        {
            var col = it.GetCollection<LocalPlayerSettings>("settings");
            col.DeleteMany(x => true);
            col.Insert(Settings);
        });
    }

    private void FillDefault()
    {
        var dummy = new LocalPlayerSettings();
        Settings.NoteRingColors = dummy.NoteRingColors.WithOverrides(Settings.NoteRingColors);
        Settings.NoteFillColors = dummy.NoteFillColors.WithOverrides(Settings.NoteFillColors);
        Settings.NoteFillColorsAlt = dummy.NoteFillColorsAlt.WithOverrides(Settings.NoteFillColorsAlt);
        if (ShouldOneShot("Reset Graphics Quality"))
        {
            Settings.GraphicsQuality = GetDefaultGraphicsQuality();
        }
        if (ShouldOneShot("Enable/Disable Menu Transitions Based On Graphics Quality"))
        {
            Settings.UseMenuTransitions = Settings.GraphicsQuality >= GraphicsQuality.High;
        }
    }

    public async UniTask Migrate()
    {
        await UniTask.DelayFrame(30);
        try
        {
            Context.Database.Let(it =>
            {
                foreach (var level in Context.LevelManager.LoadedLocalLevels.Values)
                {
                    if (level.Id == null) continue;
                    var record = new LevelRecord
                    {
                        LevelId = level.Id,
                        RelativeNoteOffset = legacy.GetLevelNoteOffset(level.Id),
                        AddedDate = legacy.GetAddedDate(level.Id).Let(time =>
                            time == default ? DateTimeOffset.MinValue : new DateTimeOffset(time)),
                        LastPlayedDate = legacy.GetLastPlayedDate(level.Id).Let(time =>
                            time == default ? DateTimeOffset.MinValue : new DateTimeOffset(time)),
                        BestPerformances = new Dictionary<string, LevelRecord.Performance>(),
                        BestPracticePerformances = new Dictionary<string, LevelRecord.Performance>(),
                        PlayCounts = new Dictionary<string, int>(),
                    };
                    foreach (var chart in level.Meta.charts)
                    {
                        record.PlayCounts[chart.type] = legacy.GetPlayCount(level.Id, chart.type);

                        if (legacy.HasPerformance(level.Id, chart.type, true))
                        {
                            var bestPerformance = legacy.GetBestPerformance(level.Id, chart.type, true).Let(p =>
                                new LevelRecord.Performance
                                {
                                    Score = p.Score,
                                    Accuracy = p.Accuracy / 100.0,
                                });
                            record.BestPerformances[chart.type] = bestPerformance;
                        }

                        if (legacy.HasPerformance(level.Id, chart.type, false))
                        {
                            var bestPracticePerformance = legacy.GetBestPerformance(level.Id, chart.type, false).Let(
                                p =>
                                    new LevelRecord.Performance
                                    {
                                        Score = p.Score,
                                        Accuracy = p.Accuracy / 100.0,
                                    });
                            record.BestPracticePerformances[chart.type] = bestPracticePerformance;
                        }
                    }

                    Context.Database.SetLevelRecord(record, true);
                    level.Record = record;
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private LocalPlayerSettings InitializeSettings()
    {
        var settings = new LocalPlayerSettings
        {
            SchemaVersion = 1,
            PlayerId = PlayerPrefs.GetString("Uid"),
            LoginToken = SecuredPlayerPrefs.GetString("JwtToken", null),
            ActiveCharacterId = null,
            Language = (int) Localization.Instance.ConvertSystemLanguage(Application.systemLanguage)
                .Let(it => Localization.Instance.SupportedLanguages.Contains(it) ? it : Language.English),
            PlayRanked = legacy.PlayRanked,
            EnabledMods = legacy.EnabledMods.ToList(),
            DisplayBoundaries = true,
            DisplayEarlyLateIndicators = true,
            HitboxSizes = new Dictionary<NoteType, int>
            {
                {NoteType.Click, legacy.ClickHitboxSize},
                {NoteType.DragChild, legacy.DragHitboxSize},
                {NoteType.DragHead, legacy.DragHitboxSize},
                {NoteType.Hold, legacy.HoldHitboxSize},
                {NoteType.LongHold, legacy.HoldHitboxSize},
                {NoteType.Flick, legacy.FlickHitboxSize},
            },
            NoteRingColors = new Dictionary<NoteType, Color>
            {
                {NoteType.Click, legacy.GetRingColor(NoteType.Click, false)},
                {NoteType.DragChild, legacy.GetRingColor(NoteType.DragChild, false)},
                {NoteType.DragHead, legacy.GetRingColor(NoteType.DragHead, false)},
                {NoteType.Hold, legacy.GetRingColor(NoteType.Hold, false)},
                {NoteType.LongHold, legacy.GetRingColor(NoteType.LongHold, false)},
                {NoteType.Flick, legacy.GetRingColor(NoteType.Flick, false)},
            },
            NoteFillColors = new Dictionary<NoteType, Color>
            {
                {NoteType.Click, legacy.GetFillColor(NoteType.Click, false)},
                {NoteType.DragChild, legacy.GetFillColor(NoteType.DragChild, false)},
                {NoteType.DragHead, legacy.GetFillColor(NoteType.DragHead, false)},
                {NoteType.Hold, legacy.GetFillColor(NoteType.Hold, false)},
                {NoteType.LongHold, legacy.GetFillColor(NoteType.LongHold, false)},
                {NoteType.Flick, legacy.GetFillColor(NoteType.Flick, false)},
            },
            NoteFillColorsAlt = new Dictionary<NoteType, Color>
            {
                {NoteType.Click, legacy.GetFillColor(NoteType.Click, true)},
                {NoteType.DragChild, legacy.GetFillColor(NoteType.DragChild, true)},
                {NoteType.DragHead, legacy.GetFillColor(NoteType.DragHead, true)},
                {NoteType.Hold, legacy.GetFillColor(NoteType.Hold, true)},
                {NoteType.LongHold, legacy.GetFillColor(NoteType.LongHold, true)},
                {NoteType.Flick, legacy.GetFillColor(NoteType.Flick, true)},
            },
            HoldHitSoundTiming = (HoldHitSoundTiming) legacy.HoldHitSoundTiming,
            NoteSize = legacy.NoteSize,
            HorizontalMargin = legacy.HorizontalMargin,
            VerticalMargin = legacy.VerticalMargin,
            CoverOpacity = legacy.CoverOpacity,
            MusicVolume = legacy.MusicVolume,
            SoundEffectsVolume = legacy.SoundEffectsVolume,
            HitSound = "none",
            HitTapticFeedback = legacy.HitTapticFeedback,
            DisplayStoryboardEffects = legacy.UseStoryboardEffects,
            GraphicsQuality = GetDefaultGraphicsQuality(),
            BaseNoteOffset = legacy.BaseNoteOffset,
            HeadsetNoteOffset = legacy.HeadsetNoteOffset,
            ClearEffectsSize = legacy.ClearFXSize,
            DisplayProfiler = legacy.DisplayProfiler,
            DisplayNoteIds = legacy.DisplayNoteIds,
            LocalLevelSort = Enum.TryParse<LevelSort>(legacy.LocalLevelsSortBy, out var sort) ? sort : LevelSort.AddedDate,
            AndroidDspBufferSize = legacy.DspBufferSize,
            LocalLevelSortIsAscending = legacy.LocalLevelsSortInAscendingOrder
        };
        return settings;
    }

    private GraphicsQuality GetDefaultGraphicsQuality()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
#if UNITY_IOS
            if (UnityEngine.iOS.Device.generation >= UnityEngine.iOS.DeviceGeneration.iPadPro2Gen)
            {
                return GraphicsQuality.Ultra;
            }
            if (UnityEngine.iOS.Device.generation >= UnityEngine.iOS.DeviceGeneration.iPhone7)
            {
                return GraphicsQuality.High;
            }
            if (UnityEngine.iOS.Device.generation >= UnityEngine.iOS.DeviceGeneration.iPhone5S)
            {
                return GraphicsQuality.Medium;
            }
            return GraphicsQuality.Low;
#endif
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            var freq = SystemInfo.processorFrequency;
            Debug.Log("Processor count: " + SystemInfo.processorCount);
            Debug.Log("Processor frequency: ");
            return GraphicsQuality.Medium;
        }
        return GraphicsQuality.Ultra;
    }
    
    public bool ShouldOneShot(string key)
    {
        if (Settings == null) throw new InvalidOperationException();
        var used = Settings.PerformedOneShots.Contains(key);
        if (used) return false;
        Settings.PerformedOneShots.Add(key);
        SaveSettings();
        return true;
    }

    public void ClearOneShot(string key)
    {
        if (Settings == null) throw new InvalidOperationException();
        Settings.PerformedOneShots.Remove(key);
        SaveSettings();
    }
    
    public bool ShouldTrigger(string key, bool clear = true)
    {
        if (Settings == null) throw new InvalidOperationException();
        var set = Settings.SetTriggers.Contains(key);
        if (!set) return false;
        if (clear)
        {
            Settings.SetTriggers.Remove(key);
            SaveSettings();
        }
        return true;
    }

    public void ClearTrigger(string key)
    {
        ShouldTrigger(key);
    }

    public void SetTrigger(string key)
    {
        if (Settings == null) throw new InvalidOperationException();
        Settings.SetTriggers.Add(key);
        SaveSettings();
    }

}

public class StringKey
{
    public const string FirstLaunch = "First Launch121";
}

public class LocalPlayerLegacy
{
    
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
        get => PlayerPrefsExtensions.GetBool("profiler", false);
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
            default;
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