using System;

using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class Note : MonoBehaviour
{

    [NonSerialized] public NoteRenderer Renderer;
    public bool IsInitialized { get; private set; }

    public bool IsCollected { get; private set; }

    public Game Game { get; private set; }
    public ChartModel.Note Model { get; private set; }
    public ChartModel.Note NextNoteModel { get; private set; }

    private bool hasNextNote;
    private Note nextNote;

    public ChartModel Chart { get; private set; }
    public ChartModel.Page Page { get; private set; }
    public NoteType Type { get; private set; }

    public float MissThreshold { get; set; }

    public bool IsCleared { get; private set; }
    public bool IsArmed { get; private set; }
    private NoteGrade armedGrade = NoteGrade.None;
    /// <summary>
    /// When set, the next <see cref="Clear"/> joins a deferred drag-stack soft budget
    /// (see <see cref="MarkDragStackBudget"/>).
    /// </summary>
    private bool dragStackBudgetPending;
    private int dragStackBudgetId;

    /// <summary>Co-located drag stack host; null when not in a multi-note stack.</summary>
    public DragStackHost DragStack { get; set; }

    /// <summary>True when this note shares visuals/collider with <see cref="DragStack"/>.Primary.</summary>
    public bool IsDragStackFollower { get; set; }

    // For ranked mode: weighted difference between the current timing and the perfect timing
    public float GreatGradeWeight { get; protected set; }

    public float JudgmentOffset { get; protected set; }

    public bool HasEmerged => Game.Time >= Model.intro_time;

    public float TimeUntilStart => Model.start_time - Game.Time;
    public float TimeUntilEnd => Model.end_time - Game.Time;

    public void Initialize(Game game)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        Game = game;
        Renderer = CreateRenderer();
    }

    public virtual void SetData(int noteId)
    {
        IsCollected = false;
        IsArmed = false;
        armedGrade = NoteGrade.None;
        dragStackBudgetPending = false;
        dragStackBudgetId = 0;
        IsDragStackFollower = false;
        DragStack = null;
    
        Chart = Game.Chart.Model;
        Model = Game.Chart.Model.note_map[noteId];
        if (Model.next_id > 0 && Chart.note_map.ContainsKey(Model.next_id))
        {
            NextNoteModel = Chart.note_map[Model.next_id];
        }

        Page = Chart.page_list[Model.page_index];
        Type = (NoteType) Model.type;

        Renderer.OnNoteLoaded();
        var collider = Renderer.GetCollider();
        if (collider != null) collider.enabled = true;

        MissThreshold = Type.GetDefaultMissThreshold();
        JudgmentOffset = Context.Player.Settings.JudgmentOffset;

        Game.onGameUpdate.RemoveListener(OnGameUpdate);
        Game.onGameLateUpdate.RemoveListener(OnGameLateUpdate);
        Game.onGameUpdate.AddListener(OnGameUpdate);
        Game.onGameLateUpdate.AddListener(OnGameLateUpdate);
    }

    public async void AwaitAndCollect()
    {
        await UniTask.DelayFrame(0);
        Collect();
    }

    public virtual void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;

        Renderer.OnCollect();
        Game.ObjectPool.DragStacks.OnNoteCollected(this);
        Game.ObjectPool.CollectNote(this);
        Game.onGameUpdate.RemoveListener(OnGameUpdate);
        Game.onGameLateUpdate.RemoveListener(OnGameLateUpdate);
        Model = default;
        NextNoteModel = default;
        hasNextNote = default;
        nextNote = default;
        Chart = default;
        Page = default;
        MissThreshold = default;
        IsCleared = default;
        IsArmed = default;
        armedGrade = NoteGrade.None;
        dragStackBudgetPending = false;
        dragStackBudgetId = 0;
        GreatGradeWeight = default;
        JudgmentOffset = default;
        DragStack = null;
        IsDragStackFollower = default;
    }

    public virtual void Clear(NoteGrade grade)
    {
        if (IsCleared) return;

        IsArmed = false;
        armedGrade = NoteGrade.None;
        IsCleared = true;

        // Deferred multi-note drag stacks join a per-stack soft budget for this Clear only.
        // Ordinary Click/Flick/Hold/Auto clears remain uncapped even in the same frame.
        var effects = Game.effectController;
        var joinedDragStackBudget = false;
        if (dragStackBudgetPending)
        {
            dragStackBudgetPending = false;
            var stackId = dragStackBudgetId;
            dragStackBudgetId = 0;
            effects.EnterDragStackClear(
                EffectController.MaxClearFxPerDragBatch,
                EffectController.MaxHitSoundsPerDragBatch,
                stackId);
            joinedDragStackBudget = true;
        }

        try
        {
            Renderer.OnClear(grade);
            Game.State.Judge(this, grade, -TimeUntilEnd, GreatGradeWeight);
            Game.onNoteJudged.Invoke(Game, this, new JudgeData(grade, -TimeUntilEnd, GreatGradeWeight));

            // Hit sound (drag-stack budget via EffectController when this clear participates)
            if (grade != NoteGrade.Miss &&
                (!(this is HoldNote) || Context.Player.Settings.HoldHitSoundTiming.Let(it => it == HoldHitSoundTiming.End || it == HoldHitSoundTiming.Both)) &&
                effects.TryConsumeHitSound())
            {
                PlayHitSound();
            }
        }
        finally
        {
            if (joinedDragStackBudget) effects.ExitDragStackClear();
        }

        Game.onNoteClear.Invoke(Game, this);
        AwaitAndCollect();
    }

    /// <summary>
    /// Marks this note so its later armed <see cref="Clear"/> shares the co-located
    /// drag-stack FX/SFX budget with siblings that received the same
    /// <paramref name="stackId"/>.
    /// </summary>
    public void MarkDragStackBudget(int stackId)
    {
        dragStackBudgetPending = true;
        dragStackBudgetId = stackId;
    }

    public virtual void PlayHitSound()
    {
        if (Context.AudioManager.IsSfxLoaded("HitSound"))
        {
            Context.AudioManager.GetSfx("HitSound").Play();
        }
        Context.Haptic(HapticTypes.LightImpact, false);
    }

    protected virtual void OnGameUpdate(Game _)
    {
        if (IsDragStackFollower) return;

        if (!IsCleared)
        {
            // Update position
            gameObject.transform.localPosition = Model.CalculatePosition(Game.Chart);

            TickJudgmentState();
        }

        Renderer.OnLateUpdate();

        // Pin followers before LateUpdate so stacked origins match the primary this frame
        // (lines still lag one Update like unstacked notes; they must not see a pooled pose).
        if (!(this is DragHeadNote))
        {
            SyncDragStackFollowersIfPrimary();
        }
    }

    /// <summary>
    /// Armed / Auto / Miss settlement shared by primary Update and stack followers.
    /// </summary>
    public void TickStackFollowerJudgment()
    {
        if (IsCleared || IsCollected) return;
        TickJudgmentState();
    }

    private void TickJudgmentState()
    {
        if (IsArmed)
        {
            if (Game.Time >= Model.start_time + JudgmentOffset)
            {
                Clear(armedGrade);
            }
        }
        else
        {
            // Autoplay
            if (IsAutoEnabled())
            {
                if (TimeUntilStart < 0)
                {
                    if (this is HoldNote)
                    {
                        ((HoldNote) this).UpdateFinger(0, true);
                    }
                    else
                    {
                        Clear(NoteGrade.Perfect);
                    }
                }
            }

            // Check removable
            if (ShouldMiss())
            {
                Clear(NoteGrade.Miss);
            }
        }
    }

    protected virtual void OnGameLateUpdate(Game _)
    {
        if (IsDragStackFollower) return;

        if (NextNoteModel != null)
        {
            if (Game.SpawnedNotes.ContainsKey(NextNoteModel.id))
            {
                if (!hasNextNote)
                {
                    hasNextNote = true;
                    nextNote = Game.SpawnedNotes[NextNoteModel.id];
                }
            }
            else
            {
                if (hasNextNote)
                {
                    hasNextNote = false;
                    nextNote = null;
                }
            }

            var position = transform.localPosition;
            // Unspawned next notes must use CalculatePosition so Storyboard Override applies
            // (baked .position would aim drag lines/heads at the pre-override chart coord).
            Vector3 nextPosition;
            if (hasNextNote)
            {
                nextPosition = nextNote.StackVisualLocalPosition();
            }
            else
            {
                nextPosition = NextNoteModel.CalculatePosition(Game.Chart);
            }

            if (position == nextPosition)
                Model.rotation = Vector3.zero;
            else if (Math.Abs(position.y - nextPosition.y) < 0.000001)
                Model.rotation = new Vector3(0, 0, position.x > nextPosition.x ? 90 : -90);
            else if (Math.Abs(position.x - nextPosition.x) < 0.000001)
                Model.rotation = new Vector3(0, 0, position.y > nextPosition.y ? -180 : 0);
            else
                Model.rotation = new Vector3(0, 0, -(
                    Mathf.Atan((nextPosition.x - position.x) /
                               (nextPosition.y - position.y)) / Mathf.PI * 180f +
                    (nextPosition.y > position.y ? 0 : 180)));
        }

        var rotation = Model.rotation;
        if (Model.Override.RotX != null) rotation.x = Model.Override.RotX.Value;
        if (Model.Override.RotY != null) rotation.y = Model.Override.RotY.Value;
        if (Model.Override.RotZ != null) rotation.z = Model.Override.RotZ.Value;

        gameObject.transform.localEulerAngles = Model.rotation = rotation;

        // DragHead syncs after it finishes moving in its LateUpdate override.
        if (!(this is DragHeadNote))
        {
            SyncDragStackFollowersIfPrimary();
        }
    }

    protected void SyncDragStackFollowersIfPrimary()
    {
        if (DragStack != null && DragStack.IsPrimary(this))
        {
            DragStack.TickFollowers();
        }
    }

    public virtual bool ShouldMiss()
    {
        return Game.Time - (Model.start_time + JudgmentOffset) > MissThreshold;
    }

    public void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        Destroy(gameObject);
        Renderer?.Dispose();
    }

    /// <returns>True if this note took the touch (cleared or otherwise handled).</returns>
    public virtual bool OnTouch(Vector2 screenPos)
    {
        if (!CanHandleTouch()) return false;
        return TryClear();
    }

    /// <summary>
    /// Handles passive contact from an already-down finger. A valid early hit is armed
    /// and resolves at the effective perfect time; a hit at or after that time resolves
    /// immediately. Misses retain the existing immediate settlement behavior.
    /// </summary>
    public virtual bool OnTouchDeferred(Vector2 screenPos)
    {
        if (!CanHandleTouch() || IsArmed) return false;

        var grade = GetTouchGrade();
        if (grade == NoteGrade.None) return false;
        if (grade == NoteGrade.Miss || Game.Time >= Model.start_time + JudgmentOffset)
        {
            Clear(grade);
        }
        else
        {
            IsArmed = true;
            armedGrade = grade;
        }
        return true;
    }

    /// <summary>
    /// Returns whether this note may consider a touch at the current chart time.
    /// Drag variants extend this with their early and cross-page admission gates.
    /// </summary>
    public virtual bool CanHandleTouch()
    {
        return Game.IsLoaded && Game.State.IsPlaying && !IsCollected && !IsCleared && !IsArmed;
    }

    /// <summary>
    /// Side-effect-free preview of the grade that an immediate touch would settle with,
    /// apart from CalculateGrade's existing GreatGradeWeight calculation.
    /// </summary>
    public NoteGrade GetTouchGrade()
    {
        if (IsAutoEnabled()) return NoteGrade.Perfect;
        if (ShouldMiss()) return NoteGrade.Miss;
        return CalculateGrade();
    }

    /// <returns>
    /// True if this call newly cleared the note. Already-cleared notes return false
    /// so a later finger on the same Down is not consumed.
    /// </returns>
    public virtual bool TryClear()
    {
        if (IsCleared || IsArmed) return false;
        var grade = GetTouchGrade();
        if (grade != NoteGrade.None) Clear(grade);
        return IsCleared;
    }

    public virtual NoteGrade CalculateGrade()
    {
        var grade = NoteGrade.None;
        var timeUntil = TimeUntilStart + JudgmentOffset;

        if (Game.State.Mode == GameMode.Practice)
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.800f) grade = NoteGrade.Bad;
                if (timeUntil < 0.400f) grade = NoteGrade.Good;
                if (timeUntil < 0.200f) grade = NoteGrade.Great;
                if (timeUntil < 0.070f) grade = NoteGrade.Perfect;
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.300f) grade = NoteGrade.Bad;
                if (timePassed < 0.200f) grade = NoteGrade.Good;
                if (timePassed < 0.150f) grade = NoteGrade.Great;
                if (timePassed < 0.070f) grade = NoteGrade.Perfect;
            }
        }
        else
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.400f) grade = NoteGrade.Bad;
                if (timeUntil < 0.200f) grade = NoteGrade.Good;
                if (timeUntil < 0.070f) grade = NoteGrade.Great;
                if (timeUntil <= 0.040f) grade = NoteGrade.Perfect;
                if (grade == NoteGrade.Great) GreatGradeWeight = 1.0f - (timeUntil - 0.040f) / (0.070f - 0.040f);
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.200f) grade = NoteGrade.Bad;
                if (timePassed < 0.150f) grade = NoteGrade.Good;
                if (timePassed < 0.070f) grade = NoteGrade.Great;
                if (timePassed <= 0.040f) grade = NoteGrade.Perfect;
                if (grade == NoteGrade.Great) GreatGradeWeight = 1.0f - (timePassed - 0.040f) / (0.070f - 0.040f);
            }
        }

        return grade;
    }

    public bool DoesCollide(Vector2 pos)
    {
        if (IsDragStackFollower && DragStack?.Primary != null)
        {
            return DragStack.Primary.DoesCollide(pos);
        }

        return Renderer.DoesCollide(pos);
    }

    /// <summary>
    /// World/local pose used for stacking: followers are pinned to the primary.
    /// </summary>
    public Vector3 StackVisualLocalPosition()
    {
        if (IsDragStackFollower && DragStack?.Primary != null)
            return DragStack.Primary.transform.localPosition;
        return transform.localPosition;
    }

    public void BecomeDragStackFollower()
    {
        if (IsDragStackFollower) return;
        IsDragStackFollower = true;
        Game.onGameUpdate.RemoveListener(OnGameUpdate);
        Game.onGameLateUpdate.RemoveListener(OnGameLateUpdate);
        SetDragStackVisualsEnabled(false);
    }

    public void PromoteToDragStackPrimary()
    {
        IsDragStackFollower = false;
        SetDragStackVisualsEnabled(true);
        Game.onGameUpdate.RemoveListener(OnGameUpdate);
        Game.onGameLateUpdate.RemoveListener(OnGameLateUpdate);
        Game.onGameUpdate.AddListener(OnGameUpdate);
        Game.onGameLateUpdate.AddListener(OnGameLateUpdate);
        // Restore Ring/Fill/Mask this frame; waiting for the next Update leaves a blank host.
        Renderer?.OnLateUpdate();
    }

    private void SetDragStackVisualsEnabled(bool enabled)
    {
        var colliders = GetComponentsInChildren<Collider2D>(true);
        for (var i = 0; i < colliders.Length; i++) colliders[i].enabled = enabled;

        if (!enabled)
        {
            var sprites = GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < sprites.Length; i++) sprites[i].enabled = false;
            var masks = GetComponentsInChildren<SpriteMask>(true);
            for (var i = 0; i < masks.Length; i++) masks[i].enabled = false;
        }
    }

    public virtual bool IsAutoEnabled()
    {
        return Game.State.Mods.Contains(Mod.Auto);
    }

    protected abstract NoteRenderer CreateRenderer();
}
