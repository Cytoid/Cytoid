using System.Text.RegularExpressions;
using Polyglot;

public static class LocalizationExtensions
{
    public static string Get(this string originalText, params object[] parameters)
    {
        return Regex.Unescape(Localization.GetFormat(originalText, parameters));
    }

    public static string GetAcceptLanguageHeaderValue(this Language language)
    {
        switch (language)
        {
            case Language.English:
                return "en";
            case Language.Czech:
                return "cs";
            case Language.Spanish:
                return "es";
            case Language.Indonesian:
                return "id";
            case Language.Portuguese_Brazil:
                return "pt-BR";
            case Language.Russian:
                return "ru";
            case Language.Filipino:
                return "fil";
            case Language.Vietnamese:
                return "vi";
            case Language.Ukrainian:
                return "uk";
            case Language.Simplified_Chinese:
                return "zh-CN";
            case Language.Traditional_Chinese:
                return "zh-TW";
            case Language.Japanese:
                return "ja";
            case Language.Korean:
                return "ko";
            case Language.Fujaoese:
                return "zh-CN";
            default:
                return "en";
        }
    }

    public static bool TryParseLanguageCode(this string code, out Language language)
    {
        switch (code?.Trim().ToLowerInvariant())
        {
            case "en": language = Language.English; return true;
            case "cs": language = Language.Czech; return true;
            case "es": language = Language.Spanish; return true;
            case "id": language = Language.Indonesian; return true;
            case "pt-br": language = Language.Portuguese_Brazil; return true;
            case "ru": language = Language.Russian; return true;
            case "fil": language = Language.Filipino; return true;
            case "vi": language = Language.Vietnamese; return true;
            case "uk": language = Language.Ukrainian; return true;
            case "zh-cn": language = Language.Simplified_Chinese; return true;
            case "zh-tw": language = Language.Traditional_Chinese; return true;
            case "ja": language = Language.Japanese; return true;
            case "ko": language = Language.Korean; return true;
            default:
                language = default;
                return false;
        }
    }
    
    public static bool ShouldUseNonBreakingSpaces(this Language language)
    {
        switch (language)
        {
            case Language.Simplified_Chinese:
            case Language.Traditional_Chinese:
            case Language.Japanese:
            case Language.Korean:
            case Language.Fujaoese:
                return true;
            default:
                return false;
        }
    }
    
}
