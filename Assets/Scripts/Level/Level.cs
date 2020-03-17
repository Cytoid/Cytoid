using System;
using System.IO;
using UnityEngine;

public class Level
{

    public LevelType Type;
    public bool IsLocal;
    public OnlineLevel OnlineLevel;
    
    public LevelMeta Meta;
    public string Id => Meta.id;

    public string Path;
    public string PackagePath;
    public DateTime AddedDate;

    public DateTime PlayedDate
    {
        get => playedDate;
        set
        {
            playedDate = value;
            Context.LocalPlayer.SetLastPlayedDate(Id, value);
        }
    }

    private DateTime playedDate;
    
    public Level(string path, LevelType type, LevelMeta meta, DateTime addedDate, DateTime playedDate)
    {
        Type = type;
        IsLocal = true;
        PackagePath = $"{Context.ApiUrl}/levels/{meta.id}/resources";
        Path = path;
        Meta = meta;
        AddedDate = addedDate;
        this.playedDate = playedDate;
    }

    public Level(string packagePath, LevelType type, LevelMeta meta)
    {
        Type = type;
        IsLocal = false;
        PackagePath = packagePath;
        Meta = meta;
    }

}

public enum LevelType {
    Community, Official, Tier
}

public static class LevelTypeExtensions {
    public static string GetDataPath(this LevelType type)
    {
        switch (type)
        {
            case LevelType.Community:
                return Context.UserDataPath;
            case LevelType.Official:
                return Path.Combine(Application.temporaryCachePath, "Levels");
            case LevelType.Tier:
                return Path.Combine(Application.temporaryCachePath, "Tiers");
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}