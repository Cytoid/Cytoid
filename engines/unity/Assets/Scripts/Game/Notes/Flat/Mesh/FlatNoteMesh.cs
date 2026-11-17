using System;
using System.Collections.Generic;
using Unity.Profiling;
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

    // Lists, not arrays: animated geometry shifts its vertex count every frame, and
    // exact-size staging arrays reallocated on each shift. Lists keep their capacity
    // and feed Mesh's non-allocating Set* overloads.
    private readonly List<Vector3> verts = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<Vector2> uvCoords = new List<Vector2>();
    private readonly List<int> tris = new List<int>();

    // One Sprites/Default instance per atlas page: the texture rides the material
    // (vertex colors keep the tint; straight alpha — no premultiplication).
    private static readonly Dictionary<Texture2D, Material> TexturedMaterials = new Dictionary<Texture2D, Material>();

    private float worldScale = 1f;

    private static readonly ProfilerMarker UploadMarker =
        new ProfilerMarker(ProfilerCategory.Render, "FlatNoteMesh.Upload");

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

    public void SetSortingLayer(int layerId)
    {
        meshRenderer.sortingLayerID = layerId;
    }

    public void SetVisible(bool visible)
    {
        if (meshRenderer.enabled != visible) meshRenderer.enabled = visible;
    }

    /// <summary>Uploads builder geometry. Design px +y down → world: (x, -y) * worldScale.</summary>
    public void Upload(FlatMeshBuilder b)
    {
        using var _ = UploadMarker.Auto();
        var count = b.VertCount;
        verts.Clear();
        colors.Clear();
        var v = b.Verts;
        for (var i = 0; i < count; i++)
        {
            var o = i * 7;
            verts.Add(new Vector3(v[o] * worldScale, -v[o + 1] * worldScale, v[o + 2]));
            colors.Add(new Color(v[o + 3], v[o + 4], v[o + 5], v[o + 6]));
        }
        SetTriangles(b);
    }

    /// <summary>Fused upload: converts design px → world and applies per-part opacity in
    /// the same pass — no intermediate builder copy. Vertex alphas below headVertexCount
    /// get headOpacity (the head's exit envelope), the rest labelOpacity (FlatJudgement
    /// keeps the label's own fade window). Source alphas in the builder stay intact.</summary>
    public void Upload(FlatMeshBuilder b, float headOpacity, float labelOpacity, int headVertexCount)
    {
        using var _ = UploadMarker.Auto();
        var count = b.VertCount;
        verts.Clear();
        colors.Clear();
        var v = b.Verts;
        var headAlphaLimit = headVertexCount * 7;
        for (var i = 0; i < count; i++)
        {
            var o = i * 7;
            verts.Add(new Vector3(v[o] * worldScale, -v[o + 1] * worldScale, v[o + 2]));
            colors.Add(new Color(v[o + 3], v[o + 4], v[o + 5],
                v[o + 6] * (o < headAlphaLimit ? headOpacity : labelOpacity)));
        }
        SetTriangles(b);
    }

    /// <summary>Textured upload for the skin image mesh: design px → world like Upload,
    /// UV0 from the per-vertex uv array (vertex-index-aligned with the builder),
    /// opacity folded into vertex alphas like the fused overload. Renders through
    /// an instanced Sprites/Default material cached per atlas texture.</summary>
    public void UploadTextured(FlatMeshBuilder b, Vector2[] uv, Texture2D texture, float opacity)
    {
        using var _ = UploadMarker.Auto();
        var count = b.VertCount;
        if (uv == null || uv.Length < count || texture == null)
        {
            mesh.Clear(false);
            meshRenderer.SetPropertyBlock(propertyBlock);
            return;
        }
        verts.Clear();
        colors.Clear();
        uvCoords.Clear();
        var v = b.Verts;
        for (var i = 0; i < count; i++)
        {
            var o = i * 7;
            verts.Add(new Vector3(v[o] * worldScale, -v[o + 1] * worldScale, v[o + 2]));
            colors.Add(new Color(v[o + 3], v[o + 4], v[o + 5], v[o + 6] * opacity));
            uvCoords.Add(uv[i]);
        }
        tris.Clear();
        tris.AddRange(b.Tris);

        mesh.Clear(false);
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvCoords);
        mesh.SetTriangles(tris, 0, calculateBounds: false);
        mesh.RecalculateBounds();
        if (!TexturedMaterials.TryGetValue(texture, out var material) || material == null)
        {
            material = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture };
            TexturedMaterials[texture] = material;
        }
        meshRenderer.sharedMaterial = material;
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetTriangles(FlatMeshBuilder b)
    {
        tris.Clear();
        tris.AddRange(b.Tris);

        mesh.Clear(false);
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0, calculateBounds: false);
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
