using UnityEngine;

public class HoldNoteRendererProvider : SingletonMonoBehavior<HoldNoteRendererProvider>
{
    public GameObject linePrefab;
    public GameObject completedLinePrefab;
    public GameObject progressRingPrefab;
    public GameObject trianglePrefab;
}