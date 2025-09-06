using System;
using UnityEngine;
using UnityEngine.Events;

public class WebViewHolder : SingletonMonoBehavior<WebViewHolder>
{
    public WebViewObject webView;
    public WebViewLoadedEvent OnWebViewLoaded = new WebViewLoadedEvent();
    
    protected override void Awake()
    {
        base.Awake();

        if (GameObject.FindGameObjectsWithTag("WebView").Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        
        webView.Init(
            cb: (msg) =>
            {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));
            },
            err: (msg) =>
            {
                Debug.Log(string.Format("CallOnError[{0}]", msg));
            },
            started: (msg) =>
            {
                Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) =>
            {
                Debug.Log(string.Format("CallOnHooked[{0}]", msg));
                try
                {
                    var uri = new Uri(msg);
                    Application.OpenURL(msg);
                }
                catch
                {
                    // Ignored
                }
            },
            ld: (msg) =>
            {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
#if UNITY_EDITOR_OSX || !UNITY_ANDROID
                webView.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        window.location = 'unity:' + msg;
                      }
                    }
                  }
                ");
#endif
                webView.EvaluateJS(@"document.body.style.background = 'none';");
                OnWebViewLoaded.Invoke(msg);
            },
            ua: $"CytoidClient/{Context.VersionIdentifier}",
            enableWKWebView: true,
            transparent: true
        );
        webView.SetMargins(0, 0, (int) ((48 + 96 + 48) / 1920f * UnityEngine.Screen.width), 0);
        webView.SetScrollBounceEnabled(true);
    }
}

public class WebViewLoadedEvent : UnityEvent<string> {
}
