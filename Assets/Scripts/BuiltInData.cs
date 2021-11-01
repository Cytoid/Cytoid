using System.Collections.Generic;
using Newtonsoft.Json;

public static class BuiltInData
{
    public const int TrainingModeVersion = 3;
    public static readonly List<string> TrainingModeLevelIds = new List<string>
    {
        "skillmode.space",
        "skillmode.midnight",
        "skillmode.promise",
        "skillmode.neutralize",
        "skillmode.specter",
        "skillmode.quartzia",
        "skillmode.arise",
        "skillmode.embrace",
        "skillmode.howling",
        "skillmode.catchingup",
        "skillmode.yumend",
        "skillmode.goodworld",
        "skillmode.hypocrisy",
        "skillmode.antinomia",
        "skillmode.thebigblack"
    };
    public static readonly List<string> BuiltInLevelIds = new List<string>
    {
        "io.cytoid.tutorial",
        "io.cytoid.ecstatic",
        "io.cytoid.8bit_adventurer"
    };
    public const string GlobalCalibrationModeLevelId = "teages.offset_guide";
    public const string TutorialLevelId = "io.cytoid.tutorial";
    public const string DefaultCharacterAssetId = "Sayaka";
    public const string DefaultCharacterId = "5eca012ff308e7638835dec7";
    public const string DefaultCharacterName = "Sayaka";
    public static readonly CharacterMeta DefaultCharacterMeta = JsonConvert.DeserializeObject<CharacterMeta>(
        @"{
        ""illustrator"": {
            ""name"": ""松尾"",
            ""url"": ""https://twitter.com/SuteinuA""
        },
        ""designer"": {
            ""name"": ""Carota侵略"",
            ""url"": ""https://www.pixiv.net/users/3795973""
        },
        ""name"": ""Sayaka"",
        ""description"": ""\""Let's shape the rhythm world... together!\"""",
        ""asset"": ""Sayaka"",
        ""tachieAsset"": ""SayakaTachie"",
        ""variantName"": null,
        ""id"": ""5eca012ff308e7638835dec7"",
        ""level"": {
            ""id"": 5905,
            ""version"": 5,
            ""uid"": ""io.cytoid.tutorial"",
            ""title"": ""Tutorial"",
            ""metadata"": {
                ""title"": ""Tutorial"",
                ""artist"": {
                    ""url"": ""https://www.youtube.com/watch?v=fWiKGA85yPo&list=OLAK5uy_nh-iDmSdBb6ofQRqI9-AGs5c2aCfgKelw&index=3"",
                    ""name"": ""PTB10""
                },
                ""charter"": {
                    ""name"": ""yyao""
                },
                ""illustrator"": {
                    ""url"": ""https://www.pixiv.net/users/7569861"",
                    ""name"": ""八里""
                },
                ""title_localized"": ""Aftermath / Sayaka's Theme""
            },
            ""duration"": 141.5314,
            ""size"": 11111345,
            ""charts"": [
                {
                    ""id"": 9825,
                    ""name"": ""Basic"",
                    ""type"": ""easy"",
                    ""difficulty"": 1,
                    ""notesCount"": 78
                },
                {
                    ""id"": 9826,
                    ""name"": ""Advanced"",
                    ""type"": ""hard"",
                    ""difficulty"": 6,
                    ""notesCount"": 301
                }
            ],
            ""owner"": {
                ""id"": ""69c88094-93ea-4ab8-8f99-bd73a9608d3e"",
                ""uid"": ""cytoid"",
                ""name"": ""Cytoid Admin"",
                ""role"": ""moderator"",
                ""active"": true,
                ""avatar"": {
                    ""original"": ""https://assets.cytoid.io/avatar/k3nNUMHztoYF7Xf1vWTg0ZRi5FrmrUvHKf4p01BR3z4P939xdVhD57h3L6sFr7GeckM"",
                    ""small"": ""https://images.cytoid.io/avatar/k3nNUMHztoYF7Xf1vWTg0ZRi5FrmrUvHKf4p01BR3z4P939xdVhD57h3L6sFr7GeckM?h=64&w=64"",
                    ""medium"": ""https://images.cytoid.io/avatar/k3nNUMHztoYF7Xf1vWTg0ZRi5FrmrUvHKf4p01BR3z4P939xdVhD57h3L6sFr7GeckM?h=128&w=128"",
                    ""large"": ""https://images.cytoid.io/avatar/k3nNUMHztoYF7Xf1vWTg0ZRi5FrmrUvHKf4p01BR3z4P939xdVhD57h3L6sFr7GeckM?h=256&w=256""
                }
            },
            ""state"": ""UNLISTED"",
            ""cover"": {
                ""original"": ""https://assets.cytoid.io/levels/bundles/0g1f8yq3WlZA1Mx7br0cCwHTTk2xU5D1ntXhXYxuAhdUTTuLZodrEoojk70wSCeQ/background.jpg"",
                ""thumbnail"": ""https://images.cytoid.io/levels/bundles/0g1f8yq3WlZA1Mx7br0cCwHTTk2xU5D1ntXhXYxuAhdUTTuLZodrEoojk70wSCeQ/background.jpg?h=360&w=576"",
                ""cover"": ""https://images.cytoid.io/levels/bundles/0g1f8yq3WlZA1Mx7br0cCwHTTk2xU5D1ntXhXYxuAhdUTTuLZodrEoojk70wSCeQ/background.jpg?h=800&w=1280"",
                ""stripe"": ""https://images.cytoid.io/levels/bundles/0g1f8yq3WlZA1Mx7br0cCwHTTk2xU5D1ntXhXYxuAhdUTTuLZodrEoojk70wSCeQ/background.jpg?h=800&w=768""
            },
            ""music"": ""https://assets.cytoid.io/levels/bundles/0g1f8yq3WlZA1Mx7br0cCwHTTk2xU5D1ntXhXYxuAhdUTTuLZodrEoojk70wSCeQ/Cytoid_Result_game_cut.mp3"",
            ""musicPreview"": ""https://assets.cytoid.io/levels/bundles/0g1f8yq3WlZA1Mx7br0cCwHTTk2xU5D1ntXhXYxuAhdUTTuLZodrEoojk70wSCeQ/Cytoid_Menu_game_cut_preview.mp3""
        },
        ""date"": ""2021-10-30T08:50:09.401Z"",
        ""owned"": true,
        ""exp"": {
            ""totalExp"": 0,
            ""currentLevel"": 1,
            ""nextLevelExp"": 350,
            ""currentLevelExp"": 0
        }
    }");
}