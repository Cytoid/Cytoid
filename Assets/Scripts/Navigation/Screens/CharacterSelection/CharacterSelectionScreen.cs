using System;
using System.Collections.Generic;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class CharacterSelectionScreen : Screen
{
    public const string Id = "CharacterSelection";

    public override string GetId() => Id;

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

    private List<CharacterMeta> availableCharacters = new List<CharacterMeta>();
    private int selectedIndex;

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

        SpinnerOverlay.Show();

        Context.CharacterManager.GetAvailableCharactersMeta()
            .Then(characters =>
            {
                if (characters.Count == 0)
                {
                    // TODO: This should not happen! We have Sayaka
                    Dialog.PromptGoBack("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
                    return;
                }
                DownloadAvailableCharacters(characters);
            })
            .CatchRequestError(error =>
            {
                if (!error.IsNetworkError)
                {
                    throw error;
                }
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
            })
            .Finally(() => SpinnerOverlay.Hide());
    }

    public async void DownloadAvailableCharacters(List<CharacterMeta> characters)
    {
        var downloadsRequired = 0;
        foreach (var meta in characters)
        {
            if (!await Context.RemoteAssetManager.IsCached(meta.AssetId))
            {
                downloadsRequired++;
            }
        }
        print($"Number of downloads required: {downloadsRequired}");

        await Context.RemoteAssetManager.UpdateCatalog();

        availableCharacters.Clear();

        var downloaded = 0;
        foreach (var meta in characters)
        {
            var (success, locallyResolved) = await Context.CharacterManager.DownloadCharacterAssetDialog(meta.AssetId);
            if (!locallyResolved)
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
                availableCharacters.Add(meta);
            }
        }
        print($"Number of downloads: {downloaded}");

        if (downloaded > downloadsRequired)
        {
            // Update was performed, which requires player to restart the game
            // Why? Too lazy to figure out Addressable stuff...
            Dialog.PromptUnclosable("DIALOG_RESTART_REQUIRED".Get());
            return;
        }

        if (availableCharacters.Count == 0)
        {
            Dialog.PromptGoBack("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
            return;
        }

        previousButton.onPointerClick.RemoveAllListeners();
        nextButton.onPointerClick.RemoveAllListeners();
        if (availableCharacters.Count == 1)
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

        selectedIndex =
            availableCharacters.FindIndex(it =>
                it.AssetId == Context.CharacterManager.SelectedCharacterAssetId);
        if (selectedIndex < 0) selectedIndex = 0; // Reset to default
        LoadCharacter(availableCharacters[selectedIndex]);
        
        Dialog.PromptAlert("ALPHA_CHARACTER_WARNING".Get());
    }

    public async void LoadCharacter(CharacterMeta meta)
    {
        ParallaxHolder.WillDelaySet = true;

        var isNewCharacter = Context.CharacterManager.ActiveCharacterAssetId != meta.AssetId;
        if (isNewCharacter)
        {
            SpinnerOverlay.Show();
            TranslucentCover.DarkMode();
            TranslucentCover.Show(1, 0.4f);

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
        levelCard.SetModel(meta.Level.ToLevel(LevelType.Official));
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
        await characterDisplay.Load(CharacterAsset.GetTachieAssetId(meta.AssetId));

        infoCard.Enter();
        characterTransitionElement.Leave(false, true);
        characterTransitionElement.Enter();
        characterTransitionElement.Apply(it =>
        {
            it.enterMultiplier = 0.4f;
            it.enterDelay = 0.4f;
            it.enterDuration = 0.8f;
        });
        SpinnerOverlay.Hide();
        TranslucentCover.Hide();

        ParallaxHolder.WillDelaySet = false;
    }

    private void NextCharacter()
    {
        selectedIndex = (selectedIndex + 1).Mod(availableCharacters.Count);
        LoadCharacter(availableCharacters[selectedIndex]);
    }

    private void PreviousCharacter()
    {
        selectedIndex = (selectedIndex - 1).Mod(availableCharacters.Count);
        LoadCharacter(availableCharacters[selectedIndex]);
    }
}