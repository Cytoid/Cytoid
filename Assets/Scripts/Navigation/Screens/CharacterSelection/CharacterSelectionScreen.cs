using System;
using System.Collections.Generic;
using Proyecto26;
using Cysharp.Threading.Tasks;
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

    public InteractableMonoBehavior helpButton;
    public InteractableMonoBehavior previousButton;
    public InteractableMonoBehavior nextButton;
    public InteractableMonoBehavior illustratorProfileButton;
    public InteractableMonoBehavior characterDesignerProfileButton;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        characterDisplay.loadOnScreenBecameActive = false;
        infoCard.enterOnScreenBecomeActive = characterTransitionElement.enterOnScreenBecomeActive = false;

        helpButton.onPointerClick.AddListener(_ => Dialog.PromptAlert("CHARACTER_TUTORIAL".Get()));
    }

    public override void OnScreenBecameActive()
    {
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
                        else
                        {
                            IntentPayload.OwnedCharacters.Add(meta);
                        }
                    }

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

        base.Render();
    }

    protected override void OnRendered()
    {
        base.OnRendered();
        
        LoadedPayload.SelectedIndex =
            LoadedPayload.OwnedCharacters.FindIndex(it =>
                it.AssetId == Context.CharacterManager.SelectedCharacterId);
        if (LoadedPayload.SelectedIndex < 0) LoadedPayload.SelectedIndex = 0; // Reset to default
        LoadCharacter(LoadedPayload.OwnedCharacters[LoadedPayload.SelectedIndex]);
    }

    public async void LoadCharacter(CharacterMeta meta)
    {
        ParallaxHolder.WillDelaySet = true;

        var isNewCharacter = Context.CharacterManager.ActiveCharacterBundleId != meta.AssetId;
        if (isNewCharacter)
        {
            SpinnerOverlay.Show();

            infoCard.Leave(false);
            characterTransitionElement.Leave(false);
        }

        var character = await Context.CharacterManager.SetActiveCharacter(meta.AssetId);
        if (character == null)
        {
            throw new Exception("Character not downloaded or corrupted");
        }

        if (isNewCharacter)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
        }

        nameText.text = meta.Name;
        nameGradient.SetGradient(character.nameGradient.GetGradient());
        descriptionText.text = meta.Description;
        levelCard.SetModel(meta.Level.ToLevel(LevelType.User));
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

        infoCard.transform.RebuildLayout();
        await characterDisplay.Load(CharacterAsset.GetTachieBundleId(meta.AssetId));
        NavigationBackdrop.Instance.UpdateBlur();

        infoCard.Enter();
        characterTransitionElement.Leave(false, true);
        characterTransitionElement.Enter();
        characterTransitionElement.Apply(it =>
        {
            it.enterMultiplier = 0.4f;
            it.enterDelay = 0.4f;
            it.enterDuration = 0.8f;
        });

        if (Context.IsOffline())
        {
            SpinnerOverlay.Hide();
        }
        else
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
                .Finally(() =>
                {
                    SpinnerOverlay.Hide();
                });
        }

        ParallaxHolder.WillDelaySet = false;
    }

    private void NextCharacter()
    {
        LoadedPayload.SelectedIndex = (LoadedPayload.SelectedIndex + 1).Mod(LoadedPayload.OwnedCharacters.Count);
        LoadCharacter(LoadedPayload.OwnedCharacters[LoadedPayload.SelectedIndex]);
    }

    private void PreviousCharacter()
    {
        LoadedPayload.SelectedIndex = (LoadedPayload.SelectedIndex - 1).Mod(LoadedPayload.OwnedCharacters.Count);
        LoadCharacter(LoadedPayload.OwnedCharacters[LoadedPayload.SelectedIndex]);
    }
    
    [Serializable]
    class CharacterPostData
    {
        public string characterId;
    }
    
    public class Payload : ScreenPayload
    {
        public readonly List<CharacterMeta> OwnedCharacters = new List<CharacterMeta>();
        public int SelectedIndex;
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