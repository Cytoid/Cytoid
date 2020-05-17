using UnityEngine;

public class CollectionCuratorSourceButton : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => Application.OpenURL($"{Context.WebsiteUrl}/profile/{CollectionDetailsScreen.LoadedContent.Collection.owner.Uid}"));
    }
}