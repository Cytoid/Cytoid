public static class LocalizationExtensions
{
    public static string Localized(this string originalText, params object[] parameters)
    {
        return string.Format(originalText, parameters);
    }
}