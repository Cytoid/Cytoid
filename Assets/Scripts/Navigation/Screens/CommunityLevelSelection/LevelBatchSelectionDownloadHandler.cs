using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Proyecto26;
using UnityEngine.Events;
using UnityEngine.UI;

public class LevelBatchSelectionDownloadHandler : LevelBatchSelection, LevelCardEventHandler
{
    private static readonly Dictionary<string, ulong> CachedDownloadSizes = new Dictionary<string, ulong>();

    public UnityEvent OnEnterBatchSelection = new UnityEvent();
    public UnityEvent OnLeaveBatchSelection = new UnityEvent();

    public Text batchActionBarMessage;
    
    public bool IsBatchSelectingLevels { get; private set; }
    public Dictionary<string, Level> BatchSelectedLevels { get; } = new Dictionary<string, Level>();
    public LevelBatchAction LevelBatchAction { get; } = LevelBatchAction.Download;

    public void EnterBatchSelection()
    {
        if (IsBatchSelectingLevels) return;
        
        IsBatchSelectingLevels = true;
        OnEnterBatchSelection.Invoke();
    }

    public void LeaveBatchSelection()
    {
        if (!IsBatchSelectingLevels) return;
        
        IsBatchSelectingLevels = false;
        BatchSelectedLevels.Clear();
        OnLeaveBatchSelection.Invoke();
    }
    
    public async void DownloadBatchSelection()
    {
        var levelsToDownload = new List<Level>(BatchSelectedLevels.Values);
        LeaveBatchSelection();

        for (var index = 0; index < levelsToDownload.Count; index++)
        {
            var levelToDownload = levelsToDownload[index];
            var aborted = false;
            var succeeded = false;
            var failed = false;
            Context.LevelManager.DownloadAndUnpackLevelDialog(
                levelToDownload,
                allowAbort: true,
                onDownloadAborted: () =>
                {
                    aborted = true;
                    Toast.Enqueue(Toast.Status.Success, "TOAST_DOWNLOAD_CANCELLED".Get());
                },
                onDownloadFailed: () =>
                {
                    failed = true;
                    Toast.Next(Toast.Status.Failure, "TOAST_COULD_NOT_DOWNLOAD_LEVEL".Get());
                },
                onUnpackFailed: () =>
                {
                    failed = true;
                    Toast.Next(Toast.Status.Failure, "TOAST_COULD_NOT_UNPACK_LEVEL".Get());
                },
                onUnpackSucceeded: level =>
                {
                    succeeded = true;
                    levelToDownload.CopyFrom(level);
                },
                batchDownloading: true,
                batchDownloadCurrent: index + 1,
                batchDownloadTotal: levelsToDownload.Count
            );
            await UniTask.WaitUntil(() => aborted || succeeded || failed);
            if (aborted) return;
        }

        Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_DOWNLOADED_X_LEVELS".Get(levelsToDownload.Count));
    }
    
    private DateTime lastFetchDownloadSizeDateTime = DateTime.UtcNow;

    public async UniTask<ulong> GetDownloadEstimatedSize()
    {
        lastFetchDownloadSizeDateTime = DateTime.UtcNow;
        
        foreach (var (levelId, level) in BatchSelectedLevels)
        {
            if (CachedDownloadSizes.ContainsKey(levelId)) continue;
            var proceed = false;
            var cancelled = false;
            var dateTime = lastFetchDownloadSizeDateTime;
            RestClient.Get<OnlineLevel>(new RequestHelper 
            {
                Uri = $"{Context.ApiUrl}/levels/{level.Id}",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true,
                Timeout = 5
            }).Then(it =>
            {
                CachedDownloadSizes[levelId] = (ulong) it.Size;
            }).Finally(() =>
            {
                proceed = true;
                if (dateTime != lastFetchDownloadSizeDateTime) cancelled = true;
            });
            await UniTask.WaitUntil(() => proceed);
            if (cancelled) return 0;
        }

        return (ulong) BatchSelectedLevels
            .Select(it => CachedDownloadSizes.ContainsKey(it.Key) ? CachedDownloadSizes[it.Key] : 0)
            .Sum(it => (long) it);
    }
    
    public bool OnLevelCardPressed(LevelView view)
    {
        if (!IsBatchSelectingLevels)
        {
            if (Context.IsOffline() && !view.Level.IsLocal)
            {
                Dialog.PromptAlert("DIALOG_OFFLINE_LEVEL_NOT_AVAILABLE".Get());
                return false;
            }

            return true;
        }
        else
        {
            if (Context.LevelManager.LoadedLocalLevels.ContainsKey(view.Level.Id))
            {
                Context.AudioManager.Get("ActionError").Play();
                Context.Haptic(HapticTypes.Warning, true);
                return false;
            }
            
            Context.AudioManager.Get("Navigate1").Play();
            Context.Haptic(HapticTypes.Selection, true);
            
            if (BatchSelectedLevels.ContainsKey(view.Level.Id))
            {
                BatchSelectedLevels.Remove(view.Level.Id);

                if (!BatchSelectedLevels.Any())
                {
                    LeaveBatchSelection();
                }
            }
            else
            {
                BatchSelectedLevels[view.Level.Id] = view.Level;
            }
            
            UpdateBatchSelectionText();
            return false;
        }
    }
    
    public void OnLevelCardLongPressed(LevelView view)
    {
        if (!IsBatchSelectingLevels)
        {
            Context.AudioManager.Get("Navigate1").Play();
            Context.Haptic(HapticTypes.Selection, true);

            if (!Context.LevelManager.LoadedLocalLevels.ContainsKey(view.Level.Id))
            {
                BatchSelectedLevels[view.Level.Id] = view.Level;
            }

            EnterBatchSelection();
            UpdateBatchSelectionText();
        }
        else
        {
            OnLevelCardPressed(view);
        }
    }
    
    public async void UpdateBatchSelectionText()
    {
        if (batchActionBarMessage == null) return;
        
        batchActionBarMessage.text = (BatchSelectedLevels.Count == 1 ? "LEVEL_SELECT_SELECTED_X_LEVEL" : "LEVEL_SELECT_SELECTED_X_LEVELS").Get(BatchSelectedLevels.Count);
        
        // Estimate download size
        var downloadSize = await GetDownloadEstimatedSize();
        if (downloadSize == 0) return;
        
        batchActionBarMessage.text =
            (BatchSelectedLevels.Count == 1 ? "LEVEL_SELECT_SELECTED_X_LEVEL" : "LEVEL_SELECT_SELECTED_X_LEVELS").Get(BatchSelectedLevels.Count)
            + "LEVEL_SELECT_ESTIMATED_DOWNLOAD_SIZE_X".Get(downloadSize.ToHumanReadableFileSize());
    }
}
