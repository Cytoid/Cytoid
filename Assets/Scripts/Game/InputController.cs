using System;
using System.Collections.Generic;
using System.Linq;
using Lean.Touch;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public Game game;

    public readonly Dictionary<int, FlickNote> FlickingNotes = new Dictionary<int, FlickNote>(); // Finger index to note
    public readonly Dictionary<int, HoldNote> HoldingNotes = new Dictionary<int, HoldNote>(); // Finger index to note
    public readonly List<Note> TouchableDragNotes = new List<Note>(); // Drag head, Drag child, CDrag child
    public readonly List<HoldNote> TouchableHoldNotes = new List<HoldNote>(); // Hold, Long hold
    public readonly List<Note> TouchableNormalNotes = new List<Note>(); // Click, CDrag head, Hold, Long hold, Flick

    private void Awake()
    {
        game.onGameUpdate.AddListener(OnGameUpdate);
        game.onGamePaused.AddListener(OnGamePaused);
    }

    public void EnableInput()
    {
        LeanTouch.OnFingerDown = OnFingerDown;
        LeanTouch.OnFingerSet = OnFingerSet;
        LeanTouch.OnFingerUp = OnFingerUp;
    }

    public void DisableInput()
    {
        LeanTouch.OnFingerDown = _ => { };
        LeanTouch.OnFingerSet = _ => { };
        LeanTouch.OnFingerUp = _ => { };
    }

    public void OnNoteCollected(Note note)
    {
        if (note.Type == NoteType.Hold || note.Type == NoteType.LongHold)
        {
            // Since you only have 10 fingers, this doesn't need to be optimized
            HoldingNotes.RemoveAll(it => it == note);
        }
    }

    public void OnGamePaused(Game game)
    {
        HoldingNotes.Values.ForEach(note =>
        {
            note.HoldingFingers.Clear();
        });
        HoldingNotes.Clear();
    }

    public void OnGameUpdate(Game game)
    {
        TouchableNormalNotes.Clear();
        TouchableDragNotes.Clear();
        TouchableHoldNotes.Clear();
        foreach (var id in game.SpawnedNotes.Keys)
        {
            var note = game.SpawnedNotes[id];
            if (!note.HasEmerged || note.IsCleared) continue;

            if (note.Type != NoteType.DragHead && note.Type != NoteType.DragChild && note.Type != NoteType.CDragChild)
            {
                TouchableNormalNotes.Add(note);
            } 
            else 
            {
                TouchableDragNotes.Add(note);
            }

            if ((note.Type == NoteType.Hold || note.Type == NoteType.LongHold) &&
                !((HoldNote) note).IsHolding)
            {
                TouchableHoldNotes.Add((HoldNote) note);
            }
        }

        // // Make sure to query non-flick notes first
        // TouchableNormalNotes.Sort((a, b) =>
        // {
        //     if (a.GetType() == b.GetType()) return a.Model.id - b.Model.id;
        //     if (a is FlickNote) return 1;
        //     return -1;
        // });
    }

    protected virtual void OnFingerDown(LeanFinger finger)
    {
        var pressedPosition = game.camera.orthographic
            ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
            : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));

        var collidedDrag = false;
        // Query drag notes first
        foreach (var note in TouchableDragNotes.Where(note => note != null).Where(note => note.DoesCollide(pressedPosition)))
        {
            note.OnTouch(finger.ScreenPosition);
            collidedDrag = true;
            break; // Query other notes too!
        }

        foreach (var note in TouchableNormalNotes.Where(note => note != null).Where(note => note.DoesCollide(pressedPosition)))
        {
            if (note is FlickNote flickNote)
            {
                if (FlickingNotes.ContainsKey(finger.Index) || FlickingNotes.ContainsValue(flickNote))
                    continue;
                FlickingNotes.Add(finger.Index, flickNote);
                flickNote.StartFlicking(pressedPosition);
            }
            else
            {
                if (collidedDrag && Math.Abs(note.TimeUntilStart) > note.Page.Duration / 8f) continue;
                if (note.Model.page_index > game.Chart.CurrentPageId &&
                    note.Model.start_time - game.Time >
                    game.Chart.Model.page_list[game.Chart.CurrentPageId].Duration * 0.5f) continue;
                note.OnTouch(finger.ScreenPosition);
            }

            return;
        }
    }

    protected virtual void OnFingerSet(LeanFinger finger)
    {
        var pos = game.camera.orthographic
            ? game.camera.ScreenToWorldPoint(finger.ScreenPosition)
            : game.camera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));

        // Query flick note
        if (FlickingNotes.ContainsKey(finger.Index))
        {
            var flickingNote = FlickingNotes[finger.Index];
            var cleared = flickingNote.UpdateFingerPosition(pos);
            if (cleared) FlickingNotes.Remove(finger.Index);
        }

        // Query drag notes
        foreach (var note in TouchableDragNotes)
        {
            if (note == null) continue;
            if (note.DoesCollide(pos))
            {
                note.OnTouch(finger.ScreenPosition);
                break; // Query other notes too!
            }
        }

        // If this is a new finger
        if (!HoldingNotes.ContainsKey(finger.Index))
        {
            var switchedToNewNote = false; // If the finger holds a new note

            // Query unheld hold notes
            foreach (var note in TouchableHoldNotes)
            {
                if (note == null) continue;
                if (note.DoesCollide(pos))
                {
                    HoldingNotes.Add(finger.Index, note);
                    note.UpdateFinger(finger.Index, true);
                    switchedToNewNote = true;
                    break;
                }
            }

            // Query held hold notes (i.e. multiple fingers on the same hold note)
            if (!switchedToNewNote)
            {
                foreach (var holdNote in HoldingNotes.Values.Where(holdNote => holdNote.DoesCollide(pos)))
                {
                    HoldingNotes.Add(finger.Index, holdNote);
                    holdNote.UpdateFinger(finger.Index, true);
                    break;
                }
            }
        }
        else // The finger is already holding a note
        {
            var holdNote = HoldingNotes[finger.Index];

            if (holdNote.IsCleared) // If cleared <-- This should be impossible since the note should have called OnNoteCollected
            {
                throw new InvalidOperationException();
                // HoldingNotes.Remove(finger.Index);
            }
            else if (!holdNote.DoesCollide(pos)) // If holding elsewhere
            {
                holdNote.UpdateFinger(finger.Index, false);
                HoldingNotes.Remove(finger.Index);
            }
        }
    }

    protected virtual void OnFingerUp(LeanFinger finger)
    {
        if (HoldingNotes.ContainsKey(finger.Index))
        {
            var holdNote = HoldingNotes[finger.Index];
            holdNote.UpdateFinger(finger.Index, false);
            HoldingNotes.Remove(finger.Index);
        }
    }
    
}