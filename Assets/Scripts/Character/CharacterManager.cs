using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using RSG;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class CharacterManager
{
    public ActiveCharacterSetEvent OnActiveCharacterSet = new ActiveCharacterSetEvent();

    public string SelectedCharacterId
    {
        get => Context.Player.Settings.ActiveCharacterId ?? "Sayaka";
        set
        {
            Context.Player.Settings.ActiveCharacterId = value;
            Context.Player.SaveSettings();
        }
    }

    public string ActiveCharacterBundleId { get; private set; }
    private AssetBundle activeCharacterAssetBundle;
    private GameObject activeCharacterGameObject;

    public CharacterAsset GetActiveCharacterAsset() => activeCharacterGameObject.GetComponent<CharacterAsset>();

    public async UniTask<CharacterAsset> SetActiveCharacter(string id)
    {
        if (id == null) throw new ArgumentNullException();
        var bundleId = "character_" + id.ToLower();
        if (ActiveCharacterBundleId == bundleId) return activeCharacterGameObject.GetComponent<CharacterAsset>();

        if (!Context.BundleManager.IsCached(bundleId))
        {
            Debug.LogWarning($"Character {bundleId} is not cached");
            return null;
        }
        
        var characterBundle = await Context.BundleManager.LoadCachedBundle(bundleId);
        if (characterBundle == null)
        {
            Debug.LogWarning($"Downloaded asset {bundleId} does not exist");
            return null;
        }

        if (activeCharacterAssetBundle != null)
        {
            // Delay the release to allow LoopAudioPlayer to transition between character songs
            var currentGameObject = activeCharacterGameObject;
            var currentBundleId = ActiveCharacterBundleId;
            Run.After(2.0f, () =>
            {
                UnityEngine.Object.Destroy(currentGameObject);
                Context.BundleManager.Release(currentBundleId);
            });
        }

        activeCharacterAssetBundle = characterBundle;
        SelectedCharacterId = id;
        ActiveCharacterBundleId = bundleId;
        
        // Instantiate the GameObject
        var loader = activeCharacterAssetBundle.LoadAssetAsync<GameObject>("Character");
        await loader;
        activeCharacterGameObject = UnityEngine.Object.Instantiate((GameObject) loader.asset);

        var characterAsset = activeCharacterGameObject.GetComponent<CharacterAsset>();
        OnActiveCharacterSet.Invoke(characterAsset);
        return characterAsset;
    }

    public async UniTask<bool> SetSelectedCharacterActive()
    {
        if (CharacterAsset.GetMainBundleId(SelectedCharacterId) == ActiveCharacterBundleId) return true;
        return await SetActiveCharacter(SelectedCharacterId) != null;
    }

    public void UnloadActiveCharacter()
    {
        if (ActiveCharacterBundleId == null) return;
        UnityEngine.Object.Destroy(activeCharacterGameObject);
        Context.BundleManager.Release(ActiveCharacterBundleId);
        activeCharacterGameObject = null;
        activeCharacterAssetBundle = null;
        ActiveCharacterBundleId = null;
    }

    public async UniTask<(bool, bool)> DownloadCharacterAssetDialog(string assetId)
    {
        var success = true;
        var locallyResolved = false;
        await Context.BundleManager.LoadBundle(assetId, false, true, true, false,
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
                Uri = $"{Context.ApiUrl}/characters",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            }).Then(array =>
            {
                var characters = array.ToList();
                // Save to DB
                Context.Database.Let(it =>
                {
                    var col = it.GetCollection<CharacterMeta>("characters");
                    col.DeleteMany(x => true);
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