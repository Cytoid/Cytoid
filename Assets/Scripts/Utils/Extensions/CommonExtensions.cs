using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Newtonsoft.Json;
using Proyecto26;
using RSG;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = System.Object;

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

    private static readonly Dictionary<string, Color> ColorLookup = new Dictionary<string, Color>();

    public static Color ToColor(this string rgbString)
    {
        Assert.IsTrue(rgbString != null);
        if (ColorLookup.ContainsKey(rgbString.ToLower())) return ColorLookup[rgbString.ToLower()];
        ColorUtility.TryParseHtmlString(rgbString, out var color);
        ColorLookup[rgbString.ToLower()] = color;
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

    public static IPromise Catch(this IPromise promise, Action<RequestException> onRejected)
    {
        return promise.Catch(exception => onRejected(exception as RequestException));
    }
    
    public static IPromise HandleRequestErrors(this IPromise promise, Action<Exception> onRejected = null)
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
            onRejected?.Invoke(error);
        });
    }
    
    public static TweenerCore<float, float, FloatOptions> DOWidth(this RectTransform target, float endValue, float duration, bool snapping = false)
    {
        var t = DOTween.To(() => target.sizeDelta.x, x => target.sizeDelta = new Vector2(x, target.sizeDelta.y), endValue, duration);
        t.SetOptions(snapping).SetTarget(target);
        return t;
    }

    public static TweenerCore<float, float, FloatOptions> DOHeight(this RectTransform target, float endValue, float duration, bool snapping = false)
    {
        var t = DOTween.To(() => target.sizeDelta.y, y => target.sizeDelta = new Vector2(target.sizeDelta.x, y), endValue, duration);
        t.SetOptions(snapping).SetTarget(target);
        return t;
    }

    public static string WithParam<T>(this string url, KeyValuePair<string, T>[] parameters)
    {
        url += "?";
        foreach (var pair in parameters)
        {
            url += pair.Key + "=" + pair.Value;
            url += "&";
        }
        return url.Substring(0, url.Length - 1);
    }
    
    public static string WithSquareSizeParam(this string url, int size = 256)
    {
        return WithParam(url, new[] {"size".Pair(size)});
    }
    
    public static string WithSizeParam(this string url, int width = -1, int height = -1)
    {
        return WithParam(url, new[] {"w".Pair(width), "h".Pair(height)});
    }

    public static KeyValuePair<T1, T2> Pair<T1, T2>(this T1 a, T2 b)
    {
        return new KeyValuePair<T1, T2>(a, b);
    }
    
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable) action(item);
    }

    public static T Also<T>(this T on, Action<T> action)
    {
        action(on);
        return on;
    }
    
    public static void Apply<T>(this T on, Action<T> action)
    {
        action(on);
    }
    
    public static TR Let<T, TR>(this T on, Func<T, TR> action)
    {
        return action(on);
    }
    
    public static void SetX(this Transform transform, float x)
    {
        var position = transform.position;
        position = new Vector3(x, position.y, position.z);
        transform.position = position;
    }
    
    public static void SetY(this Transform transform, float y)
    {
        var position = transform.position;
        position = new Vector3(position.x, y, position.z);
        transform.position = position;
    }
    
    public static void SetZ(this Transform transform, float z)
    {
        var position = transform.position;
        position = new Vector3(position.x, position.y, z);
        transform.position = position;
    }
    
    public static void SetLocalScaleX(this Transform transform, float x)
    {
        var scale = transform.localScale;
        transform.localScale = new Vector3(x, scale.y, scale.z);
    }
    
    public static void SetLocalScaleY(this Transform transform, float y)
    {
        var scale = transform.localScale;
        transform.localScale = new Vector3(scale.x, y, scale.z);
    }

    public static void SetLocalScaleXY(this Transform transform, float x, float y)
    {
        transform.localScale = new Vector3(x, y, transform.localScale.z);
    }
    
    public static void SetLocalX(this Transform transform, float x)
    {
        var localPosition = transform.localPosition;
        localPosition = new Vector3(x, localPosition.y, localPosition.z);
        transform.localPosition = localPosition;
    }
    
}