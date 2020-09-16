using System.Collections.Generic;

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
}