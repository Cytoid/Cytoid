using System;

// Flat drag child (drag.ts childDraw): solid bead + faint 600 hairline ring inside, so
// the centre never draws the eye; the stepped edge sits on the bead's own silhouette.
// Click-drag child reuses the same design with the click-drag family palette (phase-1
// scope decision).
public class FlatDragChildNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    public FlatDragChildNoteRenderer(DragChildNote note) : base(note)
    {
    }

    protected override FlatFamily Family =>
        Note.Type == NoteType.CDragChild ? FlatFamily.ClickDrag : FlatFamily.Drag;

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (HeadVisible || !ClearedSuccessfully || Note.IsDragStackFollower ||
            Game.State.Mods.Contains(Mod.HideNotes) || Game.Time > Note.Model.start_time)
            return;
        // A judged child stays visible until the head passes it (start_time) so the
        // chain reads as lit — Classic ClassicDragChildNoteRenderer semantics.
        HeadVisible = true;
        HeadMesh.SetVisible(true);
    }

    protected override void BuildGeometry(FlatMeshBuilder b) =>
        DrawEnter(b, EnterProgress, DesignSize, Palette);

    protected override Action<FlatMeshBuilder, float, float> CaptureExitHead()
    {
        var size = DesignSize;
        var palette = Palette;
        return (b, p, scale) => DrawEnter(b, p, size, palette);
    }

    private void DrawEnter(FlatMeshBuilder b, double p, double size, FlatPalette palette)
    {
        var r = size / 2;
        var alpha = FlatEase.Seg(p, 0, 0.06);
        // drag.ts STEADY = 0.2: the hairline ring fades in with the ring assembly.
        var k = FlatEase.OutCubic(FlatEase.Seg(p, 0, 0.2));
        var wake = WakeAmount(p);

        FlatGeom.Disk(b, 0, 0, r, WithOpacity(palette.Tone(WakeShade(400, wake)), alpha));
        // The bead has no ring band, so the stepped edge (FlatEdge) sits on the disk
        // silhouette itself, sized off ringWidth like the rest of the family.
        var ringWidth = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(size / FlatTokens.Unit);
        for (var i = 0; i < FlatEdge.Count; i++)
        {
            if (!FlatEdge.Band(i, ringWidth, out var offset, out var bandWidth, out var opacity)) continue;
            FlatGeom.Ring(b, 0, 0, r + offset, bandWidth, WithOpacity(Edge, alpha * opacity));
        }
        FlatGeom.Ring(b, 0, 0, r * 0.62, FlatTokens.Unit * FlatTokens.StrokeHair,
            WithOpacity(palette.Tone(WakeShade(600, wake)), alpha * 0.7 * k));
    }
}
