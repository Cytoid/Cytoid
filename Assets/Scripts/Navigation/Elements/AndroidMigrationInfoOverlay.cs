using System;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using Polyglot;
using UnityEngine;
using UnityEngine.UI;

public class AndroidMigrationInfoOverlay : SingletonMonoBehavior<AndroidMigrationInfoOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;

    public ScrollRect scrollRect;
    public Text text;
    public InteractableMonoBehavior agreeButton;
    public ScheduledPulse scheduledPulse;
    
    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.TermsOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private static bool isTransitioning;
    
    public static async UniTask<bool> Show(float duration = 0.8f, Action onFullyShown = null, Action onFullyHidden = null)
    {
        if (isTransitioning)
        {
            await UniTask.WaitUntil(() => !isTransitioning);
        }
        isTransitioning = true;
        var hasResult = false;
        var result = false;
        var it = Instance;
        it.canvas.enabled = true;
        it.canvas.overrideSorting = true;
        it.canvas.sortingOrder = NavigationSortingOrder.TermsOverlay;
        it.canvasGroup.enabled = true;
        it.canvasGroup.blocksRaycasts = true;
        it.canvasGroup.interactable = true;
        LayoutFixer.Fix(it.agreeButton.transform.parent);
        await UniTask.DelayFrame(5);
        it.canvasGroup.DOKill();
        it.canvasGroup.DOFade(1, duration).SetEase(Ease.OutCubic);
        Context.SetMajorCanvasBlockRaycasts(false);
        it.text.text = "ANDROID_MIGRATION_INFO_TEXT".Get()
            .Replace("%OLD_DIR%", Context.Instance.GetAndroidLegacyStoragePath())
            .Replace("%NEW_DIR%", Context.Instance.GetAndroidStoragePath());
        if (((Language) Context.Player.Settings.Language).ShouldUseNonBreakingSpaces())
        {
            it.text.text = it.text.text.Replace(" ", "\u00A0");
        }
        
        it.scrollRect.verticalNormalizedPosition = 1;
        it.scheduledPulse.StartPulsing();
        it.agreeButton.onPointerClick.AddListener(_ =>
        {
            hasResult = true;
            result = true;
        });
        if (onFullyShown != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            onFullyShown();
        }
        await UniTask.WaitUntil(() => hasResult);
        isTransitioning = false;
        Hide(duration, onFullyHidden);
        return result;
    }

    private static async UniTask Hide(float duration = 0.8f, Action onFullyHidden = null)
    {
        if (isTransitioning)
        {
            await UniTask.WaitUntil(() => !isTransitioning);
        }
        isTransitioning = true;
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = false;
            it.canvasGroup.interactable = false;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(0, duration).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(true);
            it.scheduledPulse.StopPulsing();
        });
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        Instance.canvas.enabled = false;
        Instance.canvasGroup.enabled = false;
        if (onFullyHidden != null)
        {
            Instance.text.text = "";
            onFullyHidden();
        }
        isTransitioning = false;
    }
}