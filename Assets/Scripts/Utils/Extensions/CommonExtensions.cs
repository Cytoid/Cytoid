using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Newtonsoft.Json;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

public static class CommonExtensions
{
    /**
     * Credits:
     * https://stackoverflow.com/a/1248/2706176
     */
    public static string Humanize(this DateTime localDate)
    {
        const int second = 1;
        const int minute = 60 * second;
        const int hour = 60 * minute;
        const int day = 24 * hour;
        const int month = 30 * day;

        var ts = new TimeSpan(DateTime.Now.Ticks - localDate.Ticks);
        var delta = Math.Abs(ts.TotalSeconds);

        if (delta < 1 * minute)
            return ts.Seconds == 1 ? "RELATIVE_TIME_SECOND_AGO_X".Get(ts.Seconds) : "RELATIVE_TIME_SECONDS_AGO_X".Get(ts.Seconds);

        if (delta < 2 * minute)
            return "RELATIVE_TIME_MINUTE_AGO_X".Get(ts.Minutes);

        if (delta < 60 * minute)
            return "RELATIVE_TIME_MINUTES_AGO_X".Get(ts.Minutes);

        if (delta < 120 * minute)
            return "RELATIVE_TIME_HOUR_AGO_X".Get(ts.Hours);

        if (delta < 24 * hour)
            return "RELATIVE_TIME_HOURS_AGO_X".Get(ts.Hours);

        if (delta < 48 * hour)
            return "RELATIVE_TIME_DAY_AGO_X".Get(ts.Days);

        if (delta < 30 * day)
            return "RELATIVE_TIME_DAYS_AGO_X".Get(ts.Days);

        if (delta < 12 * month)
        {
            var months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
            return months == 1 ? "RELATIVE_TIME_MONTH_AGO_X".Get(months) : "RELATIVE_TIME_MONTHS_AGO_X".Get(months);
        }

        var years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
        return years == 1 ? "RELATIVE_TIME_YEAR_AGO_X".Get(years) : "RELATIVE_TIME_YEARS_AGO_X".Get(years);
    }
    
    public static Dictionary<TK, TV> WithOverrides<TK, TV>(this Dictionary<TK, TV> baseDictionary, Dictionary<TK, TV> overrideDictionary)
    {
        baseDictionary = new Dictionary<TK, TV>(baseDictionary);
        overrideDictionary.ToList().ForEach(x => baseDictionary[x.Key] = x.Value);
        return baseDictionary;
    }
    
    public static void SetListener(this UnityEvent unityEvent, UnityAction call)
    {
        unityEvent.RemoveAllListeners();
        unityEvent.AddListener(call);
    }
    
    public static void SetListener<T>(this UnityEvent<T> unityEvent, UnityAction<T> call)
    {
        unityEvent.RemoveAllListeners();
        unityEvent.AddListener(call);
    }
    
    public static void SetListener<T1, T2>(this UnityEvent<T1, T2> unityEvent, UnityAction<T1, T2> call)
    {
        unityEvent.RemoveAllListeners();
        unityEvent.AddListener(call);
    }
    
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
    {
        return source.SelectMany(x => x);
    }
    
    public static float Clamp(this float f, float min, float max)
    {
        return Mathf.Max(min, Math.Min(max, f));
    }
    
    public static float Clamp01(this float f)
    {
        return Clamp(f, 0, 1);
    }
    
    public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
    {
        return new HashSet<TSource>(source);
    }
    
    public static List<(TK, TV)> RemoveAll<TK, TV>(this IDictionary<TK, TV> dictionary, Func<TV, bool> valuePredicate)
    {
        var removals = new List<(TK, TV)>();
        foreach (var kv in dictionary.Where(kv => valuePredicate(kv.Value)).ToList())
        {
            removals.Add(kv.ToPair());
            dictionary.Remove(kv.Key);
        }
        return removals;
    }
    
    public static List<(TK, TV)> RemoveAll<TK, TV>(this IDictionary<TK, TV> dictionary, Func<TK, bool> valuePredicate)
    {
        var removals = new List<(TK, TV)>();
        foreach (var kv in dictionary.Where(kv => valuePredicate(kv.Key)).ToList())
        {
            removals.Add(kv.ToPair());
            dictionary.Remove(kv.Key);
        }
        return removals;
    }

    public static (TK, TV) ToPair<TK, TV>(this KeyValuePair<TK, TV> kv)
    {
        return (kv.Key, kv.Value);
    }

