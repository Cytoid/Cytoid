using System;
using System.Collections.Generic;

public static class MockData
{
    public static Level CommunityLevel => new Level(Context.UserDataPath + "/f/", LevelType.Community, new LevelMeta
    {
        version = 1,
        schema_version = 2,
        id = "f",
        title = "(^^)",
        artist = "Yamajet",
        illustrator = "BOF2007",
        charter = "JJLin Vanquisher",
        music = new LevelMeta.MusicSection
        {
            path = "Music.wav",
        },
        music_preview = new LevelMeta.MusicSection
        {
            path = "Music.wav",
        },
        background = new LevelMeta.BackgroundSection
        {
            path = "bg_gamever.png"
        },
        charts = new List<LevelMeta.ChartSection>
        {
            new LevelMeta.ChartSection
            {
                type = "extreme",
                difficulty = 4,
                path = "output.cyt"
            }
        }
    }, DateTime.UtcNow, DateTime.UtcNow);
    
    public static Level TierLevel => new Level(Context.UserDataPath + "/f.tier/", LevelType.Tier, new LevelMeta
    {
        version = 1,
        schema_version = 2,
        id = "f",
        title = "(^^)",
        artist = "Yamajet",
        illustrator = "BOF2007",
        charter = "JJLin Vanquisher",
        music = new LevelMeta.MusicSection
        {
            path = "Music.wav",
        },
        music_preview = new LevelMeta.MusicSection
        {
            path = "Music.wav",
        },
        background = new LevelMeta.BackgroundSection
        {
            path = "bg_gamever.png"
        },
        charts = new List<LevelMeta.ChartSection>
        {
            new LevelMeta.ChartSection
            {
                type = "extreme",
                difficulty = 4,
                path = "output.cyt"
            }
        }
    }, DateTime.UtcNow, DateTime.UtcNow);

    public static Season Season => new Season
    {
        tiers = new List<Tier>
        {
            new Tier()
            {
                completion = 2,
                locked = false,
                Meta = new TierMeta
                {
                    name = "Mafumafu",
                    completionPercentage = 0.923f,
                    colorPalette = new TierMeta.ColorPalette
                    {
                        background = "#232526,#414345,-45",
                        stages = new[]
                        {
                            "#1D546A,#0000004C,-45",
                            "#1D546A,#0000004C,-45",
                            "#1D546A,#0000004C,-45"
                        }
                    },
                    thresholdAccuracy = 0.95,
                    maxHealth = 1000,
                    stages = new List<OnlineLevel>
                    {
                        new OnlineLevel
                        {
                            Uid = "f",
                            Version = 1,
                        },
                        new OnlineLevel
                        {
                            Uid = "f",
                            Version = 1,
                        },
                        new OnlineLevel
                        {
                            Uid = "f",
                            Version = 1,
                        }
                    },
                    parsedStages = new List<Level> {TierLevel},
                    character = new CharacterMeta
                    {
                        Name = "まふまふ",
                        AssetId = "Mafu",
                        TachieAssetId = "MafuTachie"
                    }
                }
            },
            new Tier
            {
                completion = 1.877f,
                locked = false,
                Meta = new TierMeta
                {
                    name = "Dan 2",
                    completionPercentage = 0.923f,
                    colorPalette = new TierMeta.ColorPalette
                    {
                        background = "#C04848,#480048",
                        stages = new[]
                        {
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C"
                        }
                    },
                    thresholdAccuracy = 0.96,
                    maxHealth = 1000,
                    stages = new List<OnlineLevel>
                    {
                        new OnlineLevel
                        {
                            Uid = "tiermode.jericho",
                            Version = 1,
                        },
                        new OnlineLevel
                        {
                            Uid = "tiermode.reflection",
                            Version = 1,
                        },
                        new OnlineLevel
                        {
                            Uid = "tiermode.cryout",
                            Version = 1
                        }
                    },
                }
            },
            new Tier
            {
                completion = 0,
                locked = true,
                Meta = new TierMeta
                {
                    name = "Dan 3",
                    completionPercentage = 0.77f,
                    colorPalette = new TierMeta.ColorPalette
                    {
                        background = "#C04848,#480048",
                        stages = new[]
                        {
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C"
                        }
                    },
                    thresholdAccuracy = 0.96,
                    maxHealth = 1000,
                    stages = new List<OnlineLevel>
                    {
                        new OnlineLevel
                        {
                            Uid = "tiermode.jericho",
                            Version = 1,
                        },
                        new OnlineLevel
                        {
                            Uid = "tiermode.reflection",
                            Version = 1,
                        },
                        new OnlineLevel
                        {
                            Uid = "tiermode.cryout",
                            Version = 1
                        }
                    },
                }
            },
        }
    };
}