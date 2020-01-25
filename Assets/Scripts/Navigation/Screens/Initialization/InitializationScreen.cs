using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class InitializationScreen : Screen
{
    public const string Id = "Initialization";
    
    public SpinnerElement spinnerElement;
    public Text statusText;
    public InteractableMonoBehavior detectionArea;

    public override string GetId() => Id;

    public override async void OnScreenBecameActive()
    {
        var image = TranslucentCover.Instance.image;
        image.color = Color.black;
        image.DOFade(0.5f, 2f);
        spinnerElement.IsSpinning = true;
        statusText.text = "Scanning levels";
        
        Context.LevelManager.OnLevelLoadProgress.AddListener(OnLevelLoadProgress);
        await Context.LevelManager.LoadAllFromDataPath();
        Context.LevelManager.OnLevelLoadProgress.RemoveListener(OnLevelLoadProgress);

        spinnerElement.gameObject.SetActive(false);
        statusText.text = "Touch to start";
        statusText.DOFade(0, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        detectionArea.onPointerDown.AddListener(_ =>
        {
            image.DOKill();
            image.DOFade(0f, 0.2f);
            Context.AudioManager.Get("LevelStart").Play();
            Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
        });
    }

    private void OnLevelLoadProgress(Level level, int current, int total)
    {
        statusText.text = $"Loaded {level.Id} ({current}/{total})";
        statusText.transform.RebuildLayout();
    }
    
}