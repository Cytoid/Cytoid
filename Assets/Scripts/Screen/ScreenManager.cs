using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;

public class ScreenManager : SingletonMonoBehavior<ScreenManager>
{
    public Canvas rootCanvas;

    public List<Screen> screenPrefabs;
    public string initialScreenId;

    public List<Screen> createdScreens;

    public Screen ActiveScreen => createdScreens.Find(it => it.GetId() == ActiveScreenId);
    public Stack<string> History { get; set; } = new Stack<string>();

    public string ActiveScreenId { get; protected set; }
    public string ChangingToScreenId { get; protected set; }
    private HashSet<ScreenChangeListener> screenChangeListeners = new HashSet<ScreenChangeListener>();
    private CancellationTokenSource screenChangeCancellationTokenSource;

    protected override void Awake()
    {
        base.Awake();
        Context.ScreenManager = this;
    }

    private void Start()
    {
        createdScreens.ForEach(it => it.gameObject.SetActive(false));
        if (!string.IsNullOrEmpty(initialScreenId))
        {
            ChangeScreen(initialScreenId, ScreenTransition.None);
        }
    }

    public Screen GetScreen(string id)
    {
        return createdScreens.Find(it => it.GetId() == id);
    }

    public T GetScreen<T>() where T : Screen
    {
        return (T) createdScreens.Find(it => it.GetType() == typeof(T));
    }

    public string PopHistoryAndPeek()
    {
        return History.Count > 1 ? History.Also(it => it.Pop()).Peek() : null;
    }

    public Screen CreateScreen(string id)
    {
        var prefab = screenPrefabs.Find(it => it.GetId() == id);
        if (prefab == null)
        {
            throw new ArgumentException($"Screen {id} does not exist");
        }

        var gameObject = Instantiate(prefab.gameObject, rootCanvas.transform);

        var newScreen = gameObject.GetComponent<Screen>();
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        screenChangeCancellationTokenSource?.Cancel();
    }

