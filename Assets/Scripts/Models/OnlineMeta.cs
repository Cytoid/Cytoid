using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Cytoid.UI;
using LunarConsolePluginInternal;
using Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class OnlineMeta
{
    public static MetaResult LastMetaResult;

    public static IEnumerator FetchMeta(string levelId)
    {
        Debug.Log("Fetching " + levelId + " meta");

        var request = UnityWebRequest.Get(
            string.Format(CytoidApplication.Host + "/meta?level={0}", levelId)
        );

        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.Send();

        if (request.isNetworkError)
        {
            LastMetaResult = new MetaResult {status = -1};
        }
        else
        {
            if (request.responseCode == 200)
            {
                try
                {
                    LastMetaResult =
                        JsonConvert.DeserializeObject<MetaResult>(request.downloadHandler.text);

#if UNITY_EDITOR
                    Debug.Log(JsonConvert.SerializeObject(LastMetaResult));
#endif

                    var level = CytoidApplication.Levels.Find(it => it.id == levelId);
                    if (level == null) yield break;

                    var willInvalidate = false;
                    
                    if (level.schema_version != LastMetaResult.schema_version)
                    {
                        level.schema_version = LastMetaResult.schema_version;
                        willInvalidate = true;
                    }
                    
                    if (!string.IsNullOrEmpty(LastMetaResult.title) && level.title != LastMetaResult.title)
                    {
                        level.title = LastMetaResult.title;
                        willInvalidate = true;
                    }

                    if (!string.IsNullOrEmpty(LastMetaResult.title_localized) &&
                        level.title_localized != LastMetaResult.title_localized)
                    {
                        level.title_localized = LastMetaResult.title_localized;
                        willInvalidate = true;
                    }

                    if (!string.IsNullOrEmpty(LastMetaResult.artist) && level.artist != LastMetaResult.artist)
                    {
                        level.artist = LastMetaResult.artist;
                        willInvalidate = true;
                    }

                    if (!string.IsNullOrEmpty(LastMetaResult.artist_localized) &&
                        level.artist_localized != LastMetaResult.artist_localized)
                    {
                        level.artist_localized = LastMetaResult.artist_localized;
                        willInvalidate = true;
                    }

                    if (!string.IsNullOrEmpty(LastMetaResult.artist_source) &&
                        level.artist_source != LastMetaResult.artist_source)
                    {
                        level.artist_source = LastMetaResult.artist_source;
                        willInvalidate = true;
                    }

                    if (!string.IsNullOrEmpty(LastMetaResult.illustrator) &&
                        level.illustrator != LastMetaResult.illustrator)
                    {
                        level.illustrator = LastMetaResult.illustrator;
                        willInvalidate = true;
                    }

                    if (!string.IsNullOrEmpty(LastMetaResult.illustrator_source) &&
                        level.illustrator_source != LastMetaResult.illustrator_source)
                    {
                        level.illustrator_source = LastMetaResult.illustrator_source;
                        willInvalidate = true;
                    }

                    if (LastMetaResult.easy_difficulty != -1 && level.GetChartSection("easy") != null &&
                        level.GetChartSection("easy").difficulty != LastMetaResult.easy_difficulty)
                    {
                        level.GetChartSection("easy").difficulty = LastMetaResult.easy_difficulty;
                        willInvalidate = true;
                    }

                    if (LastMetaResult.hard_difficulty != -1 && level.GetChartSection("hard") != null &&
                        level.GetChartSection("hard").difficulty != LastMetaResult.hard_difficulty)
                    {
                        level.GetChartSection("hard").difficulty = LastMetaResult.hard_difficulty;
                        willInvalidate = true;
                    }

                    if (LastMetaResult.extreme_difficulty != -1 && level.GetChartSection("extreme") != null &&
                        level.GetChartSection("extreme").difficulty != LastMetaResult.extreme_difficulty)
                    {
                        level.GetChartSection("extreme").difficulty = LastMetaResult.extreme_difficulty;
                        willInvalidate = true;
                    }

                    if (willInvalidate)
                    {
                        File.WriteAllText(level.BasePath + "level.json", JsonConvert.SerializeObject(level));

                        if (CytoidApplication.CurrentLevel.id == levelId)
                        {
                            EventKit.Broadcast("meta reloaded", levelId);
                        }
                    }
                }
                catch (Exception e)
                {
                    LastMetaResult = new MetaResult {status = -1};
                    yield break;
                }
            }
            else
            {
                LastMetaResult = new MetaResult
                {
                    status = (int) request.responseCode,
                    message = request.downloadHandler.text
                };
            }
        }

        request.Dispose();
    }
}