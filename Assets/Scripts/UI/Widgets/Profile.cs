using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class Profile : MonoBehaviour, ScreenChangeListener
{
    [GetComponent] private CanvasGroup canvasGroup;
    private Vector2 startLocalPosition;

    private async void Start()
    {
        Context.ScreenManager.AddHandler(this);
        startLocalPosition = transform.localPosition;

        await UniTask.WaitUntil(() => Context.ScreenManager.ActiveScreen != null);

        if (Context.ScreenManager.ActiveScreen.GetId() != MainMenuScreen.Id)
        {
            Shrink();
        }
    }

    private void OnDestroy()
    {
        Context.ScreenManager.RemoveHandler(this);
    }

    public void Enlarge()
    {
        transform.DOLocalMove(startLocalPosition, 0.4f);
        transform.DOScale(1f, 0.4f);
    }

    public void Shrink()
    {
        transform.DOLocalMove(startLocalPosition + new Vector2(12, 24), 0.4f);
        transform.DOScale(0.9f, 0.4f);
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from.GetId() == MainMenuScreen.Id)
        {
            Shrink();
        }
        else if (to.GetId() == MainMenuScreen.Id)
        {
            Enlarge();
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
    }
    
}