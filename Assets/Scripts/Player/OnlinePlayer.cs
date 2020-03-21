using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class OnlinePlayer
{
    public readonly UnityEvent OnAuthenticated = new UnityEvent();
    public readonly ProfileChangedEvent OnProfileChanged = new ProfileChangedEvent();

    public readonly LevelBestPerformanceUpdatedEvent OnLevelBestPerformanceUpdated =
        new LevelBestPerformanceUpdatedEvent();

    public Profile LastProfile { get; set; }

    public bool IsAuthenticated { get; set; }

    public bool IsAuthenticating { get; set; }

    public Promise<Profile> Authenticate(string password)
    {
        IsAuthenticating = true;

        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Post<Session>(new RequestHelper
            {
                Uri = Context.ApiUrl + "/session",
                BodyString = JObject.FromObject(new
                {
                    username = Uid,
                    password
                }).ToString()
            }).Then(session =>
                {
                    JwtToken = session.token;
                    Debug.Log(session.token);
                    Id = session.user.Id;
                    return FetchProfile();
                }
            ).Then(profile =>
            {
                IsAuthenticated = true;
                OnAuthenticated.Invoke();
                resolve(profile);
            }).Catch(result =>
            {
                Debug.LogError(result);
                if (result is RequestException requestException)
                {
                    if (requestException.IsHttpError)
                    {
                        JwtToken = null;
                    }
                }
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public Promise<Profile> AuthenticateWithJwtToken()
    {
        IsAuthenticating = true;

        var jwtToken = JwtToken;
        if (jwtToken == null) throw new ArgumentException();

        Debug.Log($"JWT token: {jwtToken}");

        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Post<Session>(new RequestHelper
            {
                Uri = Context.ApiUrl + "/session",
                BodyString = JObject.FromObject(new { }).ToString(),
                Headers = new Dictionary<string, string>
                {
                    {"Authorization", "JWT " + jwtToken}
                }
            }).Then(session =>
                {
                    JwtToken = session.token;
                    Debug.Log(session.token);
                    Id = session.user.Id;
                    return FetchProfile();
                }
            ).Then(profile =>
            {
                IsAuthenticated = true;
                OnAuthenticated.Invoke();
                resolve(profile);
            }).Catch(result =>
            {
                Debug.LogError(result);
                if (result is RequestException requestException)
                {
                    if (requestException.IsHttpError)
                    {
                        JwtToken = null;
                    }
                }
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public void Deauthenticate()
    {
        JwtToken = null;
        LastProfile = null;
        IsAuthenticating = false;
        IsAuthenticated = false;
        
        // Drop user information in DB
        Context.Database.DropCollection("characters");
    }

    public string Id
    {
        get => PlayerPrefs.GetString("Id");
        set => PlayerPrefs.SetString("Id", value);
    }

    public string Uid
    {
        get => PlayerPrefs.GetString("Uid");
        set => PlayerPrefs.SetString("Uid", value);
    }

    public string JwtToken
    {
        get => SecuredPlayerPrefs.GetString("JwtToken", null);
        set => SecuredPlayerPrefs.SetString("JwtToken", value);
    }

    public Dictionary<string, string> GetAuthorizationHeaders()
    {
        if (JwtToken == null)
        {
            return new Dictionary<string, string>();
        }
        return new Dictionary<string, string>
        {
            {"Authorization", "JWT " + JwtToken}
        };
    }

    public IPromise<Profile> FetchProfile(string uid = null)
    {
        if (Context.IsOnline())
        {
            // Online
            if (uid == null) uid = Uid;
            return RestClient.Get<Profile>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/profile/{uid}/full",
                Headers = GetAuthorizationHeaders(),
                EnableDebug = true
            }).Then(profile =>
            {
                LastProfile = profile;
                Context.Database.Let(it =>
                {
                    it.DropCollection("profile");
                    var col = it.GetCollection<Profile>("profile");
                    col.Insert(profile);
                });
                OnProfileChanged.Invoke(profile);
                return profile;
            });
        }
        
        // Offline: Load from DB
        return Context.Database.Let(it =>
        {
            var col = it.GetCollection<Profile>("profile");
            var result = col.FindOne(x => true);
            return Promise<Profile>.Resolved(result);
        });
    }

    public IPromise<(int, List<RankingEntry>)> GetLevelRankings(string levelId, string chartType)
    {
        var entries = new List<RankingEntry>();
        var top10 = new List<RankingEntry>();
        if (IsAuthenticated)
        {
            return RestClient.GetArray<RankingEntry>(new RequestHelper
                {
                    Uri = $"{Context.ApiUrl}/levels/{levelId}/charts/{chartType}/ranking",
                    Headers = GetAuthorizationHeaders(),
                    EnableDebug = true
                })
                .Then(data =>
                {
                    top10 = data.ToList();
                    // Add the first 3
                    entries.AddRange(top10.GetRange(0, Math.Min(3, top10.Count)));

                    return RestClient.GetArray<RankingEntry>(new RequestHelper
                    {
                        Uri =
                            $"{Context.ApiUrl}/levels/{levelId}/charts/{chartType}/ranking?user={Context.OnlinePlayer.Uid}&userLimit=6",
                        Headers = GetAuthorizationHeaders(),
                    });
                })
                .Then(data =>
                {
                    var list = data.ToList();

                    // Find user's position
                    var userRank = -1;
                    RankingEntry userEntry = null;
                    for (var index = 0; index < data.Length; index++)
                    {
                        var entry = data[index];
                        if (entry.owner.Uid == Context.OnlinePlayer.Uid)
                        {
                            userRank = entry.rank;
                            userEntry = entry;
                            break;
                        }
                    }

                    if (userRank == -1 || userRank <= 10)
                    {
                        // Just use top 10
                        entries = top10;
                    }
                    else
                    {
                        // Add previous 6 and next 6, and remove accordingly
                        var append = new List<RankingEntry>();
                        append.AddRange(list);
                        append.RemoveRange(0, Math.Max(3, Math.Max(0, 10 - userRank)));
                        if (append.Count > 7) append.RemoveRange(7, append.Count - 7);
                        entries.AddRange(append);
                    }

                    if (userEntry != null)
                    {
                        // Replace local performance only if higher score
                        var localPerformance = Context.LocalPlayer.GetBestPerformance(levelId, chartType, true);
                        if (userEntry.score > localPerformance.Score)
                        {
                            Context.LocalPlayer.SetBestPerformance(levelId, chartType, true, new LocalPlayer.Performance
                            {
                                Score = userEntry.score, Accuracy = userEntry.accuracy * 100f, ClearType = string.Empty
                            }); // TODO: ClearType

                            OnLevelBestPerformanceUpdated.Invoke(levelId);
                        }
                    }

                    return (userRank, entries);
                });
        }

        return RestClient.GetArray<RankingEntry>(new RequestHelper
        {
            Uri = $"{Context.ApiUrl}/levels/{levelId}/charts/{chartType}/ranking",
            Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
        }).Then(array => (-1, array.ToList()));
    }
    
    public IPromise<(int, List<TierRankingEntry>)> GetTierRankings(string tierId)
    {
        // TODO: Personal rank
        return RestClient.GetArray<TierRankingEntry>(new RequestHelper
        {
            Uri = $"{Context.ServicesUrl}/seasons/alpha/tiers/{tierId}/records",
            Headers = Context.OnlinePlayer.GetAuthorizationHeaders()
        }).Then(array => (-1, array.ToList()));
    }
}

public class ProfileChangedEvent : UnityEvent<Profile>
{
}

public class LevelBestPerformanceUpdatedEvent : UnityEvent<string>
{
}