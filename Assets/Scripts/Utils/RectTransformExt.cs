using UnityEngine;

public static class RectTransformExtensions
{
    public static void ChangeSizeDelta(this RectTransform rect, float x = float.NaN, float y = float.NaN)
    {
        var newX = float.IsNaN(x) ? rect.sizeDelta.x : x;
        var newY = float.IsNaN(y) ? rect.sizeDelta.y : y;
        var newSizeDelta = new Vector2(newX, newY);
        rect.sizeDelta = newSizeDelta;
    }
}