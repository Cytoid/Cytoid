using System;
using System.Collections;
using Cytus2.Controllers;
using Cytus2.Models;
using Cytus2.Views;
using UnityEngine;
using UnityEngine.Assertions;

public class GameNote : MonoBehaviour
{
    public Game Game;
    public RankedModeData.Note RankData;

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
            RankData = new RankedModeData.Note();
            RankData.id = note.id;
            Game.RankData.notes.Add(RankData);
        }
    }

    protected virtual void Awake()
    {
        Game = Game.Instance;
    }

    public virtual void Clear(NoteGrading grading)
    {
        if (grading == NoteGrading.Undetermined) throw new InvalidOperationException("Note grading undetermined");

        IsCleared = true;
        Game.OnClear(this);
        View.OnClear(grading);

        if (!(Game.Instance is StoryboardGame))
        {
            StartCoroutine(DestroyLater());
        }

        if (TimeUntilEnd > -5) // Prevent storyboard seeking
        {
            EventKit.Broadcast("note clear", this);
        }

        // gameObject.GetComponent<SpriteRenderer> ().material.SetFloat("_HRate", 1.0f);
        // Animation speed = 1.0f;
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
                    Clear(NoteGrading.Perfect);
                }
            }

            // Check removable
            if (IsMissed())
            {
                Clear(NoteGrading.Miss);
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
                        View.OnClear(NoteGrading.Undetermined);
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
        if (grading == NoteGrading.Undetermined) return;

        RankData.press_time = TimeExt.Millis();
        RankData.press_x = (int) screenPos.x;
        RankData.press_y = (int) screenPos.y;

        Clear(grading);
    }

    public virtual NoteGrading CalculateGrading()
    {
        if (Mod.Auto.IsEnabled()
            || (Mod.AutoDrag.IsEnabled() && (this is DragHeadNote || this is DragChildNote))
            || (Mod.AutoHold.IsEnabled() && (this is HoldNote))
            || (Mod.AutoFlick.IsEnabled() && this is FlickNote))
            return NoteGrading.Perfect;
        
        var grading = NoteGrading.Miss;
        var timeUntil = TimeUntilStart;

        if (Game.Play.IsRanked)
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.400f)
                {
                    grading = NoteGrading.Bad;
                }

                if (timeUntil < 0.200f)
                {
                    grading = NoteGrading.Good;
                }

                if (timeUntil < 0.070f)
                {
                    grading = NoteGrading.Great;
                }

                if (timeUntil <= 0.040f)
                {
                    grading = NoteGrading.Perfect;
                }

                if (grading == NoteGrading.Great)
                {
                    GreatGradeWeight = 1.0f - (timeUntil - 0.040f) / (0.070f - 0.040f);
                }
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.200f)
                {
                    grading = NoteGrading.Bad;
                }

                if (timePassed < 0.150f)
                {
                    grading = NoteGrading.Good;
                }

                if (timePassed < 0.070f)
                {
                    grading = NoteGrading.Great;
                }

                if (timePassed <= 0.040f)
                {
                    grading = NoteGrading.Perfect;
                }

                if (grading == NoteGrading.Great)
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
                    grading = NoteGrading.Bad;
                }

                if (timeUntil < 0.400f)
                {
                    grading = NoteGrading.Good;
                }

                if (timeUntil < 0.200f)
                {
                    grading = NoteGrading.Great;
                }

                if (timeUntil < 0.070f)
                {
                    grading = NoteGrading.Perfect;
                }
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.300f)
                {
                    grading = NoteGrading.Bad;
                }

                if (timePassed < 0.200f)
                {
                    grading = NoteGrading.Good;
                }

                if (timePassed < 0.150f)
                {
                    grading = NoteGrading.Great;
                }

                if (timePassed < 0.070f)
                {
                    grading = NoteGrading.Perfect;
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