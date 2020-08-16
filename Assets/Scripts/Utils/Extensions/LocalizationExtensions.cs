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
                return "en-US";
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
                return "zh-HK";
            case Language.Japanese:
                return "ja";
            case Language.Korean:
                return "ko";
            case Language.Fujaoese:
                return "zh-CN";
            default:
                return "en-US";
        }
    }
    
}