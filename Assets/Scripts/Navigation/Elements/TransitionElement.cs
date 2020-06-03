using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TransitionElement : MonoBehaviour, ScreenListener, ScreenPostActiveListener
{
    [HideInInspector] public UnityEvent onEnterStarted = new UnityEvent();
    [HideInInspector] public UnityEvent onEnterCompleted = new UnityEvent();
    [HideInInspector] public UnityEvent onLeaveStarted = new UnityEvent();
    [HideInInspector] public UnityEvent onLeaveCompleted = new UnityEvent();

    [GetComponent] public RectTransform rectTransform;
    [GetComponent] public CanvasGroup canvasGroup;

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

    public bool useEditorStateAsDefault = false;
    public bool disableRaycasts = false;
    public bool actOnOtherGameObjects = false;
    public bool printDebugInfo = false;

    public bool IsShown { get; protected set; }
    public bool IsInTransition { get; protected set; }
    public bool IsEntering { get; protected set; }

    private bool specifiedDefault;
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
        if (useEditorStateAsDefault)
        {
            UseCurrentStateAsDefault();
        }
    }

    protected void Start()
    {
        if (!actOnOtherGameObjects && (rectTransform.gameObject != gameObject || canvasGroup.gameObject != gameObject))
        {
            Debug.LogError($"WARNING! TransitionElement {name} rectTransform and canvasGroup not set to self. (rectTransform: {rectTransform.gameObject.name}, canvasGroup: {canvasGroup.gameObject.name})");
        }
    }

    public void UseCurrentStateAsDefault()
    {
        specifiedDefault = true;
        defaultAnchoredPosition = rectTransform.anchoredPosition;
        defaultScale = rectTransform.localScale;
        defaultAnchorMax = rectTransform.anchorMax;
        defaultAnchorMin = rectTransform.anchorMin;
        defaultPivot = rectTransform.pivot;
        defaultSizeDelta = rectTransform.sizeDelta;
    }

    public void Enter(bool waitForTransition = true, bool immediate = false, Action onComplete = null)
    {
        StartTransition(true, enterFrom, enterMultiplier, enterDuration, enterDelay, enterEase, waitForTransition,
            immediate, onComplete);
    }

    public void Leave(bool waitForTransition = true, bool immediate = false, Action onComplete = null)
    {
        StartTransition(false, leaveTo, leaveMultiplier, leaveDuration, leaveDelay, leaveEase, waitForTransition,
            immediate, onComplete);
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
        bool immediate,
        Action onComplete = null
    )
    {
        if (!specifiedDefault)
        {
            Debug.LogWarning(gameObject.name + ": Not specified default for TransitionElement!");
        }
        if (printDebugInfo)
        {
            print(gameObject.name + $" StartTransition(toShow: {toShow}, transition: {transition}, multiplier: {multiplier}, duration: {duration}, delay: {delay}, ease: {ease}," +
                  $"waitForTransition: {waitForTransition}, immediate: {immediate})");
            print(StackTraceUtility.ExtractStackTrace());
        }
        if (toShow == IsShown)
        {
            onComplete?.Invoke();
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
            if (toShow == IsEntering) return; // Cancel same operation
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
        IsEntering = toShow;

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

        if (this == null) return;
        RevertToDefault();

        canvasGroup.alpha = toShow ? 0 : 1;
        canvasGroup.blocksRaycasts = toShow && !disableRaycasts;
        if (duration > 0)
        {
            canvasGroup.DOFade(toShow ? 1 : 0, duration);
        }
        else
        {
            canvasGroup.alpha = toShow ? 1 : 0;
        }

        if (toShow)
        {
            onEnterStarted.Invoke();
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
            onLeaveStarted.Invoke();
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
        onComplete?.Invoke();
        if (toShow) onEnterCompleted.Invoke();
        else onLeaveCompleted.Invoke();
    }

    public void RevertToDefault(bool killTween = true)
    {
#if UNITY_EDITOR
        if (rectTransform == null)
        {
            Selection.activeGameObject = gameObject;
            throw new Exception($"{gameObject.name}: rectTransform not assigned");
        }
#endif
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

    public void OnScreenBecameActive()
    {
        // Do not manipulate any layout. Others are changing the layout groups at this time.
    }

    public async void OnScreenPostActive()
    {
        await UniTask.DelayFrame(0); // Ensure this gets executed the last
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
        if (Application.isPlaying)
        {
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
}

#endif