using System;
using UnityEngine;

// A missed note can be pooled before its exit ends (negative
// judgement offset, early hold release). Only the drawing survives collection;
// drawing callbacks read captured values, never the pooled Note/Model. Gameplay's
// lifetime is unchanged. The head capture is always the skin's exit slot (the slot
// owns fade and size); the body keeps the engine's HoldBody exit envelope. The
// head clock may run compressed (clockScale — a drop's ride fades on a shortened
// clock so it lands exactly on its travel cap); the body clock never does.
public sealed class FlatNoteExitVisual : MonoBehaviour
{
    private readonly FlatMeshBuilder builder = new FlatMeshBuilder();
    private Game game;
    private FlatNoteMesh head;
    private FlatNoteMesh body;
    private Action<FlatMeshBuilder, float, float> drawHead;
    private Action<FlatMeshBuilder, float> drawBody;
    private ExitCurve headCurve;
    private double clockScale;
    private double hitTime;
    private double approach;
    private float spawnedAt;
    private float maxOpacity;
    private float worldScale;
    private float opacityMultiplier;

    internal static void Play(Note note, float worldScale, int sortingOrder, int bodySortingLayer, ExitCurve curve,
        float maxOpacity, Action<FlatMeshBuilder, float, float> drawHead,
        Action<FlatMeshBuilder, float> drawBody, double exitAnchor, Quaternion localRotation,
        double clockScale = 1)
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
        visual.clockScale = clockScale > 1e-9 ? clockScale : 1;
        visual.drawHead = drawHead;
        visual.drawBody = drawBody;
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
        // The head may become transparent before its curve ends. The curve's
        // duration drives the cutoff (the slot owns the fade track itself); the
        // compressed clock reaches it proportionally earlier.
        if (game == null || game.Time < spawnedAt || game.Time - hitTime >= headCurve.Duration / clockScale)
        {
            Destroy(gameObject);
            return;
        }
        Render();
    }

    private void Render()
    {
        var t = game.Time - hitTime;
        // The head rides the (possibly compressed) exit clock: a miss rides its
        // captured exit anchor, including ahead-of-time judgement — negative t
        // holds p = 1 and alpha = 1, exactly like effects.ts.
        var headT = t * clockScale;
        var p = (float) FlatEase.ExitRewind(headT, approach);
        var opacityProgress = (float) FlatEase.Clamp01(1 + t / Math.Max(1e-6, approach));
        var opacity = (t < 0 ? Mathf.Clamp(opacityProgress * 2, 0, maxOpacity) : maxOpacity) * opacityMultiplier;
        var visible = !game.State.Mods.Contains(Mod.HideNotes);
        var headAlpha = (float) FlatEase.ExitAlpha(headT, headCurve);
        head.SetVisible(visible && headAlpha > 0);
        builder.Reset();
        drawHead(builder, p, 1f);
        var headVertexCount = builder.VertCount;
        head.transform.localScale = Vector3.one;
        for (var i = 6; i < headVertexCount * 7; i += 7) builder.Verts[i] *= opacity;
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
