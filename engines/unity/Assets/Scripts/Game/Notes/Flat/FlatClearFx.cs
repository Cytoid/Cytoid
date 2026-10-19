using System;
using UnityEngine;

public class FlatClearFx : MonoBehaviour
{
    private const double Tau = Math.PI * 2;
    // Overall opacity multiplier for the clear/miss effects (judgement text
    // aside). 1 — classic's burst starts at full alpha, and 0.5 read as too
    // faint (player feedback). Kept as a knob for tuning back down.
    private const double EffectAlpha = 1;
    private enum EffectShape { Circle, Diamond, Capsule }
    private enum Extra { None, Double, Beam, Streaks }

    private readonly FlatMeshBuilder normal = new FlatMeshBuilder();
    private readonly FlatMeshBuilder additive = new FlatMeshBuilder();
    private readonly double[] points = new double[8];
    private readonly double[] chevron = new double[6];
    private FlatNoteMesh normalMesh;
    private FlatNoteMesh additiveMesh;
    private FlatClearFxPool pool;
    private Game game;
    private FlatPalette palette;
    private NoteGrade grade;
    private NoteKind kind;
    private Rgba color;
    private EffectShape shape;
    private Extra extra;
    private double size;
    private float textWorldScale;
    private double flavorReach;
    private int flavorSeed;
    private int sectorCount;
    private bool tailFlash;
    private float startTime;
    private double duration;
    private bool playing;
    private double timingError;
    private bool showJudgementText;
    private bool showTimingLabel;

    internal void Initialize()
    {
        normalMesh = FlatNoteMesh.Create(transform, "FlatFxMesh", false, 0);
        additiveMesh = FlatNoteMesh.Create(transform, "FlatFxFlash", true, 1);
    }

    internal void Play(FlatClearFxPool owner, Game sourceGame, NoteGrade noteGrade, NoteKind noteKind,
        FlatPalette notePalette, Vector3 position, Quaternion rotation, float worldScale, int sortingOrder,
        double noteTimingError)
    {
        pool = owner;
        game = sourceGame;
        grade = noteGrade;
        kind = noteKind;
        palette = notePalette;
        var gradeColor = game.Config.NoteGradeEffectColors[grade];
        color = new Rgba(gradeColor.r, gradeColor.g, gradeColor.b, gradeColor.a);
        // Text settings and the label slot are snapshotted at spawn: a mid-effect
        // settings change must never jump the label between slots.
        timingError = noteTimingError;
        var settings = Context.Player.Settings;
        showJudgementText = settings.DisplayJudgementText;
        showTimingLabel = settings.DisplayEarlyLateIndicators;
        size = FlatTokens.Unit * FlatTokens.Size(kind);
        duration = Duration(grade);
        startTime = game.Time;
        SetFlavor();
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = Vector3.one;
        textWorldScale = worldScale;
        normalMesh.SetWorldScale(worldScale);
        additiveMesh.SetWorldScale(worldScale);
        normalMesh.SetSortingOrder(sortingOrder);
        additiveMesh.SetSortingOrder(sortingOrder);
        playing = true;
        gameObject.SetActive(true);
        Render(0);
    }

    private void Update()
    {
        if (!playing) return;
        if (game == null || game.Time < startTime || game.Time - startTime >= duration)
        {
            pool.Collect(this);
            return;
        }
        Render((game.Time - startTime) / duration);
    }

    internal void OnCollect()
    {
        playing = false;
        normalMesh.SetVisible(false);
        additiveMesh.SetVisible(false);
        normal.Reset();
        additive.Reset();
        pool = null;
        game = null;
        palette = null;
        grade = NoteGrade.None;
        kind = default;
        color = default;
        shape = default;
        extra = default;
        size = flavorReach = duration = 0;
        textWorldScale = 0;
        flavorSeed = sectorCount = 0;
        tailFlash = false;
        startTime = 0;
        timingError = 0;
        showJudgementText = showTimingLabel = false;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (normalMesh != null) normalMesh.Dispose();
        if (additiveMesh != null) additiveMesh.Dispose();
    }

    private void Render(double u)
    {
        normal.Reset();
        additive.Reset();
        if (grade == NoteGrade.Miss) BuildMiss(u);
        else BuildClear(u);
        BuildText(u * duration);
        normalMesh.Upload(normal);
        additiveMesh.Upload(additive);
        var visible = !game.State.Mods.Contains(Mod.HideNotes);
        normalMesh.SetVisible(visible && normal.VertCount > 0);
        additiveMesh.SetVisible(visible && additive.VertCount > 0);
    }

