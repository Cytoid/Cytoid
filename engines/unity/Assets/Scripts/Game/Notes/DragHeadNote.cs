using System;
using System.Linq;

using Cysharp.Threading.Tasks;
using UnityEngine;

public class DragHeadNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        if (Game.Config.UseFlatNoteDesign) return new FlatDragHeadNoteRenderer(this);
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicDragHeadNoteRenderer(this)
            : throw new NotSupportedException();
    }

    public bool IsCollecting;

    // Drag head is constantly moving from drag note to drag note
    public ChartModel.Note FromNoteModel { get; protected set; }
    public ChartModel.Note ToNoteModel { get; protected set; }
    public ChartModel.Note StartToNoteModel { get; protected set; }
    public ChartModel.Note EndNoteModel { get; protected set; }
    public Vector3 OriginalPosition { get; protected set; }
    
    private bool hasFromNote;
    private Note fromNote;
    private bool hasToNote;
    private Note toNote;

    public bool IsCDrag => Model.type == (int) NoteType.CDragHead;

    // The travelling indicator can appear on an early clear, before intro_time.
    public bool IsChainEndPending(Game game) =>
        !game.State.IsJudged(EndNoteModel.id) || game.Time < EndNoteModel.start_time;

    public bool IsChainActive(Game game) =>
        game.Time >= Model.intro_time && IsChainEndPending(game);

    // CDrag's anchored head ID follows the click window, independently of the chain.
    public bool IsHeadNoteIdActive(Game game) => !IsCleared &&
        game.Time >= Model.intro_time && game.Time <= Model.end_time + MissThreshold;

    public override void SetData(int noteId)
    {
        base.SetData(noteId);
        FromNoteModel = Model;
        ToNoteModel = Model.next_id > 0 ? Chart.note_map[Model.next_id] : Model;
        StartToNoteModel = ToNoteModel;
        EndNoteModel = FromNoteModel.GetDragEndNote(Game.Chart.Model);
    }

    protected override void OnGameUpdate(Game _)
    {
        base.OnGameUpdate(_);
        if (Game.Time < Model.start_time)
        {
            OriginalPosition = transform.localPosition;
        }
    }

    protected override void OnGameLateUpdate(Game _)
    {
        base.OnGameLateUpdate(_);

        transform.localEulerAngles = FromNoteModel.rotation;

        if (Game.SpawnedNotes.ContainsKey(FromNoteModel.id))
        {
            if (!hasFromNote)
            {
                hasFromNote = true;
                fromNote = Game.SpawnedNotes[FromNoteModel.id];
            }
        }
        else
        {
            if (hasFromNote)
            {
                hasFromNote = false;
                fromNote = null;
            }
        }
        if (Game.SpawnedNotes.ContainsKey(ToNoteModel.id))
        {
            if (!hasToNote)
            {
                hasToNote = true;
                toNote = Game.SpawnedNotes[ToNoteModel.id];
            }
        }
        else
        {
            if (hasToNote)
            {
                hasToNote = false;
                toNote = null;
            }
        }

        if (Game.Time >= Model.start_time)
        {
            // Consume zero-span edges in the same frame so same-tick chains do not linger on NaN/u=Inf.
            // Cap iterations against malformed charts with cyclic next_id and non-increasing start_time.
            var maxEdgeSteps = Chart.note_map.Count + 1;
            var edgeSteps = 0;
            while (edgeSteps++ < maxEdgeSteps)
            {
                var fromPos = (hasFromNote && fromNote != this)
                    ? fromNote.StackVisualLocalPosition()
                    : FromNoteModel.CalculatePosition(Game.Chart);
                var toPos = hasToNote
                    ? toNote.StackVisualLocalPosition()
                    : ToNoteModel.CalculatePosition(Game.Chart);

                var span = ToNoteModel.start_time - FromNoteModel.start_time;
                float u;
                if (span > 0f)
                    u = Mathf.Clamp01((Game.Time - FromNoteModel.start_time) / span);
                else
                    u = Game.Time >= ToNoteModel.start_time ? 1f : 0f;

                transform.localPosition = Vector3.Lerp(fromPos, toPos, u);

                if (Game.Time < ToNoteModel.start_time)
                    break;

                if (ToNoteModel == EndNoteModel)
                {
                    transform.localPosition = toPos;
                    break;
                }

                FromNoteModel = ToNoteModel;
                ToNoteModel = Chart.note_map[FromNoteModel.next_id];
                hasFromNote = false;
                hasToNote = false;
                fromNote = null;
                toNote = null;

                if (Game.SpawnedNotes.TryGetValue(FromNoteModel.id, out fromNote))
                    hasFromNote = true;
                if (Game.SpawnedNotes.TryGetValue(ToNoteModel.id, out toNote))
                    hasToNote = true;
            }

            // Moving to or already at last note
            if (ToNoteModel == EndNoteModel)
            {
                // Last note does not exist?
                if (!Game.SpawnedNotes.ContainsKey(ToNoteModel.id))
                {
                    // Clear this
                    if (!IsCleared && ShouldMiss())
                    {
                        Clear(NoteGrade.Miss);
                    }
                    SyncDragStackFollowersIfPrimary();
                    return;
                }

                // Last note does exist and is cleared?
                var lastNote = Game.SpawnedNotes[ToNoteModel.id];
                if (lastNote.IsCleared)
                {
                    // Clear this
                    if (!IsCleared && ShouldMiss())
                    {
                        Clear(NoteGrade.Miss);
                    }
                }
            }
        }
        else
        {
            hasFromNote = false;
            hasToNote = false;
            fromNote = null;
            toNote = null;
            FromNoteModel = Model;
            ToNoteModel = StartToNoteModel;
        }

        SyncDragStackFollowersIfPrimary();
    }

    public override bool CanHandleTouch() =>
        base.CanHandleTouch() && (UsesCy1Drag || IsCDrag || CanHandleNativeDragTouch());

    internal override bool CanRepresentNativeDrag() =>
        CanHandleNativeDragTouch() && CalculateNativeDragGrade(0.500f, 0.200f) == NoteGrade.Perfect;

    public override async void Collect()
    {
        if (IsCollected) return;

        IsCollecting = true;
        
        void Collect()
        {
            FromNoteModel = default;
            ToNoteModel = default;
            StartToNoteModel = default;
            EndNoteModel = default;
            OriginalPosition = default;
            hasFromNote = default;
            fromNote = default;
            hasToNote = default;
            toNote = default;
        }
        bool CanCollect()
        {
            var nativeDeadline = EndNoteModel.end_time +
                (IsCDrag ? NoteType.CDragChild : NoteType.DragChild).GetDefaultMissThreshold();
            if (Game.Time < nativeDeadline) return false;
            if (!UsesCy1Drag) return true;

            // A positive judgment offset can leave the tail hittable past the native deadline.
            var cy1TailTime = Cy1Judgment.ToMicroseconds((double)EndNoteModel.start_time + JudgmentOffset);
            return Cy1Now >= cy1TailTime + Cy1Judgment.DragClose;
        }
        if (CanCollect())
        {
            IsCollecting = false;
            Collect();
            base.Collect();
            return;
        }
        // Don't destroy until the drag is over
        await UniTask.WaitUntil(() => IsCollected || CanCollect());
        
        IsCollecting = false;
        Collect();
        base.Collect();
    }

    public override NoteGrade CalculateGrade() => UsesCy1Drag || IsCDrag
        ? base.CalculateGrade()
        : CalculateNativeDragGrade(0.500f, 0.200f);

    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoDrag);
    }
    
    public override void PlayHitSound()
    {
        if (Context.AudioManager.IsSfxLoaded("HitSound"))
        {
            Context.AudioManager.GetSfx("HitSound").Play();
        }
        Context.Haptic(HapticTypes.Selection, false);
    }
    
}
