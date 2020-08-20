using System;
using System.IO;
using UnityEngine;

public class Level
{

    public LevelType Type;
    public bool IsLocal;
    public OnlineLevel OnlineLevel;
    
    public LevelMeta Meta;
    public LevelRecord Record;
    
    public string Id => Meta.id;

    public string Path;

    private Level()
    {
    }

    public static Level FromLocal(string path, LevelType type, LevelMeta meta)
    {
        return new Level {
            Type = type,
            IsLocal = true,
            Path = path,
            Meta = meta,
            Record = Context.Database.GetLevelRecord(meta.id) ?? new LevelRecord{LevelId = meta.id}
        };
    }
    
    public static Level FromRemote(LevelType type, LevelMeta meta)
    {
        return new Level {
            Type = type,
            IsLocal = false,
            Meta = meta,
            Record = Context.Database.GetLevelRecord(meta.id) ?? new LevelRecord{LevelId = meta.id}
        };
    }

    public void SaveRecord()
    {
        Context.Database.SetLevelRecord(Record);
    }

    public void CopyFrom(Level other)
    {
        Type = other.Type;
        IsLocal = other.IsLocal;
        OnlineLevel = other.OnlineLevel;
        Meta = other.Meta;
        Record = other.Record;
        Path = other.Path;
    }

}

public enum LevelType {
    User, Tier, BuiltIn, Temp
}

public static class LevelTypeExtensions {
    public static string GetDataPath(this LevelType type)
    {
        switch (type)
        {
            case LevelType.User:
                return Context.UserDataPath;
            case LevelType.Tier:
                return Path.Combine(Application.temporaryCachePath, "Tiers");
            case LevelType.BuiltIn:
                return Path.Combine(Application.temporaryCachePath, "BuiltIn");
            case LevelType.Temp:
                return Path.Combine(Application.temporaryCachePath, "Temp");
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}