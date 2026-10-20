using System;
using UnityEngine;

// A missed note can be pooled before its exit ends (negative
// judgement offset, early hold release). Only the drawing survives collection;
// drawing callbacks read captured values, never the pooled Note/Model. Gameplay's
// lifetime is unchanged.
public sealed class FlatNoteExitVisual : MonoBehaviour
{
    private readonly FlatMeshBuilder builder = new FlatMeshBuilder();
    private Game game;
    private FlatNoteMesh head;
    private FlatNoteMesh body;
    private Action<FlatMeshBuilder, float, float> drawHead;
    private Action<FlatMeshBuilder, float> drawBody;
    private Func<float, float> scaleAt;
    private ExitCurve headCurve;
    private double hitTime;
    private double approach;
    private float spawnedAt;
    private float maxOpacity;
    private float worldScale;
    private float opacityMultiplier;
    private double missTime;
    private bool missText;

    internal static void Play(Note note, float worldScale, int sortingOrder, int bodySortingLayer, ExitCurve curve,
        float maxOpacity, Func<float, float> scaleAt, Action<FlatMeshBuilder, float, float> drawHead,
        Action<FlatMeshBuilder, float> drawBody, double exitAnchor, double missTime, bool missText,
        Quaternion localRotation)
    {
        var go = new GameObject("FlatNoteExitVisual");
        go.transform.SetParent(note.transform.parent, false);
        // A CDrag transform follows the chain even on a miss. Detached exits
        // inherit the chart anchor so their head AND miss label stay in place.
        go.transform.localPosition = note.Type == NoteType.CDragHead
            ? note.Model.CalculatePosition(note.Game.Chart) : note.transform.localPosition;
        // CDrag supplies the judgement pose, which can differ from the live
        // chain's direction by collection time. This detached transform never follows.
        go.transform.localRotation = localRotation;
        var visual = go.AddComponent<FlatNoteExitVisual>();
        visual.game = note.Game;
        visual.hitTime = exitAnchor;
        visual.approach = note.Model.start_time - note.Model.intro_time;
        visual.spawnedAt = note.Game.Time;
        visual.maxOpacity = maxOpacity;
        visual.opacityMultiplier = note.Game.Config.GlobalNoteOpacityMultiplier * note.Model.Override.OpacityMultiplier;
        visual.headCurve = curve;
        visual.scaleAt = scaleAt;
        visual.drawHead = drawHead;
        visual.drawBody = drawBody;
        visual.missTime = missTime;
        visual.missText = missText;
        visual.head = FlatNoteMesh.Create(go.transform, "Head", false, sortingOrder);
        var scale = worldScale * note.Model.Override.SizeMultiplier;
        visual.worldScale = scale;
        visual.head.SetWorldScale(scale);
        if (drawBody != null)
        {
            // Same layer and order as the live body, so collection never pops the
            // bar above the chain for its last frames.
            visual.body = FlatNoteMesh.Create(go.transform, "Body", false, sortingOrder);
            visual.body.SetSortingLayer(bodySortingLayer);
            visual.body.SetWorldScale(scale);
        }
        visual.Render();
    }

    private void Update()
    {
        // The head may become transparent before its curve ends. Keep the mesh
        // alive until both the exit duration and the judgement text window end.
        if (game == null || game.Time < spawnedAt ||
            (game.Time - hitTime >= headCurve.Duration && !MissTextActive()))
        {
            Destroy(gameObject);
            return;
        }
        Render();
    }

    private bool MissTextActive()
    {
        var t = game.Time - missTime;
        return missText && t >= 0 && t < FlatTokens.Time.ClearDuration("miss");
    }

    private void Render()
    {
        var t = game.Time - hitTime;
        // A miss rides its captured exit anchor, including ahead-of-time judgement:
        // negative t holds p = 1 and alpha = 1, exactly like effects.ts.
        var p = (float) FlatEase.ExitRewind(t, approach);
        var scale = scaleAt(p);
        var opacityProgress = (float) FlatEase.Clamp01(1 + t / Math.Max(1e-6, approach));
        var opacity = (t < 0 ? Mathf.Clamp(opacityProgress * 2, 0, maxOpacity) : maxOpacity) * opacityMultiplier;
        var visible = !game.State.Mods.Contains(Mod.HideNotes);
        var headAlpha = (float) FlatEase.ExitAlpha(t, headCurve);
        var textActive = MissTextActive();
        head.SetVisible(visible && (headAlpha > 0 || textActive));
        builder.Reset();
        drawHead(builder, p, scale);
        var headVertexCount = builder.VertCount;
        // The MISS label keeps its own clock: seconds since the miss moment, not the
        // exit's chart anchor or fade. FlatJudgement owns the full 0.5 s text envelope.
        if (textActive && scale != 0)
        {
            var first = builder.VertCount;
            // As on the live mesh, judgement is outside the rewinding head scale.
            FlatJudgement.Draw(builder, "miss", game.Time - missTime);
            FlatNoteRendererBase.ScaleGeometry(builder, first, 1 / scale);
            FlatClearFx.KeepTextUpright(builder, first, transform.rotation, worldScale * scale);
        }
        head.transform.localScale = new Vector3(scale, scale, 1);
        for (var i = 6; i < builder.Verts.Count; i += 7)
            builder.Verts[i] *= opacity * (i < headVertexCount * 7 ? headAlpha : 1);
        head.Upload(builder);
        if (body == null) return;
        var bodyAlpha = (float) FlatEase.ExitAlpha(t, FlatTokens.Time.Exit.HoldBody);
        body.SetVisible(visible && bodyAlpha > 0);
        if (bodyAlpha <= 0) return;
        builder.Reset();
        drawBody(builder, p);
        Upload(body, opacity * bodyAlpha);
    }

    private void Upload(FlatNoteMesh mesh, float opacity)
    {
        for (var i = 6; i < builder.Verts.Count; i += 7) builder.Verts[i] *= opacity;
        mesh.Upload(builder);
    }

    private void OnDestroy()
    {
        if (head != null) head.Dispose();
        if (body != null) body.Dispose();
    }
}
