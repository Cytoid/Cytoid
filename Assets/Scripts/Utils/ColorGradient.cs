using System;
using System.Linq;
using UnityEngine;

public class ColorGradient
{
    public static ColorGradient None = new ColorGradient(Color.white, Color.white, 0);
    public Color startColor;
    public Color endColor;
    public float angle;
    public ColorGradient(Color startColor, Color endColor, float angle)
    {
        this.startColor = startColor;
        this.endColor = endColor;
        this.angle = angle;
    }
    
    public ColorGradient(string gradient, float? angle = null)
    {
        var args = gradient.Split(',');
        startColor = args[0].ToColor();
        endColor = args[1].ToColor();
        this.angle = (args.Length > 2 && angle == null) ? float.Parse(args[2]) : (angle ?? 0);
    }
}

[Serializable]
public class SerializedGradient
{
    public string start;
    public string end;
    public float angle;
}