using System;
using System.Collections;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Networking;
#endif

namespace Polyglot
{
    public static class GoogleDownload
    {
        public static IEnumerator DownloadSheet(string docsId, string sheetId, Action<string> done, GoogleDriveDownloadFormat format = GoogleDriveDownloadFormat.CSV, Func<float, bool> progressbar = null)
        {
            if (progressbar != null && progressbar(0))
            {
                done(null);
                yield break;
            }

            var url = string.Format("https://docs.google.com/spreadsheets/d/{0}/export?format={2}&gid={1}", docsId, sheetId, Enum.GetName(typeof(GoogleDriveDownloadFormat), format).ToLower());
#if UNITY_2017_2_OR_NEWER
            var www = UnityWebRequest.Get(url);
            www.SendWebRequest();
#elif UNITY_5_5_OR_NEWER
            var www = UnityWebRequest.Get(url);
            www.Send();
#else
            var www = new WWW(url);
#endif
            while (!www.isDone)
            {
#if UNITY_5_5_OR_NEWER
                var progress = www.downloadProgress;
#else
                var progress = www.progress;
#endif
                if (progressbar != null && progressbar(progress))
                {
                    done(null);
                    yield break;
                }
                yield return null;
            }

            if (progressbar != null && progressbar(1))
            {
                done(null);
                yield break;
            }

#if UNITY_5_5_OR_NEWER
            var text = www.downloadHandler.text;
#else
            var text = www.text;
#endif

            if (text.StartsWith("<!"))
            {
                Debug.LogError("Google sheet could not be downloaded.\nURL:" + url + "\n" + text);
                done(null);
                yield break;
            }

            done(text);
        }
    }
}