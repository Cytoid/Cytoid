using System;
using UnityEngine;

public class FlatLongHoldNoteRenderer : FlatHoldNoteRenderer
{
    private static readonly int LongHoldLineLayer = SortingLayer.NameToID("GameLongHoldLine");
    protected override int BodySortingLayerId => LongHoldLineLayer;

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
        // storyboard can rotate; its camera controller can additionally pan, roll,
        // tilt and even switch the camera to perspective. So intersect the rail
        // rays with the camera's actual view volume in CAMERA-LOCAL space, where
        // pan and any rotation collapse into a static view shape (a rectangle for
        // ortho, a depth-scaled frustum for perspective) instead of a world-space
        // axis-aligned guess. Both long edges are cast and the farther exit kept
        // so the full rail width reaches the boundary; 1.02 covers the AA fringe
        // and MaxSpan bounds the degenerate near-parallel / toward-the-vanishing-
        // point rays, whose geometric length is otherwise unbounded.
        var camera = Game.camera;
        var cameraTransform = camera.transform;
        var origin = Note.transform.position;
        var rotation = Note.transform.rotation;
        var halfRailWorld = (float) (FlatTokens.Unit * FlatTokens.StrokeHoldBody * 0.5 * WorldScale);
        var toLocal = Quaternion.Inverse(cameraTransform.rotation);
        var localOrigin = toLocal * (origin - cameraTransform.position);
        var localPerp = toLocal * (rotation * new Vector3(1, 0, 0)) * halfRailWorld;
        var maxSpan = MaxSpan(camera, localOrigin);

        top = WorldScale != 0 ? Math.Max(0, SpanTo(localOrigin, localPerp, toLocal * (rotation * new Vector3(0, 1, 0)), camera, maxSpan) / WorldScale) : 0;
        bottom = WorldScale != 0 ? Math.Max(0, SpanTo(localOrigin, localPerp, toLocal * (rotation * new Vector3(0, -1, 0)), camera, maxSpan) / WorldScale) : 0;
    }

    /// <summary>
    /// Safety bound for degenerate rays: under perspective a rail pointing near
    /// the vanishing point never geometrically leaves the view volume. Four
    /// screen diagonals (measured at the note's depth) is far past anything
    /// visible, and the perspective taper makes the truncation unnoticeable.
    /// </summary>
    private static float MaxSpan(Camera camera, Vector3 localOrigin)
    {
        float halfHeight, halfWidth;
        if (camera.orthographic)
        {
            halfHeight = camera.orthographicSize;
            halfWidth = halfHeight * camera.aspect;
        }
        else
        {
            halfHeight = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2) * Mathf.Max(1f, localOrigin.z);
            halfWidth = halfHeight * camera.aspect;
        }
        return 4f * (2f * Mathf.Sqrt(halfHeight * halfHeight + halfWidth * halfWidth)) + Mathf.Max(0f, localOrigin.z);
    }

    private static double SpanTo(Vector3 origin, Vector3 perpendicular, Vector3 direction, Camera camera, float maxSpan)
    {
        var forward = CastDistance(origin + perpendicular, direction, camera, maxSpan);
        var backward = CastDistance(origin - perpendicular, direction, camera, maxSpan);
        return Math.Min(Math.Max(forward, backward) * 1.02, maxSpan);
    }

    /// <summary>
    /// Exit distance from `origin` (camera-local) along `direction` until the
    /// rail edge leaves the camera's view volume, via slab intersection: the
    /// axis-aligned rectangle for ortho, the |y| &lt;= z tanV / |x| &lt;= z tanV
    /// aspect frustum walls for perspective — both linear in t. Returns 0 when
    /// the ray never enters the visible volume.
    /// </summary>
    private static double CastDistance(Vector3 origin, Vector3 direction, Camera camera, float maxSpan)
    {
        double tEnter = 0, tExit = maxSpan;
        var tanV = Math.Tan(camera.fieldOfView * Math.PI / 360.0);

        // Constraint a + t*b <= 0 must hold for every visible t along the ray.
        void Constrain(double a, double b)
        {
            if (Math.Abs(b) < 1e-12)
            {
                if (a > 0) tExit = double.NegativeInfinity; // parallel to this wall and outside it: never visible
            }
            else if (b > 0) tExit = Math.Min(tExit, -a / b);
            else tEnter = Math.Max(tEnter, -a / b);
        }

        if (camera.orthographic)
        {
            var halfWidth = camera.orthographicSize * camera.aspect;
            var halfHeight = camera.orthographicSize;
            Constrain(origin.x - halfWidth, direction.x);
            Constrain(-origin.x - halfWidth, -direction.x);
            Constrain(origin.y - halfHeight, direction.y);
            Constrain(-origin.y - halfHeight, -direction.y);
        }
        else
        {
            var aspect = camera.aspect;
            Constrain(origin.y - tanV * origin.z, direction.y - tanV * direction.z);
            Constrain(-origin.y - tanV * origin.z, -direction.y - tanV * direction.z);
            Constrain(origin.x - tanV * aspect * origin.z, direction.x - tanV * aspect * direction.z);
            Constrain(-origin.x - tanV * aspect * origin.z, -direction.x - tanV * aspect * direction.z);
            Constrain(origin.z - camera.farClipPlane, direction.z);
        }

        return tExit < tEnter ? 0 : Math.Max(0, tExit);
    }

    protected override void BuildBodyGeometry(FlatMeshBuilder b)
    {
        // Style-agnostic, like classic: ClassicLongHoldNoteRenderer's fill runs
        // for both styles (its base style-2 rail stomp is a legacy artifact that
        // mostly stays off-screen) — only the scanner triangle is style-gated.
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
