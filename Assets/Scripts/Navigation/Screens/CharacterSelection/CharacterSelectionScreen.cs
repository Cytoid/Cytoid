using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionScreen : Screen
{
    public TransitionElement infoCard;
    public Text nameText;
    public GradientMeshEffect nameGradient;
    public Text descriptionText;
    public LevelCard levelCard;
    public Text illustratorText;
    public Transform characterDesignerHolder;
    public Text characterDesignerText;
    public TransitionElement characterTransitionElement;
    public CharacterDisplay characterDisplay;
    public Sprite volumeSprite;
    public Sprite volumeMuteSprite;
    public Transform variantSelectorHolder;
    public CanvasGroup lockedOverlayCanvasGroup;

    public Text levelText;
    public Text expText;

    public InteractableMonoBehavior helpButton;
    public InteractableMonoBehavior muteButton;
    public InteractableMonoBehavior previousButton;
    public InteractableMonoBehavior nextButton;
    public InteractableMonoBehavior illustratorProfileButton;
    public InteractableMonoBehavior characterDesignerProfileButton;
    public InteractableMonoBehavior unlockButton;

    private Image muteButtonImage;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        characterDisplay.loadOnScreenBecameActive = false;
        infoCard.enterOnScreenBecomeActive = characterTransitionElement.enterOnScreenBecomeActive = false;

        helpButton.onPointerClick.AddListener(_ => Dialog.PromptAlert("CHARACTER_TUTORIAL".Get()));
        muteButtonImage = muteButton.GetComponentInChildren<Image>();
        
        lockedOverlayCanvasGroup.alpha = 0;
        lockedOverlayCanvasGroup.blocksRaycasts = false;
    }

    public override void OnScreenBecameActive()
    {
        lockedOverlayCanvasGroup.alpha = 0;
        lockedOverlayCanvasGroup.blocksRaycasts = false;
        characterTransitionElement.Apply(it =>
        {
            it.enterMultiplier = 0;
            it.enterDelay = 0;
            it.enterDuration = 0.4f;
        });
        base.OnScreenBecameActive();
    }

    protected override void LoadPayload(ScreenLoadPromise promise)
    {
        SpinnerOverlay.Show();
        Context.CharacterManager.GetAvailableCharactersMeta()
            .Then(async characters =>
            {
                try
                {
                    if (Application.isEditor)
                    {
                        Debug.Log("Available characters:");
                        characters.PrintJson();
                    }

                    if (characters.Count == 0)
                    {
                        // TODO: This should not happen! We have Sayaka
                        SpinnerOverlay.Hide();
                        Dialog.PromptGoBack("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
                        promise.Reject();
                        return;
                    }

                    if (!await Context.BundleManager.DownloadAndSaveCatalog())
                    {
                        SpinnerOverlay.Hide();
                        Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                        promise.Reject();
                        return;
                    }

                    SpinnerOverlay.Hide();

                    var downloadsRequired = 0;
                    foreach (var meta in characters)
                    {
                        if (!Context.BundleManager.IsUpToDate(CharacterAsset.GetMainBundleId(meta.AssetId)))
                        {
                            downloadsRequired++;
                        }
                    }

                    print($"Number of downloads required: {downloadsRequired}");

                    var downloaded = 0;
                    foreach (var meta in characters)
                    {
                        var (success, locallyResolved) =
                            await Context.CharacterManager.DownloadCharacterAssetDialog(
                                CharacterAsset.GetMainBundleId(meta.AssetId));
                        if (success && !locallyResolved)
                        {
                            downloaded++;
                            print("Downloaded " + meta.AssetId);
                        }

                        if (!success)
                        {
                            Toast.Next(Toast.Status.Failure, "CHARACTER_FAILED_TO_DOWNLOAD".Get());
                        }
                    }

                    characters.ForEach(it =>
                    {
                        if (it.SetId == null) it.SetId = it.Id;
                    });
                    IntentPayload.OwnedCharacters.AddRange(characters
                        .Select((meta, index) => new { meta, index })
                        .GroupBy(it => it.meta.SetId)
                        .Select(it => new MergedCharacterMeta { variants = it.OrderBy(x => x.meta.SetOrder).Select(x => x.meta).ToList() })
                        .OrderBy(it => it.StandardVariant.Date)
                    );

                    print($"Number of downloads: {downloaded}");

                    if (downloaded > downloadsRequired)
                    {
                        // Update was performed, which requires player to restart the game
                        // Why? Too lazy to figure out dynamic reloads...
                        Dialog.PromptUnclosable("DIALOG_RESTART_REQUIRED".Get());
                        promise.Reject();
                        return;
                    }

                    if (IntentPayload.OwnedCharacters.Count == 0)
                    {
                        Dialog.PromptGoBack("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
                        promise.Reject();
                        return;
                    }

                    promise.Resolve(IntentPayload);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    SpinnerOverlay.Hide();
                    Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                    promise.Reject();
                }
            })
            .CatchRequestError(error =>
            {
                SpinnerOverlay.Hide();
                promise.Reject();
                if (!error.IsNetworkError)
                {
                    throw error;
                }

                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
            });
    }

    protected override void Render()
    {
        muteButton.scaleOnClick = false;
        muteButton.onPointerClick.RemoveAllListeners();
        previousButton.onPointerClick.RemoveAllListeners();
        nextButton.onPointerClick.RemoveAllListeners();
        if (LoadedPayload.OwnedCharacters.Count == 1)
        {
            previousButton.scaleOnClick = false;
            nextButton.scaleOnClick = false;
            previousButton.GetComponentInChildren<Image>().SetAlpha(0.3f);
            nextButton.GetComponentInChildren<Image>().SetAlpha(0.3f);
        }
        else
        {
            previousButton.scaleOnClick = true;
            nextButton.scaleOnClick = true;
            previousButton.GetComponentInChildren<Image>().SetAlpha(1f);
            nextButton.GetComponentInChildren<Image>().SetAlpha(1f);
            previousButton.onPointerClick.AddListener(_ => PreviousCharacter());
            nextButton.onPointerClick.AddListener(_ => NextCharacter());
        }
        
        LoadedPayload.SelectedIndex =
            LoadedPayload.OwnedCharacters.FindIndex(it => it.variants.Any(x => x.AssetId == Context.CharacterManager.SelectedCharacterId));
        if (LoadedPayload.SelectedIndex < 0) {
            LoadedPayload.SelectedIndex = 0; // Reset to default
            LoadedPayload.SelectedVariantIndex = 0;
        }
        else
        {
            LoadedPayload.SelectedVariantIndex = LoadedPayload.OwnedCharacters[LoadedPayload.SelectedIndex].variants
                .FindIndex(it => it.AssetId == Context.CharacterManager.SelectedCharacterId);
        }
        
        base.Render();
    }

    protected override void OnRendered()
    {
        base.OnRendered();
        
        Reload();
    }

    public async void Reload()
    {
        var mergedMeta = LoadedPayload.OwnedCharacters[LoadedPayload.SelectedIndex];
        var meta = mergedMeta.variants[LoadedPayload.SelectedVariantIndex];
        
        ParallaxHolder.WillDelaySet = true;

        var characterBundle = await Context.BundleManager.LoadCachedBundle(CharacterAsset.GetMainBundleId(meta.AssetId));
        var loader = characterBundle.LoadAssetAsync<GameObject>("Character");
        await loader;
        var characterAsset = Instantiate((GameObject) loader.asset).GetComponent<CharacterAsset>();

        var requireReload = IntentPayload.displayedCharacter == null
                            || IntentPayload.displayedCharacter.SetId != meta.SetId
                            || IntentPayload.displayedCharacter.VariantParallaxAsset != meta.VariantParallaxAsset
                            || IntentPayload.displayedCharacter.VariantAudioAsset != meta.VariantAudioAsset;
        Debug.Log("Require reload: " + requireReload);
        Debug.Log(IntentPayload.displayedCharacter?.SetId);
        Debug.Log(meta.SetId);
        Debug.Log(IntentPayload.displayedCharacter?.VariantParallaxAsset);
        Debug.Log(meta.VariantParallaxAsset);
        Debug.Log(IntentPayload.displayedCharacter?.VariantAudioAsset);
        Debug.Log(meta.VariantAudioAsset);

        if (requireReload)
        {
            infoCard.Leave(false);
            SpinnerOverlay.Show();
        }

        await UniTask.WaitUntil(() => !characterTransitionElement.IsInTransition);
        characterTransitionElement.Leave();

        if (meta.Owned)
        {
            lockedOverlayCanvasGroup.blocksRaycasts = false;
            lockedOverlayCanvasGroup.DOFade(0, 0.4f);
            
            await Context.CharacterManager.SetActiveCharacter(meta.AssetId, requireReload);
            if (requireReload)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
            }
        }
        else
        {
            lockedOverlayCanvasGroup.blocksRaycasts = true;
            lockedOverlayCanvasGroup.DOFade(1, 0.4f);
            
            unlockButton.onPointerClick.SetListener(_ =>
            {
                if (meta.QuestId == null)
                {
                    Dialog.PromptAlert("This is a bug. Blame Neo for that.\nJoin our Discord to report this bug: https://discord.gg/cytoid");
                    return;
                }
                
                SpinnerOverlay.Show();
                RestClient.Get<SingleQuestState>(new RequestHelper
                    {
                        Uri = $"{Context.ApiUrl}/epics/adventures/characters/{meta.QuestId}",
                        Headers = Context.OnlinePlayer.GetRequestHeaders(),
                        EnableDebug = true
                    })
                    .Then(data =>
                    {
                        QuestOverlay.Show(new AdventureState {OngoingQuests = new List<OngoingQuest>{data.Quest}});
                    })
                    .Catch(err =>
                    {
                        Debug.LogError(err);
                        Dialog.PromptAlert("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                    })
                    .Finally(() => SpinnerOverlay.Hide());
            });
        }

        nameText.text = meta.Name;
        nameGradient.SetGradient(characterAsset.nameGradient.GetGradient());
        descriptionText.text = meta.Description;

        illustratorText.text = meta.Illustrator.Name;
        illustratorProfileButton.onPointerClick.SetListener(_ => Application.OpenURL(meta.Illustrator.Url));
        if (meta.CharacterDesigner != null && !meta.CharacterDesigner.Name.IsNullOrEmptyTrimmed())
        {
            characterDesignerHolder.gameObject.SetActive(true);
            characterDesignerText.text = meta.CharacterDesigner.Name;
            characterDesignerProfileButton.onPointerClick.SetListener(_ =>
                Application.OpenURL(meta.CharacterDesigner.Url));
        }
        else
        {
            characterDesignerHolder.gameObject.SetActive(false);
        }

        if (meta.Exp == null)
        {
            meta.Exp = new CharacterMeta.ExpData
            {
                CurrentLevel = 1,
                CurrentLevelExp = 0,
                NextLevelExp = 10,
                TotalExp = 0
            };
        }
        levelText.text = $"{"PROFILE_WIDGET_LEVEL".Get()} {meta.Exp.CurrentLevel}";
        expText.text = $"{"PROFILE_WIDGET_EXP".Get()} {(int) meta.Exp.TotalExp}/{(int) meta.Exp.NextLevelExp}";
        
        if (meta.Level != null)
        {
            levelCard.SetModel(meta.Level.ToLevel(LevelType.User), ignoreIfIdEqual: true);
        }
        LayoutFixer.Fix(infoCard.transform);

        if (requireReload)
        {
            if (characterAsset.musicAudio == null)
            {
                muteButtonImage.sprite = volumeSprite;
                muteButtonImage.SetAlpha(0.3f);
                muteButton.scaleOnClick = false;
                muteButton.onPointerClick.RemoveAllListeners();
            }
            else
            {
                muteButtonImage.sprite = Context.Player.Settings.PlayCharacterTheme ? volumeSprite : volumeMuteSprite;
                muteButtonImage.SetAlpha(1f);
                muteButton.scaleOnClick = true;
                muteButton.onPointerClick.SetListener(_ =>
                {
                    Context.Player.Settings.PlayCharacterTheme = !Context.Player.Settings.PlayCharacterTheme;
                    Context.Player.SaveSettings();
                    LoopAudioPlayer.Instance.SetMainAudio(Context.Player.Settings.PlayCharacterTheme
                        ? characterAsset.musicAudio
                        : LoopAudioPlayer.Instance.defaultLoopAudio);
                    muteButtonImage.sprite = Context.Player.Settings.PlayCharacterTheme ? volumeSprite : volumeMuteSprite;
                });
            }

            NavigationBackdrop.Instance.UpdateBlur();
        }
        
        infoCard.Enter();

        // Spawn radio group
        foreach (Transform child in variantSelectorHolder)
        {
            Destroy(child.gameObject);
        }

        if (mergedMeta.HasOtherVariants)
        {
            var radioGroup = Instantiate(NavigationUiElementProvider.Instance.pillRadioGroup, variantSelectorHolder);
            radioGroup.labels = mergedMeta.variants.Select(it => it.VariantName).ToList();
            radioGroup.values = Enumerable.Range(0, mergedMeta.variants.Count).Select(it => it.ToString()).ToList();
            radioGroup.defaultValue = mergedMeta.variants.FindIndex(it => it.Id == meta.Id).ToString();
            radioGroup.Initialize();
            radioGroup.onSelect.AddListener(it =>
            {
                LoadedPayload.SelectedVariantIndex = int.Parse(it);
                Reload();
            });
            variantSelectorHolder.RebuildLayout();
        }

        await UniTask.WaitUntil(() => !characterTransitionElement.IsInTransition);
        await characterDisplay.Load(CharacterAsset.GetTachieBundleId(meta.AssetId), !meta.Owned);
        characterTransitionElement.Enter();
        characterTransitionElement.Apply(it =>
        {
            it.enterMultiplier = 0.4f;
            it.enterDelay = 0.4f;
            it.enterDuration = 0.8f;
        });

        if (requireReload)
        {
            Destroy(characterAsset.gameObject);
            Context.BundleManager.Release(CharacterAsset.GetMainBundleId(meta.AssetId));
        }

        if (Context.IsOffline())
        {
            SpinnerOverlay.Hide();
        }
        else
        {
            if (meta.Owned)
            {
                RestClient.Post(new RequestHelper
                    {
                        Uri = $"{Context.ApiUrl}/profile/{Context.Player.Id}/character",
                        Headers = Context.OnlinePlayer.GetRequestHeaders(),
                        EnableDebug = true,
                        Body = new CharacterPostData
                        {
                            characterId = meta.Id
                        }
                    })
                    .CatchRequestError(error =>
                    {
                        Debug.LogError(error);
                        Toast.Next(Toast.Status.Failure, "TOAST_FAILED_TO_UPDATE_PROFILE_CHARACTER".Get());
                    })
                    .Finally(() => { SpinnerOverlay.Hide(); });
            }
        }

        ParallaxHolder.WillDelaySet = false;
        IntentPayload.displayedCharacter = meta;
    }

    private void NextCharacter()
    {
        LoadedPayload.SelectedIndex = (LoadedPayload.SelectedIndex + 1).Mod(LoadedPayload.OwnedCharacters.Count);
        LoadedPayload.SelectedVariantIndex = 0;
        Reload();
    }

    private void PreviousCharacter()
    {
        LoadedPayload.SelectedIndex = (LoadedPayload.SelectedIndex - 1).Mod(LoadedPayload.OwnedCharacters.Count);
        LoadedPayload.SelectedVariantIndex = 0;
        Reload();
    }
    
    [Serializable]
    class CharacterPostData
    {
        public string characterId;
    }
    
    public class Payload : ScreenPayload
    {
        public readonly List<MergedCharacterMeta> OwnedCharacters = new List<MergedCharacterMeta>();
        public int SelectedIndex;
        public int SelectedVariantIndex;

        public CharacterMeta displayedCharacter;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }
    
    public override ScreenPayload GetDefaultPayload() => new Payload();
    
    public const string Id = "CharacterSelection";

    public override string GetId() => Id;
    
}

#if UNITY_EDITOR
[CustomEditor(typeof(CharacterSelectionScreen))]
public class CharacterSelectionScreenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var parent = (CharacterSelectionScreen) target;
        if (GUILayout.Button("Switch variant 0/1"))
        {
            parent.IntentPayload.SelectedVariantIndex = parent.IntentPayload.SelectedVariantIndex == 1 ? 0 : 1;
            parent.Reload();
        }
    }
}
#endif