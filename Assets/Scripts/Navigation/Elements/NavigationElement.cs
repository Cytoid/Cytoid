using System;
using System.Linq.Expressions;
using System.Threading;
using MoreMountains.NiceVibrations;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

public class NavigationElement : InteractableMonoBehavior, ScreenBecameActiveListener, ScreenBecameInactiveListener
{
    public bool navigateToLastScreen;
    public bool navigateToHomeScreenWhenLongPress = true;
    public string targetScreenId;
    public ScreenTransition transition;
    public float duration;
    public float currentScreenDelay;
    public float newScreenDelay;
    public Vector2 transitionFocus;
    public string soundName = "Navigate1";
    public bool addToHistory = true;
    public bool currentScreenNotAddedToHistory;
    
    private bool navigated;
    private CancellationTokenSource actionToken;
    private Vector2 pressPosition;
    private bool ignoreNextPointerUp;

    private void OnDestroy()
    {
        actionToken?.Cancel();
    }

    private void OnAction()
    {
        Context.AudioManager.Get(soundName).Play(ignoreDsp: true);
        Context.Haptic(HapticTypes.HeavyImpact, true);
        Context.ScreenManager.History.Clear();
        Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
    }

    public override async void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (!navigateToLastScreen || !navigateToHomeScreenWhenLongPress) return;
        pressPosition = eventData.position;
        actionToken?.Cancel();
        actionToken = new CancellationTokenSource();
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.8f), cancellationToken: actionToken.Token);
        }
        catch
        {
            // ignored
            return;
        }
        
        if (transform == null) return; // Transform destroyed?
        ignoreNextPointerUp = true;
        OnPointerUp(eventData);
        OnAction();
        actionToken = null;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!navigateToLastScreen || !navigateToHomeScreenWhenLongPress)
        {
            base.OnPointerUp(eventData);
            return;
        }
        
        var d = Vector2.Distance(pressPosition, eventData.position);
        if (d > 0.005f * Context.ReferenceWidth || ignoreNextPointerUp)
        {
            ignoreNextPointerUp = false;
            IsPointerDown = false;
        }

        actionToken?.Cancel();
        base.OnPointerUp(eventData);
    }

    public override async void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (Context.ScreenManager.ChangingToScreenId != null)
        {
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(1));
            try
            {
                await UniTask.WaitUntil(() => Context.ScreenManager.ChangingToScreenId == null,
                    cancellationToken: cancellationSource.Token);
            }
            catch
            {
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(soundName))
        {
            Context.AudioManager.Get(soundName).Play(ignoreDsp: true);
            switch (soundName)
            {
                case "Navigate1":
                    Context.Haptic(HapticTypes.MediumImpact, true);
                    break;
                case "Navigate2":
                    Context.Haptic(HapticTypes.LightImpact, true);
                    break;
                case "Navigate3":
                    Context.Haptic(HapticTypes.SoftImpact, true);
                    break;
            }
        }

        if (navigated) return;
        navigated = true;
        Context.ScreenManager.ChangeScreen(
            navigateToLastScreen ? (currentScreenNotAddedToHistory ? Context.ScreenManager.PeekHistory() : Context.ScreenManager.PopAndPeekHistory()) : new Intent(targetScreenId, null), transition,
            duration, currentScreenDelay, newScreenDelay, transitionFocus, OnScreenChanged, addTargetScreenToHistory: addToHistory && !navigateToLastScreen);

        if (Context.ScreenManager.History.Count >= 5 && Context.Player.ShouldOneShot("Tips: Main Menu Shortcut"))
        {
            Dialog.PromptAlert("DIALOG_TIPS_MAIN_MENU_SHORTCUT".Get());
        }
    }

    protected void OnScreenChanged(Screen screen) => Expression.Empty();
    
    public void OnScreenBecameActive()
    {
        navigated = false;
    }

    public void OnScreenBecameInactive()
    {
        actionToken?.Cancel();
    }
}