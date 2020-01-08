using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.Events;

public class OnlinePlayer
{
    public readonly UnityEvent onAuthenticated = new UnityEvent();
    public readonly ProfileChangedEvent onProfileChanged = new ProfileChangedEvent();
    
    public Profile LastProfile { get; private set; }

    public bool IsAuthenticated { get; private set; }
    
    public bool IsAuthenticating { get; set; }
    
    private Dictionary<string, string> setCookies = new Dictionary<string, string>();

    public Promise<Profile> Authenticate(string password)
    {
        IsAuthenticating = true;
        
        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Post<Session>(new RequestHelper
            {
                Uri = Context.ApiBaseUrl + "/session",
                BodyString = JObject.FromObject(new
                {
                    username = GetUid(),
                    password,
                    token = SecuredConstants.AuthenticationVerificationToken
                }).ToString()
            }).Then(session =>
                {
                    SetJwtToken(session.token);
                    Debug.Log(session.token);
                    return FetchProfile();
                }
            ).Then(profile =>
            {
                IsAuthenticated = true;
                resolve(profile);
            }).Catch(result =>
            {
                Debug.LogError(result);
                SetJwtToken(null);
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public Promise<Profile> AuthenticateWithJwtToken()
    {
        IsAuthenticating = true;

        var jwtToken = GetJwtToken();
        if (jwtToken == null) throw new ArgumentException();

        return new Promise<Profile>((resolve, reject) =>
        {
            RestClient.Post<Session>(new RequestHelper
            {
                Uri = Context.ApiBaseUrl + "/session",
                BodyString = JObject.FromObject(new
                {
                    token = SecuredConstants.AuthenticationVerificationToken
                }).ToString(),
                Headers = new Dictionary<string, string>
                {
                    {"Authorization", "JWT " + jwtToken}
                }
            }).Then(session =>
            {
                SetJwtToken(session.token);
                Debug.Log(session.token);
                return FetchProfile();
            }
            ).Then(profile =>
            {
                IsAuthenticated = true;
                onAuthenticated.Invoke();
                resolve(profile);
            }).Catch(result =>
            {
                Debug.LogError(result);
                SetJwtToken(null);
                reject(result);
            }).Finally(() => IsAuthenticating = false);
        });
    }

    public void Deauthenticate()
    {
        SetJwtToken(null);
        LastProfile = null;
        IsAuthenticating = false;
        IsAuthenticated = false;
    }
    
    public string GetUid()
    {
        return PlayerPrefs.GetString("last_username");
    }

    public void SetUid(string uid)
    {
        PlayerPrefs.SetString("last_username", uid);
    }

    public string GetJwtToken()
    {
        return SecuredPlayerPrefs.GetString("JwtToken", null);
    }

    public void SetJwtToken(string token)
    {
        SecuredPlayerPrefs.SetString("JwtToken", token);
    }

    public Dictionary<string, string> GetJwtAuthorizationHeaders()
    {
        return new Dictionary<string, string>
        {
            {"Authorization", "JWT " + GetJwtToken()}
        };
    }

    public IPromise<Profile> FetchProfile(string uid = null)
    {
        if (uid == null) uid = GetUid();
        return RestClient.Get<Profile>(new RequestHelper
        {
            Uri = $"{Context.ApiBaseUrl}/profile/{uid}/full",
            EnableDebug = true
        }).Then(profile =>
        {
            LastProfile = profile;
            onProfileChanged.Invoke(profile);
            return profile;
        });
    }

    public IPromise<System.Tuple<int, List<RankingEntry>>> GetLevelRankings(string levelId, string chartType)
    {
        var entries = new List<RankingEntry>();
        var top10 = new List<RankingEntry>();
        if (IsAuthenticated)
        {
            return RestClient.GetArray<RankingEntry>(new RequestHelper
                {
                    Uri = $"{Context.ApiBaseUrl}/levels/{levelId}/charts/{chartType}/ranking",
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
                            $"{Context.ApiBaseUrl}/levels/{levelId}/charts/{chartType}/ranking?user={Context.OnlinePlayer.GetUid()}&userLimit=6"
                    });
                })
                .Then(data =>
                {
                    var list = data.ToList();

                    // Find user's position
                    var userRank = -1;
                    for (var index = 0; index < data.Length; index++)
                    {
                        var entry = data[index];
                        if (entry.owner.uid == Context.OnlinePlayer.GetUid())
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
                        var append = new List<RankingEntry>();
                        append.AddRange(list);
                        append.RemoveRange(0, Math.Max(3, Math.Max(0, 10 - userRank)));
                        if (append.Count > 7) append.RemoveRange(7, append.Count - 7);
                        entries.AddRange(append);
                    }

                    return new System.Tuple<int, List<RankingEntry>>(userRank, entries);
                });
        }

        return RestClient.GetArray<RankingEntry>(new RequestHelper
        {
            Uri = $"{Context.ApiBaseUrl}/levels/{levelId}/charts/{chartType}/ranking"
        }).Then(array => new System.Tuple<int, List<RankingEntry>>(-1, array.ToList()));
    }
}

public class ProfileChangedEvent : UnityEvent<Profile>
{
}