using System.Collections.Generic;
using System.IO;
using System.Text;
using Cytus2.Models;
using Newtonsoft.Json;
using UnityEngine;

public class Level
{

    [JsonIgnore] public string BasePath;
    
    public string format { get; set; }
    public int version { get; set; }
    public string id { get; set; }
    public string title { get; set; }
    public string artist { get; set; }
    public string illustrator { get; set; }
    public string charter { get; set; }
    public MusicSection music { get; set; }
    public MusicSection music_preview { get; set; }
    public BackgroundSection background { get; set; }
    public List<ChartSection> charts { get; set; }
    public bool is_internal { get; set;  }

    public string GetMusicPath(string chartType)
    {
        foreach (var chart in charts)
        {
            if (chart.type != chartType) continue;
            if (chart.music_override != null && chart.music_override.path != null)
            {
                return chart.music_override.path;
            }
        }
        return music.path;
    }

    public int GetDifficulty(string chartType)
    {
        foreach (var chart in charts)
        {
            if (chart.type != chartType) continue;
            return chart.difficulty;
        }
        return -1;
    }
    
    public class MusicSection
    {
        public string path { get; set; }
    }

    public class BackgroundSection
    {
        public string path { get; set; }
    }

    public class ChartSection
    {
        public string type { get; set; } 
        public int difficulty { get; set; }
        public string path { get; set; }
        public MusicSection music_override { get; set; }
    }

}

public class ChartType
{
    
    public const string Easy = "easy";
    public const string Hard = "hard";
    public const string Extreme = "extreme";
    
}