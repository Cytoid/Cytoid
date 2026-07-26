using System;
using System.IO;
using UnityEngine;

public class Level
{

    public LevelType Type;

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
            Path = path,
            Meta = meta,
            Record = new LevelRecord()
        };
    }

    public static Level FromExternal(LevelMeta meta, string path = null)
    {
        return new Level {
            Type = LevelType.Temp,
            Path = path ?? string.Empty,
            Meta = meta,
            Record = new LevelRecord()
        };
    }

}

public enum LevelType {
    User, BuiltIn, Temp
}

public static class LevelTypeExtensions {
    public static string GetDataPath(this LevelType type)
    {
        switch (type)
        {
            case LevelType.User:
                return Application.persistentDataPath;
            case LevelType.BuiltIn:
                return Path.Combine(Application.temporaryCachePath, "Built In");
            case LevelType.Temp:
                return Path.Combine(Application.temporaryCachePath, "Temp");
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
