using System;

public class Level
{
    public bool IsLocal;
    public LevelMeta Meta;
    public string Id => Meta.id;

    public string Path;
    public string PackagePath;
    public DateTime AddedDate;
    public DateTime PlayedDate => Context.LocalPlayer.GetLastPlayedTime(Id);
    
    public Level(string path, LevelMeta meta, DateTime addedDate)
    {
        IsLocal = true;
        PackagePath = $"{Context.ApiBaseUrl}/levels/{meta.id}/resources";
        Path = path;
        Meta = meta;
        AddedDate = addedDate;
    }

    public Level(string packagePath, LevelMeta meta)
    {
        IsLocal = false;
        PackagePath = packagePath;
        Meta = meta;
    }

}