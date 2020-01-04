using System;
using System.Linq;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class LayoutFixer : MonoBehaviour, ScreenBecameActiveListener
{
    public bool fixOnScreenBecameActive = false;
    public bool alternativeFix = false;
    
    private void Start()
    {
        if (!fixOnScreenBecameActive)
        {
            if (!alternativeFix) Fix(transform);
            else AlternativeFix(transform);
        }
    }
    
    public async void OnScreenBecameActive()
    {
        if (fixOnScreenBecameActive)
        {
            await UniTask.DelayFrame(0);
            if (!alternativeFix) Fix(transform);
            else AlternativeFix(transform);
        }
    }

    public static async void Fix(Transform transform)
    {
        var children = transform.GetComponentsInChildren<LayoutFixer>().ToList();
        for (var i = 1; i < 5; i++)
        {
            transform.RebuildLayout();
            children.ForEach(it => it.transform.RebuildLayout());
            await UniTask.DelayFrame(0);
        }
    }

    public static async void AlternativeFix(Transform transform)
    {
        var layoutGroup = transform.GetComponent<HorizontalOrVerticalLayoutGroup>();
        var canvasGroup = transform.GetComponent<CanvasGroup>();
        var alpha = canvasGroup.alpha;
        canvasGroup.alpha = 0;
        layoutGroup.enabled = false;
        await UniTask.DelayFrame(1);
        layoutGroup.enabled = true;
        canvasGroup.alpha = alpha;
    }

}