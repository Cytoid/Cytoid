using System;
using System.Collections.Generic;

public static class MockData
{
    public static Season Season = new Season
    {
        title = "2019.1",
        tiers = new List<Tier>
        {
            new Tier()
            {
                completion = 2,
                locked = false,
                data = new OnlineTier
                {
                    name = "Dan 1",
                    completionPercentage = 0.923f,
                    colorPalette = new OnlineTier.ColorPalette
                    {
                        background = "#11998E,#728CE4",
                        stages = new[]
                        {
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C"
                        }
                    },
                    criteria = new List<string>
                    {
                        "<b>Full combo</b> every stage",
                        "<b>≥ 99.5% accuracy</b> in the 3rd",
                    },
                    stages = new List<OnlineLevel>
                    {
                        new OnlineLevel
                        {
                            uid = "tiermode.jericho",
                            version = 1,
                        },
                        new OnlineLevel
                        {
                            uid = "tiermode.reflection",
                            version = 1,
                        },
                        new OnlineLevel
                        {
                            uid = "tiermode.cryout",
                            version = 1
                        }
                    },
                    character = new OnlineCharacter
                    {
                        name = "Kaede",
                        description = "",
                        thumbnailURL = "https://assets.cytoid.io/static/characters/kaede/thumbnail.png",
                        silhouetteURL = "https://assets.cytoid.io/static/characters/kaede/silhouette.png"
                    }
                }
            },
            new Tier
            {
                completion = 1.877f,
                locked = false,
                data = new OnlineTier
                {
                    name = "Dan 2",
                    completionPercentage = 0.923f,
                    colorPalette = new OnlineTier.ColorPalette
                    {
                        background = "#C04848,#480048",
                        stages = new[]
                        {
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C"
                        }
                    },
                    criteria = new List<string>
                    {
                        "Test",
                    },
                    stages = new List<OnlineLevel>
                    {
                        new OnlineLevel
                        {
                            uid = "tiermode.jericho",
                            version = 1,
                        },
                        new OnlineLevel
                        {
                            uid = "tiermode.reflection",
                            version = 1,
                        },
                        new OnlineLevel
                        {
                            uid = "tiermode.cryout",
                            version = 1
                        }
                    },
                }
            },
            new Tier
            {
                completion = 0,
                locked = true,
                data = new OnlineTier
                {
                    name = "Dan 3",
                    completionPercentage = 0.77f,
                    colorPalette = new OnlineTier.ColorPalette
                    {
                        background = "#C04848,#480048",
                        stages = new[]
                        {
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C",
                            "#1D546A,#0000004C"
                        }
                    },
                    criteria = new List<string>
                    {
                        "Test",
                    },
                    stages = new List<OnlineLevel>
                    {
                        new OnlineLevel
                        {
                            uid = "tiermode.jericho",
                            version = 1,
                        },
                        new OnlineLevel
                        {
                            uid = "tiermode.reflection",
                            version = 1,
                        },
                        new OnlineLevel
                        {
                            uid = "tiermode.cryout",
                            version = 1
                        }
                    },
                }
            },
        }
    };
}