// https://github.com/azixMcAze/Unity-UIGradient

using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class GradientMeshEffect : BaseMeshEffect
{
    public Color startColor = Color.white;
    public Color endColor = Color.white;
    [Range(-180f, 180f)] public float angle;
    public bool ignoreRatio;

    public ColorGradient GetGradient()
    {
        return new ColorGradient(startColor, endColor, angle);
    }

    public void SetGradient(ColorGradient gradient)
    {
        startColor = gradient.startColor;
        endColor = gradient.endColor;
        angle = gradient.angle;
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!enabled) return;

        var rect = graphic.rectTransform.rect;
        var dir = RotationDir(angle);

        if (!ignoreRatio)
            dir = CompensateAspectRatio(rect, dir);
        
        var localPositionMatrix = LocalPositionMatrix(rect, dir);

        var vertex = default(UIVertex);
        for (var i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            var localPosition = localPositionMatrix * vertex.position;
            vertex.color *= Color.Lerp(endColor, startColor, localPosition.y);
            vh.SetUIVertex(vertex, i);
        }
    }

    public struct Matrix2x3
    {
        public float m00, m01, m02, m10, m11, m12;

        public Matrix2x3(float m00, float m01, float m02, float m10, float m11, float m12)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;
        }

        public static Vector2 operator *(Matrix2x3 m, Vector2 v)
        {
            var x = m.m00 * v.x - m.m01 * v.y + m.m02;
            var y = m.m10 * v.x + m.m11 * v.y + m.m12;
            return new Vector2(x, y);
        }
    }

    public static Matrix2x3 LocalPositionMatrix(Rect rect, Vector2 dir)
    {
        var cos = dir.x;
        var sin = dir.y;
        var rectMin = rect.min;
        var rectSize = rect.size;
        var c = 0.5f;
        var ax = rectMin.x / rectSize.x + c;
        var ay = rectMin.y / rectSize.y + c;
        var m00 = cos / rectSize.x;
        var m01 = sin / rectSize.y;
        var m02 = -(ax * cos - ay * sin - c);
        var m10 = sin / rectSize.x;
        var m11 = cos / rectSize.y;
        var m12 = -(ax * sin + ay * cos - c);
        return new Matrix2x3(m00, m01, m02, m10, m11, m12);
    }

    public static Vector2 RotationDir(float angle)
    {
        var angleRad = angle * Mathf.Deg2Rad;
        var cos = Mathf.Cos(angleRad);
        var sin = Mathf.Sin(angleRad);
        return new Vector2(cos, sin);
    }

    public static Vector2 CompensateAspectRatio(Rect rect, Vector2 dir)
    {
        var ratio = rect.height / rect.width;
        dir.x *= ratio;
        return dir.normalized;
    }
}