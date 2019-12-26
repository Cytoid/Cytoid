using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using Object = UnityEngine.Object;

public class ScreenManager : SingletonMonoBehavior<ScreenManager>
{
    public Canvas rootCanvas;
    
    public List<Screen> screenPrefabs;
    public string initialScreenId;

    public List<Screen> createdScreens;
    
    public Screen ActiveScreen => createdScreens.Find(it => it.GetId() == activeScreenId);
    public List<string> History { get; } = new List<string>();

    private string activeScreenId;
    private string changingToScreenId;
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

    public string GetLastScreenId()
    {
        return History.Count > 1 ? History[History.Count - 2] : null;
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
        Action<Screen> onFinished = null
    )
    {
        if (ActiveScreen != null && targetScreenId == ActiveScreen.GetId())
        {
            print($"Warning: Attempted to change to the same screen");
            return;
        }
        if (changingToScreenId == targetScreenId) return;
        changingToScreenId = targetScreenId;
        print($"Changing screen to {targetScreenId}");
        History.Add(targetScreenId);

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
        
        activeScreenId = newScreen.GetId();
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

        changingToScreenId = null;

        Run.After(duration, () =>
        {
            onFinished?.Invoke(newScreen);
            foreach (var listener in screenChangeListeners) if (lastScreen != null) listener.OnScreenChangeFinished(lastScreen, newScreen);
        });
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