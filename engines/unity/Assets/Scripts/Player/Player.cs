using Polyglot;
using UnityEngine;

public class Player
{
    public LocalPlayerSettings Settings { get; private set; }

    public void Initialize()
    {
        Settings = new LocalPlayerSettings
        {
            Language = (int) Localization.Instance.ConvertSystemLanguage(Application.systemLanguage)
                .Let(it => Localization.Instance.SupportedLanguages.Contains(it) ? it : Language.English),
            GraphicsQuality = GetDefaultGraphicsQuality(),
        };

        if (!Localization.Instance.SupportedLanguages.Contains((Language) Settings.Language))
        {
            Settings.Language = (int) Language.English;
        }
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

}
