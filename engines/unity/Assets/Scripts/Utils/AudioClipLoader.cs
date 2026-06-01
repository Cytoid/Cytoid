using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class AudioClipLoader
{
    public string Path;
    public string Error;

    private bool loaded;
    
    public AudioClip AudioClip;
    private NLayerLoader nLayerLoader;
    
    public AudioClipLoader(string path)
    {
        Path = path;
    }
    
    public async UniTask Load()
    {
        if (loaded)
        {
            Unload();
        }
        
        var type = AudioTypeExtensions.Detect(Path);
        
        // TODO: Load remote mp3 with non-mobile platform (Download with UnityWebRequest first?)
        // Load .mp3 with NLayer on non-mobile platforms
        if (
            Path.StartsWith("file://")
            && type == AudioType.MPEG
            && Application.platform != RuntimePlatform.Android 
            && Application.platform != RuntimePlatform.IPhonePlayer
        )
        {
            nLayerLoader = new NLayerLoader(Path);
            AudioClip = nLayerLoader.LoadAudioClip();
        }
        else
        {
            using (var request = UnityWebRequestMultimedia.GetAudioClip(Path, type))
            {
                await request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError)
                {
                    Error = request.error;
                    return;
                }

                AudioClip = DownloadHandlerAudioClip.GetContent(request);
            }
        }

        loaded = true;
    }

    public void Unload()
    {
        if (!loaded) return;
        if (AudioClip != null)
        {
            try
            {
                AudioClip.UnloadAudioData();
            }
            catch (MissingReferenceException)
            {
            }
            catch (NullReferenceException)
            {
            }

            UnityEngine.Object.Destroy(AudioClip);
        }
        AudioClip = null;
        DisposeDecoder();

        loaded = false;
    }

    public void DisposeDecoder()
    {
        if (nLayerLoader != null)
        {
            nLayerLoader.Dispose();
            nLayerLoader = null;
        }
    }
}
