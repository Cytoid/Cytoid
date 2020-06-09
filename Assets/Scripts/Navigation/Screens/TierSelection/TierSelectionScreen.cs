using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using MoreMountains.NiceVibrations;
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
    public static Content LoadedContent;
    private static float lastScrollPosition = -1;

    public AudioSource previewAudioSource;

    public GraphicRaycaster topCanvasGraphicRaycaster;
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

    public TransitionElement icons;
    public ActionTabs actionTabs;
    public RankingsTab rankingsTab;
    
    public Transform settingsTabContent;
    public Transform currentTierSettingsHolder;
    public Transform generalSettingsHolder;
    public Transform gameplaySettingsHolder;
    public Transform visualSettingsHolder;
    public Transform advancedSettingsHolder;

    public Vector2 ScreenCenter { get; private set; }
    public TierData SelectedTier { get; private set; }

    private TierCard selectedTierCard;
    private bool initializedSettingsTab;
    
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
            if (next.index == 2)
            {
                OnSettingsTab();
            }
        });
        
        helpButton.onPointerClick.AddListener(_ =>
        {
            Dialog.PromptAlert("TIER_TUTORIAL".Get());
        });
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        ProfileWidget.Instance.Enter();
        ScreenCenter = new Vector2(UnityEngine.Screen.width / 2f, UnityEngine.Screen.height / 2f);
        var height = RectTransform.rect.height;
        var padding = scrollRectContentLayoutGroup.padding;
        padding.top = padding.bottom = (int) ((height - 576) / 2f); // TODO: Un-hardcode tier card height but I'm lazy lol
        scrollRectContentLayoutGroup.padding = padding;

        if (LoadedContent == null)
        {
            SpinnerOverlay.Show();
            await Context.LevelManager.LoadLevelsOfType(LevelType.Tier);
            RestClient.Get(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/seasons/alpha",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                Timeout = 5,
            }).Then(res =>
            {
                print("TierSelection: " + res.Text);
                var season = JsonConvert.DeserializeObject<SeasonMeta>(res.Text);
                LoadedContent = new Content {Season = season};
                OnContentLoaded(LoadedContent);
                Run.After(0.4f, () =>
                {
                    icons.Enter();
                    SpinnerOverlay.Hide();
                    Dialog.PromptAlert("ALPHA_TIER_WARNING".Get());
                });
            }).CatchRequestError(error =>
            {
                if (!error.IsNetworkError)
                {
                    throw error;
                }

                SpinnerOverlay.Hide();
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
            });
        }
        else
        {
            SpinnerOverlay.Show();
            OnContentLoaded(LoadedContent);
            Run.After(0.4f, () =>
            {
                icons.Enter();
                SpinnerOverlay.Hide();
            });
        }
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();

        asyncCoverToken = DateTime.Now;
        asyncPreviewToken = DateTime.Now;
        
        previewAudioSource.DOFade(0, 0.5f).SetEase(Ease.Linear).onComplete = () =>
        {
            previewAudioSource.Stop();
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.PreviewMusic);
        };

        if (LoadedContent != null)
        {
            lastScrollPosition = scrollRect.verticalNormalizedPosition;
        }
    }

    public override void SetBlockRaycasts(bool blockRaycasts)
    {
        base.SetBlockRaycasts(blockRaycasts);
        topCanvasGraphicRaycaster.enabled = blockRaycasts;
    }

    public async void OnContentLoaded(Content content)
    {
        scrollRect.ClearCells();
        
        scrollRect.totalCount = content.Season.tiers.Count + 1;
        var tiers = new List<TierData>(content.Season.tiers) {new TierData {isScrollRectFix = true}};
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
        LayoutFixer.Fix(scrollRect.content);

        if (lastScrollPosition > 0)
        {
            await UniTask.DelayFrame(5);
            LayoutFixer.Fix(scrollRect.content);
            scrollRect.SetVerticalNormalizedPositionFix(lastScrollPosition);
        }
        StartCoroutine(SnapCoroutine());
    }
    
    private bool isDragging;
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

    private IEnumerator SnapCoroutine(string tierId = null)
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
        
        try
        {
            TierCard toTierCard;
            if (tierId == null)
            { 
                toTierCard = tierCards
                    .FindAll(it => !it.Tier.isScrollRectFix)
                    .MinBy(it => Math.Abs(it.rectTransform.GetScreenSpaceCenter(it.canvas).y - ScreenCenter.y));
                scrollRect.SrollToCell(toTierCard.Index, 1024);
            }
            else
            {
                toTierCard = tierCards.FirstOrDefault(it => it.Tier.Id == tierId);
                if (toTierCard == null) toTierCard = tierCards[0];
                scrollRect.SrollToCell(toTierCard.Index, 1024);
            }
            selectedTierCard = toTierCard;
            OnTierSelected(toTierCard.Tier);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            tierCards.FindAll(it => !it.Tier.isScrollRectFix).ForEach(it => print(Math.Abs(it.rectTransform.GetScreenSpaceCenter(it.canvas).y - ScreenCenter.y)));
        }

        snapCoroutine = null;
    }

    public void OnTierSelected(TierData tier)
    {
        SelectedTier = tier;
        print("Selected tier " + tier.Meta.name);

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
        
        rankingsTab.UpdateTierRankings(SelectedTier.Id); 
        initializedSettingsTab = false;

        LoadCover();
        LoadPreview();
    }
    
    private DateTime asyncCoverToken;

    private async void LoadCover()
    {
        asyncCoverToken = DateTime.Now;

        var token = asyncCoverToken;

        Sprite sprite;
        var lastStage = SelectedTier.Meta.parsedStages.Last();
        if (lastStage.IsLocal)
        {
            sprite = await Context.AssetMemory.LoadAsset<Sprite>("file://" + lastStage.Path + lastStage.Meta.background.path, AssetTag.TierCover);
        }
        else
        {
            sprite = await Context.AssetMemory.LoadAsset<Sprite>(lastStage.OnlineLevel.Cover.CoverUrl,
                AssetTag.TierCover, allowFileCache: true);
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
    
    private DateTime asyncPreviewToken;
    private string lastPreviewPath;

    private async void LoadPreview()
    {
        var lastStage = SelectedTier.Meta.parsedStages.Last();
        string path;
        if (lastStage.IsLocal)
        {
            path = "file://" + lastStage.Path + lastStage.Meta.music_preview.path;
        }
        else
        {
            path = lastStage.Meta.music_preview.path;
        }
        if (lastPreviewPath == path)
        {
            return;
        }
        lastPreviewPath = path;

        // Load
        var token = asyncPreviewToken = DateTime.Now;
        
        previewAudioSource.DOKill();
        previewAudioSource.DOFade(0, 0.5f).SetEase(Ease.Linear);
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        
        if (asyncPreviewToken != token)
        {
            return;
        }

        var audioClip = await Context.AssetMemory.LoadAsset<AudioClip>(path, AssetTag.PreviewMusic, allowFileCache: true);

        if (asyncPreviewToken != token)
        {
            return;
        }

        if (State != ScreenState.Active) return;
        
        previewAudioSource.clip = audioClip;
        previewAudioSource.volume = 0;
        previewAudioSource.DOKill();
        previewAudioSource.DOFade(Context.Player.Settings.MusicVolume, 0.5f).SetEase(Ease.Linear);
        previewAudioSource.loop = true;
        previewAudioSource.Play();
    }

    private async void OnSettingsTab()
    {
        if (!initializedSettingsTab)
        {
            SpinnerOverlay.Show();
            await UniTask.DelayFrame(5);
            InitializeSettingsTab();
            await UniTask.DelayFrame(5);
            SpinnerOverlay.Hide();
        }
    }
    
    public async void OnStartButton()
    {
        if (SelectedTier == null) return;
        Context.Haptic(HapticTypes.SoftImpact, true);
        if (SelectedTier.StagesValid)
        {
            lastScrollPosition = scrollRect.verticalNormalizedPosition;
            
            State = ScreenState.Inactive;
            Context.SelectedGameMode = GameMode.Tier;
            Context.TierState = new TierState(SelectedTier);

            LoadedContent = null;

            scrollRect.GetComponentsInChildren<TierCard>().ForEach(it => it.OnTierStart());
            ProfileWidget.Instance.FadeOut();
            LoopAudioPlayer.Instance.StopAudio(0.4f);
            
            Context.AudioManager.Get("LevelStart").Play();

            Context.SelectedMods = Context.Player.Settings.EnabledMods.ToHashSet(); // This will be filtered
            
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
        if (from == this && to != null)
        {
            lastPreviewPath = null;
            DestroySettingsTab();
            if (!(to is ProfileScreen))
            {
                UnloadResources();
                if (to is MainMenuScreen)
                {
                    LoadedContent = null;
                }
            }
        }
    }

    private void UnloadResources()
    {
        scrollRect.ClearCells();
        Context.LevelManager.UnloadLevelsOfType(LevelType.Tier);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalLevelCoverThumbnail);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.RemoteLevelCoverThumbnail);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.CharacterThumbnail);
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.TierCover);
    }
    
    public async void InitializeSettingsTab()
    {
        DestroySettingsTab();
        
        initializedSettingsTab = true;
        
        var lp = Context.Player;
        var provider = PreferenceElementProvider.Instance;

        foreach (var (stringKey, index) in new[] {("1ST", 0), ("2ND", 1), ("3RD", 2)})
        {
            var levelId = SelectedTier.Meta.stages[index].Uid;

            var record = Context.Database.GetLevelRecord(levelId);
            if (record == null)
            {
                record = new LevelRecord {LevelId = levelId};
            }

            Instantiate(provider.input, currentTierSettingsHolder)
                .SetContent("TIER_SETTINGS_LEVEL_NOTE_OFFSET".Get($"TIER_STAGE_{stringKey}".Get()),
                    "",
                    () => record.RelativeNoteOffset,
                    it =>
                    {
                        record.RelativeNoteOffset = it;
                        Context.Database.SetLevelRecord(record);

                        if (Context.LevelManager.LoadedLocalLevels.ContainsKey(levelId))
                        {
                            Context.LevelManager.LoadedLocalLevels[levelId].Record = record;
                        }
                    },
                    "SETTINGS_UNIT_SECONDS".Get(), 0.ToString());
        }
        
        SettingsFactory.InstantiateGeneralSettings(generalSettingsHolder);
        SettingsFactory.InstantiateGameplaySettings(gameplaySettingsHolder);
        SettingsFactory.InstantiateVisualSettings(visualSettingsHolder);
        SettingsFactory.InstantiateAdvancedSettings(advancedSettingsHolder);

        LayoutStaticizer.Activate(settingsTabContent);
        LayoutFixer.Fix(settingsTabContent);
        await UniTask.DelayFrame(5);
        LayoutStaticizer.Staticize(settingsTabContent);
    }
    
    public void DestroySettingsTab()
    {
        initializedSettingsTab = false;
        foreach (Transform child in currentTierSettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in generalSettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in gameplaySettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in visualSettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in advancedSettingsHolder) Destroy(child.gameObject);
    }

    public class Content
    {
        public SeasonMeta Season;
    }

}