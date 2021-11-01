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
        get => Context.Player.Settings.ActiveCharacterId ?? BuiltInData.DefaultCharacterAssetId;
        set
        {
            Context.Player.Settings.ActiveCharacterId = value;
            Context.Player.SaveSettings();
        }
    }

    public string ActiveCharacterBundleId { get; private set; }
    private AssetBundle activeCharacterAssetBundle;
    private GameObject activeCharacterGameObject;

    private CharacterAsset testCharacterAsset;
    private bool useTestCharacterAsset;

    public CharacterAsset GetActiveCharacterAsset() => useTestCharacterAsset ? testCharacterAsset : activeCharacterGameObject.GetComponent<CharacterAsset>();

    public async UniTask<CharacterAsset> SetActiveCharacter(string id, bool requiresReload = true)
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

        if (activeCharacterAssetBundle != null && requiresReload)
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
        OnActiveCharacterSet.Invoke(characterAsset, requiresReload);

        useTestCharacterAsset = false;
        return characterAsset;
    }

    // For development only.
    public void SetTestActiveCharacter(CharacterAsset asset)
    {
        testCharacterAsset = asset;
        OnActiveCharacterSet.Invoke(asset, true);
        useTestCharacterAsset = true;
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

    public async UniTask<(string, CharacterMeta.ExpData)> FetchSelectedCharacterExp(bool useLocal = false)
    {
        var characterMeta = Context.Database.Let(it =>
        {
            var col = it.GetCollection<CharacterMeta>("characters");
            try
            {
                var result = col.Find(m => m.AssetId == Context.CharacterManager.SelectedCharacterId);
                return result.FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                return null;
            }
        });
        
        if (characterMeta != null)
        {
            if (useLocal || Context.IsOffline())
            {
                return (characterMeta.Name, characterMeta.Exp);
            }

            // Fetch latest exp
            bool? success = null;
            var name = characterMeta.Name;
            CharacterMeta.ExpData exp = null;
            RestClient.Get<CharacterMeta.ExpData>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/characters/{characterMeta.Id}/exp",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            }).Then(data =>
            {
                success = true;
                name = characterMeta.Name;
                exp = data;
                
                // Update DB meta
                Context.Database.Let(it =>
                {
                    var col = it.GetCollection<CharacterMeta>("characters");
                    var localMeta = col.FindOne(meta => meta.Id == characterMeta.Id);
                    localMeta.Exp = data;
                    col.Update(localMeta);
                });
            }).Catch(err =>
            {
                success = false;
                Debug.LogError(err);
            });

            await UniTask.WaitUntil(() => success != null);

            return (name, exp);
        }

        return (null, null);
    }

    public RSG.IPromise<List<CharacterMeta>> GetAvailableCharactersMeta()
    {
        List<CharacterMeta> PostProcess(List<CharacterMeta> result)
        {
            var defaultCharacter = result.FirstOrDefault(it => it.AssetId == BuiltInData.DefaultCharacterAssetId);
            if (defaultCharacter != null) defaultCharacter.Date = DateTimeOffset.MinValue.ToString();
            return result;
        }
        if (Context.IsOnline())
        {
            // Online
            return RestClient.GetArray<CharacterMeta>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/characters/all",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            }).Then(array =>
            {
                var characters = array.ToList();
                
                // // Editor only!
                // if (Application.isEditor)
                // {
                //     characters.RemoveAll(it => MockData.AvailableCharacters.Any(x => it.Id == x.Id));
                //     characters.AddRange(MockData.AvailableCharacters.Where(it => characters.All(x => x.Id != it.Id)));
                // }
                
                // Save to DB
                Context.Database.Let(it =>
                {
                    var col = it.GetCollection<CharacterMeta>("characters");
                    col.DeleteMany(x => true);
                    col.Insert(characters);
                });

                return PostProcess(characters);
            });
        }

        // Offline
        return Context.Database.Let(it =>
        {
            var col = it.GetCollection<CharacterMeta>("characters");
            var result = PostProcess(col.FindAll().ToList());
            return Promise<List<CharacterMeta>>.Resolved(result);
        });
    }
    
}

public class ActiveCharacterSetEvent : UnityEvent<CharacterAsset, bool>
{
}