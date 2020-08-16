using System;
using Polyglot;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FontManager
{
    
    public Font RegularFont;
    public Font BoldFont;
    public Font ExtraLightFont;
    public Font ExtraBoldFont;
    public Font RegularJpFont;
    public Font BoldJpFont;
    public Font ExtraLightJpFont;
    public Font ExtraBoldJpFont;

    public bool Loaded { get; private set; }

    public void LoadFonts()
    {
        RegularFont = Resources.Load<Font>("Fonts/Nunito-Regular");
        BoldFont = Resources.Load<Font>("Fonts/Nunito-Bold");
        ExtraLightFont = Resources.Load<Font>("Fonts/Nunito-ExtraLight");
        ExtraBoldFont = Resources.Load<Font>("Fonts/Nunito-ExtraBold");
        RegularJpFont = Resources.Load<Font>("Fonts/Nunito-Regular-JP");
        BoldJpFont = Resources.Load<Font>("Fonts/Nunito-Bold-JP");
        ExtraLightJpFont = Resources.Load<Font>("Fonts/Nunito-ExtraLight-JP");
        ExtraBoldJpFont = Resources.Load<Font>("Fonts/Nunito-ExtraBold-JP");
        Loaded = true;
    }

    public async void UpdateSceneTexts()
    {
        if (!Loaded) await UniTask.WaitUntil(() => Loaded = true);

        foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObject.GetComponentsInChildren<Text>(true).ForEach(UpdateText); 
        }
    }

    public async void UpdateText(Text text)
    {
        if (text.font == null) return;
        if (!Loaded) await UniTask.WaitUntil(() => Loaded = true);
        switch (Localization.Instance.SelectedLanguage)
        {
            case Language.Japanese:
                switch (text.font.name)
                {
                    case "Nunito-Regular":
                        text.font = RegularJpFont;
                        break;
                    case "Nunito-Bold":
                        text.font = BoldJpFont;
                        break;
                    case "Nunito-ExtraLight":
                        text.font = ExtraLightJpFont;
                        break;
                    case "Nunito-ExtraBold":
                        text.font = ExtraBoldJpFont;
                        break;
                }
                break;
            default:
                switch (text.font.name)
                {
                    case "Nunito-Regular-JP":
                        text.font = RegularFont;
                        break;
                    case "Nunito-Bold-JP":
                        text.font = BoldFont;
                        break;
                    case "Nunito-ExtraLight-JP":
                        text.font = ExtraLightFont;
                        break;
                    case "Nunito-ExtraBold-JP":
                        text.font = ExtraBoldFont;
                        break;
                }
                break;
        }
    }
    
}

public enum FontWeight
{
    ExtraLight, Regular, Bold, ExtraBold
}

public static class FontWeightExtensions
{
    public static Font GetFont(this FontWeight weight)
    {
        switch (weight)
        {
            case FontWeight.ExtraLight:
                return Context.FontManager.ExtraLightFont;
            case FontWeight.Bold:
                return Context.FontManager.BoldFont;
            case FontWeight.ExtraBold:
                return Context.FontManager.ExtraBoldFont;
            default:
                return Context.FontManager.RegularFont;
        }
    }
}