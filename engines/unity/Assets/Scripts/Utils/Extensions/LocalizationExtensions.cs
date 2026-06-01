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