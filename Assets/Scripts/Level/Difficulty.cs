public class Difficulty
{
    public static readonly Difficulty Easy =
        new Difficulty("easy", "Easy", new ColorGradient("#67b26f".ToColor(), "#4ca2cd".ToColor(), 135));

    public static readonly Difficulty Hard =
        new Difficulty("hard", "Hard", new ColorGradient("#b06ab3".ToColor(), "#4568dc".ToColor(), 135));

    public static readonly Difficulty Extreme =
        new Difficulty("extreme", "Extreme", new ColorGradient("#6f0000".ToColor(), "#200122".ToColor(), 135));

    public readonly string id;
    public readonly string name;
    public readonly ColorGradient gradient;

    private Difficulty(string id, string name, ColorGradient gradient)
    {
        this.id = id;
        this.name = name;
        this.gradient = gradient;
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