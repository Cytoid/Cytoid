using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private HashSet<ScreenChangeListener> screenChangeListeners = new HashSet<ScreenChangeListener>();

    protected override void Awake()
    {
        base.Awake();
        Context.ScreenManager = this;
    }

    private async void Start()
    {
        createdScreens.ForEach(it => it.gameObject.SetActive(false));
        
        await Context.LevelManager.ReloadLocalLevels();
        ChangeScreen(initialScreenId, ScreenTransition.Fade, 0.2f, 0);
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

    public async void ChangeScreen(string targetScreenId, ScreenTransition transition, float duration = 0.4f, float currentScreenTransitionDelay = 0f, float newScreenTransitionDelay = 0f,
        Vector2? transitionFocus = null, Action<Screen> onFinished = null)
    {
        if (ActiveScreen != null && targetScreenId == ActiveScreen.GetId())
        {
            print($"Warning: Attempted to change to the same screen");
            return;
        }
        print($"Changing screen to {targetScreenId}");
        History.Add(targetScreenId);

        DOTween.defaultEaseType = Ease.OutCubic;

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
            var lastScreenCanvasGroup = lastScreen.GetComponent<CanvasGroup>();
            var lastScreenRectTransform = lastScreen.GetComponent<RectTransform>();
            
            lastScreen.State = ScreenState.Inactive;

            if (currentScreenTransitionDelay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(currentScreenTransitionDelay));

            if (transition != ScreenTransition.None)
            {
                lastScreenCanvasGroup.blocksRaycasts = false;
                lastScreenCanvasGroup.DOFade(0, duration);

                switch (transition)
                {
                    case ScreenTransition.In:
                        if (transitionFocus.HasValue && transitionFocus != Vector2.zero)
                        {
                            var difference =
                                new Vector2(Context.ReferenceWidth / 2f, Context.ReferenceHeight / 2f) -
                                transitionFocus.Value;
                            lastScreenRectTransform.DOLocalMove(difference * 2f, duration);
                        }

                        lastScreenRectTransform.DOScale(2f, duration);
                        break;
                    case ScreenTransition.Out:
                        lastScreenRectTransform.DOScale(0.5f, duration);
                        break;
                    case ScreenTransition.Left:
                        lastScreenRectTransform.DOLocalMove(new Vector3(Context.ReferenceWidth, 0), duration);
                        break;
                    case ScreenTransition.Right:
                        lastScreenRectTransform.DOLocalMove(new Vector3(-Context.ReferenceWidth, 0), duration);
                        break;
                    case ScreenTransition.Up:
                        lastScreenRectTransform.DOLocalMove(new Vector3(0, -Context.ReferenceHeight), duration);
                        break;
                    case ScreenTransition.Down:
                        lastScreenRectTransform.DOLocalMove(new Vector3(0, Context.ReferenceHeight), duration);
                        break;
                    case ScreenTransition.Fade:
                        break;
                }
            }
            else
            {
                lastScreenCanvasGroup.alpha = 1f;
                lastScreenRectTransform.localPosition = Vector3.zero;
                lastScreenRectTransform.localScale = Vector3.one;
            }

            foreach (var listener in screenChangeListeners) listener.OnScreenChangeStarted(lastScreen, newScreen);
        }
        
        var newScreenCanvasGroup = newScreen.GetComponent<CanvasGroup>();
        var newScreenRectTransform = newScreen.GetComponent<RectTransform>();

        activeScreenId = newScreen.GetId();
        newScreen.State = ScreenState.Active;

        if (newScreenTransitionDelay > 0)
            await UniTask.Delay(TimeSpan.FromSeconds(newScreenTransitionDelay));
        
        if (transition != ScreenTransition.None)
        {
            newScreenCanvasGroup.alpha = 0f;
            newScreenCanvasGroup.blocksRaycasts = true;
            newScreenCanvasGroup.DOFade(1f, duration);
            newScreenRectTransform.DOLocalMove(Vector3.zero, duration);
            switch (transition)
            {
                case ScreenTransition.In:
                    newScreenRectTransform.localScale = new Vector3(0.5f, 0.5f);
                    newScreenRectTransform.DOScale(1f, duration);
                    break;
                case ScreenTransition.Out:
                    newScreenRectTransform.localScale = new Vector3(2, 2);
                    newScreenRectTransform.DOScale(1f, duration);
                    break;
                case ScreenTransition.Left:
                    newScreenRectTransform.localPosition = new Vector3(-Context.ReferenceWidth, 0);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Right:
                    newScreenRectTransform.localPosition = new Vector3(Context.ReferenceWidth, 0);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Up:
                    newScreenRectTransform.localPosition = new Vector3(0, Context.ReferenceHeight);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Down:
                    newScreenRectTransform.localPosition = new Vector3(0, -Context.ReferenceHeight);
                    newScreenRectTransform.localScale = new Vector3(1, 1);
                    break;
                case ScreenTransition.Fade:
                    break;
            }
        }
        else
        {
            newScreenCanvasGroup.alpha = 1f;
            newScreenRectTransform.localPosition = Vector3.zero;
            newScreenRectTransform.localScale = Vector3.one;
        }

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