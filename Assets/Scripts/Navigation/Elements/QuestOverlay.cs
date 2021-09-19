using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class QuestOverlay : SingletonMonoBehavior<QuestOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public LayoutGroup layoutRoot;
    public QuestView questViewPrefab;
    public InteractableMonoBehavior closeButton;
    
    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.SpinnerOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        closeButton.onPointerClick.AddListener(_ => Hide());
    }

    public static async void Show(List<Quest> quests)
    {
        Instance.Apply(it =>
        {
            foreach (Transform child in Instance.layoutRoot.transform) Destroy(child.gameObject);
            quests.ForEach(quest => Instantiate(it.questViewPrefab, it.layoutRoot.transform).SetModel(quest));
            LayoutFixer.Fix(it.layoutRoot.transform);
            
            it.canvas.enabled = true;
            it.canvas.overrideSorting = true;
            it.canvas.sortingOrder = NavigationSortingOrder.SpinnerOverlay;
            it.canvasGroup.enabled = true;
            it.canvasGroup.blocksRaycasts = true;
            it.canvasGroup.interactable = true;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(false);
        });
    }

    public static async void Hide()
    {
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = false;
            it.canvasGroup.interactable = false;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(true);
        });
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        if (Instance == null || Instance.gameObject) return;
        Instance.canvas.enabled = false;
        Instance.canvasGroup.enabled = false;
        foreach (Transform child in Instance.layoutRoot.transform) Destroy(child.gameObject);
    }
}