using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using Proyecto26;
using Cysharp.Threading.Tasks;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TrainingSelectionScreen : Screen
{
    public static Content LoadedContent;
    private static float lastScrollPosition = -1;

    public const string Id = "TrainingSelection";

    public LoopVerticalScrollRect scrollRect;
    public RectTransform scrollRectPaddingReference;
    public CharacterDisplay characterDisplay;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        characterDisplay.Load(CharacterAsset.GetTachieBundleId("Kaede"));

        if (LoadedContent != null)
        {
            OnContentLoaded(LoadedContent);
        }
        else
        {
            LoadContent();
        }
    }

    public override async void OnScreenEnterCompleted()
    {
        base.OnScreenEnterCompleted();
        var canvasRectTransform = Canvas.GetComponent<RectTransform>();
        var canvasScreenRect = canvasRectTransform.GetScreenSpaceRect();

        scrollRect.contentLayoutGroup.padding.top = (int) ((canvasScreenRect.height -
                                                            scrollRectPaddingReference.GetScreenSpaceRect().min.y) *
                canvasRectTransform.rect.height / canvasScreenRect.height) +
            48 - 156;
        scrollRect.transform.RebuildLayout();
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        lastScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    public void OnContentLoaded(Content content)
    {
        scrollRect.totalCount = content.Levels.Count;
        scrollRect.objectsToFill = content.Levels.Select(it => new LevelView{Level = it}).ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
        if (lastScrollPosition > -1)
        {
            scrollRect.SetVerticalNormalizedPositionFix(lastScrollPosition);
        }
        scrollRect.GetComponent<TransitionElement>()
            .Let(it =>
            {
                it.Leave(false, true);
                it.Enter();
            });

        characterDisplay.OnInteract = async _ => await ShowTrainingDialogue(false);
        if (Context.Player.ShouldOneShot("Training Mode: Is First Met"))
        {
            ShowTrainingDialogue(true).Forget();
        }
    }

    public async void LoadContent()
    {
        SpinnerOverlay.Show();
        var willUnpackAll = false;
        if (Context.Player.Settings.TrainingModeVersion < BuiltInData.TrainingModeVersion)
        {
            Context.Player.Settings.TrainingModeVersion = BuiltInData.TrainingModeVersion;
            Context.Player.SaveSettings();
            willUnpackAll = true;
        }

        // Logic:
        // If online: load from API
        // If offline: load from static list
        // Then for any not found in path & data built in, extract from StreamingAssets folder

        var levels = new List<Level>();
        var builtInLevelsToUnpack = new List<string>();
        if (Context.IsOnline())
        {
            RestClient.Get<TrainingData>(Context.ApiUrl + "/training")
                .Then(data =>
                {
                    // Save to DB
                    Context.Database.Let(it =>
                    {
                        var col = it.GetCollection<TrainingData>("training");
                        col.DeleteMany(x => true);
                        col.Insert(data);
                    });
                    
                    foreach (var onlineLevel in data.Levels)
                    {
                        if (!willUnpackAll && (onlineLevel.HasLocal(LevelType.User) || !BuiltInData.TrainingModeLevelIds.Contains(onlineLevel.Uid)))
                        {
                            levels.Add(onlineLevel.ToLevel(LevelType.User));
                        }
                        else
                        {
                            builtInLevelsToUnpack.Add(onlineLevel.Uid);
                        }
                    }
                    Continuation();
                })
                .CatchRequestError(error =>
                {
                    Debug.LogError(error);
                    Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                    SpinnerOverlay.Hide();
                });
        }
        else
        {
            // Load from DB, or use default
            var levelUids =
                Context.Database.GetCollection<TrainingData>("training").FindOne(x => true)?.Levels
                    .Select(it => it.Uid) ?? BuiltInData.TrainingModeLevelIds;
            
            foreach (var uid in levelUids)
            {
                if (!willUnpackAll && Context.LevelManager.LoadedLocalLevels.ContainsKey(uid)
                                     && Context.LevelManager.LoadedLocalLevels[uid].Type == LevelType.User)
                {
                    levels.Add(Context.LevelManager.LoadedLocalLevels[uid]);
                }
                else
                {
                    builtInLevelsToUnpack.Add(uid);
                }
            }
            Continuation();
        }

        async void Continuation()
        {
            print($"Resolved levels: {string.Join(", ", levels.Select(it => it.Id))}");
            print($"Built-in levels to unpack: {string.Join(", ", builtInLevelsToUnpack)}");
            
            var packagePaths = await Context.LevelManager.CopyBuiltInLevelsToDownloads(builtInLevelsToUnpack);

            if (packagePaths.Count > 0)
            {
                Toast.Next(Toast.Status.Loading, "TOAST_INITIALIZING_TRAINING_MODE".Get());

                Context.LevelManager.OnLevelInstallProgress.AddListener(SpinnerOverlay.OnLevelInstallProgress);
                var jsonPaths = await Context.LevelManager.InstallLevels(packagePaths, LevelType.User);
                Context.LevelManager.OnLevelInstallProgress.RemoveListener(SpinnerOverlay.OnLevelInstallProgress);
            
                Context.LevelManager.OnLevelLoadProgress.AddListener(SpinnerOverlay.OnLevelLoadProgress);
                var loadedLevels =  await Context.LevelManager.LoadFromMetadataFiles(LevelType.User, jsonPaths);
                Context.LevelManager.OnLevelLoadProgress.RemoveListener(SpinnerOverlay.OnLevelLoadProgress);

                levels.AddRange(loadedLevels);
            }

            SpinnerOverlay.Hide();

            LoadedContent = new Content {Levels = levels.OrderBy(it => it.Meta.GetEasiestDifficultyLevel()).ToList()};
            OnContentLoaded(LoadedContent);
        }
    }

    public async UniTask ShowTrainingDialogue(bool isFirstMet)
    {
        var text = Resources.Load<TextAsset>("Stories/Training");
        var story = new Story(text.text);
        Resources.UnloadAsset(text);
        story.variablesState["IsFirstMet"] = isFirstMet;
        var shouldIntroduceMechanisms = Context.Player.ShouldOneShot("Training Mode: Should Introduce Mechanisms");
        story.variablesState["ShouldIntroduceMechanisms"] = shouldIntroduceMechanisms;
        story.variablesState["SignedIn"] = Context.OnlinePlayer.IsAuthenticated;
        story.variablesState["Rating"] = Context.OnlinePlayer.LastProfile?.Rating ?? 0;
        await DialogueOverlay.Show(story);
        if (shouldIntroduceMechanisms && (int) story.variablesState["IntroducedMechanisms"] == 0)
        {
            Context.Player.ClearOneShot("Training Mode: Should Introduce Mechanisms");
        }
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalLevelCoverThumbnail);
            scrollRect.ClearCells();
            if (to is MainMenuScreen)
            {
                LoadedContent = null;
                lastScrollPosition = default;
            }
        }
    }

    public class Content
    {
        public List<Level> Levels;
    }
    
    [Serializable]
    private class TrainingData
    {
        public ObjectId Id { get; set; }
        [JsonProperty("levels")] public List<OnlineLevel> Levels { get; set; }
    }
}
