using System;
using System.Collections.Generic;
using Polyglot;
using Cysharp.Threading.Tasks;
using TMPro;
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

    public TMP_FontAsset RegularTmpFont;
    public TMP_FontAsset BoldTmpFont;
    public TMP_FontAsset ExtraLightTmpFont;
    public TMP_FontAsset RegularCjkTmpFont;
    public TMP_FontAsset BoldCjkTmpFont;
    public TMP_FontAsset RegularJpTmpFont;
    public TMP_FontAsset BoldJpTmpFont;

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

        RegularTmpFont = Resources.Load<TMP_FontAsset>("Fonts/Nunito-Regular SDF");
        BoldTmpFont = Resources.Load<TMP_FontAsset>("Fonts/Nunito-Bold SDF");
        ExtraLightTmpFont = Resources.Load<TMP_FontAsset>("Fonts/Nunito-ExtraLight SDF");
        RegularCjkTmpFont = Resources.Load<TMP_FontAsset>("Fonts/SourceHanSansHWTC-Regular SDF");
        BoldCjkTmpFont = Resources.Load<TMP_FontAsset>("Fonts/SourceHanSansHWTC-Bold SDF");
        RegularJpTmpFont = Resources.Load<TMP_FontAsset>("Fonts/MPLUSRounded1c-Regular SDF");
        BoldJpTmpFont = Resources.Load<TMP_FontAsset>("Fonts/MPLUSRounded1c-Bold SDF");

        ConfigureTmpFallbacks(Localization.Instance.SelectedLanguage);
        Loaded = true;
    }

    public async void UpdateSceneTexts()
    {
        if (!Loaded) await UniTask.WaitUntil(() => Loaded);

        ConfigureTmpFallbacks(Localization.Instance.SelectedLanguage);

        foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObject.GetComponentsInChildren<Text>(true).ForEach(UpdateText);
            gameObject.GetComponentsInChildren<TMP_Text>(true).ForEach(UpdateText);
        }
    }

    public async void UpdateText(Text text)
    {
        if (text.font == null) return;
        if (!Loaded) await UniTask.WaitUntil(() => Loaded);
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

    public async void UpdateText(TMP_Text text)
    {
        if (text.font == null) return;
        if (!Loaded) await UniTask.WaitUntil(() => Loaded);
        ConfigureTmpFallbacks(Localization.Instance.SelectedLanguage);
        text.SetAllDirty();
    }

    public TMP_FontAsset GetTmpFont(FontWeight weight)
    {
        ConfigureTmpFallbacks(Localization.Instance.SelectedLanguage);
        switch (weight)
        {
            case FontWeight.ExtraLight:
                return ExtraLightTmpFont;
            case FontWeight.Bold:
            case FontWeight.ExtraBold:
                return BoldTmpFont;
            default:
                return RegularTmpFont;
        }
    }

    public void ConfigureTmpFallbacks(Language language)
    {
        var japanese = language == Language.Japanese;
        SetFallbacks(
            RegularTmpFont,
            japanese ? RegularJpTmpFont : RegularCjkTmpFont,
            japanese ? RegularCjkTmpFont : RegularJpTmpFont);
        SetFallbacks(
            BoldTmpFont,
            japanese ? BoldJpTmpFont : BoldCjkTmpFont,
            japanese ? BoldCjkTmpFont : BoldJpTmpFont);
        SetFallbacks(
            ExtraLightTmpFont,
            japanese ? RegularJpTmpFont : RegularCjkTmpFont,
            japanese ? RegularCjkTmpFont : RegularJpTmpFont);
    }

    private static void SetFallbacks(TMP_FontAsset font, params TMP_FontAsset[] fallbacks)
    {
        if (font == null) return;
        font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
        var table = font.fallbackFontAssetTable;
        var expectedCount = 0;
        foreach (var fallback in fallbacks)
            if (fallback != null) expectedCount++;

        var index = 0;
        foreach (var fallback in fallbacks)
        {
            if (fallback == null) continue;
            if (index >= table.Count || table[index] != fallback) break;
            index++;
        }
        if (index == expectedCount && table.Count == expectedCount) return;

        table.Clear();
        foreach (var fallback in fallbacks)
            if (fallback != null) table.Add(fallback);
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

    public static TMP_FontAsset GetTmpFont(this FontWeight weight)
    {
        return Context.FontManager.GetTmpFont(weight);
    }
}
