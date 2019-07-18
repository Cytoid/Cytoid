using UnityEngine;

public class ColorGradient
{
    public Color startColor;
    public Color endColor;
    public float angle;
    public ColorGradient(Color startColor, Color endColor, float angle)
    {
        this.startColor = startColor;
        this.endColor = endColor;
        this.angle = angle;
    }
}