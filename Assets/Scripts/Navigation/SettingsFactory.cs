using LeTai.Asset.TranslucentImage;
using UnityEngine;

public static class SettingsFactory
{
    public static void InstantiateGeneralSettings(Transform parent)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;
        
        Object.Instantiate(provider.select, parent).Apply(element =>
        {
            element.SetContent("Music volume", "",
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
            element.SetContent("Sound effect volume", "",
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
            it.SetContent("Hit sound", "Played when note is cleared");
            it.gameObject.AddComponent<HitSoundSelect>().Load();
        });
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Early hit sounds", "Play all hit sounds on note start",
                () => lp.PlayHitSoundsEarly, it => lp.PlayHitSoundsEarly = it);

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            /*Object.Instantiate(provider.pillRadioGroup, parent)
                .SetContent("Hit taptic feedback", "Provide taptic feedback when note is cleared",
                    () => lp.HitTapticFeedback, it => lp.HitTapticFeedback = it);*/
        }

        Object.Instantiate(provider.select, parent).Apply(element =>
            {
                element.SetContent("Graphics quality", "Lower quality saves battery and results in higher framerates",
                    () => lp.GraphicsQuality, it => lp.GraphicsQuality = it, new []
                    {
                        ("Low", "low"), ("Medium", "medium"), ("High", "high")
                    });
                element.caretSelect.onSelect.AddListener((_, quality) =>
                {
                    TranslucentImageSource.Disabled = Context.LocalPlayer.GraphicsQuality == "low";
                    Context.UpdateGraphicsQuality();
                });
            });
            
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Storyboard effects", "Recommended for high-end devices only",
                () => lp.UseStoryboardEffects, it => lp.UseStoryboardEffects = it);
        
        Object.Instantiate(provider.input, parent)
            .SetContent("Base note offset", "Added to each level's note offset",
                () => lp.BaseNoteOffset, it => lp.BaseNoteOffset = it,
                "seconds", 0.ToString());
        
        Object.Instantiate(provider.input, parent)
            .SetContent("Headset note offset", "Added to note offset when using headset",
                () => lp.HeadsetNoteOffset, it => lp.HeadsetNoteOffset = it,
                "seconds", 0.ToString());
    }

    public static void InstantiateGameplaySettings(Transform parent)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Early/Late indicators", "Displayed when notes cleared early/late",
                () => lp.DisplayEarlyLateIndicators, it => lp.DisplayEarlyLateIndicators = it);
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Hitbox size (click)", "Larger hitboxes make notes easier to hit",
                () => lp.ClickHitboxSize, it => lp.ClickHitboxSize = it, new []
                {
                    ("Small", 0), ("Medium", 1), ("Large", 2)
                });
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Hitbox size (drag)", "",
                () => lp.DragHitboxSize, it => lp.DragHitboxSize = it, new []
                {
                    ("Small", 0), ("Medium", 1), ("Large", 2)
                });
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Hitbox size (hold)", "",
                () => lp.HoldHitboxSize, it => lp.HoldHitboxSize = it, new []
                {
                    ("Small", 0), ("Medium", 1), ("Large", 2)
                });
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Hitbox size (flick)", "",
                () => lp.FlickHitboxSize, it => lp.FlickHitboxSize = it, new []
                {
                    ("Small", 0), ("Medium", 1), ("Large", 2)
                });
        
        Object.Instantiate(provider.select, parent)
            .SetContent("Horizontal margin", "The left/right spacings of play area",
                () => lp.HorizontalMargin, it => lp.HorizontalMargin = it, new[]
                {
                    ("-4", -1), ("-3", 0), ("-2", 1), ("-1", 2), ("0", 3), ("1", 4), ("2", 5), ("3", 6), ("4", 7), 
                });
        Object.Instantiate(provider.select, parent)
            .SetContent("Vertical margin", "The top/bottom spacings of play area",
                () => lp.VerticalMargin, it => lp.VerticalMargin = it, new[]
                {
                    ("-4", -1), ("-3", 0), ("-2", 1), ("-1", 2), ("0", 3), ("1", 4), ("2", 5), ("3", 6), ("4", 7)
                });
    }

    public static void InstantiateVisualSettings(Transform parent)
    {
        var lp = Context.LocalPlayer;
        var provider = PreferenceElementProvider.Instance;

        Object.Instantiate(provider.select, parent)
            .SetContent("Note size", "Display size of notes (hitboxes unaffected)",
                () => lp.NoteSize, it => lp.NoteSize = it, new[]
                {
                    ("50%", -0.5f), ("60%", -0.4f), ("70%", -0.3f), ("80%", -0.2f), ("90%", -0.1f), ("100%", 0),
                    ("110%", 0.1f), ("120%", 0.2f), ("130%", 0.3f), ("140%", 0.4f), ("150%", 0.5f)
                });
        Object.Instantiate(provider.select, parent)
            .SetContent("Clear FX size", "Display size of note clear effects",
                () => lp.ClearFXSize, it => lp.ClearFXSize = it, new[]
                {
                    ("50%", -0.5f), ("60%", -0.4f), ("70%", -0.3f), ("80%", -0.2f), ("90%", -0.1f), ("100%", 0),
                    ("110%", 0.1f), ("120%", 0.2f), ("130%", 0.3f), ("140%", 0.4f), ("150%", 0.5f)
                });
        
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Show boundaries", "Display play area boundaries",
                () => lp.ShowBoundaries, it => lp.ShowBoundaries = it);
        Object.Instantiate(provider.select, parent)
            .SetContent("Background opacity", "Adjust the background brightness",
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
            .SetContent("Ring color", "Change the note ring color",
                () => lp.GetRingColor(NoteType.Click, false), it => lp.SetRingColor(NoteType.Click, false, it),
                "", "#FFFFFF", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (click, up)", "Change the click note fill color",
                () => lp.GetFillColor(NoteType.Click, false), it => lp.SetFillColor(NoteType.Click, false, it),
                "", "#35A7FF", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (click, down)", "",
                () => lp.GetFillColor(NoteType.Click, true), it => lp.SetFillColor(NoteType.Click, true, it),
                "", "#FF5964", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (drag, up)", "Change the drag note fill color",
                () => lp.GetFillColor(NoteType.DragChild, false), it => lp.SetFillColor(NoteType.DragChild, false, it),
                "", "#39E59E", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (drag, down)", "",
                () => lp.GetFillColor(NoteType.DragChild, true), it => lp.SetFillColor(NoteType.DragChild, true, it),
                "", "#39E59E", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (hold, up)", "Change the hold note fill color",
                () => lp.GetFillColor(NoteType.Hold, false), it => lp.SetFillColor(NoteType.Hold, false, it),
                "", "#35A7FF", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (hold, down)", "",
                () => lp.GetFillColor(NoteType.Hold, true), it => lp.SetFillColor(NoteType.Hold, true, it),
                "", "#FF5964", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (long hold, up)", "Change the long hold note fill color",
                () => lp.GetFillColor(NoteType.LongHold, false), it => lp.SetFillColor(NoteType.LongHold, false, it),
                "", "#F2C85A", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (long hold, down)", "",
                () => lp.GetFillColor(NoteType.LongHold, true), it => lp.SetFillColor(NoteType.LongHold, true, it),
                "", "#F2C85A", true);

        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (flick, up)", "Change the flick note fill color",
                () => lp.GetFillColor(NoteType.Flick, false), it => lp.SetFillColor(NoteType.Flick, false, it),
                "", "#35A7FF", true);
        Object.Instantiate(provider.input, parent)
            .SetContent("Fill color (flick, down)", "",
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
                element.SetContent("DSP buffer size", "Adjust only if music is not playing properly",
                    () => lp.DspBufferSize, it => lp.DspBufferSize = it, new[]
                    {
                        ("Default", -1), ("128", 128), ("256", 256), ("512", 512), ("1024", 1024), ("2048", 2048)
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
            .SetContent("Display profiler", "Display frame rates and RAM usage",
                () => lp.DisplayProfiler, it => lp.DisplayProfiler = it);
        Object.Instantiate(provider.pillRadioGroup, parent)
            .SetContent("Display note IDs", "For debugging purposes",
                () => lp.DisplayNoteIds, it => lp.DisplayNoteIds = it);
    }
}