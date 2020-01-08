using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public class LevelMeta : IComparable<LevelMeta>
{
    public const string Easy = "easy";
    public const string Hard = "hard";
    public const string Extreme = "extreme";

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

    public int GetDifficultyLevel(string chartType)
    {
        foreach (var chart in charts)
        {
            if (chart.type != chartType) continue;
            return chart.difficulty;
        }

        return -1;
    }

    public int GetEasiestDifficultyLevel()
    {
        return charts.Min(it => it.difficulty);
    }

    public int GetHardestDifficultyLevel()
    {
        return charts.Max(it => it.difficulty);
    }

    [Serializable]
    public class MusicSection
    {
        public string path { get; set; }
    }

    [Serializable]
    public class BackgroundSection
    {
        public string path { get; set; }
    }

    [Serializable]
    public class ChartSection
    {
        public string type;
        public string name;
        public int difficulty;
        public string path;
        public MusicSection music_override;
        public StoryboardSection storyboard;
    }

    [Serializable]
    public class StoryboardSection
    {
        public string path = "storyboard.json";
    }

    public bool Validate()
    {
        if (id == null) return false;
        
        // Convert difficulty
        if (schema_version == 1)
        {
            foreach (var section in charts)
            {
                switch (section.difficulty)
                {
                    case 1:
                        section.difficulty = 2;
                        break;
                    case 2:
                        section.difficulty = 3;
                        break;
                    case 3:
                        section.difficulty = 4;
                        break;
                    case 4:
                        section.difficulty = 6;
                        break;
                    case 5:
                        section.difficulty = 8;
                        break;
                    case 6:
                        section.difficulty = 10;
                        break;
                    case 7:
                        section.difficulty = 11;
                        break;
                    case 8:
                        section.difficulty = 12;
                        break;
                    case 9:
                        section.difficulty = 14;
                        break;
                }

                if (section.difficulty >= 10 && section.difficulty <= 12)
                {
                    section.difficulty = 15;
                } 
                else if (section.difficulty >= 13)
                {
                    section.difficulty = 16;
                }
                else
                {
                    section.difficulty = 0;
                }
            }
        }

        return true;
    }

    public int CompareTo(LevelMeta other)
    {
        return string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return title;
    }
}