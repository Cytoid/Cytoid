using LiteDB;

public static class DatabaseExtensions
{

    public static LevelRecord GetLevelRecord(this LiteDatabase db, Level level)
    {
        return GetLevelRecord(db, level.Id);
    }
    
    public static LevelRecord GetLevelRecord(this LiteDatabase db, string levelId)
    {
        var col = db.GetCollection<LevelRecord>("level_records");
        col.EnsureIndex(x => x.LevelId, true);
        return col.FindOne(it => it.LevelId == levelId);
    }

    private static object SetLevelRecordLock = new object();

    public static void SetLevelRecord(this LiteDatabase db, LevelRecord record, bool overwrite = false)
    {
        var col = db.GetCollection<LevelRecord>("level_records");
        if (overwrite) col.FindOne(it => it.LevelId == record.LevelId)?.Let(it => col.Delete(it.Id));
        lock (SetLevelRecordLock)
        {
            if (!col.Update(record)) col.Insert(record);
        }
    }

    public static Profile GetProfile(this LiteDatabase db)
    {
        return db.GetCollection<Profile>("profile").FindOne(_ => true);
    }
    
    public static void SetProfile(this LiteDatabase db, Profile profile)
    {
        var col = db.GetCollection<Profile>("profile");
        col.DeleteMany(x => true);
        col.Insert(profile);
    }

}