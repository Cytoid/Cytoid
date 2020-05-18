public class Difficulty
{
    public static readonly Difficulty Easy =
        new Difficulty(0, "easy", "Easy", "#67b26f", "#4ca2cd", 135);

    public static readonly Difficulty Hard =
        new Difficulty(1, "hard", "Hard", "#b06ab3", "#4568dc", 135);

    public static readonly Difficulty Extreme =
        new Difficulty(2, "extreme", "EX", "#6f0000", "#200122", 135);
    
    public readonly string Id;
    public readonly string Name;
    public readonly string StartColor;
    public readonly string ToColor;
    public readonly int Angle;
    public ColorGradient Gradient => new ColorGradient(StartColor.ToColor(), ToColor.ToColor(), Angle);

    private int order;
    
    private Difficulty(int order, string id, string name, string startColor, string toColor, int angle)
    {
        this.order = order;
        Id = id;
        Name = name;
        StartColor = startColor;
        ToColor = toColor;
        Angle = angle;
    }
    
    public static bool operator<(Difficulty a, Difficulty b)
    {
        return a.order < b.order;
    }
    public static bool operator>(Difficulty a, Difficulty b)
    {
        return a.order > b.order;
    }

    public static string ConvertToDisplayLevel(int level)
    {
        if (level <= 0) return "?";
        if (level > 15) return "15+";
        return level.ToString();
    }

    public static Difficulty Parse(string type)
    {
        switch (type)
        {
            case "easy":
                return Easy;
            case "hard":
                return Hard;
            case "extreme":
                return Extreme;
            default:
                return Extreme;
        }
    }
}