using System;
using Newtonsoft.Json;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.UI;

public static class CommonExtensions
{
    public static Vector3 SetX(this Vector3 vector3, float x)
    {
        return new Vector3(x, vector3.y, vector3.z);
    }

    public static Vector3 SetY(this Vector3 vector3, float y)
    {
        return new Vector3(vector3.x, y, vector3.z);
    }

    public static Vector3 SetZ(this Vector3 vector3, float z)
    {
        return new Vector3(vector3.x, vector3.y, z);
    }

    public static Vector3 DeltaX(this Vector3 vector3, float x)
    {
        return new Vector3(vector3.x + x, vector3.y, vector3.z);
    }

    public static Vector3 DeltaY(this Vector3 vector3, float y)
    {
        return new Vector3(vector3.x, vector3.y + y, vector3.z);
    }

    public static Vector3 DeltaZ(this Vector3 vector3, float z)
    {
        return new Vector3(vector3.x, vector3.y, vector3.z + z);
    }

    public static Vector3 SetX(this Vector3 vector3, double x)
    {
        return vector3.SetX((float) x);
    }

    public static Vector3 SetY(this Vector3 vector3, double y)
    {
        return vector3.SetY((float) y);
    }

    public static Vector3 SetZ(this Vector3 vector3, double z)
    {
        return vector3.SetZ((float) z);
    }

    public static Vector3 DeltaX(this Vector3 vector3, double x)
    {
        return vector3.DeltaX((float) x);
    }

    public static Vector3 DeltaY(this Vector3 vector3, double y)
    {
        return vector3.DeltaY((float) y);
    }

    public static Vector3 DeltaZ(this Vector3 vector3, double z)
    {
        return vector3.DeltaZ((float) z);
    }

    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    public static void SetAlpha(this Image image, float alpha)
    {
        image.color = image.color.WithAlpha(alpha);
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

    public static void ShiftPivot(this RectTransform rectTransform, Vector2 pivot)
    {
        Vector3 deltaPosition = rectTransform.pivot - pivot;
        deltaPosition.Scale(rectTransform.rect.size);
        deltaPosition.Scale(rectTransform.localScale);
        deltaPosition = rectTransform.rotation * deltaPosition;
        rectTransform.pivot = pivot;
        rectTransform.localPosition -= deltaPosition;
    }

    public static Rect GetScreenSpaceRect(this RectTransform rectTransform)
    {
        var canvas = rectTransform.GetComponentInParent<Canvas>();
        var camera = canvas.worldCamera;
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var screenCorner1 = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
        var screenCorner3 = RectTransformUtility.WorldToScreenPoint(camera, corners[3]);
        var canvasScale = canvas.transform.localScale;
        screenCorner1 /= canvasScale;
        screenCorner3 /= canvasScale;
        var screenRect = new Rect();
        screenRect.x = screenCorner1.x;
        screenRect.width = screenCorner3.x - screenRect.x;
        screenRect.y = screenCorner3.y;
        screenRect.height = screenCorner1.y - screenRect.y;
        return screenRect;
    }

    public static Vector2 GetScreenSpaceCenter(this RectTransform rectTransform)
    {
        var rect = rectTransform.GetScreenSpaceRect();
        return new Vector2((rect.xMin + rect.xMax) / 2.0f, (rect.yMin + rect.yMax) / 2.0f);
    }

    public static Sprite CreateSprite(this Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);
    }

    public static void PrintJson(this object obj)
    {
        Debug.Log(JsonConvert.SerializeObject(obj));
    }

    public static bool IsCloseTo(this float number, float target)
    {
        return Math.Abs(number - target) < Constants.FloatingPointTolerance;
    }
    
    public static bool IsNotCloseTo(this float number, float target)
    {
        return !IsCloseTo(number, target);
    }

    public static IPromise Catch(this IPromise promise, Action<RequestException> onRejected)
    {
        return promise.Catch(exception => onRejected(exception as RequestException));
    }
    
    public static IPromise HandleRequestErrors(this IPromise promise)
    {
        return promise.Catch(error =>
        {
            if (error.IsNetworkError)
            {
                Toast.Next(Toast.Status.Failure,"Please check your network connection.");
            }
            else
            {
                switch (error.StatusCode)
                {
                    case 401:
                        Toast.Next(Toast.Status.Failure, "Incorrect Cytoid ID or password.");
                        break;
                    default:
                        Toast.Next(Toast.Status.Failure, "Status code: " + error.StatusCode);
                        break;
                }
            }
        });
    }

}