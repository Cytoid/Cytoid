using System;
using UniRx.Async;
using UnityEngine;

public abstract class Note : MonoBehaviour
{
    public Game Game { get; private set; }

    [NonSerialized] public NoteRenderer Renderer;

    public ChartModel.Note Model { get; private set; }
    public ChartModel.Note NextModel { get; private set; }
    public ChartModel Chart { get; private set; }
    public ChartModel.Page Page { get; private set; }
    public NoteType Type { get; private set; }

    public bool HasEmerged => Game.Time >= Model.intro_time;

    public float MissThreshold { get; set; }

    public float TimeUntilStart => Model.start_time - Game.Time;
    public float TimeUntilEnd => Model.end_time - Game.Time;

    public bool IsCleared { get; private set; }

    // For ranked mode: weighted difference between the current timing and the perfect timing
    public float GreatGradeWeight { get; protected set; }

    public virtual void SetData(Game game, int noteId)
    {
        Game = game;
        Chart = game.Chart.Model;
        Model = game.Chart.Model.note_map[noteId];
        if (Model.next_id > 0 && Chart.note_map.ContainsKey(Model.next_id))
        {
            NextModel = Chart.note_map[Model.next_id];
        }

        Page = Chart.page_list[Model.page_index];
        Type = (NoteType) Model.type;

        Renderer = CreateRenderer();
        Renderer.OnNoteLoaded();
        MissThreshold = Type.GetDefaultMissThreshold();
        
        Game.onGameUpdate.AddListener(_ => OnGameUpdate());
        Game.onGameLateUpdate.AddListener(_ => OnGameLateUpdate());
    }

    public virtual void Clear(NoteGrade grade)
    {
        if (IsCleared) return;

        IsCleared = true;
        Renderer.OnClear(grade);
        Game.State.Judge(this, grade, -TimeUntilEnd, GreatGradeWeight);

        if (!(Game is PlayerGame))
        {
            Game.onNoteClear.Invoke(Game, this);
            AwaitAndDestroy();
        }
        else
        {
            if (TimeUntilEnd > -5) // Prevent player seeking
            {
                Game.onNoteClear.Invoke(Game, this);
            }
        }

        // Hit sound
        if (grade != NoteGrade.Miss && (!(this is HoldNote) || Context.Player.Settings.HoldHitSoundTiming.Let(it => it == HoldHitSoundTiming.End || it == HoldHitSoundTiming.Both)))
        {
            PlayHitSound();
        }
    }

    public void PlayHitSound()
    {
        if (Context.AudioManager.IsLoaded("HitSound"))
        {
            Context.AudioManager.Get("HitSound").Play();
        }
    }

    protected virtual void OnGameUpdate()
    {
        var config = Game.Config;
        var position = Model.position;
        if (config.NoteXOverride.ContainsKey(Model.id))
        {
            position.x = config.NoteXOverride[Model.id];
        }
        if (config.NoteYOverride.ContainsKey(Model.id))
        {
            position.y = config.NoteYOverride[Model.id];
        }
        
        gameObject.transform.position = Model.position = position;

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

    protected virtual void OnGameLateUpdate()
    {
        if (NextModel != null)
        {
            if (Model.position == NextModel.position)
                Model.rotation = 0;
            else if (Math.Abs(Model.position.y - NextModel.position.y) < 0.000001)
                Model.rotation = Model.position.x > NextModel.position.x ? -90 : 90;
            else if (Math.Abs(Model.position.x - NextModel.position.x) < 0.000001)
                Model.rotation = Model.position.y > NextModel.position.y ? 180 : 0;
            else
                Model.rotation =
                    Mathf.Atan((NextModel.position.x - Model.position.x) /
                               (NextModel.position.y - Model.position.y)) / Mathf.PI * 180f +
                    (NextModel.position.y > Model.position.y ? 0 : 180);
        }

        gameObject.transform.eulerAngles = new Vector3(0, 0, -Model.rotation);
    }

    public virtual bool ShouldMiss()
    {
        return TimeUntilStart < -MissThreshold;
    }

    protected virtual async void AwaitAndDestroy()
    {
        await UniTask.DelayFrame(0);
        Destroy();
    }

    protected virtual void Destroy()
    {
        if (gameObject == null) return;
        Destroy(gameObject);
        Renderer.Cleanup();
    }

    protected void OnDestroy()
    {
        Game.Notes.Remove(Model.id);
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
        var timeUntil = TimeUntilStart;

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