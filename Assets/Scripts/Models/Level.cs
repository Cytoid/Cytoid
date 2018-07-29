using System.Collections.Generic;
using Newtonsoft.Json;

public class Level
{
    public const string Easy = "easy";
    public const string Hard = "hard";
    public const string Extreme = "extreme";

    [JsonIgnore] public string BasePath;

    public int schema_version = 1;
    public int version = 1;
    public string id;
    public string title;
    public string title_localized;
    public string artist;
    public string artist_localized;
    public string artist_source;
    public string illustrator;
    public string illustrator_source;
    public string charter;
    public string storyboarder;
    public MusicSection music;
    public MusicSection music_preview;
    public BackgroundSection background;
    public List<ChartSection> charts = new List<ChartSection>();
    public bool is_internal;

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
    
    public ChartSection GetChartSection(string chartType)
    {
        foreach (var chart in charts)
        {
            if (chart.type != chartType) continue;
            return chart;
        }

        return null;
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

    public string GetDisplayDifficulty(ChartSection section)
    {
        if (schema_version == 1)
        {
            switch (section.difficulty)
            {
                case 1:
                    return "2";
                case 2:
                    return "3";
                case 3:
                    return "4";
                case 4:
                    return "6";
                case 5:
                    return "8";
                case 6:
                    return "10";
                case 7:
                    return "11";
                case 8:
                    return "12";
                case 9:
                    return "14";
            }

            if (section.difficulty >= 10 && section.difficulty <= 12)
            {
                return "15";
            }

            return section.difficulty >= 13 ? "15+" : "?";
        }

        if (section.difficulty >= 16) {
            return "15+";
        }
        return section.difficulty <= 0 ? "?" : section.difficulty.ToString();
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
        public string type;
        public string name;
        public int difficulty;
        public string path;
        public MusicSection music_override;
        public StoryboardSection storyboard;
    }

    public class StoryboardSection
    {
        public string path = "storyboard.json";
        public bool epilepsy_warning = true;
    }
}