    public async void ChangeScreen(
        string targetScreenId,
        ScreenTransition transition,
        float duration = 0.4f,
        float currentScreenTransitionDelay = 0f,
        float newScreenTransitionDelay = 0f,
        Vector2? transitionFocus = null,
        Action<Screen> onFinished = null,
        bool willDestroy = false,
        bool addToHistory = true
    )
    {
        if (ChangingToScreenId != null)
        {
            print($"Warning: Already changing to {ChangingToScreenId}! Ignoring.");
            return;
        }

        if (ActiveScreen != null && targetScreenId == ActiveScreen.GetId())
        {
            print("Warning: Attempted to change to the same screen! Ignoring.");
            return;
        }

        if (ChangingToScreenId == targetScreenId) return;
        ChangingToScreenId = targetScreenId;
        print($"Changing screen to {targetScreenId}");

        if (transition == ScreenTransition.None)
        {
            duration = 0;
        }

        var lastScreen = ActiveScreen;
        var newScreen = createdScreens.Find(it => it.GetId() == targetScreenId);

        if (newScreen == null) newScreen = CreateScreen(targetScreenId);
        else newScreen.gameObject.SetActive(true);

        if (lastScreen != null)
        {
            lastScreen.State = ScreenState.Inactive;
            if (currentScreenTransitionDelay > 0)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(currentScreenTransitionDelay),
                        cancellationToken: (screenChangeCancellationTokenSource = new CancellationTokenSource()).Token);
                }
                catch
                {
                    ChangingToScreenId = null;
                    return;
                }
            }

            lastScreen.CanvasGroup.DOFade(0, duration);

            if (transition != ScreenTransition.None)
            {
                switch (transition)
                {
                    case ScreenTransition.In:
                        if (transitionFocus.HasValue && transitionFocus != Vector2.zero)
                        {
                            var difference =
                                new Vector2(Context.ReferenceWidth / 2f, Context.ReferenceHeight / 2f) -
                                transitionFocus.Value;
                            lastScreen.RectTransform.DOLocalMove(difference * 2f, duration);
                        }

                        lastScreen.RectTransform.DOScale(2f, duration);
                        break;
                    case ScreenTransition.Out:
                        lastScreen.RectTransform.DOScale(0.5f, duration);
                        break;
                    case ScreenTransition.Left:
                        lastScreen.RectTransform.DOLocalMove(new Vector3(Context.ReferenceWidth, 0), duration);
                        break;
                    case ScreenTransition.Right:
                        lastScreen.RectTransform.DOLocalMove(new Vector3(-Context.ReferenceWidth, 0), duration);
                        break;
                    case ScreenTransition.Up:
                        lastScreen.RectTransform.DOLocalMove(new Vector3(0, -Context.ReferenceHeight), duration);
                        break;
                    case ScreenTransition.Down:
                        lastScreen.RectTransform.DOLocalMove(new Vector3(0, Context.ReferenceHeight), duration);
                        break;
                    case ScreenTransition.Fade:
                        break;
                }
            }
        }

        foreach (var listener in screenChangeListeners) listener.OnScreenChangeStarted(lastScreen, newScreen);

        ActiveScreenId = newScreen.GetId();
        newScreen.State = ScreenState.Active;
        newScreen.CanvasGroup.blocksRaycasts = false; // Special handling

        if (newScreenTransitionDelay > 0)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(newScreenTransitionDelay),
                    cancellationToken: (screenChangeCancellationTokenSource = new CancellationTokenSource()).Token);
            }
            catch
            {
                ChangingToScreenId = null;
                return;
            }
        }

        newScreen.CanvasGroup.blocksRaycasts = true; // Special handling
        newScreen.CanvasGroup.alpha = 0f;
        newScreen.CanvasGroup.DOFade(1f, duration);
        newScreen.RectTransform.DOLocalMove(Vector3.zero, duration);

        if (transition != ScreenTransition.None)
        {
            switch (transition)
            {
                case ScreenTransition.In:
                    newScreen.RectTransform.localScale = new Vector3(0.5f, 0.5f);
                    newScreen.RectTransform.DOScale(1f, duration);
                    break;
                case ScreenTransition.Out:
                    newScreen.RectTransform.localScale = new Vector3(2, 2);
                    newScreen.RectTransform.DOScale(1f, duration);
                    break;
                case ScreenTransition.Left:
                    newScreen.RectTransform.localPosition = new Vector3(-Context.ReferenceWidth, 0);
                    newScreen.RectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Right:
                    newScreen.RectTransform.localPosition = new Vector3(Context.ReferenceWidth, 0);
                    newScreen.RectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Up:
                    newScreen.RectTransform.localPosition = new Vector3(0, Context.ReferenceHeight);
                    newScreen.RectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Down:
                    newScreen.RectTransform.localPosition = new Vector3(0, -Context.ReferenceHeight);
                    newScreen.RectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Fade:
                    break;
            }
        }

        Action action = () =>
        {
            ChangingToScreenId = null;
            
            if (lastScreen != null)
            {
                lastScreen.gameObject.SetActive(false);
                if (willDestroy) DestroyScreen(lastScreen.GetId());
            }

            onFinished?.Invoke(newScreen);
            if (lastScreen != null)
                foreach (var listener in screenChangeListeners)
                    listener.OnScreenChangeFinished(lastScreen, newScreen);
        };
        if (duration > 0) Run.After(duration, action);
        else action();
        
        if (addToHistory)
        {
            print($"Adding {newScreen.GetId()} to history");
            History.Push(newScreen.GetId());
        }
    }

    public void AddHandler(ScreenChangeListener listener)
    {
        screenChangeListeners.Add(listener);
    }

    public void RemoveHandler(ScreenChangeListener listener)
    {
        screenChangeListeners.Remove(listener);
    }
}