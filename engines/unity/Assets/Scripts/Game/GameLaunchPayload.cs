using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class GameLaunchPayload
{
    public string levelMetaJson;
    public string selectedDifficulty;
    public string chartText;
    public byte[] musicBytes;
    public string musicFormat = "mp3";
    public string storyboardText;
    public GameLaunchSettings settings;
    public GameLaunchAssets assets;
    public List<string> mods = new List<string>();
    public string gameMode;
    public TierPlayLaunch tierPlay;

    public LevelMeta ParseLevelMeta()
    {
        var meta = JsonConvert.DeserializeObject<LevelMeta>(levelMetaJson);
        if (meta == null || !meta.Validate())
        {
            throw new ArgumentException("Invalid level meta in launch payload");
        }

        return meta;
    }
}

[Serializable]
public class GameLaunchSettings
{
    public float? baseNoteOffset;
    public float? levelNoteOffset;
    public float? headsetNoteOffset;
    public float? judgmentOffset;
    public float? noteSize;
    public int? horizontalMargin;
    public int? verticalMargin;
    public bool? restrictPlayAreaAspectRatio;
    public float? coverOpacity;
    public float? musicVolume;
    public float? soundEffectsVolume;
    public string hitSound;
    public bool? displayStoryboardEffects;
    public bool? displayBoundaries;
    public bool? skipMusicOnCompletion;
    public bool? displayEarlyLateIndicators;
    public bool? displayNoteIds;
    public bool? useExperimentalNoteAr;
    public bool? useExperimentalNoteAnimations;
    public float? clearEffectsSize;
    public bool? displayProfiler;
    public bool? adaptOverlayToSafeArea;

    /// <summary>Note type id (0–7) → hitbox size tier (0 small, 1 medium, 2 large).</summary>
    public Dictionary<string, int> hitboxSizes;
    public Dictionary<string, string> noteRingColors;
    public Dictionary<string, string> noteFillColors;
    public Dictionary<string, string> noteFillColorsAlt;
    public bool? useFillColorForDragChildNodes;
    public string holdHitSoundTiming;
    public string graphicsQuality;
    public bool? hitTapticFeedback;
    public bool? useNativeAudio;
    public int? androidDspBufferSize;
}
