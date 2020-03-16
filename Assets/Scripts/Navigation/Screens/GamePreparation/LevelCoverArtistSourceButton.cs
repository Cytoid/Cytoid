using UnityEngine;

public class LevelCoverArtistSourceButton : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => Application.OpenURL(Context.SelectedLevel.Meta.illustrator_source));
    }
}