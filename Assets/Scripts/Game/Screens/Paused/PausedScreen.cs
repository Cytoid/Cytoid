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
            StartCoroutine(LoadCoroutine("Navigation"));
            
            overlay.DOFade(1, 0.8f);
            Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
                onFinished: async screen =>
                {
                    if (loadOperation == null) await UniTask.WaitUntil(() => loadOperation != null);
                    loadOperation.allowSceneActivation = true;
                });
        });
        retryButton.onPointerClick.AddListener(_ =>
        {
            StartCoroutine(LoadCoroutine("Game"));
            
            overlay.DOFade(1, 0.8f);
            Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
                onFinished: async screen =>
                {
                    if (loadOperation == null) await UniTask.WaitUntil(() => loadOperation != null);
                    loadOperation.allowSceneActivation = true;
                });
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