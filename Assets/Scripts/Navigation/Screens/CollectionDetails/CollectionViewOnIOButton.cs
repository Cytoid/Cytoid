using UnityEngine;

public class CollectionViewOnIOButton : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => Application.OpenURL($"{Context.WebsiteUrl}/collections/{this.GetScreenParent<CollectionDetailsScreen>().LoadedPayload.Collection.uid}"));
    }
}