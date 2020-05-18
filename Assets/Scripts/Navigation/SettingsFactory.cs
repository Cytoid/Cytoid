using LeTai.Asset.TranslucentImage;
using Polyglot;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SettingsFactory
{
    public static void InstantiateGeneralSettings(Transform parent, bool more = false)
    {
        var lp = Context.Player;
        var provider = PreferenceElementProvider.Instance;

        if (more)
        {
            Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("SETTINGS_LANGUAGE".Get(), "",
                    () => lp.Settings.Language, it => lp.Settings.Language = it, new[]
                    {
                        ("English", (int) Language.English),
                        ("Español", (int) Language.Spanish),
                        ("Indonesia", (int) Language.Indonesian),
                        ("Português BR", (int) Language.Portuguese_Brazil),
                        ("Pусский", (int) Language.Russian),
                        ("Tagalog", (int) Language.Filipino),
                        ("Tiếng Việt", (int) Language.Vietnamese),
                        ("简体中文", (int) Language.Simplified_Chinese),
                        ("正體中文", (int) Language.Traditional_Chinese),
                        ("日本語", (int) Language.Japanese),
                        ("한국어", (int) Language.Korean),
                        ("符语", (int) Language.Fujaoese)
                    }).SaveSettingsOnChange();
                element.caretSelect.onSelect.AddListener((_, value) =>
                {
                    SpinnerOverlay.Show();

                    Localization.Instance.SelectLanguage((Language) int.Parse(value));

                    foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        gameObject.transform.GetComponentsInChildren<Screen>(true)
                            .ForEach(it => LayoutStaticizer.Activate(it.transform));
                        
                        gameObject.transform.GetComponentsInChildren<LocalizedText>(true)
                            .ForEach(it => it.OnLocalize());
                        
                        gameObject.transform.GetComponentsInChildren<LayoutGroup>(true)
                            .ForEach(it => it.transform.RebuildLayout());
                    }

                    Context.OnLanguageChanged.Invoke();
                    
                    SpinnerOverlay.Hide();
                });
            });
        }

        Object.Instantiate(provider.select, parent).Apply(element =>
        {
            element.SetContent("SETTINGS_MUSIC_VOLUME".Get(), "",
                () => lp.Settings.MusicVolume, it =>
                {
                    lp.Settings.MusicVolume = it;
                    // Special handling
                    // TODO: Event?
                    if (Context.ScreenManager.ActiveScreen is GamePreparationScreen gamePreparationScreen)
                    {
                        gamePreparationScreen.previewAudioSource.volume = it;
                    }
                    if (Context.ScreenManager.ActiveScreen is TierSelectionScreen tierSelectionScreen)
                    {
                        tierSelectionScreen.previewAudioSource.volume = it;
                    }
                }, new []
                {
                    ("0%", 0), ("5%", 0.05f), ("10%", 0.1f), ("15%", 0.15f), ("20%", 0.2f), ("25%", 0.25f),
                    ("30%", 0.3f),
                    ("35%", 0.35f), ("40%", 0.4f), ("45%", 0.45f), ("50%", 0.5f), ("55%", 0.55f), ("60%", 0.6f),
                    ("65%", 0.65f),
                    ("70%", 0.7f), ("75%", 0.75f), ("80%", 0.8f), ("85%", 0.85f), ("90%", 0.9f), ("95%", 0.95f),
                    ("100%", 1)
                }).SaveSettingsOnChange();
            element.caretSelect.onSelect.AddListener((_, value) =>
            {
                LoopAudioPlayer.Instance.UpdateMaxVolume();
                Context.AudioManager.UpdateVolumes();
            });
        });
        Object.Instantiate(provider.select, parent).Apply(element =>
        {
            element.SetContent("SETTINGS_SOUND_EFFECT_VOLUME".Get(), "",
                () => lp.Settings.SoundEffectsVolume, it => lp.Settings.SoundEffectsVolume = it, new []
                {
                    ("0%", 0), ("5%", 0.05f), ("10%", 0.1f), ("15%", 0.15f), ("20%", 0.2f), ("25%", 0.25f),
                    ("30%", 0.3f),
                    ("35%", 0.35f), ("40%", 0.4f), ("45%", 0.45f), ("50%", 0.5f), ("55%", 0.55f), ("60%", 0.6f),
                    ("65%", 0.65f),
                    ("70%", 0.7f), ("75%", 0.75f), ("80%", 0.8f), ("85%", 0.85f), ("90%", 0.9f), ("95%", 0.95f),
                    ("100%", 1)
                }).SaveSettingsOnChange();
            element.caretSelect.onSelect.AddListener((_, value) => Context.AudioManager.UpdateVolumes());
        });
        
        Object.Instantiate(provider.select, parent).Apply(it =>
        {
            it.SetContent("SETTINGS_HIT_SOUND".Get(), "SETTINGS_HIT_SOUND_DESC".Get()).SaveSettingsOnChange();
            it.gameObject.AddComponent<HitSoundSelect>().Load();
        });
        
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_HOLD_HIT_SOUND_TIMING".Get(), "SETTINGS_HOLD_HIT_SOUND_TIMING_DESC".Get(),
                () => lp.Settings.HoldHitSoundTiming, it => lp.Settings.HoldHitSoundTiming = it, new[]
                {
                    ("SETTINGS_HOLD_HIT_SOUNDS_TIMING_BEGIN".Get(), HoldHitSoundTiming.Begin), 
                    ("SETTINGS_HOLD_HIT_SOUNDS_TIMING_END".Get(), HoldHitSoundTiming.End), 
                    ("SETTINGS_HOLD_HIT_SOUNDS_TIMING_BOTH".Get(), HoldHitSoundTiming.Both)
                })
            .SaveSettingsOnChange();

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            /*Object.Instantiate(provider.pillRadioGroup, parent)
                .SetContent("Hit taptic feedback", "Provide taptic feedback when note is cleared",
                    () => lp.HitTapticFeedback, it => lp.HitTapticFeedback = it);*/
        }

        Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("SETTINGS_GRAPHICS_QUALITY".Get(), "SETTINGS_GRAPHICS_QUALITY_DESC".Get(),
                    () => lp.Settings.GraphicsQuality, it => lp.Settings.GraphicsQuality = it, new []
                    {
                        ("SETTINGS_QUALITY_LOW".Get(), GraphicsQuality.Low), 
                        ("SETTINGS_QUALITY_MEDIUM".Get(), GraphicsQuality.Medium), 
                        ("SETTINGS_QUALITY_HIGH".Get(), GraphicsQuality.High)
                    }).SaveSettingsOnChange();
                element.caretSelect.onSelect.AddListener((_, quality) =>
                {
                    Context.UpdateGraphicsQuality();
                });
            });
            
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_STORYBOARD_EFFECTS".Get(), "SETTINGS_STORYBOARD_EFFECTS_DESC".Get(),
                () => lp.Settings.DisplayStoryboardEffects, it => lp.Settings.DisplayStoryboardEffects = it)
            .SaveSettingsOnChange();
        
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_BASE_NOTE_OFFSET".Get(), "SETTINGS_BASE_NOTE_OFFSET_DESC".Get(),
                () => lp.Settings.BaseNoteOffset, it => lp.Settings.BaseNoteOffset = it,
                "SETTINGS_UNIT_SECONDS".Get(), 0.ToString())
            .SaveSettingsOnChange();
        
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_HEADSET_NOTE_OFFSET".Get(), "SETTINGS_HEADSET_NOTE_OFFSET_DESC".Get(),
                () => lp.Settings.HeadsetNoteOffset, it => lp.Settings.HeadsetNoteOffset = it,
                "SETTINGS_UNIT_SECONDS".Get(), 0.ToString())
            .SaveSettingsOnChange();
    }

    public static void InstantiateGameplaySettings(Transform parent)
    {
        var lp = Context.Player;
        var provider = PreferenceElementProvider.Instance;
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_EARLY_LATE_INDICATORS".Get(), "SETTINGS_EARLY_LATE_INDICATORS_DESC".Get(),
                () => lp.Settings.DisplayEarlyLateIndicators, it => lp.Settings.DisplayEarlyLateIndicators = it)
            .SaveSettingsOnChange();
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_CLICK".Get(), "SETTINGS_HITBOX_SIZE_DESC".Get(),
                () => lp.Settings.HitboxSizes[NoteType.Click], it => lp.Settings.HitboxSizes[NoteType.Click] = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_DRAG".Get(), "",
                () => lp.Settings.HitboxSizes[NoteType.DragHead], it => lp.Settings.HitboxSizes[NoteType.DragHead] = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_HOLD".Get(), "",
                () => lp.Settings.HitboxSizes[NoteType.Hold], it => lp.Settings.HitboxSizes[NoteType.Hold] = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_FLICK".Get(), "",
                () => lp.Settings.HitboxSizes[NoteType.Flick], it => lp.Settings.HitboxSizes[NoteType.Flick] = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                })
            .SaveSettingsOnChange();
        
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_HORIZONTAL_MARGIN".Get(), "SETTINGS_HORIZONTAL_MARGIN_DESC".Get(),
                () => lp.Settings.HorizontalMargin, it => lp.Settings.HorizontalMargin = it, new[]
                {
                    ("-4", -1), ("-3", 0), ("-2", 1), ("-1", 2), ("0", 3), ("+1", 4), ("+2", 5), ("+3", 6), ("+4", 7), 
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_VERTICAL_MARGIN".Get(), "SETTINGS_VERTICAL_MARGIN_DESC".Get(),
                () => lp.Settings.VerticalMargin, it => lp.Settings.VerticalMargin = it, new[]
                {
                    ("-4", -1), ("-3", 0), ("-2", 1), ("-1", 2), ("0", 3), ("+1", 4), ("+2", 5), ("+3", 6), ("+4", 7)
                })
            .SaveSettingsOnChange();
    }

    public static void InstantiateVisualSettings(Transform parent)
    {
        var lp = Context.Player;
        var provider = PreferenceElementProvider.Instance;

        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_NOTE_SIZE".Get(), "SETTINGS_NOTE_SIZE_DESC".Get(),
                () => lp.Settings.NoteSize, it => lp.Settings.NoteSize = it, new[]
                {
                    ("50%", -0.5f), ("60%", -0.4f), ("70%", -0.3f), ("80%", -0.2f), ("90%", -0.1f), ("100%", 0),
                    ("110%", 0.1f), ("120%", 0.2f), ("130%", 0.3f), ("140%", 0.4f), ("150%", 0.5f)
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_CLEAR_FX_SIZE".Get(), "SETTINGS_CLEAR_FX_SIZE_DESC".Get(),
                () => lp.Settings.ClearEffectsSize, it => lp.Settings.ClearEffectsSize = it, new[]
                {
                    ("50%", -0.5f), ("60%", -0.4f), ("70%", -0.3f), ("80%", -0.2f), ("90%", -0.1f), ("100%", 0),
                    ("110%", 0.1f), ("120%", 0.2f), ("130%", 0.3f), ("140%", 0.4f), ("150%", 0.5f)
                })
            .SaveSettingsOnChange();
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_SHOW_BOUNDARIES".Get(), "SETTINGS_SHOW_BOUNDARIES_DESC".Get(),
                () => lp.Settings.DisplayStoryboardEffects, it => lp.Settings.DisplayStoryboardEffects = it)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_BACKGROUND_OPACITY".Get(), "SETTINGS_BACKGROUND_OPACITY_DESC".Get(),
                () => lp.Settings.CoverOpacity, it => lp.Settings.CoverOpacity = it,
                new[]
                {
                    ("0%", 0), ("5%", 0.05f), ("10%", 0.1f), ("15%", 0.15f), ("20%", 0.2f), ("25%", 0.25f),
                    ("30%", 0.3f),
                    ("35%", 0.35f), ("40%", 0.4f), ("45%", 0.45f), ("50%", 0.5f), ("55%", 0.55f), ("60%", 0.6f),
                    ("65%", 0.65f),
                    ("70%", 0.7f), ("75%", 0.75f), ("80%", 0.8f), ("85%", 0.85f), ("90%", 0.9f), ("95%", 0.95f),
                    ("100%", 1)
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_RING_COLOR".Get(), "SETTINGS_RING_COLOR_DESC".Get(),
                () => lp.Settings.NoteRingColors[NoteType.Click], it => lp.Settings.NoteRingColors[NoteType.Click] = it,
                "", "#FFFFFF", true)
            .SaveSettingsOnChange();

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_CLICK_UP".Get(), "SETTINGS_FILL_COLOR_CLICK_DESC".Get(),
                () => lp.Settings.NoteFillColors[NoteType.Click], it => lp.Settings.NoteFillColors[NoteType.Click] = it,
                "", "#35A7FF", true)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_CLICK_DOWN".Get(), "",
                () => lp.Settings.NoteFillColorsAlt[NoteType.Click], it => lp.Settings.NoteFillColorsAlt[NoteType.Click] = it,
                "", "#FF5964", true)
            .SaveSettingsOnChange();

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_DRAG_UP".Get(), "SETTINGS_FILL_COLOR_DRAG_DESC".Get(),
                () => lp.Settings.NoteFillColors[NoteType.DragChild], it => lp.Settings.NoteFillColors[NoteType.DragChild] = it,
                "", "#39E59E", true)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_DRAG_DOWN".Get(), "",
                () => lp.Settings.NoteFillColorsAlt[NoteType.DragChild], it => lp.Settings.NoteFillColorsAlt[NoteType.DragChild] = it,
                "", "#39E59E", true)
            .SaveSettingsOnChange();
        
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_C_DRAG_UP".Get(), "SETTINGS_FILL_COLOR_C_DRAG_DESC".Get(),
                () => lp.Settings.NoteFillColors[NoteType.CDragChild], it => lp.Settings.NoteFillColors[NoteType.CDragChild] = it,
                "", "#39E59E", true)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_C_DRAG_DOWN".Get(), "",
                () => lp.Settings.NoteFillColorsAlt[NoteType.CDragChild], it => lp.Settings.NoteFillColorsAlt[NoteType.CDragChild] = it,
                "", "#39E59E", true)
            .SaveSettingsOnChange();

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_HOLD_UP".Get(), "SETTINGS_FILL_COLOR_HOLD_DESC".Get(),
                () => lp.Settings.NoteFillColors[NoteType.Hold], it => lp.Settings.NoteFillColors[NoteType.Hold] = it,
                "", "#35A7FF", true)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_HOLD_DOWN".Get(), "",
                () => lp.Settings.NoteFillColorsAlt[NoteType.Hold], it => lp.Settings.NoteFillColorsAlt[NoteType.Hold] = it,
                "", "#FF5964", true)
            .SaveSettingsOnChange();

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_LONG_HOLD_UP".Get(), "SETTINGS_FILL_COLOR_LONG_HOLD_DESC".Get(),
                () => lp.Settings.NoteFillColors[NoteType.LongHold], it => lp.Settings.NoteFillColors[NoteType.LongHold] = it,
                "", "#F2C85A", true)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_LONG_HOLD_DOWN".Get(), "",
                () => lp.Settings.NoteFillColorsAlt[NoteType.LongHold], it => lp.Settings.NoteFillColorsAlt[NoteType.LongHold] = it,
                "", "#F2C85A", true)
            .SaveSettingsOnChange();

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_FLICK_UP".Get(), "SETTINGS_FILL_COLOR_FLICK_DESC".Get(),
                () => lp.Settings.NoteFillColors[NoteType.Flick], it => lp.Settings.NoteFillColors[NoteType.Flick] = it,
                "", "#35A7FF", true)
            .SaveSettingsOnChange();
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_FLICK_DOWN".Get(), "",
                () => lp.Settings.NoteFillColorsAlt[NoteType.Flick], it => lp.Settings.NoteFillColorsAlt[NoteType.Flick] = it,
                "", "#FF5964", true)
            .SaveSettingsOnChange();
    }

    public static void InstantiateAdvancedSettings(Transform parent)
    {
        var lp = Context.Player;
        var provider = PreferenceElementProvider.Instance;

        if (Application.platform == RuntimePlatform.Android)
        {
            Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("SETTINGS_DSP_BUFFER_SIZE".Get(), "SETTINGS_DSP_BUFFER_SIZE_DESC".Get(),
                    () => lp.Settings.AndroidDspBufferSize, it => lp.Settings.AndroidDspBufferSize = it, new[]
                    {
                        ("SETTINGS_DEFAULT".Get(), -1), ("128", 128), ("256", 256), ("512", 512), ("1024", 1024), ("2048", 2048)
                    }).SaveSettingsOnChange();
                element.caretSelect.onSelect.AddListener((_, value) =>
                {
                    var audioConfig = AudioSettings.GetConfiguration();
                    audioConfig.dspBufferSize = Context.Player.Settings.AndroidDspBufferSize > 0 ? Context.Player.Settings.AndroidDspBufferSize : Context.DefaultDspBufferSize;
                    AudioSettings.Reset(audioConfig);
                });
            });
        }

        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_DISPLAY_PROFILER".Get(), "SETTINGS_DISPLAY_PROFILER_DESC".Get(),
                () => lp.Settings.DisplayProfiler, it =>
                {
                    lp.Settings.DisplayProfiler = it;
                    Context.UpdateProfilerDisplay();
                })
            .SaveSettingsOnChange();
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_DISPLAY_NOTE_IDS".Get(), "SETTINGS_DISPLAY_NOTE_IDS_DESC".Get(),
                () => lp.Settings.DisplayNoteIds, it => lp.Settings.DisplayNoteIds = it)
            .SaveSettingsOnChange();
    }
}