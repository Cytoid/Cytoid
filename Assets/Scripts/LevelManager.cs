using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DG.Tweening;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class LevelManager
{
    public const string CoverThumbnailFilename = ".cover";

    public readonly LevelInstallProgressEvent OnLevelInstallProgress = new LevelInstallProgressEvent();
    public readonly LevelLoadProgressEvent OnLevelLoadProgress = new LevelLoadProgressEvent();
    public readonly LevelEvent OnLevelMetaUpdated = new LevelEvent();
    public readonly LevelEvent OnLevelDeleted = new LevelEvent();

    public readonly Dictionary<string, Level> LoadedLocalLevels = new Dictionary<string, Level>();
    private readonly HashSet<string> loadedPaths = new HashSet<string>();

    public async UniTask<List<string>> InstallAllFromDataPath()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            var files = new List<string>();
            var inboxPath = Context.DataPath + "/Inbox/"; 
            if (Directory.Exists(inboxPath))
            {
                files.AddRange(Directory.GetFiles(inboxPath, "*.cytoidlevel"));
                files.AddRange(Directory.GetFiles(inboxPath, "*.cytoidlevel.zip"));
            }
            if (Directory.Exists(Context.iOSTemporaryInboxPath))
            {
                files.AddRange(Directory.GetFiles(Context.iOSTemporaryInboxPath, "*.cytoidlevel"));
                files.AddRange(Directory.GetFiles(Context.iOSTemporaryInboxPath, "*.cytoidlevel.zip"));
            }
            
            foreach (var file in files)
            {
                if (file == null) continue;
                
                var toPath = Context.DataPath + "/" + Path.GetFileName(file);
                try
                {
                    if (File.Exists(toPath))
                    {
                        File.Delete(toPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Debug.LogError($"Failed to delete .cytoidlevel file at {toPath}");
                    continue;
                }

                try
                {
                    File.Move(file, toPath);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Debug.LogError($"Failed to move .cytoidlevel file from {file} to {toPath}");
                }
            }
        }

        var levelFiles = new List<string>();
        try
        {
            levelFiles.AddRange(Directory.GetFiles(Context.DataPath, "*.cytoidlevel"));
            levelFiles.AddRange(Directory.GetFiles(Context.DataPath, "*.cytoidlevel.zip"));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError("Cannot read from data path");
            return new List<string>();
        }
        
        var loadedLevelJsonFiles = new List<string>();
        var index = 1;
        foreach (var levelFile in levelFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(levelFile);
            OnLevelInstallProgress.Invoke(fileName, index, levelFiles.Count);

            var destFolder = $"{Context.DataPath}/{fileName}";
            if (await UnpackLevelPackage(levelFile, destFolder))
            {
                loadedLevelJsonFiles.Add(destFolder + "/level.json");
                Debug.Log($"Installed {index}/{levelFiles.Count}: {levelFile}");
            }
            else
            {
                Debug.LogWarning($"Could not install {index}/{levelFiles.Count}: {levelFile}");
            }

            try
            {
                File.Delete(levelFile);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError($"Could not delete level file at {levelFile}");
            }

            index++;
        }

        return loadedLevelJsonFiles;
    }

    public void DeleteLocalLevel(string id)
    {
        if (!LoadedLocalLevels.ContainsKey(id))
        {
            Debug.LogWarning($"Warning: Could not find level {id}");
            return;
        }

        var level = LoadedLocalLevels[id];
        Directory.Delete(Path.GetDirectoryName(level.Path) ?? throw new InvalidOperationException(), true);
        LoadedLocalLevels.Remove(level.Id);
        loadedPaths.Remove(level.Path);
        OnLevelDeleted.Invoke(level);
    }

    public Promise<LevelMeta> FetchLevelMeta(string levelId, bool updateLocal = false)
    {
        return new Promise<LevelMeta>((resolve, reject) =>
        {
            RestClient.Get<OnlineLevel>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/levels/{levelId}",
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
            }).Then(it =>
            {
                var remoteMeta = it.GenerateLevelMeta();
                if (updateLocal && LoadedLocalLevels.ContainsKey(levelId))
                {
                    var localLevel = LoadedLocalLevels[levelId];
                    if (UpdateLevelMeta(localLevel, remoteMeta))
                    {
                        Debug.Log($"Level meta updated for {localLevel.Id}");
                        OnLevelMetaUpdated.Invoke(localLevel);
                    }
                }

                resolve(remoteMeta);
            }).Catch(error =>
            {
                if (!error.IsNetworkError && (error.StatusCode == 404 || error.StatusCode == 403))
                {
                    if (error.StatusCode == 404)
                    {
                        Debug.Log($"Level {levelId} does not exist on the remote server");
                    } 
                    else 
                    {
                        Debug.Log($"Level {levelId} cannot be accessed on the remote server");
                    }
                }
                else
                {
                    Debug.LogError(error);
                }

                reject(error);
            });
        });
    }

    public async UniTask<bool> UnpackLevelPackage(string packagePath, string destFolder)
    {
        const int bufferSize = 256 * 1024;
        ZipStrings.CodePage = Encoding.UTF8.CodePage;
        try
        {
            Directory.CreateDirectory(destFolder);
        }
        catch (Exception error)
        {
            Debug.LogError("Failed to create level folder.");
            Debug.LogError(error);
            return false;
        }

        var fileName = Path.GetFileName(packagePath);
        var zipFileData = File.ReadAllBytes(packagePath);
        using (var fileStream = new MemoryStream())
        {
            ZipFile zipFile;
            try
            {
                fileStream.Write(zipFileData, 0, zipFileData.Length);
                fileStream.Flush();
                fileStream.Seek(0, SeekOrigin.Begin);

                zipFile = new ZipFile(fileStream);

                foreach (ZipEntry entry in zipFile)
                {
                    // Loop through all files to ensure the zip is valid
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Cannot read {fileName}. Is it a valid .zip archive file?");
                Debug.LogError(e.Message);
                return false;
            }

            foreach (ZipEntry entry in zipFile)
            {
                var targetFile = Path.Combine(destFolder, entry.Name);
                if (entry.Name.Contains("__MACOSX")) continue; // Fucking macOS...
                Debug.Log("Extracting " + entry.Name + "...");

                try
                {
                    var outputFile = File.Create(targetFile);
                    using (outputFile)
                    {
                        if (entry.Size <= 0) continue;
                        var zippedStream = zipFile.GetInputStream(entry);
                        var dataBuffer = new byte[bufferSize];

                        int readBytes;
                        while ((readBytes = zippedStream.Read(dataBuffer, 0, bufferSize)) > 0)
                        {
                            outputFile.Write(dataBuffer, 0, readBytes);
                            outputFile.Flush();
                            await UniTask.Yield(); // Prevent blocking main thread
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Cannot extract {entry.Name} from {fileName}. Is it a valid .zip archive file?");
                    Debug.LogError(e.Message);
                    return false;
                }
            }
        }

        return true;
    }

    public async UniTask<List<Level>> LoadAllInDirectory(string directory = default, bool clearExisting = true)
    {
        if (clearExisting)
        {
            LoadedLocalLevels.Clear();
            loadedPaths.Clear();
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalCoverThumbnail);
        }

        if (directory == default) directory = Context.DataPath;
        var jsonPaths = Directory.EnumerateDirectories(directory)
            .SelectMany(it => Directory.EnumerateFiles(it, "level.json"))
            .ToList();
        Debug.Log($"Found {jsonPaths.Count} levels");

        return await LoadFromMetadataFiles(jsonPaths);
    }

    public async UniTask<List<Level>> LoadFromMetadataFiles(List<string> jsonPaths, bool forceReload = false)
    {
        var results = new List<Level>();
        int index;
        for (index = 0; index < jsonPaths.Count; index++)
        {
            var jsonPath = jsonPaths[index];
            try
            {
                if (!forceReload && loadedPaths.Contains(jsonPath))
                {
                    Debug.LogWarning($"{jsonPath} is already loaded");
                    Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count} from {jsonPath}");
                    continue;
                }

                FileInfo info;
                try
                {
                    info = new FileInfo(jsonPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    Debug.LogWarning($"{jsonPath} could not be read");
                    Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count} from {jsonPath}");
                    continue;
                }

                if (info.Directory == null) continue;

                var path = info.Directory.FullName + Path.DirectorySeparatorChar;
                Debug.Log($"Loading {index + 1}/{jsonPaths.Count} from {path}");

                var meta = JsonConvert.DeserializeObject<LevelMeta>(File.ReadAllText(jsonPath));

                if (meta == null)
                {
                    Debug.LogWarning($"Invalid level.json at {jsonPath}");
                    Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count} from {path}");
                    continue;
                }
                
                OnLevelLoadProgress.Invoke(meta.id, index + 1, jsonPaths.Count);

                // Sort charts
                meta.SortCharts();

                // Reject invalid level meta
                if (!meta.Validate())
                {
                    Debug.LogWarning($"Invalid metadata in level.json at {jsonPath}");
                    Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count} from {path}");
                    continue;
                }


                var isLibrary = Context.Library.Levels.ContainsKey(meta.id);
                DateTime addedDate;
                if (isLibrary)
                {
                    addedDate = Context.Library.Levels[meta.id].addedDate;
                }
                else
                {
                    addedDate = Context.LocalPlayer.GetAddedDate(meta.id);
                    if (addedDate == default)
                    {
                        addedDate = info.LastWriteTimeUtc;
                        Context.LocalPlayer.SetAddedDate(meta.id, addedDate);
                    }
                }
                
                var level = new Level(path, 
                    isLibrary,
                    meta,
                    addedDate,
                    Context.LocalPlayer.GetLastPlayedDate(meta.id)
                );

                LoadedLocalLevels[meta.id] = level;
                loadedPaths.Add(jsonPath);
                results.Add(level);

                if (!File.Exists(level.Path + CoverThumbnailFilename))
                {
                    var thumbnailPath = "file://" + level.Path + level.Meta.background.path;

                    using (var request = UnityWebRequest.Get(thumbnailPath))
                    {
                        await request.SendWebRequest();
                        if (request.isNetworkError || request.isHttpError)
                        {
                            Debug.LogWarning(request.error);
                            Debug.LogWarning($"Cannot get background texture from {thumbnailPath}");
                            Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count}: {meta.id} ({path})");
                            continue;
                        }

                        var coverTexture = request.downloadHandler.data.ToTexture2D();
                        if (coverTexture == null)
                        {
                            Debug.LogWarning(request.error);
                            Debug.LogWarning($"Cannot get background texture from {thumbnailPath}");
                            Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count}: {meta.id} ({path})");
                            continue;
                        }

                        var croppedTexture = TextureScaler.FitCrop(coverTexture, Context.ThumbnailWidth,
                            Context.ThumbnailHeight);
                        var bytes = croppedTexture.EncodeToJPG();
                        Object.Destroy(coverTexture);
                        Object.Destroy(croppedTexture);

                        await UniTask.DelayFrame(0); // Reduce load to prevent crash

                        try
                        {
                            File.WriteAllBytes(level.Path + CoverThumbnailFilename, bytes);
                            Debug.Log($"Thumbnail generated {index + 1}/{jsonPaths.Count}: {level.Id} ({thumbnailPath})");
                            
                            await UniTask.DelayFrame(0); // Reduce load to prevent crash
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(e);
                            Debug.LogWarning($"Could not write to {level.Path + CoverThumbnailFilename}");
                            Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count} from {jsonPath}");
                        }
                    }
                }

                Debug.Log($"Loaded {index + 1}/{jsonPaths.Count}: {meta.id}");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError($"Unexpected error while loading from {jsonPath}");
                Debug.LogWarning($"Skipped {index + 1}/{jsonPaths.Count} from {jsonPath}");
            }
        }

        return results;
    }

    private bool UpdateLevelMeta(Level level, LevelMeta meta)
    {
        var local = level.Meta;
        var remote = meta;

        var updated = false;
        if (local.version > remote.version)
        {
            Debug.Log($"Local version {local.version} > {remote.version}");
            return false;
        }

        if (local.schema_version != remote.schema_version)
        {
            local.schema_version = remote.schema_version;
            updated = true;
        }

        if (remote.title != null && local.title != remote.title)
        {
            local.title = remote.title;
            updated = true;
        }

        if (remote.title_localized != null && local.title_localized != remote.title_localized)
        {
            local.title_localized = remote.title_localized;
            updated = true;
        }

        if (remote.artist != null && local.artist != remote.artist)
        {
            local.artist = remote.artist;
            updated = true;
        }

        if (remote.artist_localized != null && local.artist_localized != remote.artist_localized)
        {
            local.artist_localized = remote.artist_localized;
            updated = true;
        }

        if (remote.artist_source != null && local.artist_source != remote.artist_source)
        {
            local.artist_source = remote.artist_source;
            updated = true;
        }

        if (remote.illustrator != null && local.illustrator != remote.illustrator)
        {
            local.illustrator = remote.illustrator;
            updated = true;
        }

        if (remote.illustrator_source != null && local.illustrator_source != remote.illustrator_source)
        {
            local.illustrator_source = remote.illustrator_source;
            updated = true;
        }

        if (remote.charter != null && local.charter != remote.charter)
        {
            local.charter = remote.charter;
            updated = true;
        }

        foreach (var type in new List<string> {LevelMeta.Easy, LevelMeta.Hard, LevelMeta.Extreme})
        {
            if (remote.GetChartSection(type) != null && local.GetChartSection(type) != null &&
                local.GetChartSection(type).difficulty != remote.GetChartSection(type).difficulty)
            {
                local.GetChartSection(type).difficulty = remote.GetChartSection(type).difficulty;
                updated = true;
            }
        }

        if (updated)
        {
            File.WriteAllText($"{level.Path}/level.json", JsonConvert.SerializeObject(local));
        }

        return updated;
    }

    public void DownloadAndUnpackLevelDialog(
        Level level,
        string directory = default,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action<Level> onUnpackSucceeded = default,
        Action onUnpackFailed = default
    )
    {
        if (!Context.OnlinePlayer.IsAuthenticated)
        {
            Toast.Next(Toast.Status.Failure, "TOAST_SIGN_IN_REQUIRED".Get());
            return;
        }
        if (directory == default) directory = Context.DataPath;
        if (onDownloadSucceeded == default) onDownloadSucceeded = () => { };
        if (onDownloadAborted == default) onDownloadAborted = () => { };
        if (onDownloadFailed == default) onDownloadFailed = () => { };
        if (onUnpackSucceeded == default) onUnpackSucceeded = _ => { };
        if (onUnpackFailed == default) onUnpackFailed = () => { };

        var dialog = Dialog.Instantiate();
        dialog.Message = "DIALOG_DOWNLOADING".Get();
        dialog.UseProgress = true;
        dialog.UsePositiveButton = false;
        dialog.UseNegativeButton = allowAbort;

        ulong downloadedSize;
        var totalSize = 0UL;
        var downloading = false;
        var aborted = false;
        var targetFile = $"{Application.temporaryCachePath}/Downloads/{level.Id}.cytoidlevel";
        var destFolder = $"{directory}/{level.Id}";

        if (level.IsLocal)
        {
            // Write to the local folder instead
            destFolder = level.Path;
        }

        // Download detail first, then package
        RequestHelper req;
        var downloadHandler = new DownloadHandlerFile(targetFile)
        {
            removeFileOnAbort = true
        };
        RestClient.Get<OnlineLevel>(req = new RequestHelper
        {
            Uri = $"{Context.ApiUrl}/levels/{level.Id}",
            Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
        }).Then(it =>
        {
            if (aborted)
            {
                throw new OperationCanceledException();
            }

            totalSize = (ulong) it.size;
            downloading = true;
            Debug.Log("Package path: " + level.PackagePath);
            // Get resources
            return RestClient.Get<OnlineLevelResources>(req = new RequestHelper
            {
                Uri = level.PackagePath,
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
            });
        }).Then(res =>
        {
            if (aborted)
            {
                throw new OperationCanceledException();
            }

            Debug.Log("Asset path: " + res.package);
            // Start download
            return RestClient.Get(req = new RequestHelper
            {
                Uri = res.package,
                DownloadHandler = downloadHandler,
                WillParseBody = false
            });
        }).Then(async res =>
        {
            downloading = false;

            try
            {
                onDownloadSucceeded();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            dialog.OnNegativeButtonClicked = it => { };
            dialog.UseNegativeButton = false;
            dialog.Progress = 0;
            dialog.Message = "DIALOG_UNPACKING".Get();
            DOTween.To(() => dialog.Progress, value => dialog.Progress = value, 1f, 1f).SetEase(Ease.OutCubic);

            var success = await Context.LevelManager.UnpackLevelPackage(targetFile, destFolder);
            if (success)
            {
                try
                {
                    level =
                        (await Context.LevelManager.LoadFromMetadataFiles(new List<string> {destFolder + "/level.json"}, true))
                        .First();
                    onUnpackSucceeded(level);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    onUnpackFailed();
                }
            }
            else
            {
                try
                {
                    onUnpackFailed();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            dialog.Close();
            File.Delete(targetFile);
        }).Catch(error =>
        {
            if (aborted || error is OperationCanceledException || (req != null && req.IsAborted))
            {
                try
                {
                    onDownloadAborted();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            else
            {
                Debug.LogError(error);
                try
                {
                    onDownloadFailed();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
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
                it.Message = "DIALOG_DOWNLOADING_X_Y".Get(downloadedSize.ToHumanReadableFileSize(),
                    totalSize.ToHumanReadableFileSize());
            }
            else
            {
                it.Message = "DIALOG_DOWNLOADING".Get();
            }
        });
        if (allowAbort)
        {
            dialog.OnNegativeButtonClicked = it =>
            {
                aborted = true;
                req?.Abort();
            };
        }

        dialog.Open();
    }
    
}

public class LevelEvent : UnityEvent<Level>
{
}

public class LevelInstallProgressEvent : UnityEvent<string, int, int> // Filename, current, total
{
}

public class LevelLoadProgressEvent : UnityEvent<string, int, int> // Level ID, current, total. Note: may NOT be continuous!
{
}