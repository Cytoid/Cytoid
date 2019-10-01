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
            var sceneLoader = new SceneLoader("Navigation");
            sceneLoader.Load();
            
            overlay.DOFade(1, 0.8f);
            Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
                onFinished: screen => sceneLoader.Activate());
        });
        retryButton.onPointerClick.AddListener(_ =>
        {
            var sceneLoader = new SceneLoader("Game");
            sceneLoader.Load();
            
            overlay.DOFade(1, 0.8f);
            Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
                onFinished: screen => sceneLoader.Activate());
        });
        continueButton.onPointerClick.AddListener(_ =>
        {
            Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1);
            game.WillUnpause();
        });
    }
    
    private AsyncOperation loadOperation;

    private IEnumerator LoadCoroutine(string sceneName)
    {
        loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false;
        yield return loadOperation;
    }
}