using UnityEngine;

public class CollectionCuratorSourceButton : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => 
            Application.OpenURL($"{Context.WebsiteUrl}/profile/{this.GetScreenParent<CollectionDetailsScreen>().LoadedPayload.Collection.owner.Uid}")
        );
    }
}