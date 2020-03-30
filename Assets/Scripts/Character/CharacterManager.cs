using System;
using System.Collections.Generic;
using System.Linq;
using E7.Introloop;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

public class CharacterManager
{
    public ActiveCharacterSetEvent OnActiveCharacterSet = new ActiveCharacterSetEvent();

    public string SelectedCharacterAssetId
    {
        get => PlayerPrefs.GetString("ActiveCharacter", "Hancho");
        set
        {
            if (value == null) PlayerPrefs.DeleteKey("ActiveCharacter");
            else PlayerPrefs.SetString("ActiveCharacter", value);
        }
    }

    public string ActiveCharacterAssetId { get; private set; }
    private GameObject activeCharacterGameObject;

    public CharacterAsset GetActiveCharacterAsset() => activeCharacterGameObject.GetComponent<CharacterAsset>();

    public async UniTask<CharacterAsset> SetActiveCharacter(string assetId)
    {
        if (assetId == null) throw new ArgumentNullException();
        if (ActiveCharacterAssetId == assetId) return activeCharacterGameObject.GetComponent<CharacterAsset>();

        if (!await Context.RemoteAssetManager.Exists(assetId))
        {
            Debug.LogWarning($"Asset {assetId} does not exist");
            //return null;
        }
        
        var characterGameObject = await Context.RemoteAssetManager.LoadDownloadedAsset(assetId);
        if (characterGameObject == null)
        {
            Debug.LogWarning($"Downloaded asset {assetId} does not exist");
            return null;
        }

        if (activeCharacterGameObject != null)
        {
            // Delay the release to allow LoopAudioPlayer to transition between character songs
            var currentGameObject = activeCharacterGameObject;
            Run.After(2.0f, () => Context.RemoteAssetManager.Release(currentGameObject));
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
        Context.RemoteAssetManager.Release(activeCharacterGameObject);
        activeCharacterGameObject = null;
        ActiveCharacterAssetId = null;
    }

    public async UniTask<(bool, bool)> DownloadCharacterAssetDialog(string assetId)
    {
        var success = true;
        var locallyResolved = false;
        await Context.RemoteAssetManager.DownloadAssetDialog(assetId,
            onDownloadAborted: () => success = false,
            onDownloadFailed: () => success = false,
            onLocallyResolved: () => locallyResolved = true);
        return (success, locallyResolved);
    }

    public RSG.IPromise<List<CharacterMeta>> GetAvailableCharactersMeta()
    {
        if (Context.IsOnline())
        {
            // Online
            return RestClient.GetArray<CharacterMeta>(new RequestHelper
            {
                Uri = $"{Context.ServicesUrl}/characters",
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders(),
                EnableDebug = true
            }).Then(characters =>
            {
                // Save to DB
                Context.Database.Let(it =>
                {
                    it.DropCollection("characters");
                    var col = it.GetCollection<CharacterMeta>("characters");
                    col.Insert(characters);
                });
                
                return characters.ToList();
            });
        }

        // Offline
        return Context.Database.Let(it =>
        {
            var col = it.GetCollection<CharacterMeta>("characters");
            var result = col.FindAll().ToList();
            return Promise<List<CharacterMeta>>.Resolved(result);
        });
    }
    
}

public class ActiveCharacterSetEvent : UnityEvent<CharacterAsset>
{
}