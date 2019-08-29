using UnityEngine;

public class LongHoldNoteRendererProvider : SingletonMonoBehavior<LongHoldNoteRendererProvider>
{
    public GameObject linePrefab;
    public GameObject completedLinePrefab;
    public GameObject progressRingPrefab;
    public GameObject trianglePrefab;
}