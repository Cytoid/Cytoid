using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

public class ScreenManager : SingletonMonoBehavior<ScreenManager>
{
    
    public Canvas rootCanvas;
    
    public List<Screen> screenPrefabs;
    public string initialScreenId;
    
    [HideInInspector] public List<Screen> createdScreens;
    public Screen ActiveScreen => createdScreens.Find(it => it.GetId() == activeScreenId);
    
    private string activeScreenId;

    private async void Start()
    {
        await Context.levelManager.ReloadLocalLevels();
        ChangeScreen(initialScreenId, TransitionAnimation.None);
    }

    public Screen GetScreen(string id)
    {
        return createdScreens.Find(it => it.GetId() == id);
    }

    public Screen CreateScreen(string id)
    {
        var newScreen = Instantiate(screenPrefabs.Find(it => it.GetId() == id).gameObject, rootCanvas.transform).GetComponent<Screen>();
        newScreen.gameObject.SetActive(true);
        createdScreens.Add(newScreen);
        return newScreen;
    }

    public void DestroyScreen(string id)
    {
        var screen = createdScreens.Find(it => it.GetId() == id);
        if (screen != null)
        {
            screen.State = ScreenState.Destroyed;
            Destroy(screen.gameObject);
        }
    }

    public void ChangeScreen(string targetScreenId, TransitionAnimation transition, Vector2? transitionFocus = null, Action<Screen> onFinished = null)
    {
        print($"Changing screen to {targetScreenId}");

        DOTween.defaultEaseType = Ease.OutExpo;

        var lastScreen = ActiveScreen;
        var newScreen = createdScreens.Find(it => it.GetId() == targetScreenId);

        if (newScreen == null)
        {
            newScreen = CreateScreen(targetScreenId);
        }

        if (lastScreen != null)
        {
            var lastScreenCanvasGroup = lastScreen.GetComponent<CanvasGroup>();
            var lastScreenRectTransform = lastScreen.GetComponent<RectTransform>();

            lastScreenCanvasGroup.blocksRaycasts = false;
            lastScreenCanvasGroup.DOFade(0, TransitionAnimationExtensions.GetDuration());
            
            switch (transition)
            {
                case TransitionAnimation.In:
                    if (transitionFocus.HasValue)
                    {
                        var difference =
                            new Vector2(Context.ReferenceWidth / 2f, Context.ReferenceHeight / 2f) - transitionFocus.Value;
                        lastScreenRectTransform.DOLocalMove(difference * 2f, TransitionAnimationExtensions.GetDuration());
                    }

                    lastScreenRectTransform.DOScale(2f, TransitionAnimationExtensions.GetDuration());
                    break;
                case TransitionAnimation.Out:
                    lastScreenRectTransform.DOScale(0.5f, TransitionAnimationExtensions.GetDuration());
                    break;
                case TransitionAnimation.Left:
                    lastScreenRectTransform.DOLocalMove(new Vector3(Context.ReferenceWidth, 0),
                        TransitionAnimationExtensions.GetDuration());
                    break;
                case TransitionAnimation.Right:
                    lastScreenRectTransform.DOLocalMove(new Vector3(-Context.ReferenceWidth, 0),
                        TransitionAnimationExtensions.GetDuration());
                    break;
                case TransitionAnimation.Up:
                    lastScreenRectTransform.DOLocalMove(new Vector3(0, -Context.ReferenceHeight),
                        TransitionAnimationExtensions.GetDuration());
                    break;
                case TransitionAnimation.Down:
                    lastScreenRectTransform.DOLocalMove(new Vector3(0, Context.ReferenceHeight),
                        TransitionAnimationExtensions.GetDuration());
                    break;
                case TransitionAnimation.Fade:
                    break;
            }

            if (onFinished != null)
            {
                Run.After(TransitionAnimationExtensions.GetDuration(),
                    () => { onFinished(newScreen); });
            }
        }

        newScreen.State = ScreenState.Active;
        activeScreenId = newScreen.GetId();
        
        var newScreenCanvasGroup = newScreen.GetComponent<CanvasGroup>();
        var newScreenRectTransform = newScreen.GetComponent<RectTransform>();

        if (transition != TransitionAnimation.None)
        {
            const float delay = 0.2f;

            newScreenCanvasGroup.alpha = 0f;
            newScreenCanvasGroup.blocksRaycasts = true;
            newScreenCanvasGroup.DOFade(1f, TransitionAnimationExtensions.GetDuration()).SetDelay(delay);
            newScreenRectTransform.DOLocalMove(Vector3.zero, TransitionAnimationExtensions.GetDuration()).SetDelay(delay);
            switch (transition)
            {
                case TransitionAnimation.In:
                    newScreenRectTransform.localScale = new Vector3(0.5f, 0.5f);
                    newScreenRectTransform.DOScale(1f, TransitionAnimationExtensions.GetDuration()).SetDelay(delay);
                    break;
                case TransitionAnimation.Out:
                    newScreenRectTransform.localScale = new Vector3(2, 2);
                    newScreenRectTransform.DOScale(1f, TransitionAnimationExtensions.GetDuration()).SetDelay(delay);
                    break;
                case TransitionAnimation.Left:
                    newScreenRectTransform.localPosition = new Vector3(-Context.ReferenceWidth, 0);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case TransitionAnimation.Right:
                    newScreenRectTransform.localPosition = new Vector3(Context.ReferenceWidth, 0);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case TransitionAnimation.Up:
                    newScreenRectTransform.localPosition = new Vector3(0, Context.ReferenceHeight);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case TransitionAnimation.Down:
                    newScreenRectTransform.localPosition = new Vector3(0, -Context.ReferenceHeight);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case TransitionAnimation.Fade:
                    break;
            }
        }
        else
        {
            newScreenCanvasGroup.alpha = 1f;
            newScreenRectTransform.localPosition = Vector3.zero;
            newScreenRectTransform.localScale = Vector3.one;
        }
    }
}

public enum TransitionAnimation
{
    In,
    Out,
    Left,
    Right,
    Up,
    Down,
    Fade,
    None
}

public static class TransitionAnimationExtensions
{
    private const float DefaultDuration = 0.8f;
    public static float GetDuration()
    {
        return DefaultDuration;
    }
}