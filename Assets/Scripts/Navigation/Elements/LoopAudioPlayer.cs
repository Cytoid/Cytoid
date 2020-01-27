using DG.Tweening;
using E7.Introloop;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Audio;

public class LoopAudioPlayer : SingletonMonoBehavior<LoopAudioPlayer>
{
    public IntroloopAudio mainLoopAudio;
    public IntroloopAudio resultLoopAudio;
    public AudioMixerGroup audioMixerGroup;

    private float maxVolume;
    private bool isFadedOut;

    public void UpdateVolume()
    {
        maxVolume = Context.LocalPlayer.MusicVolume;
        if (!isFadedOut) audioMixerGroup.audioMixer.SetFloat("MasterVolume", ConvertVolume(maxVolume));
    }

    private float ConvertVolume(float f)
    {
        if (f == 0) f = 0.001f;
        return Mathf.Log(f) * 20;
    }

    public void PlayMainLoopAudio(float fadeDuration = 0.0f)
    {
        UpdateVolume();
        IntroloopPlayer.Instance.SetMixerGroup(audioMixerGroup);
        IntroloopPlayer.Instance.Play(mainLoopAudio, fadeDuration);
    }

    public void StopMainLoopAudio()
    {
        IntroloopPlayer.Instance.Stop();
    }

    public void PlayResultLoopAudio(float fadeDuration = 0.0f)
    {
        UpdateVolume();
        IntroloopPlayer.Instance.SetMixerGroup(audioMixerGroup);
        IntroloopPlayer.Instance.Play(resultLoopAudio, fadeDuration);
    }

    public void FadeOutLoopPlayer(float duration = 1f)
    {
        isFadedOut = true;
        if (duration == 0) audioMixerGroup.audioMixer.SetFloat("MasterVolume", -80f);
        else audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", -80f, duration).SetEase(Ease.Linear);
    }

    public async void FadeInLoopPlayer(float duration = 1f)
    {
        await UniTask.DelayFrame(0); // Introloop bug: switching music is not immediate
        isFadedOut = false;
        if (duration == 0) audioMixerGroup.audioMixer.SetFloat("MasterVolume", maxVolume);
        else audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", ConvertVolume(maxVolume), duration).SetEase(Ease.Linear);
    }
}