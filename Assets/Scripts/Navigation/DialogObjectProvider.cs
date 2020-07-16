using UnityEngine;

public class DialogObjectProvider : SingletonMonoBehavior<DialogObjectProvider>
{
    public Dialog dialogPrefab;
    public RateLevelDialog rateLevelDialogPrefab;
    public Transform dialogHolder;
}