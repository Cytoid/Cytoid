using System.Collections;
using DG.Tweening;
using UniRx.Async;
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

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        overlay.gameObject.SetActive(true);
        goBackButton.onPointerClick.AddListener(_ =>
        {
            game.Abort();
            overlay.DOFade(1, 0.8f);
        });
        retryButton.onPointerClick.AddListener(_ =>
        {
            game.Retry();
            overlay.DOFade(1, 0.8f);
        });
        continueButton.onPointerClick.AddListener(_ =>
        {
            game.WillUnpause();
        });
    }
    
}