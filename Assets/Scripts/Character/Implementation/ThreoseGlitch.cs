using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// LOL.
public class ThreoseGlitch : InteractableMonoBehavior
{
    public static bool Glitched = false;
    public static int Count = 0;

    public Image originalTachie;
    public Image glitchedTachie;

    private void Awake()
    {
        if (Glitched)
        {
            OnGlitched();
        }
        onPointerClick.AddListener(async _ =>
        {
            Debug.Log(Count);
            Count++;
            if (Count == 100 && !Glitched)
            {
                Glitched = true;
                OnGlitched();
                Context.PostSceneChanged.AddListener(OnPostSceneChanged);
                Context.AudioManager.Get("Glitch").Play();

#if UNITY_IOS
                Vibration.VibrateIOS(ImpactFeedbackStyle.Heavy);
#elif UNITY_ANDROID
                Vibration.VibrateAndroid(80);
#endif

                originalTachie.SetAlpha(0);
                glitchedTachie.SetAlpha(1);
                await UniTask.Delay(TimeSpan.FromSeconds(0.5));
                originalTachie.SetAlpha(1);
                glitchedTachie.SetAlpha(0);
            }
        });
    }

    private static void OnPostSceneChanged(string from, string to)
    {
        if (to == "Navigation") OnGlitched();
    }

    private static void OnGlitched()
    {
        var glitch = Camera.main.GetComponent<CameraFilterPack_FX_Glitch1>();
        var rgb = Camera.main.GetComponent<CameraFilterPack_Color_RGB>();
        LoopAudioPlayer.Instance.UpdateMaxVolume(0.000001f);
        NavigationBackdrop.TranslucentImageMaxAlpha = 0.85f;
        NavigationBackdrop.TranslucentImageSpriteBlending = 1f;
        NavigationBackdrop.Instance.translucentImage.SetAlpha(0.85f);
        NavigationBackdrop.Instance.translucentImage.spriteBlending = 1f;
        glitch.enabled = true;
        rgb.enabled = true;
    }
}
