using UnityEngine;

public class LevelViewOnCytoidIO : InteractableMonoBehavior
{
    protected void Awake()
    {
        onPointerClick.AddListener(_ => Application.OpenURL($"https://cytoid.io/levels/{Context.SelectedLevel.Id}"));
    }
}