using System.Linq;
using UniRx.Async;
using UnityEngine;

public class LayoutFixer : MonoBehaviour, ScreenPostActiveListener
{

    public bool updateTransitionElementDefaultState;
    
    public void OnScreenPostActive()
    {
        Fix(transform, updateTransitionElementDefaultState);
    }

    public static async void Fix(Transform transform, bool updateTransitionElementDefaultState = false, int count = 4)
    {
        var children = transform.GetComponentsInChildren<LayoutFixer>().ToList();
        for (var i = 1; i < count; i++)
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
        if (updateTransitionElementDefaultState) transform.GetComponent<TransitionElement>()?.UseCurrentStateAsDefault();
    }

}