using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DG.Tweening;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using Cysharp.Threading.Tasks;
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

    public async UniTask<List<string>> CopyBuiltInLevelsToDownloads(List<string> levelIds)
    {
        var packagePaths = new List<string>();
        
        // Install all missing training levels that are built in
        foreach (var uid in levelIds)
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

        return packagePaths;
    }

    public async UniTask<List<Level>> LoadOrInstallBuiltInLevels()
    {
        var levels = new List<Level>();
        foreach (var levelId in BuiltInData.BuiltInLevelIds)
        {
            levels.Add(await LoadOrInstallBuiltInLevel(levelId, LevelType.BuiltIn));
        }
        return levels;
    }

    public async UniTask<List<string>> InstallUserCommunityLevels()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            var files = new List<string>();
            var inboxPath = Context.UserDataPath + "/Inbox/";
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
                
                var toPath = Context.UserDataPath + "/" + Path.GetFileName(file);
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
            levelFiles.AddRange(Directory.GetFiles(Context.UserDataPath, "*.cytoidlevel"));
            levelFiles.AddRange(Directory.GetFiles(Context.UserDataPath, "*.cytoidlevel.zip"));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Debug.LogError("Cannot read from data path");
            return new List<string>();
        }

        return await InstallLevels(levelFiles, LevelType.User);
    }

    public async UniTask<List<string>> InstallLevels(List<string> packagePaths, LevelType type)
    {
        var loadedLevelJsonFiles = new List<string>();
        var index = 1;
        foreach (var levelFile in packagePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(levelFile);
            OnLevelInstallProgress.Invoke(fileName, index, packagePaths.Count);

            var destFolder = $"{type.GetDataPath()}/{fileName}";
            if (await UnpackLevelPackage(levelFile, destFolder))
            {
                loadedLevelJsonFiles.Add(destFolder + "/level.json");
                Debug.Log($"Installed {index}/{packagePaths.Count}: {levelFile}");
            }
            else
            {
                Debug.LogWarning($"Could not install {index}/{packagePaths.Count}: {levelFile}");
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
        level.Record.AddedDate = DateTimeOffset.MinValue;
        level.SaveRecord();
        
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
                Headers = Context.OnlinePlayer.GetRequestHeaders()
            }).Then(it =>
            {
                var remoteMeta = it.GenerateLevelMeta();
                if (updateLocal && LoadedLocalLevels.ContainsKey(levelId))
                {
                    var localLevel = LoadedLocalLevels[levelId];
                    localLevel.OnlineLevel = it;
                    if (UpdateLevelMeta(localLevel, remoteMeta))
                    {
                        Debug.Log($"Level meta updated for {localLevel.Id}");
                        OnLevelMetaUpdated.Invoke(localLevel);
                    }
                }

                resolve(remoteMeta);
            }).CatchRequestError(error =>
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

        string fileName;
        try
        {
            fileName = Path.GetFileName(packagePath);
        }
        catch (Exception error)
        {
            Debug.LogError($"Failed to get filename for path {packagePath}.");
            Debug.LogError(error);
            return false;
        }
        byte[] zipFileData;
        try
        {
            zipFileData = File.ReadAllBytes(packagePath);
        }
        catch (Exception error)
        {
            Debug.LogError($"Failed to read bytes from {packagePath}.");
            Debug.LogError(error);
            return false;
        }
       
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
                        while ((readBytes = await zippedStream.ReadAsync(dataBuffer, 0, bufferSize)) > 0)
                        {
                            outputFile.Write(dataBuffer, 0, readBytes);
                            outputFile.Flush();
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

            var info = new FileInfo(destFolder);
            var path = info.FullName + Path.DirectorySeparatorChar;
            Debug.Log($"Removing {path}");
            loadedPaths.Remove(path);

            var coverPath = path + CoverThumbnailFilename;
            Debug.Log($"Search {coverPath}");
            if (File.Exists(coverPath))
            {
                try
                {
                    File.Delete(coverPath);
                    File.Delete(coverPath + ".576.360"); // TODO: Unhardcode this (how?)
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete cover thumbnail: {coverPath}");
                    Debug.LogError(e);
                }
            }
        }

        return true;
    }

    public async UniTask<List<Level>> LoadLevelsOfType(LevelType type)
    {
        try
        {
            Directory.CreateDirectory(type.GetDataPath());
        }
        catch (Exception error)
        {
            Debug.LogError("Failed to create data folder.");
            Debug.LogError(error);
            return new List<Level>();
        }

        var jsonPaths = Directory.EnumerateDirectories(type.GetDataPath())
            .SelectMany(it => Directory.EnumerateFiles(it, "level.json"))
            .ToList();
        Debug.Log($"Found {jsonPaths.Count} levels with type {type}");

        return await LoadFromMetadataFiles(type, jsonPaths);
    }

    public void UnloadLevelsOfType(LevelType type)
    {
        var removals = LoadedLocalLevels.RemoveAll(level => level.Type == type);
        var removedPaths = removals.Select(it => it.Item2.Path).ToHashSet();
        loadedPaths.RemoveWhere(it => removedPaths.Contains(it));
    }

    public void UnloadAllLevels()
    {
        LoadedLocalLevels.Clear();
        loadedPaths.Clear();
        Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalLevelCoverThumbnail);
    }

    public async UniTask<Level> LoadOrInstallBuiltInLevel(string id, LevelType loadType, bool forceInstall = false)
    {
        async UniTask<Level> GetLevel()
        {
            var levels = await LoadFromMetadataFiles(loadType, new List<string>
            {
                $"{loadType.GetDataPath()}/{id}/level.json"
            });
            if (levels.Count > 0) return levels.First();
            return LoadedLocalLevels.ContainsKey(id) ? LoadedLocalLevels[id] : null;
        }

        var level = forceInstall ? null : await GetLevel();

        if (level == null)
        {
            var paths = await Context.LevelManager.CopyBuiltInLevelsToDownloads(new List<string> {id});
            await Context.LevelManager.InstallLevels(paths, loadType);
            level = await GetLevel();
        }

        return level;
    }

    public async UniTask<List<Level>> LoadFromMetadataFiles(LevelType type, List<string> jsonPaths, bool forceReload = false)
    {
        var lowMemory = false;
        Application.lowMemory += OnLowMemory;
        void OnLowMemory()
        {
            lowMemory = true;
        }
        var loadedCount = 0;
        var tasks = new List<UniTask>();
        var results = new List<Level>();
        int index;
        for (index = 0; index < jsonPaths.Count; index++)
        {
            var loadIndex = index;
            async UniTask LoadLevel()
            {
                var timer = new BenchmarkTimer($"Level loader ({loadIndex + 1} / {jsonPaths.Count})") {Enabled = false};
                var jsonPath = jsonPaths[loadIndex];
                try
                {
                    FileInfo info;
                    try
                    {
                        info = new FileInfo(jsonPath);
                        if (info.Directory == null)
                        {
                            throw new FileNotFoundException(info.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                        Debug.LogWarning($"{jsonPath} could not be read");
                        Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {jsonPath}");
                        return;
                    }

                    var path = info.Directory.FullName + Path.DirectorySeparatorChar;

                    if (!forceReload && loadedPaths.Contains(path))
                    {
                        Debug.LogWarning($"Level from {path} is already loaded");
                        Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {path}");
                        return;
                    }

                    Debug.Log($"Loading {loadIndex + 1}/{jsonPaths.Count} from {path}");

                    if (!File.Exists(jsonPath))
                    {
                        Debug.LogWarning($"level.json not found at {jsonPath}");
                        Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {path}");
                        return;
                    }

                    await UniTask.SwitchToThreadPool();
                    var meta = JsonConvert.DeserializeObject<LevelMeta>(File.ReadAllText(jsonPath));
                    await UniTask.SwitchToMainThread();
                    
                    timer.Time("Deserialization");

                    if (meta == null)
                    {
                        Debug.LogWarning($"Invalid level.json at {jsonPath}");
                        Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {path}");
                        return;
                    }

                    if (type != LevelType.Temp && LoadedLocalLevels.ContainsKey(meta.id))
                    {
                        if (LoadedLocalLevels[meta.id].Type == LevelType.Tier && type == LevelType.User)
                        {
                            Debug.LogWarning($"Community level cannot override tier level");
                            Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {path}");
                            return;
                        }
                        if (LoadedLocalLevels[meta.id].Meta.version > meta.version)
                        {
                            Debug.LogWarning($"Level to load has smaller version than loaded level");
                            Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {path}");
                            return;
                        }
                        loadedPaths.Remove(LoadedLocalLevels[meta.id].Path);
                    }

                    // Sort charts
                    meta.SortCharts();

                    // Reject invalid level meta
                    if (!meta.Validate())
                    {
                        Debug.LogWarning($"Invalid metadata in level.json at {jsonPath}");
                        Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {path}");
                        return;
                    }

                    timer.Time("Validate");

                    var db = Context.Database;
                    await UniTask.SwitchToThreadPool();
                    var level = Level.FromLocal(path, type, meta, db);
                    var record = level.Record;
                    if (record.AddedDate == DateTimeOffset.MinValue)
                    {
                        record.AddedDate = Context.Library.Levels.ContainsKey(level.Id) 
                            ? Context.Library.Levels[level.Id].Date 
                            : info.LastWriteTimeUtc;
                        level.SaveRecord();
                    }
                    await UniTask.SwitchToMainThread();
                    timer.Time("LevelRecord");

                    if (type != LevelType.Temp)
                    {
                        LoadedLocalLevels[meta.id] = level;
                        loadedPaths.Add(path);

                        // Generate thumbnail
                        if (!File.Exists(level.Path + CoverThumbnailFilename))
                        {
                            var thumbnailPath = "file://" + level.Path + level.Meta.background.path;

                            if (lowMemory)
                            {
                                // Give up
                                Debug.LogWarning($"Low memory!");
                                Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {jsonPath}");
                                return;
                            }

                            using (var request = UnityWebRequest.Get(thumbnailPath))
                            {
                                await request.SendWebRequest();
                                if (request.isNetworkError || request.isHttpError)
                                {
                                    Debug.LogWarning(request.error);
                                    Debug.LogWarning($"Cannot get background texture from {thumbnailPath}");
                                    Debug.LogWarning(
                                        $"Skipped generating thumbnail for {loadIndex + 1}/{jsonPaths.Count}: {meta.id} ({path})");
                                    return;
                                }

                                var coverTexture = request.downloadHandler.data.ToTexture2D();
                                if (coverTexture == null)
                                {
                                    Debug.LogWarning(request.error);
                                    Debug.LogWarning($"Cannot get background texture from {thumbnailPath}");
                                    Debug.LogWarning(
                                        $"Skipped generating thumbnail for {loadIndex + 1}/{jsonPaths.Count}: {meta.id} ({path})");
                                    return;
                                }

                                if (lowMemory)
                                {
                                    // Give up
                                    Object.Destroy(coverTexture);
                                    Debug.LogWarning($"Low memory!");
                                    Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {jsonPath}");
                                    return;
                                }

                                var croppedTexture = TextureScaler.FitCrop(coverTexture, Context.LevelThumbnailWidth,
                                    Context.LevelThumbnailHeight);

                                if (lowMemory)
                                {
                                    // Give up
                                    Object.Destroy(coverTexture);
                                    Object.Destroy(croppedTexture);
                                    Debug.LogWarning($"Low memory!");
                                    Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {jsonPath}");
                                    return;
                                }

                                var bytes = croppedTexture.EncodeToJPG();
                                Object.Destroy(coverTexture);
                                Object.Destroy(croppedTexture);

                                await UniTask.DelayFrame(0); // Reduce load to prevent crash

                                try
                                {
                                    File.WriteAllBytes(level.Path + CoverThumbnailFilename, bytes);
                                    Debug.Log(
                                        $"Thumbnail generated {loadIndex + 1}/{jsonPaths.Count}: {level.Id} ({thumbnailPath})");

                                    await UniTask.DelayFrame(0); // Reduce load to prevent crash
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning(e);
                                    Debug.LogWarning($"Could not write to {level.Path + CoverThumbnailFilename}");
                                    Debug.LogWarning(
                                        $"Skipped generating thumbnail for {loadIndex + 1}/{jsonPaths.Count} from {jsonPath}");
                                }
                            }

                            timer.Time("Generate thumbnail");
                        }
                    }
                    
                    results.Add(level);
                    OnLevelLoadProgress.Invoke(meta.id, ++loadedCount, jsonPaths.Count);
                    Debug.Log($"Loaded {loadIndex + 1}/{jsonPaths.Count}: {meta.id} ");
                    timer.Time("OnLevelLoadProgressEvent");
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Debug.LogError($"Unexpected error while loading from {jsonPath}");
                    Debug.LogWarning($"Skipped {loadIndex + 1}/{jsonPaths.Count} from {jsonPath}");
                }
                
                timer.Time();
            }

            tasks.Add(LoadLevel());
        }

        await UniTask.WhenAll(tasks);
        Application.lowMemory -= OnLowMemory;
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
            File.WriteAllText($"{level.Path.Replace("file://", "")}/level.json", JsonConvert.SerializeObject(local));
        }

        return updated;
    }

    public void DownloadAndUnpackLevelDialog(
        Level level,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action<Level> onUnpackSucceeded = default,
        Action onUnpackFailed = default,
        bool forceInternational = false
    )
    {
        if (!Context.OnlinePlayer.IsAuthenticated)
        {
            Toast.Next(Toast.Status.Failure, "TOAST_SIGN_IN_REQUIRED".Get());
            return;
        }
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
        var targetFile = $"{Application.temporaryCachePath}/Downloads/{level.Id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.cytoidlevel";
        var destFolder = $"{level.Type.GetDataPath()}/{level.Id}";
        
        try
        {
            Directory.CreateDirectory(destFolder);
        }
        catch (Exception error)
        {
            Debug.LogError("Failed to create level folder.");
            Debug.LogError(error);
            onDownloadFailed();
            return;
        }

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
            Uri = $"{(forceInternational ? CdnRegion.International.GetApiUrl() : Context.ApiUrl)}/levels/{level.Id}",
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(it =>
        {
            if (aborted)
            {
                throw new OperationCanceledException();
            }

            totalSize = (ulong) it.Size;
            downloading = true;
            var packagePath = (forceInternational ? CdnRegion.International : Context.CdnRegion).GetPackageUrl(level.Id);
            Debug.Log($"Package path: {packagePath}");
            // Get resources
            return RestClient.Post<OnlineLevelResources>(req = new RequestHelper
            {
                Uri = packagePath,
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                BodyString = SecuredOperations.WithCaptcha(new { }).ToString(),
                EnableDebug = true
            });
        }).Then(res =>
        {
            if (aborted)
            {
                throw new OperationCanceledException();
            }

            Debug.Log($"Asset path: {res.package}");
            // Start download
            // TODO: Change to HttpClient
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
                        (await Context.LevelManager.LoadFromMetadataFiles(level.Type, new List<string> {destFolder + "/level.json"}, true))
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
                if (!forceInternational 
                    && error is RequestException requestException
                    && requestException.StatusCode < 400 && requestException.StatusCode >= 500)
                {
                     DownloadAndUnpackLevelDialog(
                         level,
                         allowAbort,
                         onDownloadSucceeded,
                         onDownloadAborted,
                         onDownloadFailed,
                         onUnpackSucceeded,
                         onUnpackFailed,
                         true
                     );
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
            }

            dialog.Close();
        });

        dialog.onUpdate.AddListener(it =>
        {
            if (!downloading) return;
            if (req.Request == null)
            {
                // Download was cancelled due to Unity
                Debug.LogError("UWR download failed");
                try
                {
                    onDownloadFailed();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                dialog.Close();
                return;
            }
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

                dialog.Close();
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