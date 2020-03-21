using System;
using System.Collections.Generic;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
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

        helpButton.onPointerClick.AddListener(_ =>
        {
            var dialog = Dialog.Instantiate();
            dialog.UsePositiveButton = true;
            dialog.UseNegativeButton = false;
            dialog.Message =
                "<b><size=40>Characters!</size></b>\nUnlock more by clearing tiers, completing events, or unlocking achievements.";
            dialog.Open();
        });
        previousButton.onPointerClick.AddListener(_ => PreviousCharacter());
        nextButton.onPointerClick.AddListener(_ => NextCharacter());
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
                    var dialog = Dialog.Instantiate();
                    dialog.Message = "DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE";
                    dialog.OnPositiveButtonClicked = it =>
                    {
                        Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.Out,
                            addTargetScreenToHistory: false);
                        it.Close();
                    };
                    dialog.Open();
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

                var dialog = Dialog.Instantiate();
                dialog.Message = "DIALOG_COULD_NOT_CONNECT_TO_SERVER";
                dialog.OnPositiveButtonClicked = it =>
                {
                    Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.Out,
                        addTargetScreenToHistory: false);
                    it.Close();
                };
                dialog.Open();
            })
            .Finally(() => SpinnerOverlay.Hide());
    }

    public async void DownloadAvailableCharacters(List<CharacterMeta> characters)
    {
        foreach (var meta in characters)
        {
            var success = await Context.CharacterManager.DownloadCharacterAssetDialog(meta.AssetId);
            if (!success)
            {
                Toast.Next(Toast.Status.Failure, "CHARACTER_FAILED_TO_DOWNLOAD".Get());
            }
            else
            {
                availableCharacters.Add(meta);
            }
        }

        selectedIndex =
            availableCharacters.FindIndex(it =>
                it.AssetId == Context.CharacterManager.SelectedCharacterAssetId);

        if (selectedIndex < 0) selectedIndex = 0; // Reset to default
        LoadCharacter(availableCharacters[selectedIndex]);
    }

    public async void LoadCharacter(CharacterMeta meta)
    {
        ParallaxHolder.WillDelaySet = true;

        var isNewCharacter = Context.CharacterManager.ActiveCharacterAssetId != meta.AssetId;
        var character = await Context.CharacterManager.SetActiveCharacter(meta.AssetId);
        if (character == null)
        {
            throw new Exception("Character not downloaded or corrupted");
        }

        if (isNewCharacter)
        {
            SpinnerOverlay.Show();
            TranslucentCover.DarkMode();
            TranslucentCover.Show(1, 0.4f);

            infoCard.Leave(false);
            characterTransitionElement.Leave(false);

            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
        }

        nameText.text = meta.Name;
        nameGradient.SetGradient(character.nameGradient.GetGradient());
        descriptionText.text = meta.Description;
        levelCard.SetModel(meta.Level.ToLevel(LevelType.Official));
        illustratorText.text = meta.Illustrator.Name;
        illustratorProfileButton.onPointerClick.RemoveAllListeners();
        illustratorProfileButton.onPointerClick.AddListener(_ => Application.OpenURL(meta.Illustrator.Url));
        if (meta.CharacterDesigner != null && !meta.CharacterDesigner.Name.IsNullOrEmptyTrimmed())
        {
            characterDesignerHolder.gameObject.SetActive(true);
            characterDesignerText.text = meta.CharacterDesigner.Name;
            characterDesignerProfileButton.onPointerClick.RemoveAllListeners();
            characterDesignerProfileButton.onPointerClick.AddListener(_ =>
                Application.OpenURL(meta.CharacterDesigner.Url));
        }
        else
        {
            characterDesignerHolder.gameObject.SetActive(false);
        }

        infoCard.transform.RebuildLayout();
        characterDisplay.Load(CharacterAsset.GetTachieAssetId(meta.AssetId));

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