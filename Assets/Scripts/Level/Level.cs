using System;

public class Level
{
    public bool IsLocal;
    public bool IsLibrary;
    public bool IsTier;
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
    
    public Level(string path, bool isLibrary, LevelMeta meta, DateTime addedDate, DateTime playedDate)
    {
        IsLocal = true;
        IsLibrary = isLibrary;
        PackagePath = $"{Context.ApiBaseUrl}/levels/{meta.id}/resources";
        Path = path;
        Meta = meta;
        AddedDate = addedDate;
        PlayedDate = playedDate;
    }

    public Level(string packagePath, LevelMeta meta)
    {
        IsLocal = false;
        IsLibrary = false;
        PackagePath = packagePath;
        Meta = meta;
    }

}