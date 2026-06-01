using System;
using Polyglot;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class Player
{

    public string Id => Settings.PlayerId;

    public LocalPlayerSettings Settings { get; private set; }

    public void Initialize()
    {
        Settings = new LocalPlayerSettings
        {
            SchemaVersion = 1,
            PlayerId = "local",
            Language = (int) Localization.Instance.ConvertSystemLanguage(Application.systemLanguage)
                .Let(it => Localization.Instance.SupportedLanguages.Contains(it) ? it : Language.English),
            GraphicsQuality = GetDefaultGraphicsQuality(),
        };
        ApplyDefaultSettings();

        if (!Localization.Instance.SupportedLanguages.Contains((Language) Settings.Language))
        {
            Settings.Language = (int) Language.English;
        }
    }

    public async void BoostStoreReviewConfidence()
    {
        Settings.RequestStoreReviewConfidence++;
        if (Settings.RequestStoreReviewConfidence >= 5 && !Settings.RequestedForStoreReview)
        {
            Settings.RequestedForStoreReview = true;
            await RequestStoreReview();
        }
    }

    public async UniTask RequestStoreReview()
    {
#if UNITY_IOS
        Device.RequestStoreReview();
#elif UNITY_ANDROID
        if (Context.Distribution == Distribution.TapTap)
        {
            Context.AudioManager.Get("ActionSuccess").Play();
            Application.OpenURL("https://www.taptap.com/app/158749");
        }
#endif
    }

    public bool ShouldEnableDebug()
    {
        return Id == "tigerhix" || Id == "neo";
    }

    private void ApplyDefaultSettings()
    {
        var dummy = new LocalPlayerSettings();
        Settings.NoteRingColors = dummy.NoteRingColors.WithOverrides(Settings.NoteRingColors);
        Settings.NoteFillColors = dummy.NoteFillColors.WithOverrides(Settings.NoteFillColors);
        Settings.NoteFillColorsAlt = dummy.NoteFillColorsAlt.WithOverrides(Settings.NoteFillColorsAlt);
    }

    private GraphicsQuality GetDefaultGraphicsQuality()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
#if UNITY_IOS
            if (UnityEngine.iOS.Device.generation >= UnityEngine.iOS.DeviceGeneration.iPadPro2Gen)
            {
                return GraphicsQuality.Ultra;
            }
            if (UnityEngine.iOS.Device.generation >= UnityEngine.iOS.DeviceGeneration.iPhone8)
            {
                return GraphicsQuality.High;
            }
            if (UnityEngine.iOS.Device.generation >= UnityEngine.iOS.DeviceGeneration.iPhone7)
            {
                return GraphicsQuality.Medium;
            }
            return GraphicsQuality.Low;
#endif
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("Processor count: " + SystemInfo.processorCount);
            Debug.Log("Processor frequency: " + SystemInfo.processorFrequency);
            return GraphicsQuality.Medium;
        }
        return GraphicsQuality.Ultra;
    }

    public bool ShouldOneShot(string key) => false;

    public void ClearOneShot(string key) { }

    public bool ShouldTrigger(string key, bool clear = true) => false;

    public void ClearTrigger(string key) { }

    public void SetTrigger(string key) { }
}

public class StringKey
{
    public const string FirstLaunch = "First Launch1211111111111";
}
