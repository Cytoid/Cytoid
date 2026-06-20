using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Batched expanding ring mesh for note clear / calibration feedback.
/// Replaces the former FlatFX Ripple usage with an in-house implementation.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class NoteRippleEffect : MonoBehaviour
{
    const int MaxTriangleIndices = 100_000;

    readonly List<RippleBurst> bursts = new List<RippleBurst>(16);
    readonly List<Vector3> vertices = new List<Vector3>(512);
    readonly List<Color> colors = new List<Color>(512);
    readonly List<int> triangles = new List<int>(2048);

    Mesh mesh;
    MeshFilter meshFilter;
    Material material;

    static readonly Dictionary<int, UnitCircle> UnitCircles = new Dictionary<int, UnitCircle>();

    void Awake()
    {
    meshFilter = GetComponent<MeshFilter>();
    var meshRenderer = GetComponent<MeshRenderer>();

    mesh = new Mesh { name = "NoteRippleEffect" };
    mesh.MarkDynamic();
    meshFilter.sharedMesh = mesh;

    if (meshRenderer.sharedMaterial == null)
    {
        material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.sharedMaterial = material;
    }
    }

    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
            material = null;
        }
    }

    /// <param name="worldPosition">World-space center of the ring.</param>
    /// <param name="lifetime">Seconds until the burst is removed.</param>
    /// <param name="sectorCount">Number of segments around the circle (e.g. 4, 24, 96).</param>
    /// <param name="startColor">Color at the start of the animation (typically opaque).</param>
    /// <param name="endColor">Color at the end (typically transparent).</param>
    /// <param name="startDiameter">Outer diameter at spawn (FlatFX "size").</param>
    /// <param name="endDiameter">Outer diameter when the burst finishes.</param>
    /// <param name="startThickness">Ring band width at spawn.</param>
    /// <param name="endThickness">Ring band width when the burst finishes.</param>
    public void PlayRing(
        Vector3 worldPosition,
        float lifetime,
        int sectorCount,
        Color startColor,
        Color endColor,
        float startDiameter,
        float endDiameter,
        float startThickness,
        float endThickness)
    {
        if (lifetime <= 0f || sectorCount < 3)
        {
            return;
        }

        var localCenter = transform.InverseTransformPoint(worldPosition);
        bursts.Add(new RippleBurst(
            Time.time,
            lifetime,
            sectorCount,
            new Vector2(localCenter.x, localCenter.y),
            startColor,
            endColor,
            startDiameter,
            endDiameter,
            startThickness,
            endThickness));
    }

    void LateUpdate()
    {
        RebuildMesh();
    }

    void RebuildMesh()
    {
        if (bursts.Count == 0 && mesh.vertexCount == 0)
        {
            return;
        }

        var now = Time.time;
        for (var i = bursts.Count - 1; i >= 0; i--)
        {
            if (bursts[i].IsDead(now))
            {
                bursts.RemoveAt(i);
            }
        }

        vertices.Clear();
        colors.Clear();
        triangles.Clear();

        var triangleBudget = MaxTriangleIndices;
        for (var i = 0; i < bursts.Count && triangleBudget > 0; i++)
        {
            triangleBudget -= bursts[i].AppendMesh(now, vertices, colors, triangles, triangleBudget);
        }

        mesh.Clear();
        if (vertices.Count == 0)
        {
            return;
        }

        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    readonly struct RippleBurst
    {
        readonly float createdAt;
        readonly float lifetime;
        readonly int sectorCount;
        readonly Vector2 center;
        readonly Color startColor;
        readonly Color endColor;
        readonly float startDiameter;
        readonly float endDiameter;
        readonly float startThickness;
        readonly float endThickness;

        public RippleBurst(
            float createdAt,
            float lifetime,
            int sectorCount,
            Vector2 center,
            Color startColor,
            Color endColor,
            float startDiameter,
            float endDiameter,
            float startThickness,
            float endThickness)
        {
            this.createdAt = createdAt;
            this.lifetime = lifetime;
            this.sectorCount = sectorCount;
            this.center = center;
            this.startColor = startColor;
            this.endColor = endColor;
            this.startDiameter = startDiameter;
            this.endDiameter = endDiameter;
            this.startThickness = startThickness;
            this.endThickness = endThickness;
        }

        public bool IsDead(float now) => (now - createdAt) / lifetime > 1f;

        public int AppendMesh(float now, List<Vector3> vertices, List<Color> colors, List<int> triangles, int triangleBudget)
        {
            var t = (now - createdAt) / lifetime;
            if (t > 1f)
            {
                return 0;
            }

            var eased = EaseOutQuint(t);
            var outerRadius = Mathf.Lerp(startDiameter, endDiameter, eased) * 0.5f;
            var thickness = Mathf.Lerp(startThickness, endThickness, eased);
            var innerRadius = Mathf.Max(0f, outerRadius - thickness * 0.5f);
            var innerColor = Color.Lerp(startColor, endColor, eased);
            var outerColor = innerColor;

            if (outerRadius <= 0f)
            {
                return 0;
            }

            var indicesPerBurst = sectorCount * 6;
            if (triangleBudget < indicesPerBurst)
            {
                return 0;
            }

            var circle = GetUnitCircle(sectorCount);
            var baseVertex = vertices.Count;
            for (var i = 0; i < sectorCount; i++)
            {
                vertices.Add(new Vector3(
                    center.x + circle.Cos[i] * innerRadius,
                    center.y + circle.Sin[i] * innerRadius,
                    0f));
                colors.Add(innerColor);

                vertices.Add(new Vector3(
                    center.x + circle.Cos[i] * outerRadius,
                    center.y + circle.Sin[i] * outerRadius,
                    0f));
                colors.Add(outerColor);
            }

            for (var i = 0; i < sectorCount; i++)
            {
                var i0 = baseVertex + i * 2;
                var i1 = i0 + 1;
                var next = baseVertex + ((i + 1) % sectorCount) * 2;
                var i2 = next + 1;
                var i3 = next;

                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i3);
            }

            return indicesPerBurst;
        }
    }

    readonly struct UnitCircle
    {
        public readonly float[] Cos;
        public readonly float[] Sin;

        public UnitCircle(int sectorCount)
        {
            Cos = new float[sectorCount];
            Sin = new float[sectorCount];
            var step = Mathf.PI * 2f / sectorCount;
            for (var i = 0; i < sectorCount; i++)
            {
                var angle = step * i;
                Cos[i] = Mathf.Cos(angle);
                Sin[i] = Mathf.Sin(angle);
            }
        }
    }

    static UnitCircle GetUnitCircle(int sectorCount)
    {
        if (!UnitCircles.TryGetValue(sectorCount, out var circle))
        {
            circle = new UnitCircle(sectorCount);
            UnitCircles[sectorCount] = circle;
        }

        return circle;
    }

    static float EaseOutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);
}
