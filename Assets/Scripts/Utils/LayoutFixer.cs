using System.Linq;
using UniRx.Async;
using UnityEngine;

public class LayoutFixer : MonoBehaviour, ScreenPostActiveListener
{
    public void OnScreenPostActive()
    {
        Fix(transform);
    }

    public static async void Fix(Transform transform)
    {
        var children = transform.GetComponentsInChildren<LayoutFixer>().ToList();
        for (var i = 1; i < 5; i++)
        {
            if (transform == null) return;
            transform.RebuildLayout();
            foreach (var it in children)
            {
                if (it == null) continue;
                it.transform.RebuildLayout();
            }
            await UniTask.DelayFrame(0);
        }
        transform.GetComponent<TransitionElement>()?.UseCurrentStateAsDefault();
    }

}