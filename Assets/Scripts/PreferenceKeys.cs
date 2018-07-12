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

    public static string BestScore(Level level, string type, bool ranked)
    {
        return level.id + " : " + type + " : " + "best score" + (ranked ? " ranked" : "");
    }
    
    public static string BestAccuracy(Level level, string type, bool ranked)
    {
        return level.id + " : " + type + " : " + "best accuracy" + (ranked ? " ranked" : "");
    }
    
    public static string PlayCount(Level level, string type)
    {
        return level.id + " : " + type + " : " + "play count";
    }
    
    public static string WillOverrideOptions(Level level)
    {
        return level.id + " : " + "override options";
    }
    
    public static string NoteDelay(Level level)
    {
        return level.id + " : " + "note delay";
    }
    
}