using UnityEngine;
using UnityEngine.UI;

public class CollectionCuratorText : MonoBehaviour, ScreenInitializedListener
{
    [GetComponent] public Text text;

    public void OnScreenInitialized()
    {
        this.GetScreenParent<CollectionDetailsScreen>().onContentLoaded.AddListener(() =>
        {
            text.text = CollectionDetailsScreen.LoadedContent.Collection.owner.Uid;
        });
    }
    
}