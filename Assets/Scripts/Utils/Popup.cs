using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Popup : SingletonMonoBehavior<Popup>
{
    public GameObject spawn;
    public GameObject popup;

    public static void Make(MonoBehaviour behavior, string message)
    {
        var popup = Instantiate(Instance.popup, Instance.spawn.transform);
        popup.GetComponentInChildren<Text>().text = message;
        behavior.StartCoroutine(Coroutine(popup));
    }

    private static IEnumerator Coroutine(GameObject popup)
    {
        var startTime = Time.realtimeSinceStartup;
        var group = popup.GetComponent<CanvasGroup>();
        group.alpha = 0;
        while (group.alpha < 0.99)
        {
            group.alpha = (float) OutExpo(Time.realtimeSinceStartup - startTime, 0.0, 1.0, 1.0);
            yield return null;
        }

        yield return new WaitForSeconds(2);
        startTime = Time.realtimeSinceStartup;
        while (group.alpha > 0.01)
        {
            group.alpha = (float) OutExpo(Time.realtimeSinceStartup - startTime, 1.0, -1.0, 1.0);
            yield return null;
        }

        Destroy(popup);
    }

    public static double OutExpo(double t, double b, double c, double d)
    {
        return c * (-Math.Pow(2, -10 * t / d) + 1) + b;
    }
}