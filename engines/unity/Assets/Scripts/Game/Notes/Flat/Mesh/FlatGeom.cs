// Flat vector primitive geometry generators (scene.ts node vocabulary → triangles).
// PURE C#: no UnityEngine references (verified by Tools/FlatParity).

using System;

public enum FlatCap
{
    Butt,
    Round,
    Square,
}

public enum FlatJoin
{
    Miter,
    Round,
    Bevel,
}

public static class FlatGeom
{
    public const double EdgeFeather = 0.5;

    private const double Tau = Math.PI * 2;
    private const double Epsilon = 1e-10;
    private const int CapSegments = 16;
    private const double Inset = EdgeFeather / 2;

    public static void Disk(FlatMeshBuilder b, double cx, double cy, double r, Rgba fill, double z = 0)
    {
        if (!(r > 0)) return;
        var inner = Math.Max(0, r - Inset);
        var outer = r + Inset;
        var segments = Math.Max(26, CircleSegments(outer));
        var solid = Alpha(fill, Math.Min(1, r / Inset));
        var clear = Alpha(fill, 0);
        var center = b.Vert(cx, cy, z, solid);
        var first = b.Vert(cx, cy - inner, z, solid);
        var firstOuter = b.Vert(cx, cy - outer, z, clear);
        var previous = first;
        var previousOuter = firstOuter;
        for (var i = 1; i <= segments; i++)
        {
            var a = Tau * i / segments;
            var next = i == segments ? first : b.Vert(cx + Math.Sin(a) * inner, cy - Math.Cos(a) * inner, z, solid);
            var nextOuter = i == segments ? firstOuter : b.Vert(cx + Math.Sin(a) * outer, cy - Math.Cos(a) * outer, z, clear);
            Triangle(b, center, previous, next);
            Strip(b, previous, next, previousOuter, nextOuter);
            previous = next;
            previousOuter = nextOuter;
        }
    }

    public static void Ring(FlatMeshBuilder b, double cx, double cy, double r, double width, Rgba stroke, double z = 0)
    {
        if (!(r > 0) || !(width > 0)) return;
        Annulus(b, cx, cy, r, 0, Tau, width, stroke, CircleSegments(r + width / 2 + Inset), false, z);
    }

    /// <summary>Open stroked arc; angles in radians, 0 = 12 o'clock, clockwise.</summary>
    public static void Arc(FlatMeshBuilder b, double cx, double cy, double r, double start, double end,
        double width, Rgba stroke, bool roundCap = false, double z = 0)
    {
        if (!(r > 0) || !(width > 0)) return;
        var sweep = end - start;
        if (Math.Abs(sweep) < 1e-4) return;
        sweep = Math.Max(-Tau, Math.Min(Tau, sweep));
        var closed = Math.Abs(sweep) >= Tau;
        var outer = r + width / 2 + Inset;
        var buttCaps = !roundCap && !closed;
        var coverage = Math.Abs(sweep) + (buttCaps ? 2 * Math.Asin(Math.Min(1, Inset / outer)) : 0);
        var segments = SweepSegments(outer, coverage, 4);
        Annulus(b, cx, cy, r, start, sweep, width, stroke, segments, buttCaps, z);
        if (!roundCap || closed) return;

        var direction = Math.Sign(sweep);
        var last = start + sweep;
        var solid = Alpha(stroke, Math.Min(1, width / EdgeFeather));
        Cap(b, new Point(cx + Math.Sin(start) * r, cy - Math.Cos(start) * r),
            new Point(-direction * Math.Cos(start), -direction * Math.Sin(start)), width / 2, solid, z);
        Cap(b, new Point(cx + Math.Sin(last) * r, cy - Math.Cos(last) * r),
            new Point(direction * Math.Cos(last), direction * Math.Sin(last)), width / 2, solid, z);
    }

