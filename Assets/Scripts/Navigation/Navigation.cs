using DG.Tweening;
using E7.Introloop;
using UnityEngine.Audio;

public class Navigation : SingletonMonoBehavior<Navigation>
{
    public IntroloopAudio mainLoopAudio;
    public IntroloopAudio resultLoopAudio;
    public AudioMixerGroup audioMixerGroup;
    
    protected void Start()
    {
        IntroloopPlayer.Instance.SetMixerGroup(audioMixerGroup);
        IntroloopPlayer.Instance.Play(mainLoopAudio);
        FadeInLoopPlayer();
    }

    public void FadeOutLoopPlayer()
    {
        audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", -80f, 1f).SetEase(Ease.Linear);
    }

    public void FadeInLoopPlayer()
    {
        audioMixerGroup.audioMixer.DOSetFloat("MasterVolume", 0, 1f).SetEase(Ease.Linear);
    }
}