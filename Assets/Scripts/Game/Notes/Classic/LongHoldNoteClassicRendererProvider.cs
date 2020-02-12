using UnityEngine;

public class LongHoldNoteClassicRendererProvider : SingletonMonoBehavior<LongHoldNoteClassicRendererProvider>
{
    public GameObject linePrefab;
    public GameObject completedLinePrefab;
    public GameObject progressRingPrefab;
    public GameObject trianglePrefab;
}