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
                    var failed = false;
                    await Context.RemoteResourceManager.DownloadResourceDialog(meta.asset,
                        onDownloadAborted: () => failed = true,
                        onDownloadFailed: () => failed = true);
                    if (failed)
                    {
                        Toast.Next(Toast.Status.Failure, "CHARACTER_FAILED_TO_DOWNLOAD".Get());
                    }
                    else
                    {
                        availableCharacters.Add(meta);
                    }
                }

                SpinnerOverlay.Hide();

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
        var reloadParallax = false;
        if (meta.asset != Context.RemoteResourceManager.SelectedCharacterAssetId)
        {
            reloadParallax = true;
            var characterGameObject = await Context.RemoteResourceManager.LoadResource(meta.asset);
            if (characterGameObject == null) return;
            if (Context.RemoteResourceManager.SelectedCharacterGameObject != null)
            {
                Context.RemoteResourceManager.Release(Context.RemoteResourceManager.SelectedCharacterGameObject);
            }

            Context.RemoteResourceManager.SelectedCharacterGameObject = characterGameObject;
            Context.RemoteResourceManager.SelectedCharacterAssetId = meta.id;
        }

        var character = Context.RemoteResourceManager.SelectedCharacterGameObject.GetComponent<CharacterAsset>();
        
        SpinnerOverlay.Show();
        infoCard.Leave(false);
        characterTransitionElement.Leave(false);

        await UniTask.Delay(TimeSpan.FromSeconds(0.4f));

        //TODO: not downloaded?
        await Context.LevelManager.LoadFromMetadataFiles(
            new List<string> {Context.DataPath + "/" + meta.level.uid + "/level.json"});

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

        if (reloadParallax)
        {
            ParallaxHolder.Instance.Load(character.parallaxPrefab);
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