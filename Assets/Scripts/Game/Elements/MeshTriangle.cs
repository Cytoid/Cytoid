using System;
using UnityEngine;

public class MeshTriangle : MonoBehaviour
{
    [NonSerialized] public Note Note;

    private Mesh mesh;
    private Scanner scanner;
    private Camera mainCamera;

    private void OnEnable()
    {
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.vertices = new[]
        {
            new Vector3(),
            new Vector3(),
            new Vector3()
        };
        mesh.uv = new[]
        {
            new Vector2(),
            new Vector2(),
            new Vector2()
        };
        mesh.triangles = new[] {0, 1, 2};
        scanner = Scanner.Instance;
        mainCamera = Camera.main;
    }

    public void Reset()
    {
        mesh.vertices = new[]
        {
            new Vector3(),
            new Vector3(),
            new Vector3()
        };
        mesh.uv = new[]
        {
            new Vector2(),
            new Vector2(),
            new Vector2()
        };
        mesh.triangles = new[] {0, 1, 2};
    }

    public void OnUpdate()
    {
        var orthographicSize = mainCamera.orthographicSize;
        var scannerPosition = scanner.transform.position;
        var notePosition = Note.transform.position;
        mesh.vertices = new[]
        {
            notePosition,
            new Vector3(-orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height,
                scannerPosition.y),
            new Vector3(orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height,
                scannerPosition.y)
        };

        mesh.uv = new[]
        {
            (Vector2) notePosition,
            new Vector2(-orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height,
                scannerPosition.y),
            new Vector2(orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height,
                scannerPosition.y)
        };
        mesh.triangles = new[] {0, 1, 2};
    }
}