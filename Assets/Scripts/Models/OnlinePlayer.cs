using System;
using System.Collections;
using System.Linq;
using System.Text;
using Cytoid.UI;
using LunarConsolePluginInternal;
using Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class OnlinePlayer
{
    public static bool Authenticated;
    public static bool Authenticating;

    public static string Name;
    public static string Password;
    public static string AvatarUrl;
    public static int Level;
    public static int Exp;
    public static int NextExp;
    public static float Rating;

    public static AuthenticationResult LastAuthenticationResult;
    public static PostResult LastPostResult;
    public static RankingQueryResult LastRankingQueryResult;
    public static PlayerRankingQueryResult LastPlayerRankingQueryResult;
    public static RateResult LastRateResult;
    public static Texture2D AvatarTexture;

    public static IEnumerator Authenticate()
    {
        Debug.Log("Fetching player data");

        Authenticating = true;

        var username = PlayerPrefs.GetString(PreferenceKeys.LastUsername());
        var password = PlayerPrefs.GetString(PreferenceKeys.LastPassword());

        var request = new UnityWebRequest(CytoidApplication.Host + "/auth", "POST") {timeout = 10};
        var bodyRaw =
            Encoding.UTF8.GetBytes("{\"user\": \"" + username + "\", \"password\": \"" + password + "\"}");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        if (request.isNetworkError || request.isHttpError)
        {
            Log.e(request.responseCode.ToString());
            Log.e(request.error);
            LastAuthenticationResult = new AuthenticationResult {status = -1};
            Authenticating = false;
            yield break;
        }

        var body = request.downloadHandler.text;

        try
        {
            LastAuthenticationResult = JsonConvert.DeserializeObject<AuthenticationResult>(body);
        }
        catch (Exception e)
        {
            Log.e(e.Message);
            LastAuthenticationResult = new AuthenticationResult {status = -1};
            Authenticating = false;
            yield break;
        }

        if (LastAuthenticationResult.status == 0)
        {
            Authenticated = true;

            Name = username;
            Password = password;
            AvatarUrl = LastAuthenticationResult.avatar_url;
            Level = LastAuthenticationResult.level;
            Exp = LastAuthenticationResult.exp;
            NextExp = LastAuthenticationResult.next_exp;
            Rating = LastAuthenticationResult.rating;
        }

        request.Dispose();

        Authenticating = false;
    }

    public static IEnumerator PostPlayData(IPlayData playdata)
    {
        var uri = playdata is RankedPlayData ? "/rank/post" : "/unranked/post";
        var request = new UnityWebRequest(CytoidApplication.Host + uri, "POST") {timeout = 10};
        var body = JsonConvert.SerializeObject(playdata);

#if UNITY_EDITOR
        Debug.Log(body);
#endif

        var bodyRaw = Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        if (request.isNetworkError)
        {
            LastPostResult = new PostResult
            {
                status = -1,
                message = "Please check your network."
            };
        }
        else
        {
            if (request.responseCode == 200)
            {
                try
                {
                    LastPostResult =
                        JsonConvert.DeserializeObject<PostResult>(request.downloadHandler.text);
                    LastPostResult.status = 200;

                    Level = LastPostResult.level;
                    Exp = LastPostResult.exp;
                    NextExp = LastPostResult.next_exp;
                    Rating = LastPostResult.rating;
                }
                catch (Exception e)
                {
                    Log.e(e.Message);
                    LastRankingQueryResult = new RankingQueryResult {status = -1};
                    yield break;
                }
            }
            else
            {
                LastPostResult = new PostResult
                {
                    status = (int) request.responseCode,
                    message = request.downloadHandler.text
                };
            }
        }

        request.Dispose();
    }

    public static IEnumerator QueryRankings(string level, string type)
    {
        var request = UnityWebRequest.Get(
            string.Format(CytoidApplication.Host + "/rankings?level={0}&type={1}&player={2}",
                level, type, Name)
        );

        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        if (request.isNetworkError)
        {
            LastRankingQueryResult = new RankingQueryResult {status = -1};
        }
        else
        {
            try
            {
                LastRankingQueryResult =
                    JsonConvert.DeserializeObject<RankingQueryResult>(request.downloadHandler.text);
                LastRankingQueryResult.status = 0;

                #if UNITY_EDITOR
                Debug.Log(JsonConvert.SerializeObject(LastRankingQueryResult));
                #endif

                // Replace local score if higher
                if (LastRankingQueryResult.player_rank != -1)
                {
                    var ranking = LastRankingQueryResult.rankings.First(it => it.player == Name);

                    if (ranking != null)
                    {
                        var oldScore = ZPlayerPrefs.GetFloat(PreferenceKeys.BestScore(level,
                            type, true));

                        if (ranking.score > oldScore)
                        {
                            ZPlayerPrefs.SetFloat(
                                PreferenceKeys.BestScore(level,
                                    type,
                                    true),
                                ranking.score);
                            ZPlayerPrefs.SetFloat(
                                PreferenceKeys.BestAccuracy(level,
                                    type, true),
                                (float) ranking.accuracy / 100);

                            BestScoreText.WillInvalidate = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e.Message);
                LastRankingQueryResult = new RankingQueryResult {status = -1};
                yield break;
            }
        }

        request.Dispose();
    }

    public static IEnumerator QueryPlayerRankings(string type)
    {
        var request = UnityWebRequest.Get(
            string.Format(CytoidApplication.Host + "/player_rankings?type={0}&player={1}",
                type, Name)
        );

        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        if (request.isNetworkError)
        {
            LastPlayerRankingQueryResult = new PlayerRankingQueryResult {status = -1};
        }
        else
        {
            try
            {
                LastPlayerRankingQueryResult =
                    JsonConvert.DeserializeObject<PlayerRankingQueryResult>(request.downloadHandler.text);
                LastPlayerRankingQueryResult.status = 0;

                #if UNITY_EDITOR
                Debug.Log(JsonConvert.SerializeObject(LastPlayerRankingQueryResult));
                #endif
            }
            catch (Exception e)
            {
                Log.e(e.Message);
                LastPlayerRankingQueryResult = new PlayerRankingQueryResult {status = -1};
                yield break;
            }
        }

        request.Dispose();
    }

    public static IEnumerator Rate(RateData rateData)
    {
        var uri = "/rate/post";
        var request = new UnityWebRequest(CytoidApplication.Host + uri, "POST") {timeout = 10};
        var body = JsonConvert.SerializeObject(rateData);

#if UNITY_EDITOR
        Debug.Log(body);
#endif

        var bodyRaw = Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        if (request.isNetworkError)
        {
            LastRateResult = new RateResult
            {
                status = -1,
                message = "Please check your network."
            };
        }
        else
        {
            if (request.responseCode == 200)
            {
                try
                {
                    LastRateResult =
                        JsonConvert.DeserializeObject<RateResult>(request.downloadHandler.text);
                    LastRateResult.status = 200;
                }
                catch (Exception e)
                {
                    Log.e(e.Message);
                    LastRateResult = new RateResult
                    {
                        status = -1,
                        message = "Invalid response."
                    };
                    yield break;
                }
            }
            else
            {
                LastRateResult = new RateResult
                {
                    status = (int) request.responseCode,
                    message = request.downloadHandler.text
                };
            }
        }

        request.Dispose();
    }

    public static void Invalidate()
    {
        Authenticated = false;
        Authenticating = false;
        Name = null;
        Password = null;
        AvatarUrl = null;
        Level = 0;
        Exp = 0;
        NextExp = 0;
        Rating = 0;
        LastAuthenticationResult = null;
        LastRankingQueryResult = null;
        LastPostResult = null;
        LastRateResult = null;
        AvatarTexture = null;
    }
}