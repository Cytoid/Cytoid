using System;
using System.Collections;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PausedScreen : Screen
{
    public const string Id = "Paused";

    public Game game;

    public CanvasGroup overlay;
    public InteractableMonoBehavior goBackButton;
    public InteractableMonoBehavior retryButton;
    public InteractableMonoBehavior continueButton;

    private Action action;
    private bool invokedAction;

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        overlay.gameObject.SetActive(true);
        goBackButton.onPointerClick.AddListener(_ =>
        {
            action = Action.Abort;
            OnAction();
        });
        retryButton.onPointerClick.AddListener(_ =>
        {
            action = Action.Retry;
            OnAction();
        });
        continueButton.onPointerClick.AddListener(_ =>
        {
            action = Action.Resume;
            OnAction();
        });
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        invokedAction = false;
    }

    private void OnAction()
    {
        if (invokedAction) return;
        invokedAction = true;
        switch (action)
        {
            case Action.Abort:
                game.Abort();
                overlay.DOFade(1, 0.8f);
                break;
            case Action.Retry:
                game.Retry();
                overlay.DOFade(1, 0.8f);
                break;
            case Action.Resume:
                game.WillUnpause();
                break;
        }
    }

    private enum Action
    {
        Abort, Retry, Resume
    }
}