    // Judgement text inside the effect (text centre = effect centre), with the
    // FAST/SLOW label stacked below it when both settings allow. The FX lives
    // exactly ClearDuration(grade), matching the text's own clock.
    private void BuildText(double t)
    {
        var first = normal.VertCount;
        var name = GradeName(grade);
        if (showJudgementText) FlatJudgement.Draw(normal, name, t);
        if (showTimingLabel && FlatTiming.ShowsTiming(name))
        {
            var timingFirst = normal.VertCount;
            FlatTiming.Draw(normal, FlatTiming.TimingOf(timingError), name, t, showJudgementText);
            var offset = FlatTiming.Offset(showJudgementText);
            TransformGeometry(normal, timingFirst, offset.X, offset.Y, 0);
        }
        KeepTextUpright(normal, first, transform.rotation, textWorldScale);
    }

    // FlatNoteMesh uploads (x, -y) * worldScale, then the Transform applies
    // rotation. Invert in that same frame, only for the appended text range;
    // the pivot is the text anchor at the local origin, never the effect bounds.
    internal static void KeepTextUpright(FlatMeshBuilder b, int firstVertex, Quaternion rotation, float worldScale)
    {
        if (worldScale == 0) return;
        var inverse = Quaternion.Inverse(rotation);
        for (var i = firstVertex * 7; i < b.Verts.Count; i += 7)
        {
            var point = inverse * new Vector3(b.Verts[i] * worldScale, -b.Verts[i + 1] * worldScale, b.Verts[i + 2]);
            b.Verts[i] = point.x / worldScale;
            b.Verts[i + 1] = -point.y / worldScale;
            b.Verts[i + 2] = point.z;
        }
    }

    private static string GradeName(NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Perfect: return "perfect";
            case NoteGrade.Great: return "great";
            case NoteGrade.Good: return "good";
            case NoteGrade.Bad: return "bad";
            case NoteGrade.Miss: return "miss";
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }

