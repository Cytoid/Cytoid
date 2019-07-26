using System;
using System.Linq.Expressions;
using DG.Tweening;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TransitionElement : MonoBehaviour, ScreenEventListener
{
    [GetComponent] [HideInInspector] public RectTransform rectTransform;
    [GetComponent] [HideInInspector] public CanvasGroup canvasGroup;
    
    public Transition enterFrom = Transition.Default;
    public float enterMultiplier = 1;
    public float enterDuration = 0.2f;
    public Ease enterEase = Ease.OutCubic;
    public float enterDelay;
    
    public Transition leaveTo = Transition.Default;
    public float leaveMultiplier = 1;
    public float leaveDuration = 0.2f;
    public Ease leaveEase = Ease.OutCubic;
    public float leaveDelay;

    public bool hiddenOnStart = true;
    public bool enterOnScreenBecomeActive = true;
    public float enterOnScreenBecomeActiveDelay;
    public bool leaveOnScreenBecomeInactive = true;
    public float leaveOnScreenBecomeInactiveDelay;
    
    public bool IsShown { get; protected set; }
    public bool IsInTransition { get; protected set; }

    private Vector3 defaultAnchoredPosition;
    private Vector3 defaultScale;
    private Vector3 defaultAnchorMax;
    private Vector3 defaultAnchorMin;
    private Vector3 defaultPivot;

    private async void Start()
    {
        UseCurrentStateAsDefault();

        if (hiddenOnStart)
        {
            canvasGroup.alpha = 0;
        }
    }

    public void UseCurrentStateAsDefault()
    {
        defaultAnchoredPosition = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
        defaultAnchorMax = rectTransform.anchorMax;
        defaultAnchorMin = rectTransform.anchorMin;
        defaultPivot = rectTransform.pivot;
    }

    public void Enter(bool waitForTransition = true, bool immediate = false)
    {
        StartTransition(true, enterFrom, enterMultiplier, enterDuration, enterDelay, enterEase, waitForTransition, immediate);
    }

    public void Leave(bool waitForTransition = true, bool immediate = false)
    {
        StartTransition(false, leaveTo, leaveMultiplier, leaveDuration, leaveDelay, leaveEase, waitForTransition, immediate);
    }

    public async void StartTransition(
        bool toShow,
        Transition transition,
        float multiplier,
        float duration,
        float delay,
        Ease ease,
        bool waitForTransition,
        bool immediate
    )
    {
        if (!immediate && IsInTransition)
        {
            if (waitForTransition)
            {
                await UniTask.WaitUntil(() => !IsInTransition);
            }
            else
            {
                rectTransform.DOComplete();
                canvasGroup.DOComplete();
            }
        }

        if (immediate)
        {
            duration = 0;
            delay = 0;
        }
        
        IsInTransition = true;

        IsShown = toShow;

        if (delay > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
        }

        RevertToDefault(false);

        canvasGroup.alpha = toShow ? 0 : 1;
        canvasGroup.DOFade(toShow ? 1 : 0, duration);

        if (toShow)
        {
            switch (transition)
            {
                case Transition.Top:
                    rectTransform.ShiftPivot(new Vector2(0.5f, 0.5f));
                    rectTransform.localScale = defaultScale * (2f * multiplier);
                    rectTransform.DOScale(defaultScale, duration).SetEase(ease);
                    break;
                case Transition.Bottom:
                    rectTransform.ShiftPivot(new Vector2(0.5f, 0.5f));
                    rectTransform.localScale = defaultScale * (0.5f / multiplier);
                    rectTransform.DOScale(defaultScale, duration).SetEase(ease);
                    break;
                case Transition.Left:
                    rectTransform.anchoredPosition =
                        defaultAnchoredPosition.DeltaX(-rectTransform.rect.width * (0.5 * multiplier));
                    rectTransform.DOAnchorPos(defaultAnchoredPosition, duration).SetEase(ease);
                    break;
                case Transition.Right:
                    rectTransform.anchoredPosition =
                        defaultAnchoredPosition.DeltaX(rectTransform.rect.width * (0.5 * multiplier));
                    rectTransform.DOAnchorPos(defaultAnchoredPosition, duration).SetEase(ease);
                    break;
                case Transition.Up:
                    rectTransform.anchoredPosition =
                        defaultAnchoredPosition.DeltaY(rectTransform.rect.height * (0.5 * multiplier));
                    rectTransform.DOAnchorPos(defaultAnchoredPosition, duration).SetEase(ease);
                    break;
                case Transition.Down:
                    rectTransform.anchoredPosition =
                        defaultAnchoredPosition.DeltaY(-rectTransform.rect.height * (0.5 * multiplier));
                    rectTransform.DOAnchorPos(defaultAnchoredPosition, duration).SetEase(ease);
                    break;
            }
        }
        else
        {
            switch (transition)
            {
                case Transition.Top:
                    rectTransform.ShiftPivot(new Vector2(0.5f, 0.5f));
                    rectTransform.DOScale(defaultScale * (2f * multiplier), duration).SetEase(ease);
                    break;
                case Transition.Bottom:
                    rectTransform.ShiftPivot(new Vector2(0.5f, 0.5f));
                    rectTransform.DOScale(defaultScale * (0.5f / multiplier), duration).SetEase(ease);
                    break;
                case Transition.Left:
                    rectTransform.DOAnchorPos(defaultAnchoredPosition.DeltaX(-rectTransform.rect.width * (0.5 * multiplier)), duration).SetEase(ease);
                    break;
                case Transition.Right:
                    rectTransform.DOAnchorPos(defaultAnchoredPosition.DeltaX(rectTransform.rect.width * (0.5 * multiplier)), duration).SetEase(ease);
                    break;
                case Transition.Up:
                    rectTransform.DOAnchorPos(defaultAnchoredPosition.DeltaY(rectTransform.rect.height * (0.5 * multiplier)), duration).SetEase(ease);
                    break;
                case Transition.Down:
                    rectTransform.DOAnchorPos(defaultAnchoredPosition.DeltaY(-rectTransform.rect.height * (0.5 * multiplier)), duration).SetEase(ease);
                    break;
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        IsInTransition = false;
    }

    public void RevertToDefault(bool killTween = true)
    {
        if (killTween) rectTransform.DOKill();
        rectTransform.anchorMax = defaultAnchorMax;
        rectTransform.anchorMin = defaultAnchorMin;
        rectTransform.pivot = defaultPivot;
        rectTransform.anchoredPosition = defaultAnchoredPosition;
        rectTransform.localScale = defaultScale;
    }

    public void OnScreenInitialized() => Expression.Empty();

    public async void OnScreenBecomeActive()
    {
        if (enterOnScreenBecomeActive)
        {
            if (enterOnScreenBecomeActiveDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(enterOnScreenBecomeActiveDelay));
            }
            Enter();
        }
    }

    public void OnScreenUpdate() => Expression.Empty();

    public async void OnScreenBecomeInactive()
    {
        if (leaveOnScreenBecomeInactive)
        {
            if (leaveOnScreenBecomeInactiveDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(leaveOnScreenBecomeInactiveDelay));
            }
            Leave();
        }
    }

    public void OnScreenDestroyed() => Expression.Empty();
}

[CustomEditor(typeof(TransitionElement))]
public class TransitionElementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var component = (TransitionElement) target;
        
        if (GUILayout.Button("Enter"))
        {
            component.Enter();
        }

        if (GUILayout.Button("Leave"))
        {
            component.Leave();
        }
    }

}