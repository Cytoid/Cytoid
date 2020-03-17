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
    public CharacterHolder characterHolder;

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
        characterHolder.loadOnScreenBecameActive = false;
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

        RestClient.GetArray<CharacterMeta>(new RequestHelper
            {
                Uri = $"{Context.ServicesUrl}/characters",
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
            }).Then(async characters =>
            {
                foreach (var meta in characters)
                {
                    var success = await Context.CharacterManager.DownloadCharacterAssetDialog(meta.asset);
                    if (!success)
                    {
                        Toast.Next(Toast.Status.Failure, "CHARACTER_FAILED_TO_DOWNLOAD".Get());
                    }
                    else
                    {
                        availableCharacters.Add(meta);
                    }
                }

                SpinnerOverlay.Hide();

                selectedIndex =
                    availableCharacters.FindIndex(it => it.asset == Context.CharacterManager.SelectedCharacterAssetId);

                if (selectedIndex < 0) selectedIndex = 0; // Reset to default
                LoadCharacter(availableCharacters[selectedIndex]);
            })
            .Catch(error =>
            {
                Debug.LogError(error);
                // TODO: Retry? Offline cache?
            }).Finally(() => SpinnerOverlay.Hide());
    }

    public async void LoadCharacter(CharacterMeta meta)
    {
        ParallaxHolder.WillDelaySet = true;
        
        var isNewCharacter = Context.CharacterManager.ActiveCharacterAssetId != meta.asset;
        var character = await Context.CharacterManager.SetActiveCharacter(meta.asset);
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

        // TODO: what if not downloaded?
        await Context.LevelManager.LoadFromMetadataFiles(LevelType.Official,
            new List<string> {Context.UserDataPath + "/" + meta.level.uid + "/level.json"});

        nameText.text = meta.name;
        nameGradient.SetGradient(character.nameGradient.GetGradient());
        descriptionText.text = meta.description;
        levelCard.SetModel(Context.LevelManager.LoadedLocalLevels[meta.level.uid]);
        illustratorText.text = meta.illustrator.name;
        illustratorProfileButton.onPointerClick.RemoveAllListeners();
        illustratorProfileButton.onPointerClick.AddListener(_ => Application.OpenURL(meta.illustrator.url));
        if (!meta.characterDesigner.name.IsNullOrEmptyTrimmed())
        {
            characterDesignerHolder.gameObject.SetActive(true);
            characterDesignerText.text = meta.characterDesigner.name;
            characterDesignerProfileButton.onPointerClick.RemoveAllListeners();
            characterDesignerProfileButton.onPointerClick.AddListener(_ => Application.OpenURL(meta.characterDesigner.url));
        }
        else
        {
            characterDesignerHolder.gameObject.SetActive(false);
        }

        infoCard.transform.RebuildLayout();
        characterHolder.Load();
        
        infoCard.Enter();
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