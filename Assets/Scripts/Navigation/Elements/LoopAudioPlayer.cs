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

    public void PlayMainLoopAudio(float fadeDuration = 0.0f)
    {
        IntroloopPlayer.Instance.SetMixerGroup(audioMixerGroup);
        IntroloopPlayer.Instance.Play(mainLoopAudio, fadeDuration);
    }

    public void StopMainLoopAudio()
    {
        IntroloopPlayer.Instance.Stop();
    }

    public void PlayResultLoopAudio(float fadeDuration = 0.0f)
    {
        IntroloopPlayer.Instance.SetMixerGroup(audioMixerGroup);
        IntroloopPlayer.Instance.Play(resultLoopAudio, fadeDuration);
    }

    public void FadeOutLoopPlayer(float duration = 1f)
    {
        audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", -80f, duration).SetEase(Ease.Linear);
    }

    public async void FadeInLoopPlayer(float duration = 1f)
    {
        await UniTask.DelayFrame(0); // Introloop bug: switching music is not immediate
        audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", 0, duration).SetEase(Ease.Linear);
    }
}