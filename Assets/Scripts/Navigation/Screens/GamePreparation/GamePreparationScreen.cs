using System;
using System.Globalization;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GamePreparationScreen : Screen
{
    public const string Id = "GamePreparation";
    
    [GetComponent] public AudioSource previewAudioSource;

    [GetComponentInChildrenName] public DepthCover cover;
    public PerformanceWidget bestPerformanceWidget;
    
    public SpinnerElement rankingSpinner;
    public Text rankingText;
    public RankingContainer rankingContainer;
    public Text rankingContainerStatusText;

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

    public Level Level { get; set; }
    
    public override string GetId() => Id;

    private bool willStart = false;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        rankingContainerStatusText.text = "";
        LoadSettings();
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (Context.SelectedLevel == null)
        {
            Debug.LogWarning("Context.activeLevel is null");
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
    }

    private long updateRankingToken;

    public void UpdateRankings()
    {
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
            var selectedLevel = Context.SelectedLevel;
            var path = "file://" + selectedLevel.Path + selectedLevel.Meta.background.path;

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
            cover.OnCoverLoaded(sprite);
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
            var selectedLevel = Context.SelectedLevel;
            var path = "file://" + selectedLevel.Path + selectedLevel.Meta.music_preview.path;
            var loader = new AssetLoader(path);
            await loader.LoadAudioClip();
            if (loader.Error != null)
            {
                Debug.LogError($"Failed to download preview from {path}");
                Debug.LogError(loader.Error);
                return;
            }

            if (State == ScreenState.Active)
            {
                previewAudioSource.clip = loader.AudioClip;
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
        if (!Context.LocalPlayer.HasPerformance(Context.SelectedLevel.Meta.id, Context.SelectedDifficulty.Id,
            Context.LocalPlayer.PlayRanked))
        {
            bestPerformanceWidget.SetModel(new LocalPlayer.Performance());
        }
        else
        {
            var performance = Context.LocalPlayer.GetBestPerformance(Context.SelectedLevel.Meta.id,
                Context.SelectedDifficulty.Id,
                Context.LocalPlayer.PlayRanked);
            bestPerformanceWidget.SetModel(performance);
        }
    }

    public void LoadLevelSettings()
    {
        levelNoteOffsetTextField.SetTextWithoutNotify(
            Context.LocalPlayer.GetLevelNoteOffset(Context.SelectedLevel.Meta.id).ToString(CultureInfo.InvariantCulture));
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        cover.image.color = Color.black;
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        previewAudioSource.DOFade(0, 1f).SetEase(Ease.Linear).onComplete = () => previewAudioSource.Stop();
        if (!willStart) LoopAudioPlayer.Instance.FadeInLoopPlayer();
    }

    public async void StartGame()
    {
        willStart = true;
        State = ScreenState.Inactive;

        cover.pulseElement.Pulse();
        ProfileWidget.Instance.FadeOut();
            
        Context.AudioManager.Get("LevelStart").Play(AudioTrackIndex.RoundRobin);

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

        cover.mask.DOFade(1, 0.8f);
        
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

        sceneLoader.Activate();
    }

    public void LoadSettings()
    {
        var lp = Context.LocalPlayer;
        hitSoundCaretSelect.Select(lp.HitSound, false, false);
        earlyHitSoundsToggle.Select(lp.PlayHitSoundsEarly.BoolToString(), false);
        largerHitboxesToggle.Select(lp.UseLargerHitboxes.BoolToString(), false);
        earlyLateIndicatorsToggle.Select(lp.DisplayEarlyLateIndicators.BoolToString(), false);
        noteSizeCaretSelect.Select(lp.NoteSize.ToString(CultureInfo.InvariantCulture), false, false);
        horizontalMarginCaretSelect.Select(lp.HorizontalMargin.ToString(CultureInfo.InvariantCulture), false, false);
        verticalMarginCaretSelect.Select(lp.VerticalMargin.ToString(CultureInfo.InvariantCulture), false, false);
        baseNoteOffsetTextField.SetTextWithoutNotify(lp.BaseLevelOffset.ToString(CultureInfo.InvariantCulture));
        headsetNoteOffsetTextfield.SetTextWithoutNotify(lp.HeadsetLevelOffset.ToString(CultureInfo.InvariantCulture));
        storyboardEffectsCaretSelect.Select(lp.GraphicsLevel, false, false);
        lowerResolutionToggle.Select(lp.LowerResolution.BoolToString(), false);
        showBoundariesToggle.Select(lp.ShowBoundaries.BoolToString(), false);
        coverOpacityCaretSelect.Select(lp.CoverOpacity.ToString(CultureInfo.InvariantCulture), false, false);
        ringColorTextField.SetTextWithoutNotify(lp.GetRingColor(NoteType.Click, false).ToString());
        fillColorClickUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.Click, false).ToString());
        fillColorClickDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.Click, true).ToString());
        fillColorDragUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.DragChild, false).ToString());
        fillColorDragDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.DragChild, true).ToString());
        fillColorHoldUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.Hold, false).ToString());
        fillColorHoldDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.Hold, true).ToString());
        fillColorLongHoldUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.LongHold, false).ToString());
        fillColorLongHoldDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.LongHold, true).ToString());
        fillColorFlickUpTextField.SetTextWithoutNotify(lp.GetFillColor(NoteType.Flick, false).ToString());
        fillColorFlickDownTextfield.SetTextWithoutNotify(lp.GetFillColor(NoteType.Flick, true).ToString());
        displayProfilerToggle.Select(lp.DisplayProfiler.BoolToString(), false);
        displayNoteIdsToggle.Select(lp.DisplayNoteIds.BoolToString(), false);
    }
}