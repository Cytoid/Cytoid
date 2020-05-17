using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class RemoteAssetManager
{
    
    public async UniTask UpdateCatalog()
    {
        var list = await Addressables.CheckForCatalogUpdates().Task;
        if (list != null && list.Count > 0)
        {
            await Addressables.UpdateCatalogs(list).Task;
        }
    }

    public async UniTask<bool> Exists(string assetId)
    {
        var list = await Addressables.LoadResourceLocationsAsync(assetId).Task;        
        return list != null && list.Count > 0;
    }

    public async UniTask<bool> IsCached(string assetId)
    {
        var totalSize = (ulong) await Addressables.GetDownloadSizeAsync(assetId).Task;
        return totalSize == 0;
    }

    public async UniTask<GameObject> LoadDownloadedAsset(string assetId)
    {
        return await Addressables.InstantiateAsync(assetId).Task;
    }

    public async UniTask DownloadAssetDialog(
        string key,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action onLocallyResolved = default
    )
    {
        await LoadAssetImpl(key, true, false, allowAbort, onDownloadSucceeded, onDownloadAborted,
            onDownloadFailed, onLocallyResolved);
    }

    public async UniTask<GameObject> LoadAssetDialog(
        string key,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action onLocallyResolved = default
    )
    {
        return await LoadAssetImpl(key, true, true, allowAbort, onDownloadSucceeded, onDownloadAborted,
            onDownloadFailed, onLocallyResolved);
    }
    
    public async UniTask DownloadAsset(
        string key,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action onLocallyResolved = default
    )
    {
        await LoadAssetImpl(key, false, false, false, onDownloadSucceeded, onDownloadAborted,
            onDownloadFailed, onLocallyResolved);
    }
    
    public async UniTask<GameObject> LoadAsset(
        string key,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action onLocallyResolved = default
    )
    {
        return await LoadAssetImpl(key, false, true, false, onDownloadSucceeded, onDownloadAborted,
            onDownloadFailed, onLocallyResolved);
    }
    
    private async UniTask<GameObject> LoadAssetImpl(
        string key,
        bool showDialog = false,
        bool willInstantiate = false,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action onLocallyResolved = default
    )
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        Debug.Log($"Requested remote resource {key}");
        
        // TODO: 1) Asset not found? 2) Network exception?
        
        if (onLocallyResolved == default) onLocallyResolved = () => { };
        if (onDownloadSucceeded == default) onDownloadSucceeded = () => { };
        if (onDownloadAborted == default) onDownloadAborted = () => { };
        if (onDownloadFailed == default) onDownloadFailed = () => { };

        // Addressables.ClearDependencyCacheAsync(key);

        var totalSize = (ulong) await Addressables.GetDownloadSizeAsync(key).Task;
        Debug.Log("Download size: " + totalSize);

        var aborted = false;
        var completed = false;
        
        if (totalSize > 0)
        {
            var loader = Addressables.DownloadDependenciesAsync(key);

            Dialog dialog = null;
            if (showDialog)
            {
                dialog = Dialog.Instantiate();
                dialog.Message = "DIALOG_DOWNLOADING".Get();
                dialog.UseProgress = true;
                dialog.UsePositiveButton = false;
                dialog.UseNegativeButton = allowAbort;
                dialog.onUpdate.AddListener(it =>
                {
                    var downloadedSize = (ulong) (totalSize * loader.PercentComplete);
                    it.Progress = downloadedSize * 1.0f / totalSize;
                    it.Message = "DIALOG_DOWNLOADING_X_Y".Get(downloadedSize.ToHumanReadableFileSize(),
                        totalSize.ToHumanReadableFileSize());
                });
                if (allowAbort)
                {
                    dialog.OnNegativeButtonClicked = it =>
                    {
                        aborted = true;
                        onDownloadAborted();
                    };
                }

                dialog.Open();
            }

            loader.Completed += _ => completed = true;

            await UniTask.WaitUntil(() => aborted || completed);

            Addressables.Release(loader);
            if (showDialog) dialog.Close();
            
            if (completed)
            {
                onDownloadSucceeded();
            }
            else if (!aborted)
            {
                onDownloadFailed();
            }
        }
        else
        {
            onLocallyResolved();
        }

        if (aborted) return null;

        return willInstantiate ? await Addressables.InstantiateAsync(key).Task : null;
    }

    public void Release(GameObject gameObject)
    {
        if (gameObject == null) return;
        if (!Addressables.ReleaseInstance(gameObject))
        {
            Debug.LogError("GameObject not instantiated by Addressable!");
        }
    }
    
}