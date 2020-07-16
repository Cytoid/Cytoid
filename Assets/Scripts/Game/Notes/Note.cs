using System;
using MoreMountains.NiceVibrations;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class Note : MonoBehaviour
{
    
    [NonSerialized] public NoteRenderer Renderer;
    public bool IsInitialized { get; private set; }
    
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
        Chart = Game.Chart.Model;
        Model = Game.Chart.Model.note_map[noteId];
        if (Model.next_id > 0 && Chart.note_map.ContainsKey(Model.next_id))
        {
            NextNoteModel = Chart.note_map[Model.next_id];
        }

        Page = Chart.page_list[Model.page_index];
        Type = (NoteType) Model.type;
        
        Renderer.OnNoteLoaded();
        MissThreshold = Type.GetDefaultMissThreshold();
        JudgmentOffset = Context.Player.Settings.JudgmentOffset;
        
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
        Renderer.OnCollect();
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
        GreatGradeWeight = default;
        JudgmentOffset = default;
    }

    public virtual void Clear(NoteGrade grade)
    {
        if (IsCleared) return;

        IsCleared = true;
        Renderer.OnClear(grade);
        Game.State.Judge(this, grade, -TimeUntilEnd, GreatGradeWeight);
        Game.onNoteJudged.Invoke(Game, this, new JudgeData(grade, -TimeUntilEnd, GreatGradeWeight));

        // Hit sound
        if (grade != NoteGrade.Miss && (!(this is HoldNote) || Context.Player.Settings.HoldHitSoundTiming.Let(it => it == HoldHitSoundTiming.End || it == HoldHitSoundTiming.Both)))
        {
            PlayHitSound();
        }
        
        if (!(Game is PlayerGame))
        {
            Game.onNoteClear.Invoke(Game, this);
            AwaitAndCollect();
        }
        else
        {
            if (TimeUntilEnd > -5) // Prevent player seeking
            {
                Game.onNoteClear.Invoke(Game, this);
                AwaitAndCollect();
            }
        }
    }

    public virtual void PlayHitSound()
    {
        if (Context.AudioManager.IsLoaded("HitSound"))
        {
            Context.AudioManager.Get("HitSound").Play();
        }
        Context.Haptic(HapticTypes.LightImpact, false);
    }

    protected virtual void OnGameUpdate(Game _)
    {
        // Reset cleared status in player mode
        if (Game is PlayerGame && IsCleared)
        {
            if (TimeUntilStart >= 0)
            {
                IsCleared = false;
            }
        }

        if (!IsCleared)
        {
            // Update position
            var model = Model;
            var ovr = model.Override;
            var position = model.position;

            if (ovr.XMultiplier != 1 || ovr.XOffset != 0) position.x = Game.Chart.ConvertChartXToScreenX((float) model.x * ovr.XMultiplier + ovr.XOffset);
            if (ovr.YMultiplier != 1 || ovr.YOffset != 0) position.y = Game.Chart.ConvertChartYToScreenY(model.y * ovr.YMultiplier + ovr.YOffset);
            if (ovr.X != null) position.x = ovr.X.Value;
            if (ovr.Y != null) position.y = ovr.Y.Value;
            if (ovr.Z != null) position.z = ovr.Z.Value;
        
            gameObject.transform.localPosition = position;
            
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

        Renderer.OnLateUpdate();
    }

    protected virtual void OnGameLateUpdate(Game _)
    {
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
            var nextPosition = hasNextNote ? nextNote.transform.localPosition : NextNoteModel.position;

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

    public virtual void OnTouch(Vector2 screenPos)
    {
        if (!Game.IsLoaded || !Game.State.IsPlaying) return;
        TryClear();
    }

    public virtual void TryClear()
    {
        if (IsAutoEnabled()) Clear(NoteGrade.Perfect);
        if (ShouldMiss()) Clear(NoteGrade.Miss);
        var grade = CalculateGrade();
        if (grade != NoteGrade.None) Clear(grade);
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
        return Renderer.DoesCollide(pos);
    }

    public virtual bool IsAutoEnabled()
    {
        return Game.State.Mods.Contains(Mod.Auto);
    }

    protected abstract NoteRenderer CreateRenderer();
}