    public static void RoundedRect(FlatMeshBuilder b, double x, double y, double w, double h, double rx,
        Rgba fill, double z = 0)
    {
        if (!(w > 0) || !(h > 0)) return;
        rx = Math.Max(0, Math.Min(rx, Math.Min(w, h) / 2));
        var segments = Math.Max(7, SweepSegments(rx + Inset, Math.PI / 2, 1));
        // Contours are emitted straight into the builder: holds draw these every
        // frame, so no per-call contour arrays. Same per-corner clamps and
        // expressions as the RoundedContour they replace.
        var innerX = x + Math.Min(Inset, w / 2);
        var innerY = y + Math.Min(Inset, h / 2);
        var innerW = Math.Max(0, w - EdgeFeather);
        var innerH = Math.Max(0, h - EdgeFeather);
        var innerRadius = Math.Min(Math.Max(0, rx - Inset), Math.Min(innerW, innerH) / 2);
        var outerX = x - Inset;
        var outerY = y - Inset;
        var outerW = w + EdgeFeather;
        var outerH = h + EdgeFeather;
        var outerRadius = Math.Min(rx + Inset, Math.Min(outerW, outerH) / 2);
        var solid = Alpha(fill, Math.Min(1, Math.Min(w, h) / EdgeFeather));
        var clear = Alpha(solid, 0);
        var count = 4 * (segments + 1);
        var first = b.VertCount;
        for (var i = 0; i < count; i++)
        {
            var corner = i / (segments + 1);
            var step = i % (segments + 1);
            var angle = -Math.PI / 2 + corner * Math.PI / 2 + step * Math.PI / (2 * segments);
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var innerCx = corner < 2 ? innerX + innerW - innerRadius : innerX + innerRadius;
            var innerCy = corner == 0 || corner == 3 ? innerY + innerRadius : innerY + innerH - innerRadius;
            b.Vert(innerCx + cos * innerRadius, innerCy + sin * innerRadius, z, solid);
            var outerCx = corner < 2 ? outerX + outerW - outerRadius : outerX + outerRadius;
            var outerCy = corner == 0 || corner == 3 ? outerY + outerRadius : outerY + outerH - outerRadius;
            b.Vert(outerCx + cos * outerRadius, outerCy + sin * outerRadius, z, clear);
        }
        for (var i = 1; i + 1 < count; i++)
            Triangle(b, first, first + i * 2, first + (i + 1) * 2);
        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            Strip(b, first + i * 2, first + next * 2, first + i * 2 + 1, first + next * 2 + 1);
        }
    }

    /// <summary>pts = flat x,y pairs. Stroke with join/cap control (chevrons, diamonds, edges).</summary>
    public static void PolyStroke(FlatMeshBuilder b, double[] pts, bool closed, double width, Rgba stroke,
        FlatJoin join, FlatCap cap, double z = 0)
        => PolyStroke(b, pts, pts?.Length ?? 0, closed, width, stroke, join, cap, z);

    /// <summary>Scratch-friendly variant: only the first `length` entries of pts are stroked,
    /// so a shared buffer longer than this stroke carries no stale tail geometry.</summary>
    public static void PolyStroke(FlatMeshBuilder b, double[] pts, int length, bool closed, double width,
        Rgba stroke, FlatJoin join, FlatCap cap, double z = 0)
    {
        if (pts == null || pts.Length < 4 || length < 4 || length > pts.Length || !(width > 0)) return;
        var points = CleanPoints(pts, length, closed, out var count);
        if (count < 2) return;
        var half = width / 2;
        var innerHalf = Math.Max(0, half - Inset);
        var outerHalf = half + Inset;
        var solid = Alpha(stroke, Math.Min(1, width / EdgeFeather));
        var segmentCount = closed ? count : count - 1;
        var inner = StrokeSegments(points, count, closed, innerHalf, ref innerScratch);
        var outer = StrokeSegments(points, count, closed, outerHalf, ref outerScratch);
        for (var i = closed ? 0 : 1; i < (closed ? count : count - 1); i++)
        {
            var previous = (i + segmentCount - 1) % segmentCount;
            var next = i % segmentCount;
            Join(b, points[i], ref inner[previous], ref inner[next], ref outer[previous], ref outer[next],
                innerHalf, outerHalf, join, solid, z);
        }
        if (!closed && cap != FlatCap.Round)
        {
            // A butt's opaque edge is inside the endpoint; a square extends by half width.
            var startInset = Math.Min(Inset, Distance(points[0], points[1]) / 2);
            var endInset = Math.Min(Inset, Distance(points[count - 2], points[count - 1]) / 2);
            MoveStart(ref inner[0], cap == FlatCap.Square ? -innerHalf : startInset);
            MoveEnd(ref inner[segmentCount - 1], cap == FlatCap.Square ? innerHalf : -endInset);
            MoveStart(ref outer[0], cap == FlatCap.Square ? -outerHalf : -Inset);
            MoveEnd(ref outer[segmentCount - 1], cap == FlatCap.Square ? outerHalf : Inset);
            if (count == 2 && cap == FlatCap.Butt)
                solid = Alpha(solid, Math.Min(1, Distance(points[0], points[1]) / EdgeFeather));
        }
        var firstSegment = b.VertCount;
        var lastSegment = firstSegment;
        for (var i = 0; i < segmentCount; i++)
            lastSegment = Segment(b, inner[i], outer[i], solid, z);
        if (closed) return;
        if (cap == FlatCap.Round)
        {
            Cap(b, points[0], Scale(inner[0].Direction, -1), half, solid, z);
            Cap(b, points[count - 1], inner[inner.Length - 1].Direction, half, solid, z);
        }
        else
        {
            Strip(b, firstSegment, firstSegment + 1, firstSegment + 4, firstSegment + 5);
            Strip(b, lastSegment + 3, lastSegment + 2, lastSegment + 7, lastSegment + 6);
        }
    }

    public static void Line(FlatMeshBuilder b, double x1, double y1, double x2, double y2, double width,
        Rgba stroke, FlatCap cap, double z = 0)
    {
        if (!(width > 0)) return;
        var a = new Point(x1, y1);
        var c = new Point(x2, y2);
        var length = Distance(a, c);
        if (length <= Epsilon) return;
        var direction = Scale(Subtract(c, a), 1 / length);
        var half = width / 2;
        var innerHalf = Math.Max(0, half - Inset);
        var outerHalf = half + Inset;
        var inner = MakeSegment(a, c, direction, innerHalf);
        var outer = MakeSegment(a, c, direction, outerHalf);
        var solid = Alpha(stroke, Math.Min(1, width / EdgeFeather));
        if (cap != FlatCap.Round)
        {
            var inset = Math.Min(Inset, length / 2);
            MoveStart(ref inner, cap == FlatCap.Square ? -innerHalf : inset);
            MoveEnd(ref inner, cap == FlatCap.Square ? innerHalf : -inset);
            MoveStart(ref outer, cap == FlatCap.Square ? -outerHalf : -Inset);
            MoveEnd(ref outer, cap == FlatCap.Square ? outerHalf : Inset);
            if (cap == FlatCap.Butt) solid = Alpha(solid, Math.Min(1, length / EdgeFeather));
        }
        var first = Segment(b, inner, outer, solid, z);
        if (cap == FlatCap.Round)
        {
            Cap(b, a, Scale(direction, -1), half, solid, z);
            Cap(b, c, direction, half, solid, z);
        }
        else
        {
            Strip(b, first, first + 1, first + 4, first + 5);
            Strip(b, first + 3, first + 2, first + 7, first + 6);
        }
    }

    /// <summary>dash pattern in design px along the segment; dashOffset shifts phase forward.</summary>
    public static void DashedLine(FlatMeshBuilder b, double x1, double y1, double x2, double y2, double width,
        Rgba stroke, double[] dash, double dashOffset, FlatCap cap, double z = 0)
    {
        if (!(width > 0)) return;
        if (dash == null || dash.Length == 0)
        {
            Line(b, x1, y1, x2, y2, width, stroke, cap, z);
            return;
        }
        var period = 0.0;
        for (var i = 0; i < dash.Length; i++)
        {
            if (dash[i] < 0 || double.IsNaN(dash[i]) || double.IsInfinity(dash[i])) return;
            period += dash[i];
        }
        // Canvas repeats odd-length patterns twice so on/off parity repeats too.
        var patternCount = dash.Length % 2 == 0 ? dash.Length : dash.Length * 2;
        if (patternCount != dash.Length) period *= 2;
        if (period == 0)
        {
            Line(b, x1, y1, x2, y2, width, stroke, cap, z);
            return;
        }
        if (double.IsNaN(period) || double.IsInfinity(period) || double.IsNaN(dashOffset) || double.IsInfinity(dashOffset)) return;
        var dx = x2 - x1;
        var dy = y2 - y1;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= Epsilon) return;
        dx /= length;
        dy /= length;
        // Positive offsets move dash starts forward along the path (the phase-5 parity contract).
        // Canvas lineDashOffset uses the opposite sign when consuming the pattern.
        var phase = -dashOffset % period;
        if (phase < 0) phase += period;
        var index = 0;
        // Strict boundary handling leaves offset 0 exactly at the first dash.
        while (phase >= dash[index % dash.Length])
        {
            phase -= dash[index % dash.Length];
            index = (index + 1) % patternCount;
        }
        var remaining = dash[index % dash.Length] - phase;
        var distance = 0.0;
        var runStart = -1.0;
        var runEnd = 0.0;
        while (distance < length)
        {
            var end = Math.Min(length, distance + remaining);
            if (end > distance && index % 2 == 0)
            {
                if (runStart < 0) runStart = distance;
                runEnd = end;
            }
            else if (end > distance && runStart >= 0)
            {
                Line(b, x1 + dx * runStart, y1 + dy * runStart, x1 + dx * runEnd, y1 + dy * runEnd, width, stroke, cap, z);
                runStart = -1;
            }
            if (end == length) break;
            distance = end;
            index = (index + 1) % patternCount;
            remaining = dash[index % dash.Length];
        }
        // A zero-length gap is an internal seam, so adjacent on-runs share one cap pair.
        if (runStart >= 0)
            Line(b, x1 + dx * runStart, y1 + dy * runStart, x1 + dx * runEnd, y1 + dy * runEnd, width, stroke, cap, z);
    }

    public static void PolyFill(FlatMeshBuilder b, double[] pts, Rgba fill, double z = 0)
    {
        if (pts == null || pts.Length < 6) return;
        var points = CleanPoints(pts, pts.Length, true, out var count);
        if (count < 3) return;
        // Fills stay hard-edged on purpose: canvas AA is only meaningful on long
        // smooth outlines (strokes), while small interior fills (chain arrows,
        // shard squares) turn visibly mushy with a feather band.
        var signedArea = 0.0;
        for (var i = 0; i < count; i++)
        {
            var next = points[(i + 1) % count];
            signedArea += points[i].X * next.Y - points[i].Y * next.X;
        }
        if (Math.Abs(signedArea) <= Epsilon) return;
        var flip = signedArea < 0;
        var first = b.VertCount;
        for (var i = 0; i < count; i++) b.Vert(points[i].X, points[i].Y, z, fill);
        for (var i = 2; i < count; i++)
            if (flip) b.Tri(first, first + i, first + i - 1);
            else b.Tri(first, first + i - 1, first + i);
    }

    // The outermost feather contour, rather than the centerline, controls chord sagitta.
    private static int CircleSegments(double r)
    {
        var tol = Math.Min(1.0, 0.1 / Math.Max(r, 1e-9));
        return Math.Max(8, (int) Math.Ceiling(Math.PI / Math.Acos(1 - tol)));
    }

    private static int SweepSegments(double r, double sweep, int minimum) =>
        Math.Max(minimum, (int) Math.Ceiling(CircleSegments(r) * Math.Abs(sweep) / Tau));

    private static void Annulus(FlatMeshBuilder b, double cx, double cy, double r, double start, double sweep,
        double width, Rgba color, int segments, bool buttCaps, double z)
    {
        var half = width / 2;
        var innerHalf = Math.Max(0, half - Inset);
        // Locals, not an array: Annulus runs on every Ring/Arc of every animated frame.
        var innerFeather = Math.Max(0, r - half - Inset);
        var innerSolid = Math.Max(0, r - innerHalf);
        var outerSolid = r + innerHalf;
        var outerFeather = r + half + Inset;
        var peak = Math.Min(1, width / EdgeFeather);
        // Very short butt arcs lose their solid length, just like short straight dashes.
        if (buttCaps) peak *= Math.Min(1, r * Math.Abs(sweep) / EdgeFeather);
        var solid = Alpha(color, peak);
        var clear = Alpha(color, 0);
        var first = b.VertCount;
        for (var i = 0; i <= segments; i++)
        {
            var progress = (double) i / segments;
            for (var j = 0; j < 4; j++)
            {
                var radius = j == 0 ? innerFeather : j == 1 ? innerSolid : j == 2 ? outerSolid : outerFeather;
                var opaque = j == 1 || j == 2;
                var angle = start + sweep * progress;
                if (buttCaps)
                {
                    // Intersect each circle with the transverse cap planes and
                    // sample between those intersections. Moving only the end
                    // vertices would fold strips on arcs shorter than a feather.
                    var offset = Math.Asin(Math.Min(1, Inset / Math.Max(radius, Epsilon)));
                    if (opaque) offset = Math.Min(offset, Math.Abs(sweep) / 2);
                    else offset = -offset;
                    angle += Math.Sign(sweep) * offset * (1 - 2 * progress);
                    if (opaque && r * Math.Abs(sweep) <= EdgeFeather) angle = start + sweep / 2;
                }
                b.Vert(cx + Math.Sin(angle) * radius, cy - Math.Cos(angle) * radius, z, opaque ? solid : clear);
            }
            if (i == 0) continue;
            var previous = first + (i - 1) * 4;
            var current = first + i * 4;
            for (var j = 0; j < 3; j++)
                Strip(b, previous + j, current + j, previous + j + 1, current + j + 1);
        }
        if (buttCaps)
        {
            // Close the transverse ends; no feather is emitted on internal strip seams.
            Strip(b, first + 1, first + 2, first, first + 3);
            var last = first + segments * 4;
            Strip(b, last + 1, last + 2, last, last + 3);
        }
    }

    // Shared scratch for the per-frame stroke path (renderers draw on Unity's main
    // thread only). Grows monotonically, so steady-state rebuilds never allocate.
    private static Point[] cleanScratch = Array.Empty<Point>();
    private static StrokeSegment[] innerScratch = Array.Empty<StrokeSegment>();
    private static StrokeSegment[] outerScratch = Array.Empty<StrokeSegment>();

    private static StrokeSegment[] StrokeSegments(Point[] points, int count, bool closed, double half,
        ref StrokeSegment[] scratch)
    {
        var segments = closed ? count : count - 1;
        if (scratch.Length < segments) scratch = new StrokeSegment[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = points[i];
            var c = points[(i + 1) % count];
            scratch[i] = MakeSegment(a, c, Scale(Subtract(c, a), 1 / Distance(a, c)), half);
        }
        return scratch;
    }

    private static StrokeSegment MakeSegment(Point a, Point c, Point direction, double half)
    {
        var normal = new Point(-direction.Y * half, direction.X * half);
        return new StrokeSegment
        {
            Direction = direction,
            StartLeft = Add(a, normal), StartRight = Subtract(a, normal),
            EndLeft = Add(c, normal), EndRight = Subtract(c, normal),
        };
    }

    private static void MoveStart(ref StrokeSegment segment, double distance)
    {
        var offset = Scale(segment.Direction, distance);
        segment.StartLeft = Add(segment.StartLeft, offset);
        segment.StartRight = Add(segment.StartRight, offset);
    }

    private static void MoveEnd(ref StrokeSegment segment, double distance)
    {
        var offset = Scale(segment.Direction, distance);
        segment.EndLeft = Add(segment.EndLeft, offset);
        segment.EndRight = Add(segment.EndRight, offset);
    }

    private static int Segment(FlatMeshBuilder b, StrokeSegment inner, StrokeSegment outer, Rgba solid, double z)
    {
        var first = b.VertCount;
        var clear = Alpha(solid, 0);
        b.Vert(inner.StartLeft.X, inner.StartLeft.Y, z, solid);
        b.Vert(inner.StartRight.X, inner.StartRight.Y, z, solid);
        b.Vert(inner.EndRight.X, inner.EndRight.Y, z, solid);
        b.Vert(inner.EndLeft.X, inner.EndLeft.Y, z, solid);
        b.Vert(outer.StartLeft.X, outer.StartLeft.Y, z, clear);
        b.Vert(outer.StartRight.X, outer.StartRight.Y, z, clear);
        b.Vert(outer.EndRight.X, outer.EndRight.Y, z, clear);
        b.Vert(outer.EndLeft.X, outer.EndLeft.Y, z, clear);
        Triangle(b, first, first + 1, first + 2);
        Triangle(b, first, first + 2, first + 3);
        Strip(b, first, first + 3, first + 4, first + 7);
        Strip(b, first + 1, first + 2, first + 5, first + 6);
        return first;
    }

    private static void EndBand(FlatMeshBuilder b, Point a, Point c, Point outerA, Point outerC, Rgba solid, double z)
    {
        var first = b.VertCount;
        var clear = Alpha(solid, 0);
        b.Vert(a.X, a.Y, z, solid);
        b.Vert(c.X, c.Y, z, solid);
        b.Vert(outerA.X, outerA.Y, z, clear);
        b.Vert(outerC.X, outerC.Y, z, clear);
        Strip(b, first, first + 1, first + 2, first + 3);
    }

    private static void Join(FlatMeshBuilder b, Point point, ref StrokeSegment previous, ref StrokeSegment next,
        ref StrokeSegment outerPrevious, ref StrokeSegment outerNext, double innerHalf, double outerHalf,
        FlatJoin join, Rgba solid, double z)
    {
        var a = previous.Direction;
        var c = next.Direction;
        var cross = a.X * c.Y - a.Y * c.X;
        var dot = Math.Max(-1, Math.Min(1, Dot(a, c)));
        if (Math.Abs(cross) <= Epsilon)
        {
            if (dot < 0 && join == FlatJoin.Round)
                Cap(b, point, a, outerHalf - Inset, solid, z);
            return;
        }
        var denominator = 1 + dot;
        var miter = denominator > Epsilon ? Scale(new Point(-a.Y - c.Y, a.X + c.X), 1 / denominator) : new Point(0, 0);
        var clamped = denominator <= Epsilon || Dot(miter, miter) > 64;
        if (join == FlatJoin.Miter && !clamped)
        {
            previous.EndLeft = next.StartLeft = Add(point, Scale(miter, innerHalf));
            previous.EndRight = next.StartRight = Subtract(point, Scale(miter, innerHalf));
            outerPrevious.EndLeft = outerNext.StartLeft = Add(point, Scale(miter, outerHalf));
            outerPrevious.EndRight = outerNext.StartRight = Subtract(point, Scale(miter, outerHalf));
            return;
        }
        var start = cross > 0 ? previous.EndRight : previous.EndLeft;
        var end = cross > 0 ? next.StartRight : next.StartLeft;
        var outerStart = cross > 0 ? outerPrevious.EndRight : outerPrevious.EndLeft;
        var outerEnd = cross > 0 ? outerNext.StartRight : outerNext.StartLeft;
        var inner = clamped ? point : Add(point, Scale(miter, Math.Sign(cross) * innerHalf));
        var outerInner = clamped ? point : Add(point, Scale(miter, Math.Sign(cross) * outerHalf));
        if (cross > 0)
        {
            previous.EndLeft = next.StartLeft = inner;
            outerPrevious.EndLeft = outerNext.StartLeft = outerInner;
        }
        else
        {
            previous.EndRight = next.StartRight = inner;
            outerPrevious.EndRight = outerNext.StartRight = outerInner;
        }
        if (join != FlatJoin.Round)
        {
            SolidTriangle(b, inner, start, end, solid, z);
            EndBand(b, start, end, outerStart, outerEnd, solid, z);
            return;
        }
        SolidTriangle(b, inner, start, point, solid, z);
        SolidTriangle(b, inner, point, end, solid, z);
        var offset = Subtract(start, point);
        // With a vanished core its direction still comes from the outer boundary.
        if (innerHalf == 0) offset = Subtract(outerStart, point);
        var sweep = Math.Atan2(cross, dot);
        var segments = Math.Max(SweepSegments(outerHalf, sweep, 1), (int) Math.Ceiling(CapSegments * Math.Abs(sweep) / Math.PI));
        Sector(b, point, innerHalf, outerHalf, Math.Atan2(offset.Y, offset.X), sweep, segments, solid, z);
    }

    private static void Cap(FlatMeshBuilder b, Point center, Point outward, double half, Rgba solid, double z)
    {
        var outer = half + Inset;
        Sector(b, center, Math.Max(0, half - Inset), outer, Math.Atan2(outward.Y, outward.X) - Math.PI / 2,
            Math.PI, Math.Max(CapSegments, SweepSegments(outer, Math.PI, 1)), solid, z);
    }

    // Sectors use atan2 angles internally; only the public circle/arc API uses 0 = up.
    private static void Sector(FlatMeshBuilder b, Point center, double inner, double outer, double start, double sweep,
        int segments, Rgba solid, double z)
    {
        var origin = b.Vert(center.X, center.Y, z, solid);
        var clear = Alpha(solid, 0);
        var previous = -1;
        var previousOuter = -1;
        for (var i = 0; i <= segments; i++)
        {
            var angle = start + sweep * i / segments;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var next = b.Vert(center.X + cos * inner, center.Y + sin * inner, z, solid);
            var nextOuter = b.Vert(center.X + cos * outer, center.Y + sin * outer, z, clear);
            if (i > 0)
            {
                Triangle(b, origin, previous, next);
                Strip(b, previous, next, previousOuter, nextOuter);
            }
            previous = next;
            previousOuter = nextOuter;
        }
    }

    private static Point[] CleanPoints(double[] pts, int length, bool closed, out int count)
    {
        if (cleanScratch.Length < length / 2) cleanScratch = new Point[length / 2];
        var points = cleanScratch;
        count = 0;
        for (var i = 0; i + 1 < length; i += 2)
        {
            var point = new Point(pts[i], pts[i + 1]);
            if (count == 0 || Distance(points[count - 1], point) > Epsilon)
                points[count++] = point;
        }
        if (closed && count > 1 && Distance(points[0], points[count - 1]) <= Epsilon) count--;
        return points;
    }

    private static void SolidTriangle(FlatMeshBuilder b, Point a, Point c, Point d, Rgba color, double z)
    {
        var first = b.Vert(a.X, a.Y, z, color);
        var second = b.Vert(c.X, c.Y, z, color);
        var third = b.Vert(d.X, d.Y, z, color);
        Triangle(b, first, second, third);
    }

    private static void Strip(FlatMeshBuilder b, int a, int c, int outerA, int outerC)
    {
        Triangle(b, a, outerA, outerC);
        Triangle(b, a, outerC, c);
    }

    private static void Triangle(FlatMeshBuilder b, int a, int c, int d)
    {
        var verts = b.Verts;
        var cross = ((double) verts[c * 7] - verts[a * 7]) * ((double) verts[d * 7 + 1] - verts[a * 7 + 1]) -
            ((double) verts[c * 7 + 1] - verts[a * 7 + 1]) * ((double) verts[d * 7] - verts[a * 7]);
        if (cross > 0) b.Tri(a, c, d);
        else if (cross < 0) b.Tri(a, d, c);
    }

    private static Rgba Alpha(Rgba color, double alpha) => new Rgba(color.R, color.G, color.B, (float) (color.A * alpha));
    private static Point Add(Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
    private static Point Subtract(Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
    private static Point Scale(Point a, double scale) => new Point(a.X * scale, a.Y * scale);
    private static double Dot(Point a, Point b) => a.X * b.X + a.Y * b.Y;
    private static double Distance(Point a, Point b)
    {
        var delta = Subtract(b, a);
        return Math.Sqrt(Dot(delta, delta));
    }

    private readonly struct Point
    {
        public readonly double X, Y;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    private struct StrokeSegment
    {
        public Point Direction;
        public Point StartLeft, StartRight, EndLeft, EndRight;
    }
}
