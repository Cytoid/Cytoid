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
    
    public ColorGradient(string gradient, float angle)
    {
        var colors = gradient.Split(',').Select(it => it.ToColor()).ToArray();
        startColor = colors[0];
        endColor = colors[1];
        this.angle = angle;
    }
}