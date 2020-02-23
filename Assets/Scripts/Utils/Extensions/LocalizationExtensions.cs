using System.Text.RegularExpressions;
using Polyglot;

public static class LocalizationExtensions
{
    public static string Get(this string originalText, params object[] parameters)
    {
        return Regex.Unescape(Localization.GetFormat(originalText, parameters));
    }
    
}