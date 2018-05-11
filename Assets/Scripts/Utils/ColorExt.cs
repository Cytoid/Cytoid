using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ColorExt
{
    
    public static void ChangeColor(this Text text, float r = float.NaN, float g = float.NaN, float b = float.NaN, float a = float.NaN)
    {
        var newR = float.IsNaN(r) ? text.color.r : r;
        var newG = float.IsNaN(g) ? text.color.g : g;
        var newB = float.IsNaN(b) ? text.color.b : b;
        var newA = float.IsNaN(a) ? text.color.a : a;
        var newColor = new Color(newR, newG, newB, newA);
        text.color = newColor;
    }
    
    public static void AlterColor(this Text text, float r = float.NaN, float g = float.NaN, float b = float.NaN, float a = float.NaN)
    {
        var newR = float.IsNaN(r) ? text.color.r : text.color.r + r;
        var newG = float.IsNaN(g) ? text.color.g : text.color.g + g;
        var newB = float.IsNaN(b) ? text.color.b : text.color.b + b;
        var newA = float.IsNaN(a) ? text.color.a : text.color.a + a;
        var newColor = new Color(newR, newG, newB, newA);
        text.color = newColor;
    }
    
    public static void AlterColor(this TextMeshProUGUI text, float r = float.NaN, float g = float.NaN, float b = float.NaN, float a = float.NaN)
    {
        var newR = float.IsNaN(r) ? text.color.r : text.color.r + r;
        var newG = float.IsNaN(g) ? text.color.g : text.color.g + g;
        var newB = float.IsNaN(b) ? text.color.b : text.color.b + b;
        var newA = float.IsNaN(a) ? text.color.a : text.color.a + a;
        var newColor = new Color(newR, newG, newB, newA);
        text.color = newColor;
    }
    
}