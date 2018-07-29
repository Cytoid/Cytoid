public static class PreferenceKeys
{
    public static string LastUsername()
    {
        return "last_username";
    }

    public static string LastPassword()
    {
        return "last_password";
    }

    public static string BestScore(string level, string type, bool ranked)
    {
        return level + " : " + type + " : " + "best score" + (ranked ? " ranked" : "");
    }

    public static string BestAccuracy(string level, string type, bool ranked)
    {
        return level + " : " + type + " : " + "best accuracy" + (ranked ? " ranked" : "");
    }
    
    public static string BestClearType(string level, string type, bool ranked)
    {
        return level + " : " + type + " : " + "best clear type" + (ranked ? " ranked" : "");
    }

    public static string PlayCount(string level, string type)
    {
        return level + " : " + type + " : " + "play count";
    }

    public static string ChartRelativeOffset(string level)
    {
        return level + " : " + "relative offset";
    }
}