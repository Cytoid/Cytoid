using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GamePreparationScreen : Screen
{ 
    [GetComponentInChildrenName] public DepthCover cover;

    public override string GetId() => "GamePreparation";

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        
        if (Context.activeLevel == null)
        {
            Debug.LogWarning("Context.activeLevel is null");
            return;
        }

        LoadCover();
    }

    private async void LoadCover()
    {
        var selectedLevel = Context.activeLevel;
        var path = "file://" + selectedLevel.path + selectedLevel.meta.background.path;
        using (var request = UnityWebRequestTexture.GetTexture(path))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                print(path);
                print(request.error);
            }
            else
            {
                cover.OnCoverLoaded(DownloadHandlerTexture.GetContent(request));
            }
        }
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        
        cover.image.color = Color.black;
    }
    
}