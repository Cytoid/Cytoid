using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class ExternalGameContentProvider : IGameContentProvider
{
    private readonly GameLaunchPayload payload;
    private readonly Level level;
    private readonly Difficulty difficulty;
    private readonly string vfsRoot;
    private AudioClipLoader audioClipLoader;
    private AudioClip audioClip;

    public bool IsExternal => true;
    public Level Level => level;
    public Difficulty Difficulty => difficulty;
    public LevelMeta.ChartSection ChartSection => level.Meta.GetChartSection(difficulty.Id);
    public GameLaunchPayload Payload => payload;

    public ExternalGameContentProvider(GameLaunchPayload payload)
    {
        this.payload = payload ?? throw new ArgumentNullException(nameof(payload));
        var meta = payload.ParseLevelMeta();
        difficulty = Difficulty.Parse(payload.selectedDifficulty);
        if (meta.GetChartSection(difficulty.Id) == null)
        {
            throw new ArgumentException($"Missing chart for difficulty: {difficulty.Id}");
        }

        if (payload.assets == null)
        {
            throw new ArgumentException("assets is required in launch payload");
        }

        vfsRoot = GameLaunchVfs.ResolveRootDirectoryPath(payload.assets.vfsUri);
        level = Level.FromExternal(meta, vfsRoot);
    }

    public UniTask<string> LoadChartText()
    {
        var chartPath = GameLaunchVfs.ResolveRequiredFilePath(vfsRoot, payload.assets.chartPath, "assets.chartPath");
        if (File.Exists(chartPath))
        {
            return UniTask.FromResult(File.ReadAllText(chartPath));
        }

        throw new ArgumentException($"Missing chart file in launch payload: {payload.assets.chartPath}");
    }

    public async UniTask<AudioClip> LoadMusic()
    {
        var musicPath = GameLaunchVfs.ResolveRequiredFilePath(vfsRoot, payload.assets.musicPath, "assets.musicPath");
        if (!File.Exists(musicPath))
        {
            throw new ArgumentException($"Missing music file in launch payload: {payload.assets.musicPath}");
        }

        audioClipLoader?.Unload();
        audioClipLoader = new AudioClipLoader(GameLaunchVfs.ToFileUri(musicPath));
        await audioClipLoader.Load();
        if (!string.IsNullOrEmpty(audioClipLoader.Error))
        {
            throw new ArgumentException($"Failed to load music: {audioClipLoader.Error}");
        }

        audioClip = audioClipLoader.AudioClip;
        return audioClip;
    }

    public UniTask<string> LoadStoryboardText()
    {
        var storyboardRelativePath = !string.IsNullOrWhiteSpace(payload.assets.storyboardPath)
            ? payload.assets.storyboardPath
            : ChartSection.storyboard?.path ?? "storyboard.json";
        var storyboardPath = GameLaunchVfs.ResolveOptionalFilePath(
            vfsRoot,
            storyboardRelativePath,
            "assets.storyboardPath");
        return UniTask.FromResult(storyboardPath != null && File.Exists(storyboardPath)
            ? File.ReadAllText(storyboardPath)
            : null);
    }

    public void Dispose()
    {
        audioClip = null;
        audioClipLoader?.DisposeDecoder();
        audioClipLoader = null;
    }

    public HashSet<Mod> ParseMods()
    {
        var result = new HashSet<Mod>();
        if (payload.mods == null) return result;

        foreach (var mod in payload.mods)
        {
            if (Enum.TryParse(mod, true, out Mod parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    public void ApplySettings()
    {
        ApplySettings(payload.settings);
    }

    public static void ApplySettings(GameLaunchSettings settings, bool realtimeVolumeOnly = false)
    {
        if (settings == null || Context.Player.Settings == null) return;

        var target = Context.Player.Settings;
        if (settings.musicVolume.HasValue) target.MusicVolume = settings.musicVolume.Value;
        if (settings.soundEffectsVolume.HasValue) target.SoundEffectsVolume = settings.soundEffectsVolume.Value;
        Context.AudioManager?.UpdateVolumes();
        if (realtimeVolumeOnly) return;

        if (settings.baseNoteOffset.HasValue) target.BaseNoteOffset = settings.baseNoteOffset.Value;
        if (settings.levelNoteOffset.HasValue && Context.SelectedLevel?.Record != null)
        {
            Context.SelectedLevel.Record.RelativeNoteOffset = settings.levelNoteOffset.Value;
        }
        if (settings.headsetNoteOffset.HasValue) target.HeadsetNoteOffset = settings.headsetNoteOffset.Value;
        if (settings.judgmentOffset.HasValue) target.JudgmentOffset = settings.judgmentOffset.Value;
        if (settings.noteSize.HasValue) target.NoteSize = settings.noteSize.Value;
        if (settings.horizontalMargin.HasValue) target.HorizontalMargin = settings.horizontalMargin.Value;
        if (settings.verticalMargin.HasValue) target.VerticalMargin = settings.verticalMargin.Value;
        if (settings.restrictPlayAreaAspectRatio.HasValue) target.RestrictPlayAreaAspectRatio = settings.restrictPlayAreaAspectRatio.Value;
        if (settings.coverOpacity.HasValue) target.CoverOpacity = settings.coverOpacity.Value;
        if (!string.IsNullOrEmpty(settings.hitSound)) target.HitSound = settings.hitSound;
        if (settings.displayStoryboardEffects.HasValue) target.DisplayStoryboardEffects = settings.displayStoryboardEffects.Value;
        if (settings.displayBoundaries.HasValue) target.DisplayBoundaries = settings.displayBoundaries.Value;
        if (settings.skipMusicOnCompletion.HasValue) target.SkipMusicOnCompletion = settings.skipMusicOnCompletion.Value;
        if (settings.displayEarlyLateIndicators.HasValue) target.DisplayEarlyLateIndicators = settings.displayEarlyLateIndicators.Value;
        if (settings.displayNoteIds.HasValue) target.DisplayNoteIds = settings.displayNoteIds.Value;
        if (settings.useExperimentalNoteAr.HasValue) target.UseExperimentalNoteAr = settings.useExperimentalNoteAr.Value;
        if (settings.useExperimentalNoteAnimations.HasValue) target.UseExperimentalNoteAnimations = settings.useExperimentalNoteAnimations.Value;
        if (settings.clearEffectsSize.HasValue) target.ClearEffectsSize = settings.clearEffectsSize.Value;
        if (settings.adaptOverlayToSafeArea.HasValue) target.AdaptOverlayToSafeArea = settings.adaptOverlayToSafeArea.Value;
        if (settings.displayProfiler.HasValue)
        {
            target.DisplayProfiler = settings.displayProfiler.Value;
            if (Context.IsInitialized)
            {
                Context.UpdateProfilerDisplay();
            }
        }

        ApplyNoteTypeIntDictionary(settings.hitboxSizes, target.HitboxSizes);
        ApplyNoteTypeColorDictionary(settings.noteRingColors, target.NoteRingColors);
        ApplyNoteTypeColorDictionary(settings.noteFillColors, target.NoteFillColors);
        ApplyNoteTypeColorDictionary(settings.noteFillColorsAlt, target.NoteFillColorsAlt);
        if (settings.useFillColorForDragChildNodes.HasValue)
        {
            target.UseFillColorForDragChildNodes = settings.useFillColorForDragChildNodes.Value;
        }

        if (!string.IsNullOrEmpty(settings.holdHitSoundTiming) &&
            Enum.TryParse(settings.holdHitSoundTiming, true, out HoldHitSoundTiming holdTiming))
        {
            target.HoldHitSoundTiming = holdTiming;
        }

        if (!string.IsNullOrEmpty(settings.graphicsQuality) &&
            Enum.TryParse(settings.graphicsQuality, true, out GraphicsQuality graphicsQuality))
        {
            target.GraphicsQuality = graphicsQuality;
            if (Context.IsInitialized)
            {
                Context.UpdateGraphicsQuality();
            }
        }

        if (settings.hitTapticFeedback.HasValue)
        {
            target.HitTapticFeedback = settings.hitTapticFeedback.Value;
        }

        if (settings.useNativeAudio.HasValue)
        {
            target.UseNativeAudio = settings.useNativeAudio.Value;
            Context.AudioManager?.SetUseNativeAudio(settings.useNativeAudio.Value);
        }

        if (settings.androidDspBufferSize.HasValue)
        {
            target.AndroidDspBufferSize = settings.androidDspBufferSize.Value;
            var audioConfig = AudioSettings.GetConfiguration();
            audioConfig.dspBufferSize = target.AndroidDspBufferSize > 0
                ? target.AndroidDspBufferSize
                : Context.DefaultDspBufferSize;
            AudioSettings.Reset(audioConfig);
        }
    }

    private static void ApplyNoteTypeIntDictionary(
        Dictionary<string, int> source,
        Dictionary<NoteType, int> target)
    {
        if (source == null) return;

        foreach (var pair in source)
        {
            if (!int.TryParse(pair.Key, out var typeInt) || !Enum.IsDefined(typeof(NoteType), typeInt)) continue;
            target[(NoteType)typeInt] = pair.Value;
        }
    }

    private static void ApplyNoteTypeColorDictionary(
        Dictionary<string, string> source,
        Dictionary<NoteType, Color> target)
    {
        if (source == null) return;

        foreach (var pair in source)
        {
            if (!int.TryParse(pair.Key, out var typeInt) || !Enum.IsDefined(typeof(NoteType), typeInt)) continue;
            if (string.IsNullOrEmpty(pair.Value)) continue;
            target[(NoteType)typeInt] = pair.Value.ToColor();
        }
    }

    public static ExternalGameContentProvider FromJson(string launchJson)
    {
        var payloadObj = JObject.Parse(launchJson);
        var levelObj = RequireObject(payloadObj, "level");
        var settingsObj = RequireObject(payloadObj, "settings");
        var mode = RequireString(payloadObj, "mode");

        var payload = new GameLaunchPayload
        {
            levelMetaJson = JsonConvert.SerializeObject(RequireToken(levelObj, "meta")),
            selectedDifficulty = RequireString(levelObj, "selectedDifficulty"),
            assets = RequireObject(levelObj, "assets").ToObject<GameLaunchAssets>(),
            mods = payloadObj["mods"]?.ToObject<List<string>>() ?? new List<string>(),
            gameMode = FlattenGameMode(mode).ToString(),
            settings = FlattenLaunchSettings(settingsObj, requireFullSnapshot: true)
        };
        var optionsObj = payloadObj["options"] as JObject;
        if (optionsObj != null)
        {
            payload.settings.recordPlayEvents = OptionalBool(optionsObj, "recordPlayEvents");
        }

        if (string.Equals(mode, "tier", StringComparison.OrdinalIgnoreCase))
        {
            payload.tierPlay = FlattenTierLaunch(RequireObject(payloadObj, "tier"));
        }

        return new ExternalGameContentProvider(payload);
    }

    internal static GameLaunchSettings FlattenLaunchSettings(JObject settingsObj, bool requireFullSnapshot)
    {
        if (settingsObj == null) throw new ArgumentException("invalid_payload: settings is required");

        var profile = GetSettingsGroup(settingsObj, "profile", requireFullSnapshot);
        var runtime = GetSettingsGroup(settingsObj, "runtime", requireFullSnapshot);
        var visual = GetSettingsGroup(settingsObj, "visual", requireFullSnapshot);
        var audio = GetSettingsGroup(settingsObj, "audio", requireFullSnapshot);
        var noteStyle = GetSettingsGroup(settingsObj, "noteStyle", requireFullSnapshot);

        var settings = new GameLaunchSettings();
        ApplyProfileSettings(settings, profile);
        ApplyRuntimeSettings(settings, runtime);
        ApplyVisualSettings(settings, visual);
        ApplyAudioSettings(settings, audio);
        ApplyNoteStyleSettings(settings, noteStyle, requireFullSnapshot);
        return settings;
    }

    internal static GameLaunchSettings FlattenSettingsPatch(
        JObject settingsObj,
        out List<string> appliedFields,
        out List<string> deferredFields,
        out List<string> rejectedFields)
    {
        appliedFields = new List<string>();
        deferredFields = new List<string>();
        rejectedFields = new List<string>();

        if (settingsObj == null) throw new ArgumentException("invalid_payload: settings payload is required");

        var settings = FlattenLaunchSettings(settingsObj, requireFullSnapshot: false);
        CollectSettingsFields(settingsObj, appliedFields, rejectedFields);
        return settings;
    }

    internal static TierPlayLaunch FlattenTierLaunch(JObject tierObj)
    {
        if (tierObj == null) throw new ArgumentException("invalid_payload: tier is required for tier mode");

        var tierPlay = new TierPlayLaunch
        {
            tierId = RequireString(tierObj, "tierId"),
            stageIndex = RequireInt(tierObj, "stageIndex"),
            stageCount = RequireInt(tierObj, "stageCount"),
            maxHealth = RequireDouble(tierObj, "maxHealth"),
            initialHealth = RequireDouble(tierObj, "initialHealth"),
            initialCombo = RequireInt(tierObj, "initialCombo"),
            introLabel = tierObj["introLabel"]?.Type == JTokenType.String ? tierObj["introLabel"]?.Value<string>() : null
        };
        tierPlay.Validate();
        return tierPlay;
    }

    private static GameMode FlattenGameMode(string mode)
    {
        switch (mode?.ToLowerInvariant())
        {
            case "ranked":
                return GameMode.Standard;
            case "practice":
                return GameMode.Practice;
            case "calibration":
                return GameMode.Calibration;
            case "globalcalibration":
                return GameMode.GlobalCalibration;
            case "tier":
                return GameMode.Tier;
            default:
                throw new ArgumentException($"unsupported_mode: {mode}");
        }
    }

    private static JObject GetSettingsGroup(JObject settingsObj, string groupName, bool requireFullSnapshot)
    {
        var token = settingsObj[groupName];
        if (token == null || token.Type == JTokenType.Null)
        {
            if (requireFullSnapshot) throw new ArgumentException($"invalid_payload: settings.{groupName} is required");
            return null;
        }
        if (token.Type != JTokenType.Object)
        {
            throw new ArgumentException($"invalid_payload: settings.{groupName} must be an object");
        }
        return (JObject) token;
    }

    private static void ApplyProfileSettings(GameLaunchSettings settings, JObject profile)
    {
        if (profile == null) return;
        settings.baseNoteOffset = OptionalFloat(profile, "baseNoteOffset");
        settings.levelNoteOffset = OptionalFloat(profile, "levelNoteOffset");
        settings.headsetNoteOffset = OptionalFloat(profile, "headsetNoteOffset");
        settings.judgmentOffset = OptionalFloat(profile, "judgmentOffset");
        settings.hitTapticFeedback = OptionalBool(profile, "hitTapticFeedback");
        // profile.language and profile.menuTapticFeedback have no flat GameLaunchSettings equivalent.
    }

    private static void ApplyRuntimeSettings(GameLaunchSettings settings, JObject runtime)
    {
        if (runtime == null) return;
        settings.musicVolume = OptionalFloat(runtime, "musicVolume");
        settings.soundEffectsVolume = OptionalFloat(runtime, "soundEffectsVolume");
    }

    private static void ApplyVisualSettings(GameLaunchSettings settings, JObject visual)
    {
        if (visual == null) return;
        settings.noteSize = OptionalFloat(visual, "noteSize");
        settings.horizontalMargin = OptionalInt(visual, "horizontalMargin");
        settings.verticalMargin = OptionalInt(visual, "verticalMargin");
        settings.restrictPlayAreaAspectRatio = OptionalBool(visual, "restrictPlayAreaAspectRatio");
        settings.coverOpacity = OptionalFloat(visual, "coverOpacity");
        settings.displayStoryboardEffects = OptionalBool(visual, "displayStoryboardEffects");
        settings.displayBoundaries = OptionalBool(visual, "displayBoundaries");
        settings.skipMusicOnCompletion = OptionalBool(visual, "skipMusicOnCompletion");
        settings.displayEarlyLateIndicators = OptionalBool(visual, "displayEarlyLateIndicators");
        settings.displayNoteIds = OptionalBool(visual, "displayNoteIds");
        settings.useExperimentalNoteAr = OptionalBool(visual, "useExperimentalNoteAr");
        settings.useExperimentalNoteAnimations = OptionalBool(visual, "useExperimentalNoteAnimations");
        settings.clearEffectsSize = OptionalFloat(visual, "clearEffectsSize");
        settings.displayProfiler = OptionalBool(visual, "displayProfiler");
        settings.adaptOverlayToSafeArea = OptionalBool(visual, "adaptOverlayToSafeArea");
        settings.graphicsQuality = OptionalString(visual, "graphicsQuality");
    }

    private static void ApplyAudioSettings(GameLaunchSettings settings, JObject audio)
    {
        if (audio == null) return;
        settings.hitSound = OptionalString(audio, "hitSound");
        settings.holdHitSoundTiming = OptionalString(audio, "holdHitSoundTiming");
        settings.useNativeAudio = OptionalBool(audio, "useNativeAudio");
        settings.androidDspBufferSize = OptionalInt(audio, "androidDspBufferSize");
    }

    private static void ApplyNoteStyleSettings(GameLaunchSettings settings, JObject noteStyle, bool requireFullSnapshot)
    {
        if (noteStyle == null) return;

        var hitboxSizesToken = noteStyle["hitboxSizes"];
        if (requireFullSnapshot || (hitboxSizesToken != null && hitboxSizesToken.Type != JTokenType.Null))
        {
            settings.hitboxSizes = FlattenHitboxSizes(hitboxSizesToken as JObject, "settings.noteStyle.hitboxSizes");
        }

        var ringColorsToken = noteStyle["ringColors"];
        if (requireFullSnapshot || (ringColorsToken != null && ringColorsToken.Type != JTokenType.Null))
        {
            settings.noteRingColors = FlattenNoteTypeStringMap(ringColorsToken as JObject, "settings.noteStyle.ringColors");
        }

        var fillColorsToken = noteStyle["fillColors"];
        if (requireFullSnapshot || (fillColorsToken != null && fillColorsToken.Type != JTokenType.Null))
        {
            settings.noteFillColors = FlattenNoteTypeStringMap(fillColorsToken as JObject, "settings.noteStyle.fillColors");
        }

        var fillColorsAltToken = noteStyle["fillColorsAlt"];
        if (requireFullSnapshot || (fillColorsAltToken != null && fillColorsAltToken.Type != JTokenType.Null))
        {
            settings.noteFillColorsAlt = FlattenNoteTypeStringMap(fillColorsAltToken as JObject, "settings.noteStyle.fillColorsAlt");
        }

        settings.useFillColorForDragChildNodes = OptionalBool(noteStyle, "useFillColorForDragChildNodes");
    }

    private static Dictionary<string, int> FlattenHitboxSizes(JObject source, string path)
    {
        RequireCompleteNoteStyleMap(source, path);
        var result = new Dictionary<string, int>();
        foreach (var pair in NoteTypeWireKeys)
        {
            result[((int) pair.Value).ToString()] = HitboxSizeToInt(RequireString(source, pair.Key));
        }
        return result;
    }

    private static Dictionary<string, string> FlattenNoteTypeStringMap(JObject source, string path)
    {
        RequireCompleteNoteStyleMap(source, path);
        var result = new Dictionary<string, string>();
        foreach (var pair in NoteTypeWireKeys)
        {
            result[((int) pair.Value).ToString()] = RequireString(source, pair.Key);
        }
        return result;
    }

    private static void RequireCompleteNoteStyleMap(JObject source, string path)
    {
        if (source == null) throw new ArgumentException($"invalid_payload: {path} is required");
        foreach (var key in NoteTypeWireKeys.Keys)
        {
            if (source[key] == null || source[key].Type == JTokenType.Null)
            {
                throw new ArgumentException($"invalid_payload: {path}.{key} is required");
            }
        }
    }

    private static int HitboxSizeToInt(string value)
    {
        switch (value)
        {
            case "small": return 0;
            case "medium": return 1;
            case "large": return 2;
            default: throw new ArgumentException($"invalid_payload: unsupported hitbox size {value}");
        }
    }

    private static void CollectSettingsFields(JObject settingsObj, List<string> appliedFields, List<string> rejectedFields)
    {
        foreach (var group in settingsObj.Properties())
        {
            if (!(group.Value is JObject groupObj))
            {
                rejectedFields.Add(group.Name);
                continue;
            }

            foreach (var field in groupObj.Properties())
            {
                var fullPath = $"{group.Name}.{field.Name}";
                if (KnownAppliedFields.Contains(fullPath))
                {
                    appliedFields.Add(fullPath);
                }
                else
                {
                    rejectedFields.Add(fullPath);
                }
            }
        }
    }

    private static JToken RequireToken(JObject obj, string field)
    {
        var token = obj[field];
        if (token == null || token.Type == JTokenType.Null) throw new ArgumentException($"invalid_payload: {field} is required");
        return token;
    }

    private static JObject RequireObject(JObject obj, string field)
    {
        var token = RequireToken(obj, field);
        if (token.Type != JTokenType.Object) throw new ArgumentException($"invalid_payload: {field} must be an object");
        return (JObject) token;
    }

    private static string RequireString(JObject obj, string field)
    {
        var token = RequireToken(obj, field);
        if (token.Type != JTokenType.String) throw new ArgumentException($"invalid_payload: {field} must be a string");
        return token.Value<string>();
    }

    private static int RequireInt(JObject obj, string field)
    {
        var token = RequireToken(obj, field);
        if (token.Type != JTokenType.Integer) throw new ArgumentException($"invalid_payload: {field} must be an int");
        return token.Value<int>();
    }

    private static double RequireDouble(JObject obj, string field)
    {
        var token = RequireToken(obj, field);
        if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
        {
            throw new ArgumentException($"invalid_payload: {field} must be a number");
        }
        return token.Value<double>();
    }

    private static string OptionalString(JObject obj, string field)
    {
        var token = obj[field];
        return token == null || token.Type == JTokenType.Null ? null : token.Value<string>();
    }

    private static bool? OptionalBool(JObject obj, string field)
    {
        var token = obj[field];
        return token == null || token.Type == JTokenType.Null ? (bool?) null : token.Value<bool>();
    }

    private static int? OptionalInt(JObject obj, string field)
    {
        var token = obj[field];
        return token == null || token.Type == JTokenType.Null ? (int?) null : token.Value<int>();
    }

    private static float? OptionalFloat(JObject obj, string field)
    {
        var token = obj[field];
        return token == null || token.Type == JTokenType.Null ? (float?) null : token.Value<float>();
    }

    private static readonly Dictionary<string, NoteType> NoteTypeWireKeys = new Dictionary<string, NoteType>
    {
        ["click"] = NoteType.Click,
        ["hold"] = NoteType.Hold,
        ["longHold"] = NoteType.LongHold,
        ["dragHead"] = NoteType.DragHead,
        ["dragChild"] = NoteType.DragChild,
        ["flick"] = NoteType.Flick,
        ["cDragHead"] = NoteType.CDragHead,
        ["cDragChild"] = NoteType.CDragChild
    };

    private static readonly HashSet<string> KnownAppliedFields = new HashSet<string>
    {
        // profile
        "profile.baseNoteOffset",
        "profile.levelNoteOffset",
        "profile.headsetNoteOffset",
        "profile.judgmentOffset",
        "profile.hitTapticFeedback",
        // runtime
        "runtime.musicVolume",
        "runtime.soundEffectsVolume",
        // visual
        "visual.noteSize",
        "visual.horizontalMargin",
        "visual.verticalMargin",
        "visual.restrictPlayAreaAspectRatio",
        "visual.coverOpacity",
        "visual.displayStoryboardEffects",
        "visual.displayBoundaries",
        "visual.skipMusicOnCompletion",
        "visual.displayEarlyLateIndicators",
        "visual.displayNoteIds",
        "visual.useExperimentalNoteAr",
        "visual.useExperimentalNoteAnimations",
        "visual.clearEffectsSize",
        "visual.displayProfiler",
        "visual.adaptOverlayToSafeArea",
        "visual.graphicsQuality",
        // audio
        "audio.hitSound",
        "audio.holdHitSoundTiming",
        "audio.useNativeAudio",
        "audio.androidDspBufferSize",
        // noteStyle
        "noteStyle.hitboxSizes",
        "noteStyle.ringColors",
        "noteStyle.fillColors",
        "noteStyle.fillColorsAlt",
        "noteStyle.useFillColorForDragChildNodes"
    };
}
