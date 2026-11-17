using System;
using System.Collections.Generic;
using UnityEngine;

public class FlatClearFxPool : SingletonMonoBehavior<FlatClearFxPool>
{
    private const int PrewarmCount = 24;
    private readonly Stack<FlatClearFx> available = new Stack<FlatClearFx>();
    private Game game;

    public static FlatClearFxPool GetInstance(Game game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        var pool = SingletonMonoBehavior<FlatClearFxPool>.Instance;
        if (pool != null && pool.game == game) return pool;
        if (pool != null)
        {
            // A new game owns a new pool; do not let the singleton's async Awake wait on the old session.
            SingletonMonoBehavior<FlatClearFxPool>.Instance = null;
            Destroy(pool.gameObject);
        }
        var go = new GameObject("FlatClearFxPool");
        go.transform.SetParent(game.contentParent.transform, false);
        pool = go.AddComponent<FlatClearFxPool>();
        pool.game = game;
        for (var i = 0; i < PrewarmCount; i++) pool.available.Push(pool.CreateEffect());
        return pool;
    }

    /// <summary>
    /// Snapshots the note before collection. worldScale is the base's design-pixel scale;
    /// the effect itself renders on the topmost overlay layer (see FlatClearFx).
    /// timingError is the signed press time minus chart time in seconds (positive = late),
    /// feeding the FAST/SLOW label. Optional flick rotation is Unity world Z degrees:
    /// 0 points right, 90 up, 180 left. Omit it to use the note's world rotation.
    /// Skin-only: the effect stays inactive (returned to the pool) when the skin cannot
    /// serve the note kind's clear slot.
    /// </summary>
    public void Play(Note note, NoteGrade grade, float worldScale,
        double timingError, float? flickRotationDegrees = null)
    {
        if (grade == NoteGrade.None || note.Game.State.Mods.Contains(Mod.HideNotes) ||
            note.Game.State.Mode == GameMode.GlobalCalibration) return;
        if (game != note.Game) throw new ArgumentException("The note must belong to this pool's game.", nameof(note));
        if (!note.Game.effectController.TryConsumeClearFx()) return;

        var at = note.transform.position;
        if ((note.Type == NoteType.Hold || note.Type == NoteType.LongHold) && note.Model.style == 1)
            at.y = Scanner.Instance.transform.position.y;

        var rotation = note.transform.rotation;
        if (note.Type == NoteType.Flick && flickRotationDegrees.HasValue)
            rotation = Quaternion.Euler(0, 0, flickRotationDegrees.Value);
        var fx = available.Count > 0 ? available.Pop() : CreateEffect();
        if (!fx.Play(this, note.Game, grade, FlatNoteRendererBase.KindFor(note.Type),
                at, rotation, worldScale * note.Model.Override.SizeMultiplier, timingError,
                FlatNoteRendererBase.ResolvePaletteSeed(note), note.Model.start_time - note.Model.intro_time,
                note))
            Collect(fx);
    }

    /// <summary>Skin-mode miss label for notes whose miss rides the head's exit slot:
    /// feedback-only (no clear burst, no clear-fx budget), matching the old
    /// in-geometry MISS text path which never consumed pool budget.</summary>
    public void PlayFeedback(Note note, float worldScale)
    {
        if (!FlatSkinRuntime.Ready) return;
        if (game != note.Game) throw new ArgumentException("The note must belong to this pool's game.", nameof(note));
        if (note.Game.State.Mods.Contains(Mod.HideNotes)) return;
        var fx = available.Count > 0 ? available.Pop() : CreateEffect();
        if (!fx.PlayFeedbackOnly(this, note.Game, note, worldScale * note.Model.Override.SizeMultiplier))
            Collect(fx);
    }

    private FlatClearFx CreateEffect()
    {
        var go = new GameObject("FlatClearFx");
        go.transform.SetParent(transform, false);
        var fx = go.AddComponent<FlatClearFx>();
        fx.Initialize();
        fx.OnCollect();
        return fx;
    }

    internal void Collect(FlatClearFx fx)
    {
        fx.OnCollect();
        available.Push(fx);
    }

    protected override void OnDestroy()
    {
        // An old session can finish destruction after its replacement has registered.
        if (SingletonMonoBehavior<FlatClearFxPool>.Instance == this) base.OnDestroy();
    }
}
