using System;
using System.Threading;
using UniRx.Async;
using UnityEditor;
using UnityEngine;

public class AwaitableAnimatedElement : InteractableMonoBehavior
{
    [GetComponent] public Animator animator;
    [GetComponent] public CanvasGroup canvasGroup;
    public bool entryOnScreenBecameActive;
    public bool exitOnScreenBecameInactive;
    
    public string entryState = "Entry";
    public string exitState = "Exit";
    public string finalState = "Exit";

    protected virtual void Awake()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        var screen = this.GetScreenParent();
        if (screen != null)
        {
            screen.onScreenBecameActive.AddListener(OnScreenBecameActive);
            screen.onScreenBecameInactive.AddListener(OnScreenBecameInactive);
        }
    }

    public async void OnScreenBecameActive()
    {
        if (entryOnScreenBecameActive) await Animate();
    }
    
    public async void OnScreenBecameInactive()
    {
        if (exitOnScreenBecameInactive) await Animate(false);
    }

    private CancellationTokenSource animateCancelSource;

    public async UniTask Animate(bool entry = true)
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        animator.Play(entry ? entryState : exitState);
        
        animateCancelSource?.Cancel();
        animateCancelSource = new CancellationTokenSource();
        try
        {
            await UniTask.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0)
                    .Let(it => it.IsName(entry ? entryState : exitState)));
            await UniTask.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0)
                    .Let(it => it.IsName(finalState) && it.normalizedTime >= 1),
                cancellationToken: animateCancelSource.Token);
        }
        catch
        {
            return;
        }
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AwaitableAnimatedElement), true)]
public class AwaitableAnimatedElementEditor : Editor
{
    public override async void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var component = (AwaitableAnimatedElement) target;

        if (GUILayout.Button("Animate"))
        {
            await component.Animate();
        }
    }
}

#endif