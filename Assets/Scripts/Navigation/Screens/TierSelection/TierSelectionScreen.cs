using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Proyecto26;
using UniRx;
using UniRx.Async;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class TierSelectionScreen : Screen
{
    public const string Id = "TierSelection";
    public static Content SavedContent;

    public LoopVerticalScrollRect scrollRect;
    public VerticalLayoutGroup scrollRectContentLayoutGroup;
    public TransitionElement lowerLeftColumn;
    public Text rewardCharacterName;
    public TransitionElement lowerRightColumn;
    public Text completionRateText;
    public GradientMeshEffect completionRateGradient;
    public CircleButton startButton;
    public DepthCover cover;
    public InteractableMonoBehavior helpButton;

    public ActionTabs actionTabs;
    public RankingsTab rankingsTab;
    
    public Vector2 ScreenCenter { get; private set; }
    public TierData SelectedTier { get; private set; }

    private TierCard selectedTierCard;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
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
        Context.OnOfflineModeToggled.AddListener(offline =>
        {
            if (offline)
            {
                UnloadResources();
            }
        });
        
        actionTabs.onTabChanged.AddListener((prev, next) =>
        {
            if (next.index == 0)
            {
                OnRankingsTab();
            }   
        });
        
        helpButton.onPointerClick.AddListener(_ =>
        {
            var dialog = Dialog.Instantiate();
            dialog.UsePositiveButton = true;
            dialog.UseNegativeButton = false;
            dialog.Message = "TIER_TUTORIAL".Get();
            dialog.Open();
        });
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        ProfileWidget.Instance.Enter();
        ScreenCenter = new Vector2(UnityEngine.Screen.width / 2f, UnityEngine.Screen.height / 2f);
        var height = RectTransform.rect.height;
        print("Rect height: " + height);
        var padding = scrollRectContentLayoutGroup.padding;
        padding.top = padding.bottom = (int) ((height - 576) / 2f); // TODO: Un-hardcode tier card height but I'm lazy lol
        scrollRectContentLayoutGroup.padding = padding;
        
        SpinnerOverlay.Show();
        await Context.LevelManager.LoadLevelsOfType(LevelType.Tier);

        /*SpinnerOverlay.Hide();
        OnContentLoaded(new Content{season = MockData.Season});
        return;*/
        
        RestClient.Get(new RequestHelper
        {
            Uri = $"{Context.ServicesUrl}/seasons/alpha",
            Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
        }).Then(res =>
        {
            print("TierSelection: " + res.Text);
            var season = JsonConvert.DeserializeObject<SeasonData>(res.Text);
            SavedContent = new Content {season = season};
            OnContentLoaded(SavedContent);
        }).CatchRequestError(error =>
        {
            if (!error.IsNetworkError)
            {
                throw error;
            }
            Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
        }).Finally(() => SpinnerOverlay.Hide());
        
        /*if (SavedContent != null)
        {
            OnContentLoaded(SavedContent);
        }
        else
        {
            // request
        }*/
    }

    public void OnContentLoaded(Content content)
    {
        scrollRect.ClearCells();
        
        scrollRect.totalCount = content.season.tiers.Count + 1;
        var tiers = new List<TierData>(content.season.tiers) {new TierData {isScrollRectFix = true}};
        for (var i = 0; i < tiers.Count - 1; i++)
        {
            var tier = tiers[i];
            tier.index = i;
            if (tier.Meta.parsedCriteria == default)
            {
                tier.Meta.parsedCriteria = tier.Meta.criteria.Select(Criterion.Parse).ToList();
            }
            if (tier.Meta.parsedStages == default)
            {
                tier.Meta.parsedStages = new List<Level>();
                tier.Meta.validStages = new List<bool>();
                for (var stage = 0; stage < Math.Min(tier.Meta.stages.Count, 3); stage++)
                {
                    var level = tier.Meta.stages[stage].ToLevel(LevelType.Tier);
                    tier.Meta.parsedStages.Add(level);
                    tier.Meta.validStages.Add(level.IsLocal && level.Type == LevelType.Tier &&
                                              level.Meta.version == tier.Meta.stages[stage].Version);
                }
            }
        }

        scrollRect.objectsToFill = tiers.Cast<object>().ToArray();
        scrollRect.RefillCells();
        scrollRect.GetComponent<TransitionElement>().Apply(it =>
        {
            it.Leave(false, true);
            it.Enter();
        });

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

        cover.OnCoverUnloaded();
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
                .MinBy(it => Math.Abs(it.rectTransform.GetScreenSpaceCenter(it.canvas).y - ScreenCenter.y));
            scrollRect.SrollToCell(toTierCard.Index, 1024);
            selectedTierCard = toTierCard;
            OnTierSelected(toTierCard.Tier);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            print(tierCards.Count);
            tierCards.FindAll(it => !it.Tier.isScrollRectFix).ForEach(it => print(Math.Abs(it.rectTransform.GetScreenSpaceCenter(it.canvas).y - ScreenCenter.y)));
        }

        snapCoroutine = null;
    }

    private DateTime asyncCoverToken;

    public async void OnTierSelected(TierData tier)
    {
        SelectedTier = tier;
        print("Selected tier " + (tier.Meta.name));

        if (tier.Meta.character != null)
        {
            lowerLeftColumn.Enter();
            rewardCharacterName.text = tier.Meta.character.Name;
        }

        if (!tier.locked)
        {
            lowerRightColumn.Enter();
            startButton.State = tier.StagesValid ? CircleButtonState.Start : CircleButtonState.Download;
            startButton.StartPulsing();
        }
        
        rankingsTab.SetRanking(SelectedTier.rank ?? -1);
        
        asyncCoverToken = DateTime.Now;

        var token = asyncCoverToken;

        Sprite sprite;
        var lastStage = tier.Meta.parsedStages.Last();
        if (lastStage.IsLocal)
        {
            sprite = await Context.AssetMemory.LoadAsset<Sprite>("file://" + lastStage.Path + lastStage.Meta.background.path, AssetTag.TierCover);
        }
        else
        {
            sprite = await Context.AssetMemory.LoadAsset<Sprite>(lastStage.OnlineLevel.Cover.CoverUrl,
                AssetTag.TierCover, useFileCache: true);
        }

        if (token != asyncCoverToken)
        {
            return;
        }

        if (sprite != null)
        {
            cover.OnCoverLoaded(sprite);
        }
    }

    private void OnRankingsTab()
    {
        rankingsTab.UpdateTierRankings(SelectedTier.Id);
    }
    
    public async void OnStartButton()
    {
        if (SelectedTier.StagesValid)
        {
            State = ScreenState.Inactive;
            Context.SelectedGameMode = GameMode.Tier;
            Context.TierState = new TierState(SelectedTier);

            scrollRect.GetComponentsInChildren<TierCard>().ForEach(it => it.OnTierStart());
            ProfileWidget.Instance.FadeOut();
            LoopAudioPlayer.Instance.StopAudio(0.4f);
            
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
        var newLocalStages = new List<Level>(SelectedTier.Meta.parsedStages);
        for (var index = 0; index < SelectedTier.Meta.parsedStages.Count; index++)
        {
            var level = SelectedTier.Meta.parsedStages[index];
            if (SelectedTier.Meta.validStages[index]) continue;
            bool? error = null;
            Context.LevelManager.DownloadAndUnpackLevelDialog(
                level,
                false,
                onDownloadAborted: () => { error = true; },
                onDownloadFailed: () => { error = true; },
                onUnpackSucceeded: downloadedLevel =>
                {
                    newLocalStages[index] = downloadedLevel;
                    SelectedTier.Meta.validStages[index] = true;
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
        SelectedTier.Meta.parsedStages = newLocalStages;

        selectedTierCard.ScrollCellContent(SelectedTier);
        OnTierSelected(SelectedTier);
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this && to != null && !(to is ProfileScreen))
        {
            UnloadResources();
        }
    }

    private void UnloadResources()
    {
        scrollRect.ClearCells();
        Context.LevelManager.UnloadLevelsOfType(LevelType.Tier);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalCoverThumbnail);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.OnlineCoverThumbnail);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.CharacterThumbnail);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.TierCover);
    }
    
    public class Content
    {
        public SeasonData season;
    }

}