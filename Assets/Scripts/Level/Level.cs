using System;

public class Level
{

    public string Path;
    public LevelMeta Meta;
    public DateTime AddedDate;
    public DateTime PlayedDate;
    
    public bool IsInternal;

    public Level(string path, LevelMeta meta, DateTime addedDate, DateTime playedDate)
    {
        Path = path;
        Meta = meta;
        AddedDate = addedDate;
        PlayedDate = playedDate;
    }

}