using UnityEngine;

public static class TransformExtensions
{
    public static void ChangePosition(this Transform transform, float x = float.NaN, float y = float.NaN,
        float z = float.NaN)
    {
        var newX = float.IsNaN(x) ? transform.position.x : x;
        var newY = float.IsNaN(y) ? transform.position.y : y;
        var newZ = float.IsNaN(z) ? transform.position.z : z;
        var newPos = new Vector3(newX, newY, newZ);
        transform.position = newPos;
    }

    public static void ChangeLocalPosition(this Transform transform, float x = float.NaN, float y = float.NaN,
        float z = float.NaN)
    {
        var newX = float.IsNaN(x) ? transform.localPosition.x : x;
        var newY = float.IsNaN(y) ? transform.localPosition.y : y;
        var newZ = float.IsNaN(z) ? transform.localPosition.z : z;
        var newPos = new Vector3(newX, newY, newZ);
        transform.localPosition = newPos;
    }

    public static void ChangeLocalScale(this Transform transform, float x = float.NaN, float y = float.NaN,
        float z = float.NaN)
    {
        var newX = float.IsNaN(x) ? transform.localScale.x : x;
        var newY = float.IsNaN(y) ? transform.localScale.y : y;
        var newZ = float.IsNaN(z) ? transform.localScale.z : z;
        var newPos = new Vector3(newX, newY, newZ);
        transform.localScale = newPos;
    }
}