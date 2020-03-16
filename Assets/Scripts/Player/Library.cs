using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.Events;

public class Library
{

    public readonly Dictionary<string, LibraryLevel> Levels = new Dictionary<string, LibraryLevel>();
    public readonly LibraryFetchEvent OnLibraryFetch = new LibraryFetchEvent();

    public void LoadFromLocal()
    {
        Levels.Clear();
        
        var libraryJsonPath = Path.Combine(Application.temporaryCachePath, "library.json");
        FileInfo info;
        try
        {
            info = new FileInfo(libraryJsonPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            Debug.LogWarning($"{libraryJsonPath} not found or could not be read");
            return;
        }

        if (info.Directory == null) return;

        var list = JsonConvert.DeserializeObject<List<LibraryLevel>>(File.ReadAllText(libraryJsonPath));
        if (list == null)
        {
            Debug.LogError("library.json is corrupt!");
            return;
        }
        
        OnLevelsLoaded(list);
    }

    public Promise Fetch()
    {
        return new Promise((resolve, reject) =>
        {
            RestClient.Get<OnlineLevel>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/levels/io.cytoid.interference2",
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders(),
            }).Then(it =>
            {
                OnLevelsLoaded(new List<LibraryLevel> {new LibraryLevel {addedDate = DateTime.UtcNow, level = it}});
                resolve();
                OnLibraryFetch.Invoke();
            }).Catch(error =>
            {
                Debug.LogError(error);
                reject(error);
            });
        });
    }

    private void OnLevelsLoaded(List<LibraryLevel> list)
    {
        list.ForEach(it => Levels[it.level.uid] = it);
    }

}

public class LibraryFetchEvent : UnityEvent
{
}