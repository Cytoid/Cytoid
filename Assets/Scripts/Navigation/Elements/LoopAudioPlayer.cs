using System;
using System.Linq.Expressions;
using DG.Tweening;
using E7.Introloop;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class LoopAudioPlayer : SingletonMonoBehavior<LoopAudioPlayer>, ScreenChangeListener
{
    private const bool PrintDebugMessages = true;

    public IntroloopAudio defaultLoopAudio;
    public IntroloopAudio resultLoopAudio;
    public AudioMixerGroup audioMixerGroup;

    public float PlaybackTime => IntroloopPlayer.Instance.GetPlayheadTime();
    public float MaxVolume { get; private set; } = 1f;
    public IntroloopAudio MainLoopAudio { get; private set; }
    public IntroloopAudio PlayingAudio { get; private set; }
    public bool IsFadedOut { get; private set; }

    private DateTime asyncToken;

    protected override void Awake()
    {
        base.Awake();
        
        if (GameObject.FindGameObjectsWithTag("LoopAudioPlayer").Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        IntroloopPlayer.Instance.SetMixerGroup(audioMixerGroup);
        Context.PostSceneChanged.AddListener(PostSceneChanged);
        Context.CharacterManager.OnActiveCharacterSet.AddListener((asset, reload) =>
        {
            if (!reload) return;
            SetMainAudio(Context.Player.Settings.PlayCharacterTheme && asset.musicAudio != null ? asset.musicAudio : defaultLoopAudio);
        });

        Context.OnApplicationInitialized.AddListener(Initialize);
    }

    private void Initialize()
    {
        Context.ScreenManager.AddHandler(this);
        UpdateMaxVolume();
    }

    public void UpdateMaxVolume()
    {
        var previousMaxVolume = MaxVolume;
        MaxVolume = Context.Player.Settings.MusicVolume;
        if (MaxVolume == 0) MaxVolume = 0.000001f;
        audioMixerGroup.audioMixer.GetFloat("MasterVolume", out var currentMixerGroupVolume);
        if (PrintDebugMessages) print($"LoopAudioPlayer: Current mixer group volume is {currentMixerGroupVolume}");
        var currentVolume = ConvertTo01Volume(currentMixerGroupVolume);
        var currentVolumePercentage = Mathf.Clamp01(currentVolume / previousMaxVolume);
        var mixerGroupVolume = ConvertToMixerGroupVolume(currentVolumePercentage * MaxVolume);
        audioMixerGroup.audioMixer.SetFloat("MasterVolume", mixerGroupVolume);
        if (PrintDebugMessages) print($"LoopAudioPlayer: Mixer group volume set to {mixerGroupVolume}");
    }

    private static float ConvertToMixerGroupVolume(float f)
    {
        if (f == 0) f = 0.001f;
        return Mathf.Log(f) * 20;
    }
    
    private static float ConvertTo01Volume(float f)
    {
        return Mathf.Exp(f / 20);
    }

    public void PlayAudio(IntroloopAudio audio, float fadeInDuration = 0.5f, float crossfadeDuration = 0.5f, bool forceReplay = false)
    {
        if (PlayingAudio == audio && !forceReplay) return;
        var duration = PlayingAudio != null ? crossfadeDuration : fadeInDuration;
        if (PlayingAudio == audio && forceReplay)
        {
            IntroloopPlayer.Instance.Stop();
            IntroloopPlayer.Instance.Play(audio, duration);
            return;
        }
        if (PrintDebugMessages) print("LoopAudioPlayer: Played audio " + audio.name);
        PlayingAudio = audio;
        IntroloopPlayer.Instance.Play(audio, duration);
    }

    public void StopAudio(float fadeOutDuration = 0.5f)
    {
        if (PlayingAudio == null) return;
        if (PrintDebugMessages) print("LoopAudioPlayer: Stopped audio " + PlayingAudio.name);
        PlayingAudio = null;
        if (fadeOutDuration > 0)
        {
            IntroloopPlayer.Instance.Stop(fadeOutDuration);
        }
        else
        {
            IntroloopPlayer.Instance.Stop();
        }
    }
    
    public void FadeOutLoopPlayer(float duration = 1f)
    {
        IsFadedOut = true;
        audioMixerGroup.audioMixer.DOKill();
        if (duration == 0) audioMixerGroup.audioMixer.SetFloat("MasterVolume", -80f);
        else audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", -80f, duration).SetEase(Ease.Linear);
    }

    public void FadeInLoopPlayer(float duration = 1f)
    {
        IsFadedOut = false;
        audioMixerGroup.audioMixer.DOKill();
        if (duration == 0) audioMixerGroup.audioMixer.SetFloat("MasterVolume", ConvertToMixerGroupVolume(MaxVolume));
        else audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", ConvertToMixerGroupVolume(MaxVolume), duration).SetEase(Ease.Linear);
    }

    public void SetMainAudio(IntroloopAudio audio)
    {
        MainLoopAudio = audio;
        if (PlayingAudio != null)
        {
            PlayAudio(MainLoopAudio, 1f);
        }
    }

    public async void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (to is GamePreparationScreen || to is TierSelectionScreen)
        {
            FadeOutLoopPlayer();
            return;
        }
        if ((from is GamePreparationScreen || from is TierSelectionScreen) && to != null)
        {
            FadeInLoopPlayer();
            PlayAudio(MainLoopAudio);
            return;
        }
        if ((from == null || from is InitializationScreen) && to is MainMenuScreen)
        {
            PlayAudio(MainLoopAudio, 0);
            return;
        }
        if (to is ResultScreen || to is TierBreakScreen)
        {
            PlayAudio(resultLoopAudio, 0);
            await UniTask.DelayFrame(5); // Introloop bug: Audio not switched immediately, causing ear rape
            FadeInLoopPlayer(0);
        }
    }

    public void PostSceneChanged(string prev, string next)
    {
        UpdateMaxVolume();
        Context.ScreenManager.AddHandler(this);
    }

    public void OnScreenChangeFinished(Screen from, Screen to) => Expression.Empty();
}