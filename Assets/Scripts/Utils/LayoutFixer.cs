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

}