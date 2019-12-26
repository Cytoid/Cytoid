using System;
using System.Linq;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class LayoutFixer : MonoBehaviour, ScreenBecameActiveListener
{
    public bool fixOnScreenBecameActive = false;
    
    private void Start()
    {
        Fix(transform);
    }
    
    public void OnScreenBecameActive()
    {
        if (fixOnScreenBecameActive) Fix(transform);
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

    public static async void Staticize(HorizontalLayoutGroup layoutGroup)
    {
        var gameObject = layoutGroup.gameObject;
        Destroy(layoutGroup);
        var contentSizeFitter = gameObject.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null) Destroy(contentSizeFitter);
        foreach (RectTransform child in gameObject.transform)
        {
            contentSizeFitter = child.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null) Destroy(contentSizeFitter);
        }
        foreach (RectTransform child in gameObject.transform)
        {
            // TODO
            print(child.name);
            print(child.anchoredPosition);
            // child.anchoredPosition = new Vector2(123 * 10, 0);
            print(child.anchoredPosition);
        }
    }
    
}