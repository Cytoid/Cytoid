public class Level
{

    public string path;
    public LevelMeta meta;
    public bool isInternal;

    public Level(string path, LevelMeta meta)
    {
        this.path = path;
        this.meta = meta;
    }

}