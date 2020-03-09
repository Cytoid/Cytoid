using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public TransitionElement lowerLeftColumn;
    public Text rewardCharacterName;
    public TransitionElement lowerRightColumn;
    public Text completionRateText;
    public GradientMeshEffect completionRateGradient;
    public CircleButton startButton;
    
    public Vector2 ScreenCenter { get; private set; }
    public Tier SelectedTier { get; private set; }

    private TierCard selectedTierCard;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        Context.ScreenManager.AddHandler(this);
        scrollRect.OnBeginDragAsObservable().Subscribe(_ => { OnBeginDrag(); });
        scrollRect.OnEndDragAsObservable().Subscribe(_ => { OnEndDrag(); });
        
        lowerRightColumn.onEnterStarted.AddListener(() =>
        {
            ColorGradient gradient;
            if (SelectedTier.completion == 2) gradient = ScoreGrade.MAX.GetGradient();
            else if (SelectedTier.completion >= 1.9f) gradient = ScoreGrade.SSS.GetGradient();
            else gradient = ColorGradient.None;
            completionRateGradient.SetGradient(gradient);
            completionRateText.text = $"{(Mathf.FloorToInt((float) (SelectedTier.completion * 100 * 100)) / 100f):0.00}%";
        });
        
        startButton.interactableMonoBehavior.onPointerClick.AddListener(_ => OnStartButton());
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Context.ScreenManager.RemoveHandler(this);
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        ProfileWidget.Instance.Enter();
        ScreenCenter = new Vector2(UnityEngine.Screen.width / 2f, UnityEngine.Screen.height / 2f);

        SpinnerOverlay.Show();
        await Context.LevelManager.LoadAllInDirectory(Context.TierDataPath);
        
        //TEST CODE
        Promise<OnlineLevel>.All(
            RestClient.Get<OnlineLevel>($"{Context.ApiBaseUrl}/levels/f"),
            RestClient.Get<OnlineLevel>($"{Context.ApiBaseUrl}/levels/gfsd.jojoksm"),
            RestClient.Get<OnlineLevel>($"{Context.ApiBaseUrl}/levels/prettyfish.weidong_meng.xinwen_lianbo_opening_theme")
        ).Then(it =>
        {
            var list = it.ToList();
            foreach (var userTier in SavedContent.season.tiers)
            {
                userTier.Meta.stages = list;
            }

            OnContentLoaded(SavedContent);
            SpinnerOverlay.Hide();
        }).Catch(error => Debug.LogError(error.Response));

        /*if (SavedContent != null)
        {
            OnContentLoaded(SavedContent);
        }
        else
        {
            // request
        }*/
    }

    public async void OnContentLoaded(Content content)
    {
        scrollRect.ClearCells();
        
        scrollRect.totalCount = content.season.tiers.Count + 1;
        var tiers = new List<Tier>(content.season.tiers) {new Tier {isScrollRectFix = true}};
        for (var i = 0; i < tiers.Count - 1; i++)
        {
            var tier = tiers[i];
            tier.index = i;
            tier.Meta.localStages = new List<Level>();
            var allUpToDate = true;
            for (var stage = 0; stage < Math.Min(tier.Meta.stages.Count, 3); stage++)
            {
                var level = tier.Meta.stages[stage].ToLevel();
                allUpToDate = allUpToDate && level.IsLocal && level.Meta.version == tier.Meta.stages[stage].version;
                tier.Meta.localStages.Add(level);
            }
        }

        scrollRect.objectsToFill = tiers.Cast<object>().ToArray();
        scrollRect.RefillCells();

        StartCoroutine(SnapCoroutine());
    }
    
    private bool isDragging = false;
    private IEnumerator snapCoroutine;

    public void OnBeginDrag()
    {
        if (snapCoroutine != null) {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }
        isDragging = true;
        lowerLeftColumn.Leave();
        lowerRightColumn.Leave();
    }

    public void OnEndDrag()
    {
        isDragging = false;
        StartCoroutine(snapCoroutine = SnapCoroutine());
    }

    private IEnumerator SnapCoroutine()
    {
        while (Math.Abs(scrollRect.velocity.y) > 1024)
        {
            yield return null;
        }
        yield return null;
        var tierCards = scrollRect.GetComponentsInChildren<TierCard>().ToList();
        if (tierCards.Count <= 1)
        {
            snapCoroutine = null;
            yield break;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        try
        {
            var toTierCard = tierCards
                .FindAll(it => !it.Tier.isScrollRectFix)
                .MinBy(it => Math.Abs(it.rectTransform.GetScreenSpaceCenter().y - ScreenCenter.y));
            scrollRect.SrollToCell(toTierCard.Index, 1024);
            selectedTierCard = toTierCard;
            OnTierSelected(toTierCard.Tier);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            print(tierCards.Count);
            tierCards.FindAll(it => !it.Tier.isScrollRectFix).ForEach(it => print(Math.Abs(it.rectTransform.GetScreenSpaceCenter().y - ScreenCenter.y)));
        }

        snapCoroutine = null;
    }

    public void OnTierSelected(Tier tier)
    {
        SelectedTier = tier;
        print("Selected tier " + (tier.Meta.name));

        if (tier.Meta.character != null)
        {
            lowerLeftColumn.Enter();
            rewardCharacterName.text = tier.Meta.character.name;
        }

        if (!tier.locked)
        {
            lowerRightColumn.Enter();
            startButton.State = tier.StagesDownloaded ? CircleButtonState.Start : CircleButtonState.Download;
        }
    }
    
    public async void OnStartButton()
    {
        if (SelectedTier.StagesDownloaded)
        {
            State = ScreenState.Inactive;
            Context.SelectedGameMode = GameMode.Tier;
            Context.TierState = new TierState(SelectedTier);

            scrollRect.GetComponentsInChildren<TierCard>().ForEach(it => it.OnTierStart());
            ProfileWidget.Instance.FadeOut();

            Context.AudioManager.Get("LevelStart").Play();

            Context.SelectedMods = Context.LocalPlayer.EnabledMods; // This will be filtered
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

            OpaqueOverlay.Show();

            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
            
            var sceneLoader = new SceneLoader("Game");
            sceneLoader.Load();

            sceneLoader.Activate();
        }
        else
        {
            DownloadAndUnpackStages();
        }
    }

    public async void DownloadAndUnpackStages()
    {
        var newLocalStages = new List<Level>(SelectedTier.Meta.localStages);
        for (var index = 0; index < SelectedTier.Meta.localStages.Count; index++)
        {
            var level = SelectedTier.Meta.localStages[index];
            if (level.IsLocal) continue;
            bool? error = null;
            Context.LevelManager.DownloadAndUnpackLevelDialog(
                level,
                Context.TierDataPath,
                false,
                onDownloadAborted: () => { error = true; },
                onDownloadFailed: () => { error = true; },
                onUnpackSucceeded: downloadedLevel =>
                {
                    newLocalStages[index] = downloadedLevel;
                    error = false;
                },
                onUnpackFailed: () => { error = true; }
            );

            await UniTask.WaitUntil(() => error.HasValue);
            if (error.Value) break;
        }

        if (newLocalStages.Any(it => !it.IsLocal))
        {
            Toast.Next(Toast.Status.Failure, "TOAST_COULD_NOT_DOWNLOAD_TIER_DATA".Get());
        }
        else
        {
            Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_DOWNLOADED_TIER_DATA".Get());
        }
        SelectedTier.Meta.localStages = newLocalStages;

        selectedTierCard.ScrollCellContent(SelectedTier);
        OnTierSelected(SelectedTier);
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