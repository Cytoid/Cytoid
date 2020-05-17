using UnityEngine;

public class LevelViewOnCytoidIOButton : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => Application.OpenURL($"{Context.WebsiteUrl}/levels/{Context.SelectedLevel.Id}"));
    }
}