using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Proyecto26;
using RSG;
using UniRx;
using UniRx.Async;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class TierSelectionScreen : Screen, ScreenChangeListener
{
    public const string Id = "TierSelection";
    public static Content SavedContent = new Content {season = MockData.Season};

    public LoopVerticalScrollRect scrollRect;
    public RectTransform scrollRectAnchor;

    public Vector2 ScreenCenter { get; private set; }
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        Context.ScreenManager.AddHandler(this);
        scrollRect.OnBeginDragAsObservable().Subscribe(_ => { OnBeginDrag(); });
        scrollRect.OnEndDragAsObservable().Subscribe(_ => { OnEndDrag(); });
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Context.ScreenManager.RemoveHandler(this);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        ScreenCenter = scrollRectAnchor.GetScreenSpaceCenter();

        if (SavedContent != null)
        {
            OnContentLoaded(SavedContent);
        }
        else
        {
            // request
        }
    }

    public void OnContentLoaded(Content content)
    {
        scrollRect.totalCount = content.season.tiers.Count + 1;
        var tiers = new List<UserTier>(content.season.tiers) {new UserTier {isScrollRectFix = true}};
        for (var i = 0; i < tiers.Count - 1; i++) tiers[i].index = i;
        scrollRect.objectsToFill = tiers.Cast<object>().ToArray();
        scrollRect.RefillCells();
    }
    
    private bool isDragging = false;
    private IEnumerator snapCoroutine;

    public void OnBeginDrag()
    {
        if (snapCoroutine != null) {
            StopCoroutine(snapCoroutine);
        }
        isDragging = true;
    }

    public void OnEndDrag()
    {
        isDragging = false;
        StartCoroutine(snapCoroutine = SnapCoroutine());
    }

    private IEnumerator SnapCoroutine()
    {
        while (Math.Abs(scrollRect.velocity.y) > 512)
        {
            yield return null;
        }
        var tierCards = scrollRect.GetComponentsInChildren<TierCard>().ToList();
        var toTierCard = tierCards.FindAll(it => it.Index < 12).MinBy(it => Math.Abs(it.rectTransform.GetScreenSpaceCenter().y - ScreenCenter.y));
        scrollRect.SrollToCell(toTierCard.Index, 512);
        OnTierSelected(toTierCard.Index);
    }

    public void OnTierSelected(int index)
    {
        print("Selected tier " + (index + 1));
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
    }

    public class Content
    {
        public Season season;
    }

}