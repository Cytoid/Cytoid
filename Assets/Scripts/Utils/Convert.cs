using QuickEngine.Extensions;
using UnityEngine;

public static class Convert
{
    public static Color HexToColor(string hex)
    {
        if (hex.IsNullOrEmpty()) return Color.clear;
        
        hex = hex.Replace("0x", "").Replace("#", "");
        byte a = 255;
        var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }

        return new Color32(r, g, b, a);
    }
}