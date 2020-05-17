using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Proyecto26;
using UniRx.Async;
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
        characterDisplay.Load("KaedeTachie");

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
        scrollRect.objectsToFill = content.Levels.ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
        if (lastScrollPosition > 0)
        {
            scrollRect.SetVerticalNormalizedPositionFix(lastScrollPosition);
        }
        scrollRect.GetComponent<TransitionElement>()
            .Let(it =>
            {
                it.Leave(false, true);
                it.Enter();
            });
    }

    public async void LoadContent()
    {
        SpinnerOverlay.Show();

        // Logic:
        // If online: load from API
        // If offline: load from static list
        // Then for any not found in path & data built in, extract from StreamingAssets folder

        await Context.LevelManager.LoadLevelsOfType(LevelType.Training);

        var levels = new List<Level>();
        var builtInLevelsToUnpack = new List<string>();
        if (Context.IsOnline())
        {
            RestClient.Get<TrainingData>(Context.ServicesUrl + "/training")
                .Then(data =>
                {
                    // Save to DB
                    Context.Database.Let(it =>
                    {
                        it.DropCollection("training");
                        it.GetCollection<TrainingData>("training").Insert(data);
                    });
                    
                    foreach (var onlineLevel in data.Levels)
                    {
                        if (onlineLevel.HasLocal(LevelType.Training) || !BuiltInData.TrainingModeLevelUids.Contains(onlineLevel.Uid))
                        {
                            levels.Add(onlineLevel.ToLevel(LevelType.Training));
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
                    .Select(it => it.Uid) ?? BuiltInData.TrainingModeLevelUids;
            
            foreach (var uid in levelUids)
            {
                if (Context.LevelManager.LoadedLocalLevels.ContainsKey(uid)
                    && Context.LevelManager.LoadedLocalLevels[uid].Type == LevelType.Training)
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
            
            var packagePaths = new List<string>();
            
            // Install all missing training levels that are built in
            foreach (var uid in builtInLevelsToUnpack)
            {
                var packagePath = Application.streamingAssetsPath + "/Levels/" + uid + ".cytoidlevel";
                if (Application.platform == RuntimePlatform.IPhonePlayer) packagePath = "file://" + packagePath;
                
                // Copy the file from StreamingAssets to temp directory
                using (var request = UnityWebRequest.Get(packagePath))
                {
                    await request.SendWebRequest();

                    if (request.isNetworkError || request.isHttpError)
                    {
                        Debug.LogError(request.error);
                        Debug.LogError($"Failed to copy level {uid} from StreamingAssets");
                        continue;
                    }

                    var bytes = request.downloadHandler.data;
                    var targetDirectory = $"{Application.temporaryCachePath}/Downloads";
                    var targetFile = $"{targetDirectory}/{uid}.cytoidlevel";

                    try
                    {
                        Directory.CreateDirectory(targetDirectory);
                        File.WriteAllBytes(targetFile, bytes);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogError($"Failed to copy level {uid} from StreamingAssets to {targetFile}");
                        continue;
                    }

                    packagePaths.Add(targetFile);
                }
            }

            if (packagePaths.Count > 0)
            {
                Toast.Next(Toast.Status.Loading, "TOAST_INITIALIZING_TRAINING_MODE".Get());

                Context.LevelManager.OnLevelInstallProgress.AddListener(SpinnerOverlay.OnLevelInstallProgress);
                var jsonPaths = await Context.LevelManager.InstallLevels(packagePaths, LevelType.Training);
                Context.LevelManager.OnLevelInstallProgress.RemoveListener(SpinnerOverlay.OnLevelInstallProgress);
            
                Context.LevelManager.OnLevelLoadProgress.AddListener(SpinnerOverlay.OnLevelLoadProgress);
                var loadedLevels =  await Context.LevelManager.LoadFromMetadataFiles(LevelType.Training, jsonPaths);
                Context.LevelManager.OnLevelLoadProgress.RemoveListener(SpinnerOverlay.OnLevelLoadProgress);

                levels.AddRange(loadedLevels);
            }

            SpinnerOverlay.Hide();

            LoadedContent = new Content {Levels = levels.OrderBy(it => it.Meta.GetEasiestDifficultyLevel()).ToList()};
            OnContentLoaded(LoadedContent);
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
                Context.LevelManager.UnloadLevelsOfType(LevelType.Training);
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
        [JsonProperty("levels")] public List<OnlineLevel> Levels { get; set; }
    }
}
