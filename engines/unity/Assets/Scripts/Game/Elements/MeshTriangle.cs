using System;
using UnityEngine;

public class MeshTriangle : MonoBehaviour
{
    private static readonly int MaterialColor = Shader.PropertyToID("Tint");
    
    [NonSerialized] public Note Note;

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Material material;
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
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        material = meshRenderer.material;
        scanner = Scanner.Instance;
        mainCamera = Camera.main;
        if (scanner != null) scanner.RegisterTriangle(this);
    }

    private void OnDisable()
    {
        if (scanner != null) scanner.UnregisterTriangle(this);
    }

    public void Reset()
    {
        if (mesh == null) return;
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
        
        ApplyOpacity(scanner.EffectiveOpacity);
    }

    public void ApplyOpacity(float scannerOpacity)
    {
        if (material != null) material.color = Color.white.WithAlpha(0.1f * scannerOpacity);
    }
}
