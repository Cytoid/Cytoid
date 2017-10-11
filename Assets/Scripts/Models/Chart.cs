using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chart
{

    public float bpm;
    public float pageDuration;
    public float pageShift;
    public Dictionary<int, Note> notes = new Dictionary<int, Note>();
    public List<int> chronologicalIds = new List<int>();

    public float offset;

    public Chart(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var data = line.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
            if (data.Length == 0) continue;
            var type = data[0];
            switch (type)
            {
                case "BPM":
                    bpm = float.Parse(data[1]);
                    break;
                case "PAGE_SIZE":
                    pageDuration = float.Parse(data[1]);
                    break;
                case "PAGE_SHIFT":
                    pageShift = float.Parse(data[1]) + pageDuration;
                    break;
                case "NOTE":
                    var note = new Note(int.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]),
                        float.Parse(data[4]), false);
                    notes.Add(int.Parse(data[1]), note);
                    if (note.duration > 0) note.type = NoteType.Hold;
                    break;
                case "LINK":
                    var notesInChain = new List<Note>();
                    for (var i = 1; i < data.Length; i++)
                    {
                        int id;
                        if (!int.TryParse(data[i], out id)) continue;
                        note = notes[id];
                        note.type = NoteType.Chain;
                        notesInChain.Add(note);
                    }
                    for (var i = 0; i < notesInChain.Count - 1; i++)
                    {
                        notesInChain[i].connectedNote = notesInChain[i + 1];
                    }
                    notesInChain[0].isChainHead = true;
                    break;
                case "OFFSET":
                    offset = float.Parse(data[1]);
                    break;
            }
        }
        // Calculate chronological note ids
        var noteList = notes.Values.ToList();
        noteList.Sort((a, b) => a.time.CompareTo(b.time));
        chronologicalIds = noteList.Select(note => note.id).ToList();
    }

}