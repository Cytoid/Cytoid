using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public sealed class ExternalGameContentProvider : IGameContentProvider
{
    private readonly GameLaunchPayload payload;
    private readonly Level level;
    private readonly Difficulty difficulty;
    private NLayerMemoryLoader nLayerMemoryLoader;
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

        var vfsPath = GameLaunchVfs.ResolveDirectoryPath(payload.assets?.vfsUri);
        level = Level.FromExternal(meta, vfsPath);
    }

    public UniTask<string> LoadChartText()
    {
        if (!string.IsNullOrEmpty(payload.chartText))
        {
            return UniTask.FromResult(payload.chartText);
        }

        var chartUri = payload.assets?.chartUri ??
                       GameLaunchVfs.ResolveFileUri(payload.assets?.vfsUri, ChartSection.path);
        var chartPath = GameLaunchVfs.ResolveFilePath(chartUri);
        if (!string.IsNullOrEmpty(chartPath) && File.Exists(chartPath))
        {
            return UniTask.FromResult(File.ReadAllText(chartPath));
        }

        throw new ArgumentException("Missing chart text in launch payload");
    }

    public async UniTask<AudioClip> LoadMusic()
    {
        if (payload.musicBytes != null && payload.musicBytes.Length > 0)
        {
            var normalizedFormat = (payload.musicFormat ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedFormat == "mpeg") normalizedFormat = "mp3";
            if (normalizedFormat != "mp3")
            {
                throw new NotSupportedException($"External audio format is not supported yet: {payload.musicFormat}");
            }

            nLayerMemoryLoader?.Dispose();
            nLayerMemoryLoader = new NLayerMemoryLoader(payload.musicBytes, $"{level.Id}-{difficulty.Id}");
            audioClip = nLayerMemoryLoader.LoadAudioClip();
            return audioClip;
        }

        var musicUri = payload.assets?.musicUri ??
                       GameLaunchVfs.ResolveFileUri(payload.assets?.vfsUri, level.Meta.GetMusicPath(difficulty.Id));
        if (!string.IsNullOrEmpty(musicUri))
        {
            audioClipLoader?.Unload();
            audioClipLoader = new AudioClipLoader(musicUri);
            await audioClipLoader.Load();
            if (!string.IsNullOrEmpty(audioClipLoader.Error))
            {
                throw new ArgumentException($"Failed to load music: {audioClipLoader.Error}");
            }

            audioClip = audioClipLoader.AudioClip;
            return audioClip;
        }

        throw new ArgumentException("Missing music bytes or music URI in launch payload");
    }

    public UniTask<string> LoadStoryboardText()
    {
        if (!string.IsNullOrEmpty(payload.storyboardText))
        {
            return UniTask.FromResult(payload.storyboardText);
        }

        var storyboardUri = payload.assets?.storyboardUri ??
                            GameLaunchVfs.ResolveFileUri(payload.assets?.vfsUri,
                                ChartSection.storyboard?.path ?? "storyboard.json");
        var storyboardPath = GameLaunchVfs.ResolveFilePath(storyboardUri);
        return UniTask.FromResult(!string.IsNullOrEmpty(storyboardPath) && File.Exists(storyboardPath)
            ? File.ReadAllText(storyboardPath)
            : null);
    }

    public void Dispose()
    {
        audioClip = null;
        audioClipLoader?.DisposeDecoder();
        audioClipLoader = null;
        nLayerMemoryLoader?.Dispose();
        nLayerMemoryLoader = null;
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
        return new ExternalGameContentProvider(JsonConvert.DeserializeObject<GameLaunchPayload>(launchJson));
    }
}
