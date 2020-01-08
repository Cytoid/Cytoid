using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
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

    public SpinnerElement rankingSpinner;
    public Text rankingText;
    public RankingContainer rankingContainer;
    public Text rankingContainerStatusText;

    public RadioGroup practiceModeToggle;

    public InteractableMonoBehavior levelNoteOffsetCalibrateButton;
    public InputField levelNoteOffsetTextField;

    public CaretSelect hitSoundCaretSelect;
    public RadioGroup earlyHitSoundsToggle;
    public RadioGroup largerHitboxesToggle;
    public RadioGroup earlyLateIndicatorsToggle;
    public CaretSelect noteSizeCaretSelect;
    public CaretSelect horizontalMarginCaretSelect;
    public CaretSelect verticalMarginCaretSelect;
    public InputField baseNoteOffsetTextField;
    public InputField headsetNoteOffsetTextfield;

    public CaretSelect storyboardEffectsCaretSelect;
    public RadioGroup lowerResolutionToggle;
    public RadioGroup showBoundariesToggle;
    public CaretSelect coverOpacityCaretSelect;
    public InputField ringColorTextField;
    public InputField fillColorClickUpTextField;
    public InputField fillColorClickDownTextfield;
    public InputField fillColorDragUpTextField;
    public InputField fillColorDragDownTextfield;
    public InputField fillColorHoldUpTextField;
    public InputField fillColorHoldDownTextfield;
    public InputField fillColorLongHoldUpTextField;
    public InputField fillColorLongHoldDownTextfield;
    public InputField fillColorFlickUpTextField;
    public InputField fillColorFlickDownTextfield;

    public RadioGroup displayProfilerToggle;
    public RadioGroup displayNoteIdsToggle;

    private DateTime asyncRequestsToken;
    private Sprite coverSprite;
    private AudioClip previewAudioClip;

    public Level Level { get; set; }

    public override string GetId() => Id;

    private bool willStart = false;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        rankingContainerStatusText.text = "";
        LoadSettings();
        Context.ScreenManager.AddHandler(this);
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

        if (needReload)
        {
            Level = Context.SelectedLevel;
            UpdateRankings();
        }

        LoadLevelPerformance();
        LoadLevelSettings();
        LoadCover(needReload);
        LoadPreview(needReload);

        UpdateStartButton();
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

    private long updateRankingToken;

    public void UpdateRankings()
    {
        rankingText.text = "";
        rankingContainer.Clear();
        rankingSpinner.IsSpinning = true;
        rankingContainerStatusText.text = "Downloading level rankings...";
        updateRankingToken = DateTime.Now.ToFileTimeUtc();
        var token = updateRankingToken;
        Context.OnlinePlayer.GetLevelRankings(Context.SelectedLevel.Meta.id, Context.SelectedDifficulty.Id)
            .Then(ret =>
            {
                if (token != updateRankingToken) return;
                var (rank, entries) = ret;
                rankingContainer.SetData(entries);
                if (rank > 0)
                {
                    if (rank > 99) rankingText.text = "#99+";
                    else rankingText.text = "#" + rank;
                }
                else rankingText.text = "N/A";

                rankingContainerStatusText.text = "";
            })
            .Catch(error =>
            {
                if (token != updateRankingToken) return;
                Debug.LogError(error);
                rankingText.text = "N/A";
                rankingContainerStatusText.text = "Could not download level rankings.";
            })
            .Finally(() =>
            {
                if (token != updateRankingToken) return;
                rankingSpinner.IsSpinning = false;
            });
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
                previewAudioClip = loader.AudioClip;
            }
        }

        previewAudioSource.volume = 0;
        previewAudioSource.DOKill();
        previewAudioSource.DOFade(1, 1f).SetEase(Ease.Linear);
        previewAudioSource.loop = true;
        previewAudioSource.Play();
    }

    public void LoadLevelPerformance()
    {
        bestPerformanceDescriptionText.text =
            Context.LocalPlayer.PlayRanked ? "BEST PERFORMANCE" : "BEST PERFORMANCE (PRACTICE)";
        if (!Context.LocalPlayer.HasPerformance(Context.SelectedLevel.Meta.id, Context.SelectedDifficulty.Id,
            Context.LocalPlayer.PlayRanked))
        {
            bestPerformanceWidget.SetModel(new LocalPlayer.Performance()); // 0
        }
        else
        {
            var performance = Context.LocalPlayer.GetBestPerformance(Context.SelectedLevel.Meta.id,
                Context.SelectedDifficulty.Id,
                Context.LocalPlayer.PlayRanked);
            bestPerformanceWidget.SetModel(performance);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(bestPerformanceDescriptionText.transform as RectTransform);
    }

    public void LoadLevelSettings()
    {
        var lp = Context.LocalPlayer;
        levelNoteOffsetTextField.SetTextWithoutNotify(
            lp.GetLevelNoteOffset(Context.SelectedLevel.Meta.id)
                .ToString(CultureInfo.InvariantCulture));
        levelNoteOffsetTextField.onEndEdit.AddListener(FloatSettingHandler(levelNoteOffsetTextField,
            () => 0, value => lp.SetLevelNoteOffset(Context.SelectedLevel.Meta.id, value)));
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
        asyncRequestsToken = DateTime.Now;
        previewAudioSource.DOFade(0, 1f).SetEase(Ease.Linear).onComplete = () =>
        {
            previewAudioSource.Stop();
        };
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
            previewAudioClip.UnloadAudioData();
            Destroy(previewAudioClip);
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

            Context.AudioManager.Get("LevelStart").Play(AudioTrackIndex.RoundRobin);

            if (coverSprite == null)
            {
                await UniTask.WaitUntil(() => coverSprite != null);
            }
            Context.SpriteCache.PutSprite("game://cover", "GameCover", coverSprite);
            coverSprite = null; // Prevent sprite being unloaded by UnloadCoverSprite()

            var sceneLoader = new SceneLoader("Game");
            sceneLoader.Load();

            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

            cover.mask.DOFade(1, 0.8f);

            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

            sceneLoader.Activate();
        }
        else
        {
            var dialog = Dialog.Instantiate();
            dialog.Message = "Downloading...";
            dialog.UseProgress = true;
            dialog.UsePositiveButton = false;
            dialog.UseNegativeButton = true;

            ulong downloadedSize;
            var totalSize = 0UL;
            var downloading = false;
            var aborted = false;
            var targetFile = $"{Application.temporaryCachePath}/Downloads/{Level.Meta.id}.cytoidlevel";
            var destFolder = $"{Context.DataPath}/{Level.Meta.id}";

            // TODO: Check destFolder exists

            // Download detail first, then package
            RequestHelper req = null;
            var downloadHandler = new DownloadHandlerFile(targetFile)
            {
                removeFileOnAbort = true
            };
            RestClient.Get<OnlineLevel>(new RequestHelper
            {
                Uri = $"{Context.ApiBaseUrl}/levels/{Level.Meta.id}"
            }).Then(it =>
            {
                if (aborted)
                {
                    throw new OperationCanceledException();
                }

                totalSize = (ulong) it.size;
                downloading = true;
                return RestClient.Get<OnlineLevelResources>(req = new RequestHelper
                {
                    Uri = Level.PackagePath,
                    Headers = new Dictionary<string, string>
                    {
                        {"Authorization", "JWT " + Context.OnlinePlayer.GetJwtToken()}
                    }
                });
            }).Then(res => {
                if (aborted)
                {
                    throw new OperationCanceledException();
                }
                
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

                var success = await Context.LevelManager.UnpackLevelPackage(targetFile, destFolder);
                if (success)
                {
                    // Load with level manager and reload screen
                    await Context.LevelManager.LoadFromMetadataFiles(new List<string> { destFolder + "/level.json" });
                    Toast.Enqueue(Toast.Status.Success, "Successfully downloaded level.");

                    Context.SelectedLevel = Context.LevelManager.LoadedLevels.Find(it => it.Meta.id == Level.Meta.id);
                    Level = Context.SelectedLevel;

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
                if (error is OperationCanceledException || (req != null && req.IsAborted))
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
    }
    
    public void LoadSettings()
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
        hitSoundCaretSelect.Select(lp.HitSound, false, false);
        hitSoundCaretSelect.onSelect.AddListener((_, it) =>
        {
            lp.HitSound = it;
            var resource = Resources.Load<AudioClip>("Audio/HitSounds/" + Context.LocalPlayer.HitSound);
            var hitSound = Context.AudioManager.Load("HitSound", resource);
            hitSound.Play(AudioTrackIndex.RoundRobin);
        });
        earlyHitSoundsToggle.Select(lp.PlayHitSoundsEarly.BoolToString(), false);
        earlyHitSoundsToggle.onSelect.AddListener(it => lp.PlayHitSoundsEarly = bool.Parse(it));
        largerHitboxesToggle.Select(lp.UseLargerHitboxes.BoolToString(), false);
        largerHitboxesToggle.onSelect.AddListener(it => lp.UseLargerHitboxes = bool.Parse(it));
        earlyLateIndicatorsToggle.Select(lp.DisplayEarlyLateIndicators.BoolToString(), false);
        earlyLateIndicatorsToggle.onSelect.AddListener(it => lp.DisplayEarlyLateIndicators = bool.Parse(it));
        noteSizeCaretSelect.Select(lp.NoteSize.ToString(CultureInfo.InvariantCulture), false, false);
        noteSizeCaretSelect.onSelect.AddListener((_, it) => lp.NoteSize = float.Parse(it));
        horizontalMarginCaretSelect.Select(lp.HorizontalMargin.ToString(CultureInfo.InvariantCulture), false, false);
        horizontalMarginCaretSelect.onSelect.AddListener((_, it) => lp.HorizontalMargin = int.Parse(it));
        verticalMarginCaretSelect.Select(lp.VerticalMargin.ToString(CultureInfo.InvariantCulture), false, false);
        verticalMarginCaretSelect.onSelect.AddListener((_, it) => lp.VerticalMargin = int.Parse(it));
        baseNoteOffsetTextField.SetTextWithoutNotify(lp.BaseNoteOffset.ToString(CultureInfo.InvariantCulture));
        baseNoteOffsetTextField.onEndEdit.AddListener(FloatSettingHandler(baseNoteOffsetTextField,
            () => lp.BaseNoteOffset, value => lp.BaseNoteOffset = value));
        headsetNoteOffsetTextfield.SetTextWithoutNotify(lp.HeadsetNoteOffset.ToString(CultureInfo.InvariantCulture));
        headsetNoteOffsetTextfield.onEndEdit.AddListener(FloatSettingHandler(headsetNoteOffsetTextfield,
            () => lp.HeadsetNoteOffset, value => lp.HeadsetNoteOffset = value));
        storyboardEffectsCaretSelect.Select(lp.GraphicsLevel, false, false);
        storyboardEffectsCaretSelect.onSelect.AddListener((_, it) => lp.GraphicsLevel = it);
        lowerResolutionToggle.Select(lp.LowerResolution.BoolToString(), false);
        lowerResolutionToggle.onSelect.AddListener(it => lp.LowerResolution = bool.Parse(it));
        showBoundariesToggle.Select(lp.ShowBoundaries.BoolToString(), false);
        showBoundariesToggle.onSelect.AddListener(it => lp.ShowBoundaries = bool.Parse(it));
        coverOpacityCaretSelect.Select(lp.CoverOpacity.ToString(CultureInfo.InvariantCulture), false, false);
        coverOpacityCaretSelect.onSelect.AddListener((_, it) => lp.CoverOpacity = float.Parse(it));
        ringColorTextField.SetTextWithoutNotify(lp.GetRingColor(NoteType.Click, false).ColorToString());
        ringColorTextField.onEndEdit.AddListener(ColorSettingHandler(ringColorTextField,
            () => lp.GetRingColor(NoteType.Click, false), value => lp.SetRingColor(NoteType.Click, false, value)));
        fillColorClickUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.Click, false).ColorToString());
        fillColorClickUpTextField.onEndEdit.AddListener(ColorSettingHandler(fillColorClickUpTextField,
            () => lp.GetFillColor(NoteType.Click, false), value => lp.SetFillColor(NoteType.Click, false, value)));
        fillColorClickDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.Click, true).ColorToString());
        fillColorClickDownTextfield.onEndEdit.AddListener(ColorSettingHandler(fillColorClickDownTextfield,
            () => lp.GetFillColor(NoteType.Click, true), value => lp.SetFillColor(NoteType.Click, true, value)));
        fillColorDragUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.DragChild, false).ColorToString());
        fillColorDragUpTextField.onEndEdit.AddListener(ColorSettingHandler(fillColorDragUpTextField,
            () => lp.GetFillColor(NoteType.DragChild, false),
            value => lp.SetFillColor(NoteType.DragChild, false, value)));
        fillColorDragDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.DragChild, true).ColorToString());
        fillColorDragDownTextfield.onEndEdit.AddListener(ColorSettingHandler(fillColorDragDownTextfield,
            () => lp.GetFillColor(NoteType.DragChild, true),
            value => lp.SetFillColor(NoteType.DragChild, true, value)));
        fillColorHoldUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.Hold, false).ColorToString());
        fillColorHoldUpTextField.onEndEdit.AddListener(ColorSettingHandler(fillColorHoldUpTextField,
            () => lp.GetFillColor(NoteType.Hold, false), value => lp.SetFillColor(NoteType.Hold, false, value)));
        fillColorHoldDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.Hold, true).ColorToString());
        fillColorHoldDownTextfield.onEndEdit.AddListener(ColorSettingHandler(fillColorHoldDownTextfield,
            () => lp.GetFillColor(NoteType.Hold, true), value => lp.SetFillColor(NoteType.Hold, true, value)));
        fillColorLongHoldUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.LongHold, false).ColorToString());
        fillColorLongHoldUpTextField.onEndEdit.AddListener(ColorSettingHandler(fillColorLongHoldUpTextField,
            () => lp.GetFillColor(NoteType.LongHold, false),
            value => lp.SetFillColor(NoteType.LongHold, false, value)));
        fillColorLongHoldDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.LongHold, true).ColorToString());
        fillColorLongHoldDownTextfield.onEndEdit.AddListener(ColorSettingHandler(fillColorLongHoldDownTextfield,
            () => lp.GetFillColor(NoteType.LongHold, true), value => lp.SetFillColor(NoteType.LongHold, true, value)));
        fillColorFlickUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.Flick, false).ColorToString());
        fillColorFlickUpTextField.onEndEdit.AddListener(ColorSettingHandler(fillColorFlickUpTextField,
            () => lp.GetFillColor(NoteType.Flick, false), value => lp.SetFillColor(NoteType.Flick, false, value)));
        fillColorFlickDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.Flick, true).ColorToString());
        fillColorFlickDownTextfield.onEndEdit.AddListener(ColorSettingHandler(fillColorFlickDownTextfield,
            () => lp.GetFillColor(NoteType.Flick, true), value => lp.SetFillColor(NoteType.Flick, true, value)));
        displayProfilerToggle.Select(lp.DisplayProfiler.BoolToString(), false);
        displayNoteIdsToggle.Select(lp.DisplayNoteIds.BoolToString(), false);
    }

    private static UnityAction<string> FloatSettingHandler(InputField inputField, Func<float> defaultValueGetter,
        Action<float> setter)
    {
        return it =>
        {
            if (float.TryParse(it, out var value))
            {
                setter(value);
            }
            else
            {
                inputField.text = defaultValueGetter().ToString(CultureInfo.InvariantCulture);
            }
        };
    }

    private static UnityAction<string> ColorSettingHandler(InputField inputField, Func<Color> defaultValueGetter,
        Action<Color> setter)
    {
        return it =>
        {
            var value = it.ToColor();
            if (value != Color.clear)
            {
                setter(value);
            }
            else
            {
                inputField.text = defaultValueGetter().ColorToString();
            }
        };
    }
}