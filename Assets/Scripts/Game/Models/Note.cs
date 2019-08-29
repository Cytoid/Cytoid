using System;
using UniRx.Async;
using UnityEngine;

public abstract class Note : MonoBehaviour
{
    public Game Game { get; private set; }

    [NonSerialized] public NoteRenderer Renderer;

    public ChartModel.Note Model { get; private set; }
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
        Model = game.Chart.Model.note_list[noteId];
        Page = Chart.page_list[Model.page_index];
        Type = (NoteType) Model.type;

        Renderer = CreateRenderer();
        Renderer.OnNoteLoaded();
        gameObject.transform.position = Model.position;
        MissThreshold = Type.GetDefaultMissThreshold();
        
        Game.onGameUpdate.AddListener(_ => OnGameUpdate());
    }

    public virtual void Clear(NoteGrade grade)
    {
        if (IsCleared) return;

        IsCleared = true;
        Renderer.OnClear(grade);
        Game.State.Judge(this, grade, TimeUntilEnd, GreatGradeWeight);

        if (!(Game is StoryboardGame))
        {
            AwaitAndDestroy();
        }
        else
        {
            if (TimeUntilEnd > -5) // Prevent storyboard seeking
            {
                Game.onNoteClear.Invoke(Game, this);
            }
        }

        // Hit sound
        if (grade != NoteGrade.Miss && (!(this is HoldNote) || !Context.LocalPlayer.PlayHitSoundsEarly))
        {
            PlayHitSound();
        }
    }

    public void PlayHitSound()
    {
        if (Context.AudioManager.IsLoaded("hitSound")) Context.AudioManager.Get("hitSound").Play(AudioTrackIndex.RoundRobin);
    }

    protected virtual void OnGameUpdate()
    {
        // Reset cleared status in storyboarding mode
        if (Game is StoryboardGame && IsCleared)
        {
            if (Game.Time <= Model.intro_time)
            {
                IsCleared = false;
            }
        }

        if (!IsCleared)
        {
            // Autoplay
            if (TimeUntilStart < 0)
            {
                if (this is HoldNote && IsAutoEnabled())
                {
                    ((HoldNote) this).Holding = true;
                }
                else if (IsAutoEnabled())
                {
                    Clear(NoteGrade.Perfect);
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

        if (Game.State.IsRanked)
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
        else
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