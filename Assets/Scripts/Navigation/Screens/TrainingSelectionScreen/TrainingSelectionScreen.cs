using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrainingSelectionScreen : Screen
{
    private static float savedScrollPosition = -1;

    public const string Id = "TrainingSelection";

    public LoopVerticalScrollRect scrollRect;
    public RectTransform scrollRectPaddingReference;
    public CharacterDisplay characterDisplay;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            RefillLevels(true);
        });
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        RefillLevels();
        characterDisplay.Load("KaedeTachie");
        if (savedScrollPosition > 0)
        {
            scrollRect.SetVerticalNormalizedPositionFix(savedScrollPosition);
        }
    }

    public override async void OnScreenEnterCompleted()
    {
        base.OnScreenEnterCompleted();
        var canvasRectTransform = Canvas.GetComponent<RectTransform>();
        var canvasScreenRect = canvasRectTransform.GetScreenSpaceRect();
        
        scrollRect.contentLayoutGroup.padding.top = (int) ((canvasScreenRect.height -
                                                            scrollRectPaddingReference.GetScreenSpaceRect().min.y) *
            canvasRectTransform.rect.height / canvasScreenRect.height) + 48 - 156;
        scrollRect.transform.RebuildLayout();
    }
    
    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        savedScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    public async void RefillLevels(bool saveScrollPosition = false)
    {
        if (saveScrollPosition)
        {
            savedScrollPosition = scrollRect.verticalNormalizedPosition;
        }

        await Context.LevelManager.LoadLevelsOfType(LevelType.Community);
        
        var levels = new List<Level>(Context.LevelManager.LoadedLocalLevels.Values.Where(it => it.Type == LevelType.Community));

        scrollRect.totalCount = levels.Count;
        scrollRect.objectsToFill = levels.ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
        
        if (saveScrollPosition)
        {
            scrollRect.SetVerticalNormalizedPositionFix(savedScrollPosition);
        }
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalCoverThumbnail);
            scrollRect.ClearCells();
            if (to is MainMenuScreen)
            {
                savedScrollPosition = default;
            }
        }
    }
    
}