using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

public class AssetLoader
{
    public string Path;
    public string Error;
    public AudioClip AudioClip;
    
    public AssetLoader(string path)
    {
        Path = path;
    }
    
    public async UniTask LoadAudioClip()
    {
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
            AudioClip = NLayerLoader.LoadMpeg(Path);
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
    }
}