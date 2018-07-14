using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToastUtil : MonoBehaviour
{
    public static Color imgColor = new Color(1.0f, 0.5f, 0.5f, 0.9f);
    public static Color textColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public static Vector2 startPos = new Vector2(0, -500); // 開始場所
    public static Vector2 endPos = new Vector2(0, -300); // 終了場所
    public static int fontSize = 60;
    public static int moveFrame = 30; // 浮き上がりの時間(フレーム)
    public static int waitFrame = 30; // 浮き上がり後の時間(フレーム)
    public static int pad = 100; // padding
    public static Sprite imgSprite;
    public static Font textFont;

    public static void Toast<T>(MonoBehaviour mb, T m)
    {
        string msg = m.ToString();
        GameObject g = new GameObject("ToastCanvas");
        Canvas canvas = g.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; //最前
        g.AddComponent<CanvasScaler>();
        g.AddComponent<GraphicRaycaster>();

        GameObject g2 = new GameObject("Image");
        g2.transform.parent = g.transform;
        Image im = g2.AddComponent<Image>();
        if (imgSprite) im.sprite = imgSprite;
        im.color = imgColor;
        g2.GetComponent<RectTransform>().anchoredPosition = startPos;

        GameObject g3 = new GameObject("Text");
        g3.transform.parent = g2.transform;
        Text t = g3.AddComponent<Text>();
        g3.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        t.alignment = TextAnchor.MiddleCenter;
        if (textFont)
            t.font = textFont;
        else
            t.font = (Font) Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        t.fontSize = fontSize;
        t.text = msg;
        t.enabled = true;
        t.color = textColor;

        g3.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth, t.preferredHeight);
        g3.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth, t.preferredHeight); //2回必要
        g2.GetComponent<RectTransform>().sizeDelta = new Vector2(t.preferredWidth + pad, t.preferredHeight + pad);

        mb.StartCoroutine(
            DoToast(
                g2.GetComponent<RectTransform>(), (endPos - startPos) * (1f / moveFrame), g
            )
        );
    }

    static IEnumerator DoToast(RectTransform rec, Vector2 dif, GameObject g)
    {
        for (var i = 1; i <= moveFrame; i++)
        {
            rec.anchoredPosition += dif;
            yield return null;
        }

        for (var i = 1; i <= waitFrame; i++) yield return null;
        Destroy(g);
    }
}