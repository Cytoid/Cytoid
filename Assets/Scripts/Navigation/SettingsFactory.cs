using LeTai.Asset.TranslucentImage;
using Polyglot;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SettingsFactory
{
    public static void InstantiateGeneralSettings(Transform parent, bool more = false)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;

        if (more)
        {
            Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("SETTINGS_LANGUAGE".Get(), "",
                    () => lp.Language, it => lp.Language = it, new[]
                    {
                        ("English", (int) Language.English), ("简体中文", (int) Language.Simplified_Chinese),
                        ("符语", (int) Language.Fujaoese)
                    });
                element.caretSelect.onSelect.AddListener(async (_, value) =>
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
                () => lp.MusicVolume, it => lp.MusicVolume = it, new []
                {
                    ("0%", 0), ("5%", 0.05f), ("10%", 0.1f), ("15%", 0.15f), ("20%", 0.2f), ("25%", 0.25f),
                    ("30%", 0.3f),
                    ("35%", 0.35f), ("40%", 0.4f), ("45%", 0.45f), ("50%", 0.5f), ("55%", 0.55f), ("60%", 0.6f),
                    ("65%", 0.65f),
                    ("70%", 0.7f), ("75%", 0.75f), ("80%", 0.8f), ("85%", 0.85f), ("90%", 0.9f), ("95%", 0.95f),
                    ("100%", 1)
                });
            element.caretSelect.onSelect.AddListener((_, value) =>
            {
                LoopAudioPlayer.Instance.UpdateVolume();
                Context.AudioManager.UpdateVolumes();
            });
        });
        Object.Instantiate(provider.select, parent).Apply(element =>
        {
            element.SetContent("SETTINGS_SOUND_EFFECT_VOLUME".Get(), "",
                () => lp.SoundEffectsVolume, it => lp.SoundEffectsVolume = it, new []
                {
                    ("0%", 0), ("5%", 0.05f), ("10%", 0.1f), ("15%", 0.15f), ("20%", 0.2f), ("25%", 0.25f),
                    ("30%", 0.3f),
                    ("35%", 0.35f), ("40%", 0.4f), ("45%", 0.45f), ("50%", 0.5f), ("55%", 0.55f), ("60%", 0.6f),
                    ("65%", 0.65f),
                    ("70%", 0.7f), ("75%", 0.75f), ("80%", 0.8f), ("85%", 0.85f), ("90%", 0.9f), ("95%", 0.95f),
                    ("100%", 1)
                });
            element.caretSelect.onSelect.AddListener((_, value) => Context.AudioManager.UpdateVolumes());
        });
        
        Object.Instantiate(provider.select, parent).Apply(it =>
        {
            it.SetContent("SETTINGS_HIT_SOUND".Get(), "SETTINGS_HIT_SOUND_DESC".Get());
            it.gameObject.AddComponent<HitSoundSelect>().Load();
        });
        
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_HOLD_HIT_SOUND_TIMING".Get(), "SETTINGS_HOLD_HIT_SOUND_TIMING_DESC".Get(),
                () => lp.HoldHitSoundTiming, it => lp.HoldHitSoundTiming = it, new[]
                {
                    ("SETTINGS_HOLD_HIT_SOUNDS_TIMING_BEGIN".Get(), (int) HoldHitSoundTiming.Begin), 
                    ("SETTINGS_HOLD_HIT_SOUNDS_TIMING_END".Get(), (int) HoldHitSoundTiming.End), 
                    ("SETTINGS_HOLD_HIT_SOUNDS_TIMING_BOTH".Get(), (int) HoldHitSoundTiming.Both)
                });

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            /*Object.Instantiate(provider.pillRadioGroup, parent)
                .SetContent("Hit taptic feedback", "Provide taptic feedback when note is cleared",
                    () => lp.HitTapticFeedback, it => lp.HitTapticFeedback = it);*/
        }

        Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("SETTINGS_GRAPHICS_QUALITY".Get(), "SETTINGS_GRAPHICS_QUALITY_DESC".Get(),
                    () => lp.GraphicsQuality, it => lp.GraphicsQuality = it, new []
                    {
                        ("SETTINGS_QUALITY_LOW".Get(), "low"), ("SETTINGS_QUALITY_MEDIUM".Get(), "medium"), ("SETTINGS_QUALITY_HIGH".Get(), "high")
                    });
                element.caretSelect.onSelect.AddListener((_, quality) =>
                {
                    TranslucentImageSource.Disabled = Context.LocalPlayer.GraphicsQuality == "low";
                    Context.UpdateGraphicsQuality();
                });
            });
            
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_STORYBOARD_EFFECTS".Get(), "SETTINGS_STORYBOARD_EFFECTS_DESC".Get(),
                () => lp.UseStoryboardEffects, it => lp.UseStoryboardEffects = it);
        
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_BASE_NOTE_OFFSET".Get(), "SETTINGS_BASE_NOTE_OFFSET_DESC".Get(),
                () => lp.BaseNoteOffset, it => lp.BaseNoteOffset = it,
                "SETTINGS_UNIT_SECONDS".Get(), 0.ToString());
        
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_HEADSET_NOTE_OFFSET".Get(), "SETTINGS_HEADSET_NOTE_OFFSET_DESC".Get(),
                () => lp.HeadsetNoteOffset, it => lp.HeadsetNoteOffset = it,
                "SETTINGS_UNIT_SECONDS".Get(), 0.ToString());
    }

    public static void InstantiateGameplaySettings(Transform parent)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_EARLY_LATE_INDICATORS".Get(), "SETTINGS_EARLY_LATE_INDICATORS_DESC".Get(),
                () => lp.DisplayEarlyLateIndicators, it => lp.DisplayEarlyLateIndicators = it);
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_CLICK".Get(), "SETTINGS_HITBOX_SIZE_DESC".Get(),
                () => lp.ClickHitboxSize, it => lp.ClickHitboxSize = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                });
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_DRAG".Get(), "",
                () => lp.DragHitboxSize, it => lp.DragHitboxSize = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                });
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_HOLD".Get(), "",
                () => lp.HoldHitboxSize, it => lp.HoldHitboxSize = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                });
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_HITBOX_SIZE_FLICK".Get(), "",
                () => lp.FlickHitboxSize, it => lp.FlickHitboxSize = it, new []
                {
                    ("SETTINGS_SIZE_SMALL".Get(), 0), ("SETTINGS_SIZE_MEDIUM".Get(), 1), ("SETTINGS_SIZE_LARGE".Get(), 2)
                });
        
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_HORIZONTAL_MARGIN".Get(), "SETTINGS_HORIZONTAL_MARGIN_DESC".Get(),
                () => lp.HorizontalMargin, it => lp.HorizontalMargin = it, new[]
                {
                    ("-4", -1), ("-3", 0), ("-2", 1), ("-1", 2), ("0", 3), ("+1", 4), ("+2", 5), ("+3", 6), ("+4", 7), 
                });
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_VERTICAL_MARGIN".Get(), "SETTINGS_VERTICAL_MARGIN_DESC".Get(),
                () => lp.VerticalMargin, it => lp.VerticalMargin = it, new[]
                {
                    ("-4", -1), ("-3", 0), ("-2", 1), ("-1", 2), ("0", 3), ("+1", 4), ("+2", 5), ("+3", 6), ("+4", 7)
                });
    }

    public static void InstantiateVisualSettings(Transform parent)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;

        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_NOTE_SIZE".Get(), "SETTINGS_NOTE_SIZE_DESC".Get(),
                () => lp.NoteSize, it => lp.NoteSize = it, new[]
                {
                    ("50%", -0.5f), ("60%", -0.4f), ("70%", -0.3f), ("80%", -0.2f), ("90%", -0.1f), ("100%", 0),
                    ("110%", 0.1f), ("120%", 0.2f), ("130%", 0.3f), ("140%", 0.4f), ("150%", 0.5f)
                });
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_CLEAR_FX_SIZE".Get(), "SETTINGS_CLEAR_FX_SIZE_DESC".Get(),
                () => lp.ClearFXSize, it => lp.ClearFXSize = it, new[]
                {
                    ("50%", -0.5f), ("60%", -0.4f), ("70%", -0.3f), ("80%", -0.2f), ("90%", -0.1f), ("100%", 0),
                    ("110%", 0.1f), ("120%", 0.2f), ("130%", 0.3f), ("140%", 0.4f), ("150%", 0.5f)
                });
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_SHOW_BOUNDARIES".Get(), "SETTINGS_SHOW_BOUNDARIES_DESC".Get(),
                () => lp.ShowBoundaries, it => lp.ShowBoundaries = it);
        Object.Instantiate(provider.select, parent)
            .SetContent("SETTINGS_BACKGROUND_OPACITY".Get(), "SETTINGS_BACKGROUND_OPACITY_DESC".Get(),
                () => lp.CoverOpacity, it => lp.CoverOpacity = it,
                new[]
                {
                    ("0%", 0), ("5%", 0.05f), ("10%", 0.1f), ("15%", 0.15f), ("20%", 0.2f), ("25%", 0.25f),
                    ("30%", 0.3f),
                    ("35%", 0.35f), ("40%", 0.4f), ("45%", 0.45f), ("50%", 0.5f), ("55%", 0.55f), ("60%", 0.6f),
                    ("65%", 0.65f),
                    ("70%", 0.7f), ("75%", 0.75f), ("80%", 0.8f), ("85%", 0.85f), ("90%", 0.9f), ("95%", 0.95f),
                    ("100%", 1)
                });
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_RING_COLOR".Get(), "SETTINGS_RING_COLOR_DESC".Get(),
                () => lp.GetRingColor(NoteType.Click, false), it => lp.SetRingColor(NoteType.Click, false, it),
                "", "#FFFFFF", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_CLICK_UP".Get(), "SETTINGS_FILL_COLOR_CLICK_DESC".Get(),
                () => lp.GetFillColor(NoteType.Click, false), it => lp.SetFillColor(NoteType.Click, false, it),
                "", "#35A7FF", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_CLICK_DOWN".Get(), "",
                () => lp.GetFillColor(NoteType.Click, true), it => lp.SetFillColor(NoteType.Click, true, it),
                "", "#FF5964", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_DRAG_UP".Get(), "SETTINGS_FILL_COLOR_DRAG_DESC".Get(),
                () => lp.GetFillColor(NoteType.DragChild, false), it => lp.SetFillColor(NoteType.DragChild, false, it),
                "", "#39E59E", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_DRAG_DOWN".Get(), "",
                () => lp.GetFillColor(NoteType.DragChild, true), it => lp.SetFillColor(NoteType.DragChild, true, it),
                "", "#39E59E", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_HOLD_UP".Get(), "SETTINGS_FILL_COLOR_HOLD_DESC".Get(),
                () => lp.GetFillColor(NoteType.Hold, false), it => lp.SetFillColor(NoteType.Hold, false, it),
                "", "#35A7FF", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_HOLD_DOWN".Get(), "",
                () => lp.GetFillColor(NoteType.Hold, true), it => lp.SetFillColor(NoteType.Hold, true, it),
                "", "#FF5964", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_LONG_HOLD_UP".Get(), "SETTINGS_FILL_COLOR_LONG_HOLD_DESC".Get(),
                () => lp.GetFillColor(NoteType.LongHold, false), it => lp.SetFillColor(NoteType.LongHold, false, it),
                "", "#F2C85A", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_LONG_HOLD_DOWN".Get(), "",
                () => lp.GetFillColor(NoteType.LongHold, true), it => lp.SetFillColor(NoteType.LongHold, true, it),
                "", "#F2C85A", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_FLICK_UP".Get(), "SETTINGS_FILL_COLOR_FLICK_DESC".Get(),
                () => lp.GetFillColor(NoteType.Flick, false), it => lp.SetFillColor(NoteType.Flick, false, it),
                "", "#35A7FF", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("SETTINGS_FILL_COLOR_FLICK_DOWN".Get(), "",
                () => lp.GetFillColor(NoteType.Flick, true), it => lp.SetFillColor(NoteType.Flick, true, it),
                "", "#FF5964", true);
    }

    public static void InstantiateAdvancedSettings(Transform parent)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;

        if (Application.platform == RuntimePlatform.Android)
        {
            Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("SETTINGS_DSP_BUFFER_SIZE".Get(), "SETTINGS_DSP_BUFFER_SIZE_DESC".Get(),
                    () => lp.DspBufferSize, it => lp.DspBufferSize = it, new[]
                    {
                        ("SETTINGS_DEFAULT".Get(), -1), ("128", 128), ("256", 256), ("512", 512), ("1024", 1024), ("2048", 2048)
                    });
                element.caretSelect.onSelect.AddListener((_, value) =>
                {
                    var audioConfig = AudioSettings.GetConfiguration();
                    audioConfig.dspBufferSize = Context.LocalPlayer.DspBufferSize > 0 ? Context.LocalPlayer.DspBufferSize : Context.DefaultDspBufferSize;
                    AudioSettings.Reset(audioConfig);
                });
            });
        }

        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_DISPLAY_PROFILER".Get(), "SETTINGS_DISPLAY_PROFILER_DESC".Get(),
                () => lp.DisplayProfiler, it => lp.DisplayProfiler = it);
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("SETTINGS_DISPLAY_NOTE_IDS".Get(), "SETTINGS_DISPLAY_NOTE_IDS_DESC".Get(),
                () => lp.DisplayNoteIds, it => lp.DisplayNoteIds = it);
    }
}