using System;
using System.Linq;
using UniRx.Async;
using UnityEngine;

public class LayoutFixer : MonoBehaviour
{
    private void Start()
    {
        Fix(transform);
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