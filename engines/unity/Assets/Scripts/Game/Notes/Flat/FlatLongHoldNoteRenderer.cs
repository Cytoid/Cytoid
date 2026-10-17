using System;
using UnityEngine;

public class FlatLongHoldNoteRenderer : FlatHoldNoteRenderer
{
    private readonly double[] square = new double[8];
    private readonly FlatLongHoldRail rail = new FlatLongHoldRail();

    public FlatLongHoldNoteRenderer(LongHoldNote note) : base(note)
    {
    }

    protected override void BuildGlyphGeometry(FlatMeshBuilder b, double radius, double width, Rgba color)
    {
        // 0.82 half-size gives the square the same visual area as the hold circle.
        var s = radius * 0.82;
        square[0] = square[6] = -s;
        square[2] = square[4] = s;
        square[1] = square[3] = -s;
        square[5] = square[7] = s;
        FlatGeom.PolyStroke(b, square, true, width, color, FlatJoin.Miter, FlatCap.Butt);
    }

    protected override void GetBodyDimensions(out double top, out double bottom, out bool flipY)
    {
        base.GetBodyDimensions(out top, out bottom, out flipY);
        // The rail is a rectangle drawn along the note's own body axis, which the
        // storyboard can rotate. Measuring the centerline alone leaves the outer
        // long edge short of the screen edge by halfWidth * sin(angle) once rotated,
        // so cast from BOTH long edges and keep the farther hit; the 2% margin only
        // covers the anti-aliasing fringe at the boundary.
        var cameraTransform = Game.camera.transform;
        var halfHeight = Game.camera.orthographicSize;
        var halfWidth = halfHeight * Game.camera.aspect;
        var origin = Note.transform.position;
        var rotation = Note.transform.rotation;
        var halfRailWorld = (float) (FlatTokens.Unit * FlatTokens.StrokeHoldBody * 0.5 * WorldScale);
        var perpendicular = rotation * new Vector3(1, 0, 0) * halfRailWorld;
        top = WorldScale != 0 ? Math.Max(0, SpanTo(origin, perpendicular, rotation * new Vector3(0, 1, 0), cameraTransform, halfWidth, halfHeight) / WorldScale) : 0;
        bottom = WorldScale != 0 ? Math.Max(0, SpanTo(origin, perpendicular, rotation * new Vector3(0, -1, 0), cameraTransform, halfWidth, halfHeight) / WorldScale) : 0;
    }

    /// <summary>
    /// Distance to the viewport boundary along `direction` from both long edges of
    /// the rail rectangle (origin ± perpendicular), so the whole rail width reaches
    /// the edge. The 2% margin covers the AA fringe at the boundary.
    /// </summary>
    private static double SpanTo(Vector3 origin, Vector3 perpendicular, Vector3 direction, Transform cameraTransform,
        float halfWidth, float halfHeight)
    {
        var forward = CastDistance(origin + perpendicular, direction, cameraTransform, halfWidth, halfHeight);
        var backward = CastDistance(origin - perpendicular, direction, cameraTransform, halfWidth, halfHeight);
        return Math.Max(forward, backward) * 1.02;
    }

    private static double CastDistance(Vector3 origin, Vector3 direction, Transform cameraTransform,
        float halfWidth, float halfHeight)
    {
        var distance = double.MaxValue;
        if (Math.Abs(direction.y) > 1e-6)
        {
            var ty = direction.y > 0
                ? (cameraTransform.position.y + halfHeight - origin.y) / direction.y
                : (cameraTransform.position.y - halfHeight - origin.y) / direction.y;
            if (ty > 0) distance = Math.Min(distance, ty);
        }
        if (Math.Abs(direction.x) > 1e-6)
        {
            var tx = direction.x > 0
                ? (cameraTransform.position.x + halfWidth - origin.x) / direction.x
                : (cameraTransform.position.x - halfWidth - origin.x) / direction.x;
            if (tx > 0) distance = Math.Min(distance, tx);
        }
        return distance == double.MaxValue ? 0 : distance;
    }

    protected override void BuildBodyGeometry(FlatMeshBuilder b)
    {
        rail.Draw(b, Palette, BodyTop / BodySizeMultiplier, BodyBottom / BodySizeMultiplier,
            BodyProgress, BodyTime, Note.Model.Duration, EnterProgress);
    }

    protected override Action<FlatMeshBuilder, float> CaptureExitBody()
    {
        if (BodySizeMultiplier == 0) return null;
        var palette = Palette;
        var top = BodyTop / BodySizeMultiplier;
        var bottom = BodyBottom / BodySizeMultiplier;
        var duration = Note.Model.Duration;
        return (b, p) => rail.Draw(b, palette, top, bottom, 0, 0, duration, p);
    }
}
