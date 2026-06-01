using UnityEngine;

public class ClassicLongHoldNoteRendererProvider : SingletonMonoBehavior<ClassicLongHoldNoteRendererProvider>
{
    public GameObject linePrefab;
    public GameObject completedLinePrefab;
    public GameObject progressRingPrefab;
    public GameObject trianglePrefab;
}