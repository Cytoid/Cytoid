using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GamePreparationScreen : Screen, ScreenChangeListener
{
    public const string Id = "GamePreparation";

    [GetComponent] public AudioSource previewAudioSource;

    [GetComponentInChildrenName] public DepthCover cover;
    public Text bestPerformanceDescriptionText;
    public PerformanceWidget bestPerformanceWidget;

    public CircleButton startButton;

    public ActionTabs actionTabs;
    public RankingsTab rankingsTab;
    public RatingTab ratingTab;

    public GameObject gameplayIcon;
    public GameObject settingsIcon;

    public RadioGroup practiceModeToggle;
    
    public Transform settingsTabContent;
    public CalibratePreferenceElement calibratePreferenceElement;
    public Transform generalSettingsHolder;
    public Transform gameplaySettingsHolder;
    public Transform visualSettingsHolder;
    public Transform advancedSettingsHolder;
    
    private DateTime asyncRequestsToken;
    private Sprite coverSprite;
    private AssetLoader previewAudioClip;
    private bool initializedSettingsTab;
    
    public Level Level { get; set; }

    public override string GetId() => Id;

    private bool willStart;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        Context.ScreenManager.AddHandler(this);
        actionTabs.OnTabChanged.AddListener(async (prev, next) => 
        {
            if (!initializedSettingsTab && next.index == 3)
            {
                initializedSettingsTab = true;
                SpinnerOverlay.Show();
                await UniTask.DelayFrame(5);
                InitializeSettingsTab();
                await UniTask.DelayFrame(5);
                SpinnerOverlay.Hide();
            }
        });

        var lp = Context.LocalPlayer;
        practiceModeToggle.Select((!lp.PlayRanked).BoolToString(), false);
        practiceModeToggle.onSelect.AddListener(it =>
        {
            var ranked = !bool.Parse(it);
            lp.PlayRanked = ranked;
            LoadLevelPerformance();
            UpdateStartButton();
        });

        startButton.interactableMonoBehavior.onPointerClick.AddListener(_ => OnStartButton());
        
        Context.OnLanguageChanged.AddListener(() => initializedSettingsTab = false);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        asyncRequestsToken = DateTime.Now;

        if (Context.SelectedLevel == null)
        {
            Debug.LogWarning("Context.SelectedLevel is null");
            return;
        }

        LoopAudioPlayer.Instance.FadeOutLoopPlayer();
        ProfileWidget.Instance.Enter();

        var needReload = Level != Context.SelectedLevel;
        Level = Context.SelectedLevel;

        rankingsTab.UpdateRankings(Level.Id, Context.SelectedDifficulty.Id);
        ratingTab.UpdateLevelRating(Level.Id);
        Context.LevelManager.OnLevelMetaUpdated.AddListener(OnLevelMetaUpdated);
        Context.OnlinePlayer.OnLevelBestPerformanceUpdated.AddListener(OnLevelBestPerformanceUpdated);

        var localVersion = Level.Meta.version;
        Context.LevelManager.FetchLevelMeta(Level.Id, true).Then(it =>
        {
            print($"Remote version: {it.version}, local version: {localVersion}");
            if (it.version > Level.Meta.version)
            {
                // Ask the user to update
                var dialog = Dialog.Instantiate();
                dialog.Message = "DIALOG_LEVEL_OUTDATED".Get();
                dialog.UsePositiveButton = true;
                dialog.UseNegativeButton = true;
                dialog.OnPositiveButtonClicked = _ =>
                {
                    DownloadAndUnpackLevel();
                    dialog.Close();
                };
                dialog.Open();
            }
        });

        LoadLevelPerformance();
        LoadLevelSettings();
        LoadCover(needReload);
        LoadPreview(needReload);

        UpdateTopMenu();
        UpdateStartButton();
    }

    private void UpdateTopMenu()
    {
        gameplayIcon.SetActive(Level.IsLocal);
        settingsIcon.SetActive(Level.IsLocal);
    }

    private void UpdateStartButton()
    {
        if (Level.IsLocal)
        {
            startButton.State = Context.LocalPlayer.PlayRanked ? CircleButtonState.Start : CircleButtonState.Practice;
        }
        else
        {
            startButton.State = CircleButtonState.Download;
        }
    }

    private void OnLevelMetaUpdated(Level level)
    {
        if (level != Level) return;
        Toast.Enqueue(Toast.Status.Success, "TOAST_LEVEL_METADATA_SYNCHRONIZED".Get());
        Context.OnSelectedLevelChanged.Invoke(Context.SelectedLevel);
    }

    public async void LoadCover(bool load)
    {
        if (load)
        {
            string path;
            if (Level.IsLocal)
            {
                path = "file://" + Level.Path + Level.Meta.background.path;
            }
            else
            {
                path = Level.Meta.background.path.WithImageCdn().WithSizeParam(1920, 1080);
            }

            var token = asyncRequestsToken;
            Sprite sprite;
            using (var request = UnityWebRequestTexture.GetTexture(path))
            {
                await request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError($"Failed to download cover from {path}");
                    Debug.LogError(request.error);
                    return;
                }

                sprite = DownloadHandlerTexture.GetContent(request).CreateSprite();
            }

            if (asyncRequestsToken != token)
            {
                Destroy(sprite);
                return;
            }

            if (State == ScreenState.Active)
            {
                cover.OnCoverLoaded(sprite);
                coverSprite = sprite;
            }
        }
        else
        {
            cover.OnCoverLoaded(null);
        }
    }

    public async void LoadPreview(bool load)
    {
        if (load)
        {
            string path;
            if (Level.IsLocal)
            {
                path = "file://" + Level.Path + Level.Meta.music_preview.path;
            }
            else
            {
                path = Level.Meta.music_preview.path;
            }
            
            // Unload the current
            if (previewAudioClip != null)
            {
                previewAudioSource.clip = null;
                previewAudioClip.UnloadAudioClip();
                previewAudioClip = null;
            }

            // Load
            var token = asyncRequestsToken;
            var loader = new AssetLoader(path);
            await loader.LoadAudioClip();
            if (loader.Error != null)
            {
                Debug.LogError($"Failed to download preview from {path}");
                Debug.LogError(loader.Error);
                return;
            }

            if (asyncRequestsToken != token)
            {
                Destroy(loader.AudioClip);
                return;
            }

            if (State == ScreenState.Active)
            {
                previewAudioSource.clip = loader.AudioClip;
                previewAudioClip = loader;
            }
        }

        previewAudioSource.volume = 0;
        previewAudioSource.DOKill();
        previewAudioSource.DOFade(1, 1f).SetEase(Ease.Linear);
        previewAudioSource.loop = true;
        previewAudioSource.Play();
    }

    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();
        previewAudioSource.volume = Context.LocalPlayer.MusicVolume; // TODO: Migrate preview to audio manager
    }

    public void LoadLevelPerformance()
    {
        bestPerformanceDescriptionText.text =
            (Context.LocalPlayer.PlayRanked ? "GAME_PREP_BEST_PERFORMANCE" : "GAME_PREP_BEST_PERFORMANCE_PRACTICE").Get();
        if (!Context.LocalPlayer.HasPerformance(Context.SelectedLevel.Id, Context.SelectedDifficulty.Id,
            Context.LocalPlayer.PlayRanked))
        {
            bestPerformanceWidget.SetModel(new LocalPlayer.Performance()); // 0
        }
        else
        {
            var performance = Context.LocalPlayer.GetBestPerformance(Context.SelectedLevel.Id,
                Context.SelectedDifficulty.Id,
                Context.LocalPlayer.PlayRanked);
            bestPerformanceWidget.SetModel(performance);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(bestPerformanceDescriptionText.transform as RectTransform);
    }

    public void OnLevelBestPerformanceUpdated(string levelId)
    {
        if (levelId != Level.Id) return;
        LoadLevelPerformance();
        Toast.Next(Toast.Status.Success, "TOAST_BEST_PERFORMANCE_SYNCHRONIZED".Get());
    }

    public void LoadLevelSettings()
    {
        var lp = Context.LocalPlayer;
        calibratePreferenceElement.SetContent(null, null,
            () => lp.GetLevelNoteOffset(Context.SelectedLevel.Id),
            it => lp.SetLevelNoteOffset(Context.SelectedLevel.Id, it),
            "SETTINGS_UNIT_SECONDS".Get(),
            0.ToString());
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        cover.image.color = Color.black;
        Context.ScreenManager.RemoveHandler(this);
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        Level = null;
        Context.LevelManager.OnLevelMetaUpdated.RemoveListener(OnLevelMetaUpdated);
        Context.OnlinePlayer.OnLevelBestPerformanceUpdated.RemoveListener(OnLevelBestPerformanceUpdated);

        asyncRequestsToken = DateTime.Now;
        previewAudioSource.DOFade(0, 1f).SetEase(Ease.Linear).onComplete = () => { previewAudioSource.Stop(); };
        if (!willStart) LoopAudioPlayer.Instance.FadeInLoopPlayer();
    }

    public void OnScreenChangeStarted(Screen @from, Screen to) => Expression.Empty();

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from.GetId() == Id)
        {
            // Unload resources
            UnloadPreviewAudioClip();
            UnloadCoverSprite();
        }
    }

    private void UnloadPreviewAudioClip()
    {
        if (previewAudioClip != null)
        {
            print("Unloaded preview");
            previewAudioClip.UnloadAudioClip();
            previewAudioClip = null;
        }
    }

    private void UnloadCoverSprite()
    {
        if (coverSprite != null)
        {
            print("Unloaded cover");
            Destroy(coverSprite.texture);
            Destroy(coverSprite);
            coverSprite = null;
        }
    }

    public async void OnStartButton()
    {
        if (Level.IsLocal)
        {
            willStart = true;
            State = ScreenState.Inactive;
            startButton.StopPulsing();

            cover.pulseElement.Pulse();
            ProfileWidget.Instance.FadeOut();

            Context.AudioManager.Get("LevelStart").Play();

            if (coverSprite == null)
            {
                await UniTask.WaitUntil(() => coverSprite != null);
            }

            Context.SpriteCache.PutSpriteInMemory("game://cover", SpriteTag.GameCover, coverSprite);
            coverSprite = null; // Prevent sprite being unloaded by UnloadCoverSprite()

            Context.SelectedMods = Context.LocalPlayer.EnabledMods;

            var sceneLoader = new SceneLoader("Game");
            sceneLoader.Load();

            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

            cover.mask.DOFade(1, 0.8f);

            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

            sceneLoader.Activate();
        }
        else
        {
            DownloadAndUnpackLevel();
        }
    }

    public void DownloadAndUnpackLevel()
    {
        Context.LevelManager.DownloadAndUnpackLevelDialog(
            Level,
            allowAbort: true,
            onDownloadSucceeded: () =>
            {
                if (Level.IsLocal)
                {
                    // Unload the current preview
                    if (previewAudioClip != null)
                    {
                        previewAudioSource.clip = null;
                        previewAudioClip.UnloadAudioClip();
                    }
                }
            },
            onDownloadAborted: () =>
            {
                Toast.Enqueue(Toast.Status.Success, "TOAST_DOWNLOAD_CANCELLED".Get());
            },
            onDownloadFailed: () =>
            {
                Toast.Next(Toast.Status.Failure, "TOAST_COULD_NOT_DOWNLOAD_LEVEL".Get());
            },
            onUnpackSucceeded: level =>
            {
                // Load with level manager and reload screen
                Level = level;
                Context.SelectedLevel = level;
                Toast.Enqueue(Toast.Status.Success, "TOAST_SUCCESSFULLY_DOWNLOADED_LEVEL".Get());

                UpdateTopMenu();
                LoadPreview(true);
                LoadCover(true);
                if (!previewAudioSource.isPlaying) LoadPreview(true);
                UpdateStartButton();
            },
            onUnpackFailed: () =>
            {
                Toast.Next(Toast.Status.Failure, "TOAST_COULD_NOT_UNPACK_LEVEL".Get());
            }
        );
    }

    public async void InitializeSettingsTab()
    {
        calibratePreferenceElement.SetContent("GAME_PREP_SETTINGS_LEVEL_NOTE_OFFSET".Get(), "GAME_PREP_SETTINGS_LEVEL_NOTE_OFFSET_DESC".Get());
        calibratePreferenceElement.calibrateButton.onPointerClick.AddListener(_ =>
        {
            Context.WillCalibrate = true;
            OnStartButton();
        });

        foreach (Transform child in generalSettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in gameplaySettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in visualSettingsHolder) Destroy(child.gameObject);
        foreach (Transform child in advancedSettingsHolder) Destroy(child.gameObject);
        SettingsFactory.InstantiateGeneralSettings(generalSettingsHolder);
        SettingsFactory.InstantiateGameplaySettings(gameplaySettingsHolder);
        SettingsFactory.InstantiateVisualSettings(visualSettingsHolder);
        SettingsFactory.InstantiateAdvancedSettings(advancedSettingsHolder);

        LayoutFixer.Fix(settingsTabContent);
        await UniTask.DelayFrame(5);
        LayoutStaticizer.Staticize(settingsTabContent);
    }

}