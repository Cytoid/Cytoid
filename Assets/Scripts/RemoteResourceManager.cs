using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class RemoteResourceManager
{

    
    
    public async UniTask UpdateCatalog()
    {
        await Addressables.InitializeAsync().Task;
        var list = await Addressables.CheckForCatalogUpdates().Task;
        if (list != null && list.Count > 0)
        {
            await Addressables.UpdateCatalogs(list).Task;
        }
    }

    public async UniTask<GameObject> LoadResource(string key)
    {
        return await Addressables.InstantiateAsync(key).Task;
    }

    public async UniTask DownloadResourceDialog(
        string key,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default
    )
    {
        await LoadResourceDialogImpl(key, false, allowAbort, onDownloadSucceeded, onDownloadAborted,
            onDownloadFailed);
    }

    public async UniTask<GameObject> LoadResourceDialog(
        string key,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default
    )
    {
        return await LoadResourceDialogImpl(key, true, allowAbort, onDownloadSucceeded, onDownloadAborted,
            onDownloadFailed);
    }
    
    private async UniTask<GameObject> LoadResourceDialogImpl(
        string key,
        bool willInstantiate = false,
        bool allowAbort = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default
    )
    {
        Debug.Log($"Requested remote resource {key}");
        
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
            
            var dialog = Dialog.Instantiate();
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

            loader.Completed += _ => completed = true;

            await UniTask.WaitUntil(() => aborted || completed);

            Addressables.Release(loader);
            dialog.Close();
            
            if (completed)
            {
                onDownloadSucceeded();
            }
            else if (!aborted)
            {
                onDownloadFailed();
            }
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