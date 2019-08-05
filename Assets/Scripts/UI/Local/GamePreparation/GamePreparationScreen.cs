using System;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GamePreparationScreen : Screen
{ 
    public const string Id = "GamePreparation";
    
    [GetComponentInChildrenName] public DepthCover cover;

    public override string GetId() => Id;

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        
        if (Context.ActiveLevel == null)
        {
            Debug.LogWarning("Context.activeLevel is null");
            return;
        }

        LoadCover();
    }

    private async void LoadCover()
    {
        var selectedLevel = Context.ActiveLevel;
        var path = "file://" + selectedLevel.Path + selectedLevel.Meta.background.path;
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