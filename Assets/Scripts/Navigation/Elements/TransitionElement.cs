using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TransitionElement : MonoBehaviour, ScreenListener
{
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;

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
    private Vector3 defaultSizeDelta;

    private CancellationTokenSource waitingForTransition;
    private List<CancellationTokenSource> transitioning = new List<CancellationTokenSource>();

    protected void Awake()
    {
        if (hiddenOnStart)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void UseCurrentStateAsDefault()
    {
        defaultAnchoredPosition = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
        defaultAnchorMax = rectTransform.anchorMax;
        defaultAnchorMin = rectTransform.anchorMin;
        defaultPivot = rectTransform.pivot;
        defaultSizeDelta = rectTransform.sizeDelta;
    }

    public void Enter(bool waitForTransition = true, bool immediate = false)
    {
        StartTransition(true, enterFrom, enterMultiplier, enterDuration, enterDelay, enterEase, waitForTransition,
            immediate);
    }

    public void Leave(bool waitForTransition = true, bool immediate = false)
    {
        StartTransition(false, leaveTo, leaveMultiplier, leaveDuration, leaveDelay, leaveEase, waitForTransition,
            immediate);
    }

    protected void OnDestroy()
    {
        waitingForTransition?.Cancel();
        transitioning.ForEach(it => it.Cancel());
        transitioning.Clear();
        canvasGroup.DOKill();
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
        if (toShow == IsShown)
        {
            return;
        }
        IsShown = toShow;
        
        if (immediate)
        {
            waitForTransition = false;
            duration = 0;
            delay = 0;
        }
        
        waitingForTransition?.Cancel();
        waitingForTransition = new CancellationTokenSource();

        if (IsInTransition)
        {
            if (waitForTransition)
            {
                try
                {
                    await UniTask.WaitUntil(() => !IsInTransition, cancellationToken: waitingForTransition.Token);
                }
                catch
                {
                    return;
                }
            }
            else
            {
                transitioning.ForEach(it => it.Cancel());
                transitioning.Clear();
                canvasGroup.DOKill();
            }
        }
        
        var cancellationTokenSource = new CancellationTokenSource();
        transitioning.Add(cancellationTokenSource);

        IsInTransition = true;

        if (delay > 0)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationTokenSource.Token);
            }
            catch
            {
                // Cancelled
                IsInTransition = false;
                return;
            }
        }

        RevertToDefault();

        canvasGroup.alpha = toShow ? 0 : 1;
        canvasGroup.blocksRaycasts = toShow;
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
                    rectTransform
                        .DOAnchorPos(defaultAnchoredPosition.DeltaX(-rectTransform.rect.width * (0.5 * multiplier)),
                            duration).SetEase(ease);
                    break;
                case Transition.Right:
                    rectTransform
                        .DOAnchorPos(defaultAnchoredPosition.DeltaX(rectTransform.rect.width * (0.5 * multiplier)),
                            duration).SetEase(ease);
                    break;
                case Transition.Up:
                    rectTransform
                        .DOAnchorPos(defaultAnchoredPosition.DeltaY(rectTransform.rect.height * (0.5 * multiplier)),
                            duration).SetEase(ease);
                    break;
                case Transition.Down:
                    rectTransform
                        .DOAnchorPos(defaultAnchoredPosition.DeltaY(-rectTransform.rect.height * (0.5 * multiplier)),
                            duration).SetEase(ease);
                    break;
            }
        }

        if (duration > 0)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationTokenSource.Token);
            }
            catch
            {
                // Cancelled
                IsInTransition = false;
            }
        }
        IsInTransition = false;
    }

    public void RevertToDefault(bool killTween = true)
    {
        if (killTween)
        {
            rectTransform.DOKill();
            canvasGroup.DOKill();
        }
        rectTransform.anchorMax = defaultAnchorMax;
        rectTransform.anchorMin = defaultAnchorMin;
        rectTransform.pivot = defaultPivot;
        rectTransform.anchoredPosition = defaultAnchoredPosition;
        rectTransform.localScale = defaultScale;
        rectTransform.sizeDelta = defaultSizeDelta;
        canvasGroup.alpha = 0;
    }

    public void OnScreenInitialized()
    {
        UseCurrentStateAsDefault();
    }

    public async void OnScreenBecameActive()
    {
        if (enterOnScreenBecomeActive)
        {
            if (enterOnScreenBecomeActiveDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(enterOnScreenBecomeActiveDelay));
            }

            Enter(false);
        }
    }

    public void OnScreenUpdate() => Expression.Empty();

    public async void OnScreenBecameInactive()
    {
        if (leaveOnScreenBecomeInactive)
        {
            if (leaveOnScreenBecomeInactiveDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(leaveOnScreenBecomeInactiveDelay));
            }

            Leave(false);
        }
    }

    public void OnScreenDestroyed() => Expression.Empty();
}

#if UNITY_EDITOR

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

        if (GUILayout.Button("Use Current State As Default"))
        {
            component.UseCurrentStateAsDefault();
        }
    }
}

#endif