using System;

public class Level
{
    public bool IsLocal;
    public bool IsInternal;
    public LevelMeta Meta;

    public string Path;
    public string PackagePath;
    public DateTime AddedDate;
    public DateTime PlayedDate;
    
    public Level(string path, LevelMeta meta, DateTime addedDate, DateTime playedDate)
    {
        IsLocal = true;
        Path = path;
        Meta = meta;
        AddedDate = addedDate;
        PlayedDate = playedDate;
    }

    public Level(string packagePath, LevelMeta meta)
    {
        IsLocal = false;
        PackagePath = packagePath;
        Meta = meta;
    }

}