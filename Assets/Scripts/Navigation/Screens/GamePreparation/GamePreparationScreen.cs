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

    public GradientMeshEffect startButtonGradient;
    public Text startButtonText;

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
                dialog.Message = "This level is outdated.\nWould you like to update now?";
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
            startButtonGradient.SetGradient(
                Context.LocalPlayer.PlayRanked
                    ? new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45)
                    : new ColorGradient("#F953C6".ToColor(), "#B91D73".ToColor(), 135)
            );
            startButtonText.text = Context.LocalPlayer.PlayRanked ? "Start!" : "Practice!";
        }
        else
        {
            startButtonGradient.SetGradient(
                new ColorGradient("#476ADC".ToColor(), "#9CAFEC".ToColor(), -45)
            );
            startButtonText.text = "Download!";
        }
    }

    private void OnLevelMetaUpdated(Level level)
    {
        if (level != Level) return;
        Toast.Enqueue(Toast.Status.Success, "Level metadata synchronized.");
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
            Context.LocalPlayer.PlayRanked ? "BEST PERFORMANCE" : "BEST PERFORMANCE (PRACTICE)";
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
        Toast.Next(Toast.Status.Success, "Best performance synchronized.");
    }

    public void LoadLevelSettings()
    {
        var lp = Context.LocalPlayer;
        calibratePreferenceElement.SetContent(null, null,
            () => lp.GetLevelNoteOffset(Context.SelectedLevel.Id),
            it => lp.SetLevelNoteOffset(Context.SelectedLevel.Id, it),
            "seconds",
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
        if (from.GetId() == Id && to.GetId() != ProfileScreen.Id)
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

            cover.pulseElement.Pulse();
            ProfileWidget.Instance.FadeOut();

            Context.AudioManager.Get("LevelStart").Play();

            if (coverSprite == null)
            {
                await UniTask.WaitUntil(() => coverSprite != null);
            }

            Context.SpriteCache.PutSprite("game://cover", "GameCover", coverSprite);
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
        if (!Context.OnlinePlayer.IsAuthenticated)
        {
            Toast.Next(Toast.Status.Failure, "Please sign in first.");
            return;
        }
        
        var dialog = Dialog.Instantiate();
        dialog.Message = "Downloading...";
        dialog.UseProgress = true;
        dialog.UsePositiveButton = false;
        dialog.UseNegativeButton = true;

        ulong downloadedSize;
        var totalSize = 0UL;
        var downloading = false;
        var aborted = false;
        var targetFile = $"{Application.temporaryCachePath}/Downloads/{Level.Id}.cytoidlevel";
        var destFolder = $"{Context.DataPath}/{Level.Id}";

        if (Level.IsLocal)
        {
            // Write to the local folder instead
            destFolder = Level.Path;
        }

        // Download detail first, then package
        RequestHelper req;
        var downloadHandler = new DownloadHandlerFile(targetFile)
        {
            removeFileOnAbort = true
        };
        RestClient.Get<OnlineLevel>(req = new RequestHelper
        {
            Uri = $"{Context.ApiBaseUrl}/levels/{Level.Id}"
        }).Then(it =>
        {
            if (aborted)
            {
                throw new OperationCanceledException();
            }

            totalSize = (ulong) it.size;
            downloading = true;
            Debug.Log("Package path: " + Level.PackagePath);
            return RestClient.Get<OnlineLevelResources>(req = new RequestHelper
            {
                Uri = Level.PackagePath,
                Headers = Context.OnlinePlayer.GetJwtAuthorizationHeaders()
            });
        }).Then(res =>
        {
            if (aborted)
            {
                throw new OperationCanceledException();
            }

            Debug.Log("Asset path: " + res.package);
            return RestClient.Get(req = new RequestHelper
            {
                Uri = res.package,
                DownloadHandler = downloadHandler,
                WillParseBody = false
            });
        }).Then(async res =>
        {
            downloading = false;
            dialog.OnNegativeButtonClicked = it => { };
            dialog.UseNegativeButton = false;
            dialog.Progress = 0;
            dialog.Message = "Unpacking...";
            DOTween.To(() => dialog.Progress, value => dialog.Progress = value, 1f, 1f).SetEase(Ease.OutCubic);

            if (Level.IsLocal)
            {
                // Unload the current preview
                if (previewAudioClip != null)
                {
                    previewAudioSource.clip = null;
                    previewAudioClip.UnloadAudioClip();
                }
            }

            var success = await Context.LevelManager.UnpackLevelPackage(targetFile, destFolder);
            if (success)
            {
                // Load with level manager and reload screen
                Level =
                    (await Context.LevelManager.LoadFromMetadataFiles(new List<string> {destFolder + "/level.json"}))
                    .First();
                Context.SelectedLevel = Level;
                Toast.Enqueue(Toast.Status.Success, "Successfully downloaded level.");

                UpdateTopMenu();
                LoadPreview(true);
                LoadCover(true);
                if (!previewAudioSource.isPlaying) LoadPreview(true);
                UpdateStartButton();
            }
            else
            {
                Toast.Next(Toast.Status.Failure, "Could not unpack level.");
            }

            dialog.Close();
            File.Delete(targetFile);
        }).Catch(error =>
        {
            if (aborted || error is OperationCanceledException || (req != null && req.IsAborted))
            {
                Toast.Enqueue(Toast.Status.Success, "Download cancelled.");
            }
            else
            {
                Debug.LogError(error);
                Toast.Next(Toast.Status.Failure, "Could not download level.");
            }

            dialog.Close();
        });

        dialog.onUpdate.AddListener(it =>
        {
            if (!downloading) return;
            if (totalSize > 0)
            {
                downloadedSize = req.DownloadedBytes;
                it.Progress = downloadedSize * 1.0f / totalSize;
                it.Message =
                    $"Downloading... ({downloadedSize.ToHumanReadableFileSize()} / {totalSize.ToHumanReadableFileSize()})";
            }
            else
            {
                it.Message = "Downloading...";
            }
        });
        dialog.OnNegativeButtonClicked = it =>
        {
            aborted = true;
            req?.Abort();
        };
        dialog.Open();
    }

    public async void InitializeSettingsTab()
    {
        var lp = Context.LocalPlayer;
        practiceModeToggle.Select((!lp.PlayRanked).BoolToString(), false);
        practiceModeToggle.onSelect.AddListener(it =>
        {
            var ranked = !bool.Parse(it);
            lp.PlayRanked = ranked;
            LoadLevelPerformance();
            UpdateStartButton();
        });
        
        calibratePreferenceElement.SetContent("Level note offset", "Notes not syncing well with music?\n" +
                                                                   "Press \"Calibrate\" or manually enter\n" +
                                                                   "a desired note offset.");
        calibratePreferenceElement.calibrateButton.onPointerClick.AddListener(_ =>
        {
            Context.WillCalibrate = true;
            OnStartButton();
        });

        SettingsFactory.InstantiateGeneralSettings(generalSettingsHolder);
        SettingsFactory.InstantiateGameplaySettings(gameplaySettingsHolder);
        SettingsFactory.InstantiateVisualSettings(visualSettingsHolder);
        SettingsFactory.InstantiateAdvancedSettings(advancedSettingsHolder);

        LayoutFixer.Fix(settingsTabContent);
        await UniTask.DelayFrame(5);
        LayoutStaticizer.Staticize(settingsTabContent);
    }

}