using Newtonsoft.Json;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

public class GamePreparationScreen : Screen
{
    public const string Id = "GamePreparation";

    [GetComponentInChildrenName] public DepthCover cover;

    public override string GetId() => Id;

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (Context.SelectedLevel == null)
        {
            Debug.LogWarning("Context.activeLevel is null");
            return;
        }

        LoadCover();
    }

    private void LoadCover()
    {
        var selectedLevel = Context.SelectedLevel;
        var path = "file://" + selectedLevel.Path + selectedLevel.Meta.background.path;

        RestClient.Get(new RequestHelper {Uri = path, DownloadHandler = new DownloadHandlerTexture()})
            .Then(response => { cover.OnCoverLoaded(DownloadHandlerTexture.GetContent(response.Request)); })
            .Catch(Debug.LogError);
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        cover.image.color = Color.black;
    }
}