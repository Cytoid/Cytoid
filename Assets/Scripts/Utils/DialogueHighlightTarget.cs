using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

public class DialogueHighlightTarget : MonoBehaviour
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public InteractableMonoBehavior interactableMonoBehavior;
    public string id;

    private bool highlighted;
    private bool defaultOverrideSorting;
    private int defaultSortingOrder;
    private bool defaultInteractable;
    private bool defaultBlocksRaycasts;
    private bool defaultIgnoreParentGroups;

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            if (highlighted == value) return;
            highlighted = value;
            UpdateHighlight();
        }
    }

    private void UpdateHighlight()
    {
        if (highlighted)
        {
            defaultOverrideSorting = canvas.overrideSorting;
            defaultSortingOrder = canvas.sortingOrder;
            defaultInteractable = canvasGroup.interactable;
            defaultBlocksRaycasts = canvasGroup.blocksRaycasts;
            defaultIgnoreParentGroups = canvasGroup.ignoreParentGroups;
            
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = true;
        }
        else
        {
            canvas.overrideSorting = defaultOverrideSorting;
            canvas.sortingOrder = defaultSortingOrder;
            canvasGroup.interactable = defaultInteractable;
            canvasGroup.blocksRaycasts = defaultBlocksRaycasts;
            canvasGroup.ignoreParentGroups = defaultIgnoreParentGroups;
        }
    }

    public async UniTask WaitForOnClick()
    {
        if (!highlighted || interactableMonoBehavior == null) throw new ArgumentException();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (interactableMonoBehavior is NavigationElement navigationElement && navigationElement.navigateToHomeScreenWhenLongPress)
        {
            // Workaround
            navigationElement.navigateToHomeScreenWhenLongPress = false;
            await interactableMonoBehavior.onPointerClick.OnInvokeAsync(CancellationToken.None);
            navigationElement.navigateToHomeScreenWhenLongPress = true;
        }
        else
        {
            await interactableMonoBehavior.onPointerClick.OnInvokeAsync(CancellationToken.None);
        }
    }

    public static async UniTask<DialogueHighlightTarget> Find(string id)
    {
        if (id.StartsWith("@"))
        {
            var lookup = id.Substring(1);
            switch (lookup)
            {
                case "LevelSelectionScreen/HighlightedLevelCard":
                    await UniTask.WaitUntil(() => Context.ScreenManager.ActiveScreen is LevelSelectionScreen);
                    await UniTask.DelayFrame(5);
                    var card = ((LevelSelectionScreen) Context.ScreenManager.ActiveScreen).scrollRect.content
                        .GetComponentsInChildren<LevelCard>()
                        .FirstOrDefault(it => it.Level.Id == LevelSelectionScreen.HighlightedLevelId);
                    if (card == null)
                    {
                        Debug.LogWarning($"Could not find LevelCard with ID {LevelSelectionScreen.HighlightedLevelId}");
                        return null;
                    }
                    return card.gameObject.GetComponent<DialogueHighlightTarget>();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return FindObjectsOfType<DialogueHighlightTarget>().ToList().Find(it => it.id == id);
    }

}