    public static string ToHexString(this byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    public static void Update(this HMAC hmac, byte[] bytes)
    {
        hmac.TransformBlock(bytes, 0, bytes.Length, null, 0);
    }

    public static void Update(this HMAC hmac, string str)
    {
        Update(hmac, str.ToByteArray());
    }

    public static void Finalize(this HMAC hmac)
    {
        hmac.TransformFinalBlock(new byte[] { }, 0, 0);
    }

    /*
     * Credits: https://stackoverflow.com/a/8235530/2706176
     */
    public static byte[] HexToByteArray(this string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                "The binary key cannot have an odd number of digits: {0}", hex));
        }

        var data = new byte[hex.Length / 2];
        for (var index = 0; index < data.Length; index++)
        {
            var byteValue = hex.Substring(index * 2, 2);
            data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return data;
    }

    public static byte[] ToByteArray(this string str)
    {
        return str.ToCharArray().Select(it => (byte) it).ToArray();
    }

    public static Dictionary<T1, T2> With<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 value)
    {
        dictionary.Add(key, value);
        return dictionary;
    }

    public static bool IsNullOrEmptyTrimmed(this string str)
    {
        return str == null || string.IsNullOrEmpty(str.Trim());
    }

    public static TSource MinBy<TSource, TMin>(
        this IEnumerable<TSource> source,
        Func<TSource, TMin> selector)
    {
        return Enumerable.Range(0, int.MaxValue)
            .Zip(source, (index, it) => (selector(it), index, it)).Min().Item3;
    }
    
    public static TSource MaxBy<TSource, TMin>(
        this IEnumerable<TSource> source,
        Func<TSource, TMin> selector)
    {
        return Enumerable.Range(0, int.MaxValue)
            .Zip(source, (index, it) => (selector(it), index, it)).Max().Item3;
    }

    public static void SetLayerRecursively(this GameObject go, int layerNumber)
    {
        foreach (var trans in go.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber;
        }
    }

    // Credits: https://forum.unity.com/threads/test-if-ui-element-is-visible-on-screen.276549/#post-5075102
    /// <summary>
    /// Counts the bounding box corners of the given RectTransform that are visible in screen space.
    /// </summary>
    /// <returns>The amount of bounding box corners that are visible.</returns>
    /// <param name="rectTransform">Rect transform.</param>
    /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
    private static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera = null)
    {
        var screenBounds =
            new Rect(0f, 0f, UnityEngine.Screen.width,
                UnityEngine.Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
        var objectCorners = new Vector3[4];
        rectTransform.GetWorldCorners(objectCorners);

        var visibleCorners = 0;
        foreach (var t in objectCorners)
        {
            Vector3 tempScreenSpaceCorner; // Cached
            if (camera != null)
                tempScreenSpaceCorner =
                    camera.WorldToScreenPoint(t); // Transform world space position of corner to screen space
            else
            {
                // Debug.Log(rectTransform.gameObject.name+" :: "+objectCorners[i].ToString("F2"));
                tempScreenSpaceCorner =
                    t; // If no camera is provided we assume the canvas is Overlay and world space == screen space
            }

            if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
            {
                visibleCorners++;
            }
        }

        return visibleCorners;
    }

    /// <summary>
    /// Determines if this RectTransform is fully visible.
    /// Works by checking if each bounding box corner of this RectTransform is inside the screen space view frustrum.
    /// </summary>
    /// <returns><c>true</c> if is fully visible; otherwise, <c>false</c>.</returns>
    /// <param name="rectTransform">Rect transform.</param>
    /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
    public static bool IsFullyVisible(this RectTransform rectTransform, Camera camera = null)
    {
        if (!rectTransform.gameObject.activeInHierarchy)
            return false;

        return CountCornersVisibleFrom(rectTransform, camera) == 4; // True if all 4 corners are visible
    }

    /// <summary>
    /// Determines if this RectTransform is at least partially visible.
    /// Works by checking if any bounding box corner of this RectTransform is inside the screen space view frustrum.
    /// </summary>
    /// <returns><c>true</c> if is at least partially visible; otherwise, <c>false</c>.</returns>
    /// <param name="rectTransform">Rect transform.</param>
    /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
    public static bool IsVisible(this RectTransform rectTransform, Camera camera = null)
    {
        if (!rectTransform.gameObject.activeInHierarchy)
            return false;

        return CountCornersVisibleFrom(rectTransform, camera) > 0; // True if any corners are visible
    }

    // Credits: https://stackoverflow.com/a/11124118/2706176
    // Returns the human-readable file size for an arbitrary, 64-bit file size 
    // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
    public static string ToHumanReadableFileSize(this ulong bytes)
    {
        // Determine the suffix and readable value
        string suffix;
        double readable;
        if (bytes >= 0x1000000000000000) // Exabyte
        {
            suffix = "EB";
            readable = bytes >> 50;
        }
        else if (bytes >= 0x4000000000000) // Petabyte
        {
            suffix = "PB";
            readable = bytes >> 40;
        }
        else if (bytes >= 0x10000000000) // Terabyte
        {
            suffix = "TB";
            readable = bytes >> 30;
        }
        else if (bytes >= 0x40000000) // Gigabyte
        {
            suffix = "GB";
            readable = bytes >> 20;
        }
        else if (bytes >= 0x100000) // Megabyte
        {
            suffix = "MB";
            readable = bytes >> 10;
        }
        else if (bytes >= 0x400) // Kilobyte
        {
            suffix = "KB";
            readable = bytes;
        }
        else
        {
            return bytes.ToString("0 B"); // Byte
        }

        // Divide by 1024 to get fractional value
        readable = readable / 1024;
        // Return formatted number with suffix
        return readable.ToString("0.## ") + suffix;
    }

    public static string BoolToString(this bool b)
    {
        return b ? "true" : "false";
    }

    public static string ColorToString(this Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }

    public static int Mod(this int x, int m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }

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

    public static void SetAlpha(this Graphic graphic, float alpha)
    {
        graphic.color = graphic.color.WithAlpha(alpha);
    }

    private static readonly Dictionary<string, Color> ColorLookup = new Dictionary<string, Color>();

    public static Color ToColor(this string rgbString)
    {
        Assert.IsTrue(rgbString != null);
        if (ColorLookup.ContainsKey(rgbString.ToLower())) return ColorLookup[rgbString.ToLower()];
        if (!ColorUtility.TryParseHtmlString(rgbString, out var color))
        {
            Debug.LogError($"Invalid color string: {rgbString}");
            return Color.clear;
        }
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

    /* Provide canvas to save GetComponent lookup. */
    public static Rect GetScreenSpaceRect(this RectTransform rectTransform, Canvas canvas = default)
    {
        if (canvas == default) canvas = rectTransform.GetComponent<Canvas>();
        if (canvas == null) canvas = rectTransform.GetComponentInParent<Canvas>();
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

    public static Vector2 GetScreenSpaceCenter(this RectTransform rectTransform, Canvas canvas = default)
    {
        var rect = rectTransform.GetScreenSpaceRect(canvas);
        return new Vector2((rect.xMin + rect.xMax) / 2f, (rect.yMin + rect.yMax) / 2f);
    }

    public static Bounds GetScreenSpaceBounds(this RectTransform rectTransform, Canvas canvas = default)
    {
        var rect = rectTransform.GetScreenSpaceRect(canvas);
        return new Bounds(
            new Vector3((rect.xMin + rect.xMax) / 2f, (rect.yMin + rect.yMax) / 2f, 0),
            new Vector3(rect.xMax - rect.xMin, rect.yMax - rect.yMin, 0)
        );
    }
    
    public static void SetSize(this RectTransform rectTransform, Vector2 newSize)
    {
        var oldSize = rectTransform.rect.size;
        var deltaSize = newSize - oldSize;
        var pivot = rectTransform.pivot;
        rectTransform.offsetMin -= new Vector2(deltaSize.x * pivot.x, deltaSize.y * pivot.y);
        rectTransform.offsetMax += new Vector2(deltaSize.x * (1f - pivot.x), deltaSize.y * (1f - pivot.y));
    }

    public static void SetWidth(this RectTransform rectTransform, float newSize)
    {
        SetSize(rectTransform, new Vector2(newSize, rectTransform.rect.size.y));
    }

    public static void SetHeight(this RectTransform rectTransform, float newSize)
    {
        SetSize(rectTransform, new Vector2(rectTransform.rect.size.x, newSize));
    }

    public static Texture2D ToTexture2D(this byte[] bytes)
    {
        var texture = new Texture2D(2, 2); // Texture size does not matter
        texture.LoadImage(bytes);
        return texture;
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

    public static IPromise<T> CatchRequestError<T>(this IPromise<T> promise, Func<RequestException, T> onRejected)
    {
        return promise.Catch(exception =>
        {
            if (exception is RequestException requestException)
            {
                return onRejected(requestException);
            }
            Debug.LogError($"Exception thrown by promise");
            Debug.LogError(exception);
            throw exception;
        });
    }
    
    public static IPromise CatchRequestError<T>(this IPromise<T> promise, Action<RequestException> onRejected)
    {
        return promise.Catch(exception =>
        {
            if (exception is RequestException requestException)
            {
                onRejected(requestException);
            }
            else
            {
                Debug.LogError($"Exception thrown by promise");
                Debug.LogError(exception);
                throw exception;
            }
        });
    }
    
    public static IPromise CatchRequestError(this IPromise promise, Action<RequestException> onRejected)
    {
        return promise.Catch(exception =>
        {
            if (exception is RequestException requestException)
            {
                onRejected(requestException);
            }
            else
            {
                Debug.LogError($"Exception thrown by promise");
                Debug.LogError(exception);
                throw exception;
            }
        });
    }

    public static void CopyRectFrom(this RectTransform rectTransform, RectTransform target)
    {
        rectTransform.anchoredPosition = target.anchoredPosition;
        rectTransform.localScale = target.localScale;
        rectTransform.anchorMax = target.anchorMax;
        rectTransform.anchorMin = target.anchorMin;
        rectTransform.pivot = target.pivot;
        rectTransform.sizeDelta = target.sizeDelta;
    }

    public static TweenerCore<float, float, FloatOptions> DOWidth(this RectTransform target, float endValue,
        float duration, bool snapping = false)
    {
        var t = DOTween.To(() => target.sizeDelta.x, x => target.sizeDelta = new Vector2(x, target.sizeDelta.y),
            endValue, duration);
        t.SetOptions(snapping).SetTarget(target);
        return t;
    }

    public static TweenerCore<float, float, FloatOptions> DOHeight(this RectTransform target, float endValue,
        float duration, bool snapping = false)
    {
        var t = DOTween.To(() => target.sizeDelta.y, y => target.sizeDelta = new Vector2(target.sizeDelta.x, y),
            endValue, duration);
        t.SetOptions(snapping).SetTarget(target);
        return t;
    }

    public static string LowerCaseFirstChar(this string s)
    {
        if (char.IsUpper(s[0]))
        {
            return s[0] - ('A' - 'a') + s.Substring(1);
        }

        return s;
    }

    public static string WithParam<T>(this string url, ValueTuple<string, T>[] parameters)
    {
        var sepIndex = url.LastIndexOf("?", StringComparison.Ordinal);
        url = sepIndex >= 0 ? url.Substring(0, sepIndex) : url;
        url += "?";
        foreach (var (key, value) in parameters)
        {
            url += key + "=" + value;
            url += "&";
        }

        return url.Substring(0, url.Length - 1);
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
    
    public static void Let<T>(this T on, Action<T> action)
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
    
    public static void SetAnchoredX(this RectTransform transform, float x)
    {
        var position = transform.anchoredPosition;
        position = new Vector2(x, position.y);
        transform.anchoredPosition = position;
    }

    public static void SetAnchoredY(this RectTransform transform, float y)
    {
        var position = transform.anchoredPosition;
        position = new Vector2(position.x, y);
        transform.anchoredPosition = position;
    }
    
    public static void DeltaX(this Transform transform, float dx)
    {
        var position = transform.position;
        position = new Vector3(position.x + dx, position.y, position.z);
        transform.position = position;
    }

    public static void DeltaY(this Transform transform, float dy)
    {
        var position = transform.position;
        position = new Vector3(position.x, position.y + dy, position.z);
        transform.position = position;
    }

    public static void DeltaZ(this Transform transform, float dz)
    {
        var position = transform.position;
        position = new Vector3(position.x, position.y, position.z + dz);
        transform.position = position;
    }

    public static void SetLocalScale(this Transform transform, float xyz)
    {
        transform.localScale = new Vector3(xyz, xyz, xyz);
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

    public static void SetLocalScaleXY(this Transform transform, float x, float y = default)
    {
        if (y == default) y = x;
        transform.localScale = new Vector3(x, y, transform.localScale.z);
    }

    public static void SetLocalX(this Transform transform, float x)
    {
        var localPosition = transform.localPosition;
        localPosition = new Vector3(x, localPosition.y, localPosition.z);
        transform.localPosition = localPosition;
    }

    public static void SetLocalY(this Transform transform, float y)
    {
        var localPosition = transform.localPosition;
        localPosition = new Vector3(localPosition.x, y, localPosition.z);
        transform.localPosition = localPosition;
    }
    
    public static void SetLocalZ(this Transform transform, float z)
    {
        var localPosition = transform.localPosition;
        localPosition = new Vector3(localPosition.x, localPosition.y, z);
        transform.localPosition = localPosition;
    }
    
    public static void SetLocalEulerAnglesX(this Transform transform, float x)
    {
        var eulerAngles = transform.localEulerAngles;
        eulerAngles = new Vector3(x, eulerAngles.y, eulerAngles.z);
        transform.localEulerAngles = eulerAngles;
    }
    
    public static void SetLocalEulerAnglesY(this Transform transform, float y)
    {
        var eulerAngles = transform.localEulerAngles;
        eulerAngles = new Vector3(eulerAngles.x, y, eulerAngles.z);
        transform.localEulerAngles = eulerAngles;
    }
    
    public static void SetLocalEulerAnglesZ(this Transform transform, float z)
    {
        var eulerAngles = transform.localEulerAngles;
        eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, z);
        transform.localEulerAngles = eulerAngles;
    }

    public static HslColor ToHslColor(this Color color)
    {
        return HslColor.FromRgbColor(color);
    }

    public static List<T> ListOf<T>(this object self, params T[] objects)
    {
        return objects.ToList();
    }

    public static void FitSpriteAspectRatio(this Image image)
    {
        if (image == null || image.sprite == null || image.sprite.texture == null) return;
        var texture = image.sprite.texture;
        image.GetComponent<AspectRatioFitter>().aspectRatio = texture.width * 1.0f / texture.height;
    }

    public static T JsonDeepCopy<T>(this T source)
    {
        if (ReferenceEquals(source, null))
        {
            return default;
        }

        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source),
            new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace});
    }
}

public class HslColor
{
    public double H;
    public double S;
    public double L;
    public float A;

    public HslColor(double h, double s, double l, float a = 1)
    {
        H = h;
        S = s;
        L = l;
        A = a;
    }

    public Color ToRgbColor()
    {
        HslToRgb(H, S, L, out var r, out var g, out var b);
        return new Color((float) r, (float) g, (float) b, A);
    }

    public static HslColor FromRgbColor(Color color)
    {
        RgbToHsl(color.r, color.g, color.b, out var h, out var s, out var l);
        return new HslColor(h, s, l, color.a);
    }

    private static void RgbToHsl(double r, double g, double b,
        out double h, out double s, out double l)
    {
        // Convert RGB to a 0.0 to 1.0 range.
        var doubleR = r;
        var doubleG = g;
        var doubleB = b;

        // Get the maximum and minimum RGB components.
        var max = doubleR;
        if (max < doubleG) max = doubleG;
        if (max < doubleB) max = doubleB;

        var min = doubleR;
        if (min > doubleG) min = doubleG;
        if (min > doubleB) min = doubleB;

        var diff = max - min;
        l = (max + min) / 2;
        if (Math.Abs(diff) < 0.00001)
        {
            s = 0;
            h = 0; // H is really undefined.
        }
        else
        {
            if (l <= 0.5) s = diff / (max + min);
            else s = diff / (2 - max - min);

            var rDist = (max - doubleR) / diff;
            var gDist = (max - doubleG) / diff;
            var bDist = (max - doubleB) / diff;

            if (Math.Abs(doubleR - max) < 0.000001) h = bDist - gDist;
            else if (Math.Abs(doubleG - max) < 0.000001) h = 2 + rDist - bDist;
            else h = 4 + gDist - rDist;

            h = h * 60;
            if (h < 0) h += 360;
        }
    }

    public static void HslToRgb(double h, double s, double l,
        out double r, out double g, out double b)
    {
        double p2;
        if (l <= 0.5) p2 = l * (1 + s);
        else p2 = l + s - l * s;

        var p1 = 2 * l - p2;
        double doubleR, doubleG, doubleB;
        if (Math.Abs(s) < 0.000001)
        {
            doubleR = l;
            doubleG = l;
            doubleB = l;
        }
        else
        {
            doubleR = QqhToRgb(p1, p2, h + 120);
            doubleG = QqhToRgb(p1, p2, h);
            doubleB = QqhToRgb(p1, p2, h - 120);
        }

        r = doubleR;
        g = doubleG;
        b = doubleB;
    }

    private static double QqhToRgb(double q1, double q2, double hue)
    {
        if (hue > 360) hue -= 360;
        else if (hue < 0) hue += 360;

        if (hue < 60) return q1 + (q2 - q1) * hue / 60;
        if (hue < 180) return q2;
        if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
        return q1;
    }
}

public static class AudioTypeExtensions
{
    public static AudioType Detect(string path)
    {
        if (path.EndsWith(".mp3")) return AudioType.MPEG;
        if (path.EndsWith(".wav")) return AudioType.WAV;
        if (path.EndsWith(".ogg")) return AudioType.OGGVORBIS;
        return AudioType.UNKNOWN;
    }
}