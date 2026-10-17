using System;
using UnityEngine;

// Unity boundary for the flat note vector renderer: converts a FlatMeshBuilder's
// design-pixel geometry (+y down, origin = note centre) into a world-space mesh
// and uploads it. One FlatNoteMesh per blended group — notes use the normal
// material; clear effects that flash additively use the additive one.
//
// All parts of a note share ONE mesh so a note is a single draw call; part
// layering inside the mesh comes from triangle emission order (transparent
// queue renders triangles in order within a mesh, ZWrite off).
public class FlatNoteMesh : MonoBehaviour
{
    private static Material normalMaterial;
    private static Material additiveMaterial;

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    private Vector3[] verts = new Vector3[0];
    private Color[] colors = new Color[0];
    private int[] tris = new int[0];

    private float worldScale = 1f;

    // Optional refresh for geometry anchored independently of gameplay movement.
    internal Action UpdateGeometryAfterMovement;

    private void LateUpdate()
    {
        UpdateGeometryAfterMovement?.Invoke();
    }

    public static FlatNoteMesh Create(Transform parent, string name, bool additive, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var flat = go.AddComponent<FlatNoteMesh>();
        flat.meshRenderer = go.AddComponent<MeshRenderer>();
        flat.meshRenderer.sortingOrder = sortingOrder;
        flat.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flat.meshRenderer.receiveShadows = false;
        flat.meshRenderer.sharedMaterial = additive ? GetAdditiveMaterial() : GetNormalMaterial();
        flat.propertyBlock = new MaterialPropertyBlock();

        flat.mesh = new Mesh();
        flat.mesh.MarkDynamic();
        flat.meshFilter = go.AddComponent<MeshFilter>();
        flat.meshFilter.sharedMesh = flat.mesh;
        return flat;
    }

    private static Material GetNormalMaterial()
    {
        if (normalMaterial == null)
            normalMaterial = new Material(Shader.Find("Sprites/Default"));
        return normalMaterial;
    }

    private static Material GetAdditiveMaterial()
    {
        if (additiveMaterial == null)
        {
            var shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            additiveMaterial = new Material(shader);
        }
        return additiveMaterial;
    }

    public void SetWorldScale(float scale)
    {
        worldScale = scale;
    }

    public void SetSortingOrder(int order)
    {
        meshRenderer.sortingOrder = order;
    }

    public void SetVisible(bool visible)
    {
        if (meshRenderer.enabled != visible) meshRenderer.enabled = visible;
    }

    /// <summary>Uploads builder geometry. Design px +y down → world: (x, -y) * worldScale.</summary>
    public void Upload(FlatMeshBuilder b)
    {
        var count = b.VertCount;
        if (verts.Length != count)
        {
            verts = new Vector3[count];
            colors = new Color[count];
        }
        var v = b.Verts;
        for (var i = 0; i < count; i++)
        {
            var o = i * 7;
            verts[i] = new Vector3(v[o] * worldScale, -v[o + 1] * worldScale, v[o + 2]);
            colors[i] = new Color(v[o + 3], v[o + 4], v[o + 5], v[o + 6]);
        }
        var triCount = b.Tris.Count;
        if (tris.Length != triCount) tris = new int[triCount];
        for (var i = 0; i < triCount; i++) tris[i] = b.Tris[i];

        mesh.Clear(false);
        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        // Sprites/Default has no per-renderer color; keep the block empty so the
        // shared material never gets instanced. Vertex colors carry all tinting.
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void Dispose()
    {
        UpdateGeometryAfterMovement = null;
        if (mesh != null) Destroy(mesh);
        Destroy(gameObject);
    }

    private MeshFilter meshFilter;
}
