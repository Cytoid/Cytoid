using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.Events;

public class Library
{

    public readonly Dictionary<string, LibraryLevel> Levels = new Dictionary<string, LibraryLevel>();
    public readonly UnityEvent OnLibraryFetched = new UnityEvent();
    public readonly UnityEvent OnLibraryLoaded = new UnityEvent();

    public void Initialize()
    {
        LoadFromLocal();
        Context.OnlinePlayer.OnAuthenticated.AddListener(() => Fetch());
    }
    
    public void LoadFromLocal()
    {
        Levels.Clear();

        var col = Context.Database.GetCollection<LibraryLevel>("library");
        col.EnsureIndex(x => x.Level.Uid, true);

        OnLevelsLoaded(col.FindAll());
    }

    public void Clear()
    {
        Levels.Clear();
        Context.Database.DropCollection("library");
    }

    public Promise<List<LibraryLevel>> Fetch()
    {
        var beforeLevelCount = Levels.Count; // This may not be accurate, but whatever!
        return new Promise<List<LibraryLevel>>((resolve, reject) =>
        {
            RestClient.GetArray<LibraryLevel>(new RequestHelper
            {
                Uri = $"{Context.ServicesUrl}/library?granted=true",
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders(),
                EnableDebug = true
            }).Then(data =>
            {
                var libraryLevels = data.ToList();
                Debug.Log($"Fetched {libraryLevels.Count} library levels");
                libraryLevels.Sort((a, b) => DateTimeOffset.Compare(a.Date, b.Date));

                Context.Database.DropCollection("library");
                var col = Context.Database.GetCollection<LibraryLevel>("library");
                col.InsertBulk(libraryLevels);
                
                OnLibraryFetched.Invoke();
                OnLevelsLoaded(libraryLevels);
                if (beforeLevelCount != Levels.Count)
                {
                    Toast.Next(Toast.Status.Success, "TOAST_OFFICIAL_LIBRARY_SYNCHRONIZED".Get());
                }
                resolve(libraryLevels);
            }).CatchRequestError(error =>
            {
                Debug.LogWarning(error.Response);
                if (!error.IsNetworkError)
                {
                    Debug.LogError(error);
                    reject(error);
                }
                else
                {
                    resolve(new List<LibraryLevel>());
                }
            });
        });
    }

    private void OnLevelsLoaded(IEnumerable<LibraryLevel> list)
    {
        Levels.Clear();
        list.ForEach(it => Levels[it.Level.Uid] = it);
        OnLibraryLoaded.Invoke();
    }

}