    private void SetFlavor()
    {
        shape = EffectShape.Circle;
        extra = Extra.None;
        sectorCount = 24;
        tailFlash = false;
        switch (kind)
        {
            case NoteKind.Click:
            case NoteKind.ClickDragHead:
                flavorReach = 1.45; flavorSeed = 1; extra = Extra.Double;
                break;
            case NoteKind.Flick:
                shape = EffectShape.Diamond; flavorReach = 1.4; flavorSeed = 2;
                extra = Extra.Streaks; sectorCount = 4;
                break;
            case NoteKind.Hold:
                flavorReach = 1.5; flavorSeed = 3; extra = Extra.Double; tailFlash = true;
                break;
            case NoteKind.LongHold:
                flavorReach = 1.6; flavorSeed = 4; extra = Extra.Beam; tailFlash = true;
                break;
            case NoteKind.DragHead:
                flavorReach = 1.4; flavorSeed = 5;
                break;
            case NoteKind.DragChild:
                flavorReach = 1.4; flavorSeed = 6;
                break;
            case NoteKind.ClickDragChild:
                flavorReach = 1.4; flavorSeed = 7;
                break;
            case NoteKind.DropClick:
                shape = EffectShape.Capsule; flavorReach = 1.3; flavorSeed = 8;
                break;
            case NoteKind.DropDrag:
                shape = EffectShape.Capsule; flavorReach = 1.3; flavorSeed = 9;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private void BuildClear(double u)
    {
        var r = size / 2;
        var w = FlatTokens.Unit * FlatTokens.StrokeRing;
        double gradeReach;
        int shardCount;
        switch (grade)
        {
            case NoteGrade.Perfect: gradeReach = 1; shardCount = 6; break;
            case NoteGrade.Great: gradeReach = 0.9; shardCount = 4; break;
            case NoteGrade.Good: gradeReach = 0.75; shardCount = 3; break;
            default: gradeReach = 0.6; shardCount = 0; break;
        }
        var reach = 1 + (flavorReach - 1) * gradeReach;
        if (extra == Extra.Beam)
        {
            var k = FlatEase.OutCubic(FlatEase.Seg(u, 0, 0.6));
            var h = r * FlatEase.Lerp(1, 3, k);
            var width = w * FlatEase.Lerp(1.4, 0.2, k);
            FlatGeom.RoundedRect(normal, -width / 2, -h, width, h * 2, 0,
                Alpha(color, 0.85 * (1 - FlatEase.Seg(u, 0.25, 0.75))));
        }

        var sk = FlatEase.OutExpo(FlatEase.Seg(u, 0, 0.85));
        // The shock ring fade rides classic's curve — 1 - quintic-out over the
        // whole effect, no delay or early cutoff — at the design's 0.5 peak.
        Outline(r * FlatEase.Lerp(0.95, reach, sk), w * FlatEase.Lerp(1.6, 0.35, sk),
            Alpha(color, 0.5 * (1 - FlatEase.OutQuint(u))));
        if (extra == Extra.Double)
            Outline(r * FlatEase.Lerp(0.7, reach * 0.78, FlatEase.OutQuart(FlatEase.Seg(u, 0.08, 0.9))),
                w * FlatEase.Lerp(0.9, 0.2, FlatEase.Seg(u, 0.08, 0.9)), Alpha(color, 1 - FlatEase.Seg(u, 0.4, 0.95)));
        if ((grade == NoteGrade.Perfect || grade == NoteGrade.Great) && shape != EffectShape.Capsule)
            BuildSectors(u, r, w, reach);

        var grow = FlatEase.Lerp(1, 1.08, FlatEase.OutCubic(FlatEase.Seg(u, 0, 0.32))) *
                   (1 - FlatEase.OutQuad(FlatEase.Seg(u, 0.12, 0.32)));
        var flashColor = Alpha(color, 0.25 * (1 - FlatEase.Seg(u, 0.2, 0.32)));
        if (tailFlash) FlatGeom.Disk(additive, 0, 0, FlatTokens.Unit * FlatTokens.StrokeHoldBody * grow, flashColor);
        else Solid(additive, r * grow, flashColor);

        if (shardCount > 0)
            BuildShards(FlatEase.Seg(u, 0.02, 1), shardCount + (extra == Extra.Double || extra == Extra.Beam ? 4 : 0),
                (uint) (flavorSeed * 31 + shardCount), r * 0.8, r * reach * 1.12,
                grade == NoteGrade.Perfect ? new Rgba(1, 1, 1) : color);
        if (extra == Extra.Streaks) BuildStreaks(u, r, w);
    }

    private void BuildSectors(double u, double r, double w, double reach)
    {
        var k = FlatEase.OutQuart(FlatEase.Seg(u, 0.05, 1));
        var duty = FlatEase.Lerp(0.62, 0.12, k);
        var radius = r * FlatEase.Lerp(reach * 0.8, reach * 1.02, k);
        var tint = Alpha(color, 1 - FlatEase.Seg(u, 0.55, 1));
        for (var i = 0; i < sectorCount; i++)
        {
            var center = sectorCount == 4 ? i * Tau / 4 + Tau / 8 : k * 0.35 + i * Tau / sectorCount;
            var half = sectorCount == 4 ? 0.42 * duty * 1.6 : Tau / sectorCount * duty / 2;
            var width = sectorCount == 4 ? w * 0.45 : w * FlatEase.Lerp(0.55, 0.25, k);
            FlatGeom.Arc(normal, 0, 0, radius, center - half, center + half, width, tint);
        }
    }

    private void BuildShards(double u, int count, uint seed, double r0, double r1, Rgba tint)
    {
        var rng = new FlatEase.Rng(seed);
        for (var i = 0; i < count; i++)
        {
            var a = extra == Extra.Streaks
                ? FlatEase.Lerp((i % 2 == 0 ? Tau / 4 : Tau * 0.75) - 0.5,
                    (i % 2 == 0 ? Tau / 4 : Tau * 0.75) + 0.5, rng.Next())
                : (i + rng.Next() * 0.8) / count * Tau;
            var reach = FlatEase.Lerp(0.55, 1, rng.Next());
            var delay = rng.Next() * 0.12;
            var life = FlatEase.Lerp(0.55, 0.86, rng.Next());
            var k = FlatEase.Seg(u, delay, delay + life);
            // Preserve the TS's conditional RNG consumption, including skipped shards.
            if (k <= 0 || k >= 1) continue;
            var d = FlatEase.Lerp(r0, r0 + (r1 - r0) * reach, FlatEase.OutQuart(k));
            var s = FlatTokens.Unit * 0.06 * FlatEase.Lerp(0.6, 1.2, rng.Next()) *
                    (1 - FlatEase.OutCubic(FlatEase.Seg(k, 0.35, 1)));
            var rotation = a + k * 2.2 * (rng.Next() > 0.5 ? 1 : -1);
            var first = normal.VertCount;
            FlatGeom.RoundedRect(normal, -s / 2, -s / 2, s, s, 0, Alpha(tint, 1));
            TransformGeometry(normal, first, Math.Sin(a) * d, -Math.Cos(a) * d, rotation);
        }
    }

    private void BuildStreaks(double u, double r, double w)
    {
        var k = FlatEase.OutQuart(FlatEase.Seg(u, 0, 0.7));
        var len = r * FlatEase.Lerp(0.3, 1.2, k);
        var off = r * FlatEase.Lerp(0.9, 1.25, k);
        var th = w * FlatEase.Lerp(0.8, 0.15, k);
        var tint = Alpha(color, 1 - FlatEase.Seg(u, 0.35, 0.8));
        FlatGeom.RoundedRect(normal, off, -th / 2, len, th, 0, tint);
        FlatGeom.RoundedRect(normal, -off - len, -th / 2, len, th, 0, tint);
        var count = grade == NoteGrade.Perfect ? 3 : grade == NoteGrade.Great ? 2 : 1;
        for (var i = 0; i < count; i++)
        {
            var ka = FlatEase.Seg(u, i * 0.08, 0.7 + i * 0.08);
            if (ka <= 0 || ka >= 1) continue;
            var x = r * FlatEase.Lerp(0.2, 1.5 - i * 0.42, FlatEase.OutQuart(ka));
            var h = r * 0.38;
            chevron[0] = x - h; chevron[1] = -h;
            chevron[2] = x; chevron[3] = 0;
            chevron[4] = x - h; chevron[5] = h;
            FlatGeom.PolyStroke(normal, chevron, false, w * 1.1,
                Alpha(color, 0.42 * FlatEase.Seg(ka, 0, 0.15) * (1 - FlatEase.Seg(ka, 0.45, 1)) * (1 - i * 0.18)),
                FlatJoin.Miter, FlatCap.Butt);
        }
    }

    private void BuildMiss(double u)
    {
        var r = size / 2;
        var w = FlatTokens.Unit * FlatTokens.StrokeRing;
        var k = FlatEase.OutCubic(u);
        var opacity = 1 - FlatEase.Seg(u, 0.25, 1);
        Solid(normal, r * FlatEase.Lerp(0.92, 0.75, k), Alpha(palette.Shade(800), 0.9 * opacity));
        // Only drop misses reach this effect; other kinds keep their own exit visuals.
        Outline(r * FlatEase.Lerp(1, 0.78, k) - w / 2, w * FlatEase.Lerp(1, 0.6, k), Alpha(color, opacity));
        TransformGeometry(normal, 0, 0, FlatEase.Lerp(0, FlatTokens.Unit * 0.08, k), 0);
    }

    private void Solid(FlatMeshBuilder b, double r, Rgba tint)
    {
        if (shape == EffectShape.Diamond)
        {
            DiamondPoints(r);
            FlatGeom.PolyFill(b, points, tint);
        }
        else if (shape == EffectShape.Capsule)
        {
            var ratio = r / (size / 2);
            var w = size * 1.15 * ratio;
            var h = size * 0.3 * ratio;
            FlatGeom.RoundedRect(b, -w / 2, -h / 2, w, h, h / 2, tint);
        }
        else FlatGeom.Disk(b, 0, 0, r, tint);
    }

    private void Outline(double r, double width, Rgba tint)
    {
        if (shape == EffectShape.Diamond)
        {
            DiamondPoints(r);
            FlatGeom.PolyStroke(normal, points, true, width, tint, FlatJoin.Miter, FlatCap.Butt);
        }
        else if (shape == EffectShape.Capsule)
        {
            // effects.ts expands the capsule path by one stroke width before stroking.
            var ratio = r / (size / 2);
            var w = size * 1.15 * ratio + width;
            var h = size * 0.3 * ratio + width;
            var radius = h / 2;
            var end = (w - h) / 2;
            FlatGeom.Line(normal, -end, -radius, end, -radius, width, tint, FlatCap.Butt);
            FlatGeom.Line(normal, -end, radius, end, radius, width, tint, FlatCap.Butt);
            FlatGeom.Arc(normal, end, 0, radius, 0, Math.PI, width, tint);
            FlatGeom.Arc(normal, -end, 0, radius, Math.PI, Tau, width, tint);
        }
        else FlatGeom.Ring(normal, 0, 0, r, width, tint);
    }

    private void DiamondPoints(double r)
    {
        points[0] = 0; points[1] = -r;
        points[2] = r; points[3] = 0;
        points[4] = 0; points[5] = r;
        points[6] = -r; points[7] = 0;
    }

    private static Rgba Alpha(Rgba tint, double opacity) =>
        new Rgba(tint.R, tint.G, tint.B, (float) (tint.A * opacity * EffectAlpha));

    private static void TransformGeometry(FlatMeshBuilder b, int firstVertex, double x, double y, double rotation)
    {
        var cos = Math.Cos(rotation);
        var sin = Math.Sin(rotation);
        for (var i = firstVertex * 7; i < b.Verts.Count; i += 7)
        {
            var px = b.Verts[i];
            var py = b.Verts[i + 1];
            b.Verts[i] = (float) (x + px * cos - py * sin);
            b.Verts[i + 1] = (float) (y + px * sin + py * cos);
        }
    }

    private static double Duration(NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Perfect: return FlatTokens.Time.ClearPerfect;
            case NoteGrade.Great: return FlatTokens.Time.ClearGreat;
            case NoteGrade.Good: return FlatTokens.Time.ClearGood;
            case NoteGrade.Bad: return FlatTokens.Time.ClearBad;
            case NoteGrade.Miss: return FlatTokens.Time.ClearMiss;
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }
}
