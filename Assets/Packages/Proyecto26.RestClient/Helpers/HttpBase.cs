using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Proyecto26.Common;
using UnityEngine.SceneManagement;

namespace Proyecto26
{
    public static class HttpBase
    {
        public static IEnumerator CreateRequestAndRetry(RequestHelper options, Action<RequestException, ResponseHelper> callback)
        {
            var retries = 0;
            do
            {
                using (var request = CreateRequest(options))
                {
                    yield return request.SendWebRequestWithOptions(options);
                    if (IsInGame()) break;
                    var response = request.CreateWebResponse();
                    if (request.IsValidRequest(options))
                    {
                        if (options.EnableDebug)
                        {
                            DebugLog(options.EnableDebug,
                                string.Format("Url: {0}\nMethod: {1}\nStatus: {2}\nResponse: {3}", options.Uri,
                                    options.Method, request.responseCode, response.Text), false);
                        }
                        
                        callback(null, response);
                        break;
                    }
                    else if (!options.IsAborted && retries < options.Retries)
                    {
                        yield return new WaitForSeconds(options.RetrySecondsDelay);
                        if (IsInGame()) break; // EDIT: Cytoid
                        retries++;
                        if(options.RetryCallback != null)
                        {
                            options.RetryCallback(CreateException(request), retries);
                        }
                        // EDIT: Cytoid
                        if (options.Retries == 0)
                        {
                            var err = CreateException(request);
                            DebugLog(options.EnableDebug, $"Error: {err}, Url: {options.Uri}, Body: {(request.uploadHandler != null ? Encoding.UTF8.GetString(request.uploadHandler.data) : "null")}, Response: {response.Text}", true);
                            callback(err, response);
                            break;
                        }
                        // End of EDIT
                        DebugLog(options.EnableDebug, string.Format("Retry Request\nUrl: {0}\nMethod: {1}", options.Uri, options.Method), false);
                    }
                    else
                    {
                        var err = CreateException(request);
                        DebugLog(options.EnableDebug, $"Error: {err}, Url: {options.Uri}, Body: {(request.uploadHandler != null ? Encoding.UTF8.GetString(request.uploadHandler.data) : "null")}, Response: {response.Text}", true);
                        callback(err, response);
                        break;
                    }
                }
                options.Request = null;
            }
            while (retries <= options.Retries);
        }

        private static UnityWebRequest CreateRequest(RequestHelper options)
        {
            if (options.FormData is WWWForm && options.Method == UnityWebRequest.kHttpVerbPOST)
            {
                return UnityWebRequest.Post(options.Uri, options.FormData);
            }
            else
            {
                return new UnityWebRequest(options.Uri, options.Method);
            }
        }

        private static RequestException CreateException(UnityWebRequest request)
        {
            return new RequestException(request.error, request.isHttpError, request.isNetworkError, request.responseCode, 
                  !request.url.Contains("/levels/packages/") ? request.downloadHandler.text : "Response muted");
        }

        private static void DebugLog(bool debugEnabled, object message, bool isError)
        {
            if (debugEnabled)
            {
                if (isError)
                    Debug.LogWarning(message);
                else
                    Debug.Log(message);
            }
        }

        public static IEnumerator DefaultUnityWebRequest(RequestHelper options, Action<RequestException, ResponseHelper> callback)
        {
            return CreateRequestAndRetry(options, callback);
        }

        public static IEnumerator DefaultUnityWebRequest<TResponse>(RequestHelper options, Action<RequestException, ResponseHelper, TResponse> callback)
        {
            return CreateRequestAndRetry(options, (RequestException err, ResponseHelper res) => {
                if (IsInGame()) return; // EDIT: Cytoid
                var body = default(TResponse);
                if (err == null && res.Data != null && options.WillParseBody)
                {
                    try {
                        body = JsonConvert.DeserializeObject<TResponse>(res.Text); // EDIT: Cytoid
                    }
                    catch (Exception error) {
                        DebugLog(options.EnableDebug, string.Format("Invalid JSON format\nError: {0}", error.Message), true);
                    }
                }
                callback(err, res, body);
            });
        }

        public static IEnumerator DefaultUnityWebRequest<TResponse>(RequestHelper options, Action<RequestException, ResponseHelper, TResponse[]> callback)
        {
            return CreateRequestAndRetry(options, (RequestException err, ResponseHelper res) => {
                if (IsInGame()) return; // EDIT: Cytoid
                var body = default(TResponse[]);
                if (err == null && res.Data != null && options.WillParseBody)
                {
                    try { 
                        body = JsonHelper.ArrayFromJson<TResponse>(res.Text); // EDIT: Cytoid
                    }
                    catch (Exception error)
                    {
                        DebugLog(options.EnableDebug, string.Format("Invalid JSON format\nError: {0}", error.Message), true);
                    }
                }
                callback(err, res, body);
            });
        }
        
        // EDIT: Cytoid
        private static bool IsInGame() => SceneManager.GetActiveScene().name == "Game";
        // End of EDIT

    }
}
