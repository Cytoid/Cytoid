public static class PreferenceKeys
{

    public static string BestScore(Level level, string type)
    {
        return level.id + " : " + type + " : " + "best score";
    }
    
    public static string BestAccuracy(Level level, string type)
    {
        return level.id + " : " + type + " : " + "best accuracy";
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
    
    public static string WillInverse(Level level)
    {
        return level.id + " : " + "inverse";
    }
    
}