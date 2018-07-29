using System;
using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using Cytus2.Views;
using E7.Native;
using UnityEngine;
using UnityEngine.Assertions;

public class GameNote : MonoBehaviour
{
    public Game Game;
    public RankedPlayData.Note RankData;

    public ChartRoot Chart;
    public ChartNote Note;
    public ChartPage Page;
    public NoteView View;

    public bool HasEmerged
    {
        get { return Game.Time >= Note.intro_time; }
    }

    public float MaxMissThreshold;

    public float TimeUntilStart;
    public float TimeUntilEnd;

    public bool IsCleared;

    public double
        GreatGradeWeight; // For ranked mode: weighted difference between the current timing and the perfect timing

    public void Init(ChartRoot chart, ChartNote note)
    {
        Chart = chart;
        Note = note;

        Page = Chart.page_list[Note.page_index];
        TimeUntilStart = Note.start_time;
        TimeUntilEnd = Note.end_time;
        MaxMissThreshold = Mathf.Max(0.300f, 0.300f); // TODO: 0.300f?
        View.OnInit(chart, note);
        gameObject.transform.position = Note.position;

        if (Game.Play.IsRanked)
        {
            RankData = new RankedPlayData.Note();
            RankData.id = note.id;
            // Game.RankedPlayData.notes.Add(RankData); // Removed in 1.5
        }
    }

    protected virtual void Awake()
    {
        Game = Game.Instance;
    }

    public virtual void Clear(NoteGrade grade)
    {
        if (IsCleared || grade == NoteGrade.Undetermined) return;

        IsCleared = true;
        Game.OnClear(this);
        View.OnClear(grade);

        if (!(Game.Instance is StoryboardGame))
        {
            StartCoroutine(DestroyLater());
        }

        if (TimeUntilEnd > -5) // Prevent storyboard seeking
        {
            EventKit.Broadcast("note clear", this);
        }
        
        // Hit sound
        if (grade != NoteGrade.Miss && (!(this is HoldNote) || !PlayerPrefsExt.GetBool("early hit sounds")))
        {    
            PlayHitSound();
        }

        // gameObject.GetComponent<SpriteRenderer> ().material.SetFloat("_HRate", 1.0f);
        // Animation speed = 1.0f;
    }

    public void PlayHitSound()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if (GameOptions.Instance.HitSound != null)
        {
            GameOptions.Instance.HitSound.Play(NativeAudio.PlayOptions.defaultOptions).SetVolume(1);
        }
#endif
    }

    protected virtual void LateUpdate()
    {
        if (Game is StoryboardGame)
        {
            // Show note id
            transform.Find("NoteFill").GetChild(0).gameObject
                .SetActive(Game.Time >= Note.intro_time && Game.Time <= Note.end_time);
        }

        TimeUntilStart = Note.start_time - Game.Time;
        TimeUntilEnd = Note.end_time - Game.Time;

        if (Game.Instance is StoryboardGame && IsCleared)
        {
            if (Game.Time <= Note.intro_time)
            {
                IsCleared = false;
            }
        }

        if (!IsCleared)
        {
            // Autoplay
            if (TimeUntilStart < 0)
            {
                if (this is HoldNote && (Mod.AutoHold.IsEnabled() || Mod.Auto.IsEnabled()))
                {
                    ((HoldNote) this).StartHolding();
                }
                else if (
                    Mod.Auto.IsEnabled()
                    || (Mod.AutoDrag.IsEnabled() && (this is DragChildNote || this is DragHeadNote))
                    || (Mod.AutoHold.IsEnabled() && this is HoldNote)
                    || (Mod.AutoFlick.IsEnabled() && this is FlickNote)
                )
                {
                    Clear(NoteGrade.Perfect);
                }
            }

            // Check removable
            if (IsMissed())
            {
                Clear(NoteGrade.Miss);
            }

            // If still not cleared, render
            if (!IsCleared)
            {
                if (Game.Time >= Note.intro_time)
                {
                    View.OnRender();
                }
                else if (View.IsRendered()) // This only happens under Storyboard mode
                {
                    if (Game is StoryboardGame)
                    {
                        View.OnClear(NoteGrade.Undetermined);
                    }
                }
            }
        }

        View.OnLateUpdate();
    }

    public virtual bool IsMissed()
    {
        return TimeUntilStart < -MaxMissThreshold;
    }

    protected virtual IEnumerator DestroyLater()
    {
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }

    public virtual void Touch(Vector2 screenPos)
    {
        if (!Game.IsLoaded || !Game.IsPlaying) return;
        var grading = CalculateGrading();
        if (grading == NoteGrade.Undetermined) return;

        RankData.press_time = TimeExt.Millis();
        RankData.press_x = (int) screenPos.x;
        RankData.press_y = (int) screenPos.y;

        Clear(grading);
    }

    public virtual NoteGrade CalculateGrading()
    {
        if (Mod.Auto.IsEnabled()
            || (Mod.AutoDrag.IsEnabled() && (this is DragHeadNote || this is DragChildNote))
            || (Mod.AutoHold.IsEnabled() && (this is HoldNote))
            || (Mod.AutoFlick.IsEnabled() && this is FlickNote))
            return NoteGrade.Perfect;

        if (IsMissed()) return NoteGrade.Miss;

        var grading = NoteGrade.Undetermined;
        var timeUntil = TimeUntilStart;

        if (Game.Play.IsRanked)
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.400f)
                {
                    grading = NoteGrade.Bad;
                }

                if (timeUntil < 0.200f)
                {
                    grading = NoteGrade.Good;
                }

                if (timeUntil < 0.070f)
                {
                    grading = NoteGrade.Great;
                }

                if (timeUntil <= 0.040f)
                {
                    grading = NoteGrade.Perfect;
                }

                if (grading == NoteGrade.Great)
                {
                    GreatGradeWeight = 1.0f - (timeUntil - 0.040f) / (0.070f - 0.040f);
                }
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.200f)
                {
                    grading = NoteGrade.Bad;
                }

                if (timePassed < 0.150f)
                {
                    grading = NoteGrade.Good;
                }

                if (timePassed < 0.070f)
                {
                    grading = NoteGrade.Great;
                }

                if (timePassed <= 0.040f)
                {
                    grading = NoteGrade.Perfect;
                }

                if (grading == NoteGrade.Great)
                {
                    GreatGradeWeight = 1.0f - (timePassed - 0.040f) / (0.070f - 0.040f);
                }
            }
        }
        else
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.800f)
                {
                    grading = NoteGrade.Bad;
                }

                if (timeUntil < 0.400f)
                {
                    grading = NoteGrade.Good;
                }

                if (timeUntil < 0.200f)
                {
                    grading = NoteGrade.Great;
                }

                if (timeUntil < 0.070f)
                {
                    grading = NoteGrade.Perfect;
                }
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.300f)
                {
                    grading = NoteGrade.Bad;
                }

                if (timePassed < 0.200f)
                {
                    grading = NoteGrade.Good;
                }

                if (timePassed < 0.150f)
                {
                    grading = NoteGrade.Great;
                }

                if (timePassed < 0.070f)
                {
                    grading = NoteGrade.Perfect;
                }
            }
        }

        return grading;
    }

    public bool DoesCollide(Vector2 pos)
    {
        return View.DoesCollide(pos);
    }
}