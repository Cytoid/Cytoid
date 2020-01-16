using UnityEngine;

public class NavigationObjectProvider : SingletonMonoBehavior<NavigationObjectProvider>
{
    public Dialog dialogPrefab;
    public RateLevelDialog rateLevelDialogPrefab;
    public Transform dialogHolder;
}