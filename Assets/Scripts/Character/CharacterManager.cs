using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Events;

public class CharacterManager
{
    public ActiveCharacterSetEvent OnActiveCharacterSet = new ActiveCharacterSetEvent();

    public string SelectedCharacterAssetId
    {
        get => PlayerPrefs.GetString("ActiveCharacter", "Mafu");
        set => PlayerPrefs.SetString("ActiveCharacter", value);
    }

    public string ActiveCharacterAssetId { get; private set; }
    private GameObject activeCharacterGameObject;

    public CharacterAsset GetActiveCharacterAsset() => activeCharacterGameObject.GetComponent<CharacterAsset>();

    public async UniTask<CharacterAsset> SetActiveCharacter(string assetId)
    {
        if (assetId == null) throw new ArgumentNullException();
        if (ActiveCharacterAssetId == assetId) return activeCharacterGameObject.GetComponent<CharacterAsset>();

        var characterGameObject = await Context.RemoteResourceManager.LoadLocalResource(assetId);
        if (characterGameObject == null) return null;

        if (activeCharacterGameObject != null)
        {
            Context.RemoteResourceManager.Release(activeCharacterGameObject);
        }

        activeCharacterGameObject = characterGameObject;
        ActiveCharacterAssetId = SelectedCharacterAssetId = assetId;

        var characterAsset = activeCharacterGameObject.GetComponent<CharacterAsset>();
        OnActiveCharacterSet.Invoke(characterAsset);
        return characterAsset;
    }

    public async UniTask<bool> SetSelectedCharacterActive()
    {
        if (SelectedCharacterAssetId == ActiveCharacterAssetId) return true;
        return await SetActiveCharacter(SelectedCharacterAssetId) != null;
    }

    public void UnloadActiveCharacter()
    {
        Context.RemoteResourceManager.Release(activeCharacterGameObject);
        activeCharacterGameObject = null;
        ActiveCharacterAssetId = null;
    }

    public async UniTask<bool> DownloadCharacterAssetDialog(string assetId)
    {
        var success = true;
        await Context.RemoteResourceManager.DownloadResourceDialog(assetId,
            onDownloadAborted: () => success = false,
            onDownloadFailed: () => success = false);
        return success;
    }
}

public class ActiveCharacterSetEvent : UnityEvent<CharacterAsset>
{
}