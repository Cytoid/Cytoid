using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Polyglot;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.Events;

public class OnlinePlayer
{
    public readonly UnityEvent OnAuthenticated = new UnityEvent();
    public readonly ProfileChangedEvent OnProfileChanged = new ProfileChangedEvent();

    public readonly LevelBestPerformanceUpdatedEvent OnLevelBestPerformanceUpdated =
        new LevelBestPerformanceUpdatedEvent();

    public Profile LastProfile { get; set; }
    
    public FullProfile LastFullProfile { get; set; }

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
                EnableDebug = true,
                BodyString = SecuredOperations.WithCaptcha(new
                {
                    username = Context.Player.Id,
                    password,
                }).ToString()
            }).Then(session =>
                {
                    Context.Player.Settings.PlayerId = session.user.Uid;
                    Context.Player.Settings.LoginToken = session.token;
                    Context.Player.SaveSettings();
                    return FetchProfile();
                }
            ).Then(profile =>
            {
                if (profile == null)
                {
                    reject(new RequestException("Profile not found", true, false, 404, null));
                    return;
                }
                IsAuthenticated = true;
                OnAuthenticated.Invoke();
                resolve(profile);
            }).CatchRequestError(result =>
            {
                IsAuthenticated = false;
                Debug.LogError(result);
                if (result.IsHttpError)
                {
                    Context.Player.Settings.LoginToken = null;
                    Context.Player.SaveSettings();
                }
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public Promise<Profile> AuthenticateWithJwtToken()
    {
        IsAuthenticating = true;

        var jwtToken = Context.Player.Settings.LoginToken;
        if (jwtToken == null) throw new ArgumentException();

        Debug.Log($"JWT token: {jwtToken}");

        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Get<Session>(new RequestHelper
            {
                Uri = Context.ApiUrl + $"/session?captcha={SecuredOperations.GetCaptcha()}",
                Headers = new Dictionary<string, string>
                {
                    {"Authorization", "JWT " + jwtToken}
                },
                EnableDebug = true
            }).Then(session =>
                {
                    Context.Player.Settings.PlayerId = session.user.Uid;
                    Context.Player.Settings.LoginToken = session.token;
                    Context.Player.SaveSettings();
                    return FetchProfile();
                }
            ).Then(profile =>
            {
                if (profile == null)
                {
                    reject(new RequestException("Profile not found", true, false, 404, null));
                    return;
                }
                IsAuthenticated = true;
                OnAuthenticated.Invoke();
                resolve(profile);
            }).CatchRequestError(result =>
            {
                IsAuthenticated = false; 
                Debug.LogError(result);
                if (!result.IsNetworkError)
                {
                    Context.Player.Settings.LoginToken = null;
                    Context.Player.SaveSettings();
                }
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public void Deauthenticate()
    {
        Context.Player.Settings.LoginToken = null;
        Context.Player.SaveSettings();
        LastProfile = null;
        LastFullProfile = null;
        IsAuthenticating = false;
        IsAuthenticated = false;

        Context.ScreenManager.GetScreen<EventSelectionScreen>().LoadedPayload = null;
        TierSelectionScreen.LoadedContent = null;
        
        // Drop user information in DB
        Context.Database.DropCollection("characters");
        Context.Library.Clear();
    }

    public Dictionary<string, string> GetRequestHeaders()
    {
        if (Context.Player.Settings.LoginToken == null)
        {
            return new Dictionary<string, string>
            {
                {"Accept-Language", ((Language) Context.Player.Settings.Language).GetAcceptLanguageHeaderValue()}
            };
        }
        return new Dictionary<string, string>
        {
            {"Authorization", "JWT " + Context.Player.Settings.LoginToken},
            {"Accept-Language", ((Language) Context.Player.Settings.Language).GetAcceptLanguageHeaderValue()}
        };
    }

    public IPromise<Profile> FetchProfile()
    {
        var uid = Context.Player.Id;
        if (IsAuthenticating || Context.IsOnline())
        {
            // Online
            return RestClient.Get<Profile>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/profile/{uid}",
                Headers = GetRequestHeaders(),
                EnableDebug = true
            }).Then(profile =>
            {
                Debug.Log($"Profile: {profile}");
                LastProfile = profile ?? throw new InvalidOperationException("Profile is null");
                Context.Database.SetProfile(profile);
                OnProfileChanged.Invoke(profile);
                return profile;
            }).CatchRequestError(error =>
            {
                Debug.LogError(error);
                return null;
            });
        }
        
        // Offline: Load from DB
        return Promise<Profile>.Resolved(LastProfile = Context.Database.GetProfile());
    }

    public IPromise<(int, List<RankingEntry>)> GetLevelRankings(string levelId, string chartType)
    {
        var entries = new List<RankingEntry>();
        var top10 = new List<RankingEntry>();
        if (IsAuthenticated)
        {
            return RestClient.GetArray<RankingEntry>(new RequestHelper
                {
                    Uri = $"{Context.ApiUrl}/levels/{levelId}/charts/{chartType}/records?limit=10",
                    Headers = GetRequestHeaders(),
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
                            $"{Context.ApiUrl}/levels/{levelId}/charts/{chartType}/user_ranking?limit=6",
                        Headers = GetRequestHeaders(),
                        EnableDebug = true
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
                        if (entry.owner.Uid == Context.Player.Id)
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
                        // Replace local performance only if higher or equal score
                        var record = Context.Database.GetLevelRecord(levelId);
                        if (record == null || !record.BestPerformances.ContainsKey(chartType) ||
                            record.BestPerformances[chartType].Score < userEntry.score ||
                            (record.BestPerformances[chartType].Score == userEntry.score && record.BestPerformances[chartType].Accuracy < userEntry.accuracy))
                        {
                            if (record == null) record = new LevelRecord
                            {
                                LevelId = levelId
                            };
                            
                            var newBest = new LevelRecord.Performance
                            {
                                Score = userEntry.score,
                                Accuracy = userEntry.accuracy
                            };
                            record.BestPerformances[chartType] = newBest;
                            Context.Database.SetLevelRecord(record);

                            if (Context.LevelManager.LoadedLocalLevels.ContainsKey(levelId))
                            {
                                Context.LevelManager.LoadedLocalLevels[levelId].Record = record;
                            }
                            
                            OnLevelBestPerformanceUpdated.Invoke(levelId);
                            Debug.Log("Updating: " + levelId);
                        }
                    }

                    return (userRank, entries);
                });
        }

        return RestClient.GetArray<RankingEntry>(new RequestHelper
        {
            Uri = $"{Context.ApiUrl}/levels/{levelId}/charts/{chartType}/records",
            EnableDebug = true,
        }).Then(array => (-1, array.ToList()));
    }
    
    public IPromise<(int, List<TierRankingEntry>)> GetTierRankings(string tierId)
    {
        var entries = new List<TierRankingEntry>();
        var top10 = new List<TierRankingEntry>();
        return RestClient.GetArray<TierRankingEntry>(new RequestHelper
        {
            Uri = $"{Context.ApiUrl}/seasons/alpha/tiers/{tierId}/records?limit=10",
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(data =>
                {
                    top10 = data.ToList();
                    // Add the first 3
                    entries.AddRange(top10.GetRange(0, Math.Min(3, top10.Count)));

                    return RestClient.GetArray<TierRankingEntry>(new RequestHelper
                    {
                        Uri =
                            $"{Context.ApiUrl}/seasons/alpha/tiers/{tierId}/user_ranking?limit=6",
                        Headers = GetRequestHeaders(),
                        EnableDebug = true
                    });
                })
                .Then(data =>
                {
                    var list = data.ToList();

                    // Find user's position
                    var userRank = -1;
                    TierRankingEntry userEntry = null;
                    for (var index = 0; index < data.Length; index++)
                    {
                        var entry = data[index];
                        if (entry.owner.Uid == Context.Player.Id)
                        {
                            userRank = entry.rank;
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
                        var append = new List<TierRankingEntry>();
                        append.AddRange(list);
                        append.RemoveRange(0, Math.Max(3, Math.Max(0, 10 - userRank)));
                        if (append.Count > 7) append.RemoveRange(7, append.Count - 7);
                        entries.AddRange(append);
                    }

                    return (userRank, entries);
                });
    }

}

public class ProfileChangedEvent : UnityEvent<Profile>
{
}

public class LevelBestPerformanceUpdatedEvent : UnityEvent<string>
{
}