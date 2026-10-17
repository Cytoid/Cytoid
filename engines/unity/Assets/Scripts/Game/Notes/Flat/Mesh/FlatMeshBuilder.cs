// Pure geometry accumulator for flat note meshes. Vertex stride: x,y,z,r,g,b,a.
// Coordinates are DESIGN pixels (+y down); conversion to world happens only in
// FlatNoteMesh. PURE C#: no UnityEngine references (verified by Tools/FlatParity).

using System.Collections.Generic;

public sealed class FlatMeshBuilder
{
    public readonly List<float> Verts = new();
    public readonly List<int> Tris = new();

    public int VertCount => Verts.Count / 7;

    public void Reset()
    {
        Verts.Clear();
        Tris.Clear();
    }

    public int Vert(double x, double y, double z, Rgba c)
    {
        var index = VertCount;
        Verts.Add((float) x);
        Verts.Add((float) y);
        Verts.Add((float) z);
        Verts.Add(c.R);
        Verts.Add(c.G);
        Verts.Add(c.B);
        Verts.Add(c.A);
        return index;
    }

    public void Tri(int a, int b, int c)
    {
        Tris.Add(a);
        Tris.Add(b);
        Tris.Add(c);
    }

    public void Quad(int a, int b, int c, int d)
    {
        Tri(a, b, c);
        Tri(a, c, d);
    }
}
