using UnityEngine;
using UnityEngine.UI;

public static class CommonExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    public static Color ToColor(this string rgbString)
    {
        ColorUtility.TryParseHtmlString(rgbString, out var color);
        return color;
    }

    public static void RebuildLayout(this Transform transform)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}