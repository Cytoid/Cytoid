using UnityEngine;

public class LevelArtistSourceButton : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => Application.OpenURL(Context.SelectedLevel.Meta.artist_source));
    }
}