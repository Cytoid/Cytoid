using System;
using System.Collections.Generic;
using System.Linq;

namespace Cytus.Models
{

    public class Chart : BaseChart
    {

        public float PageDuration;
        public float PageShift;
        public Dictionary<int, Note> Notes = new Dictionary<int, Note>();
        public List<int> ChronologicalIds;
        public float Offset;
        
        public Chart(string text) : base(text)
        {
        }

        public override string Parse(string text)
        {
            var checksumSource = string.Empty;

            foreach (var line in text.Split('\n'))
            {
                var data = line.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
                if (data.Length == 0) continue;
                var type = data[0];
                switch (type)
                {
                    case "PAGE_SIZE":
                        PageDuration = float.Parse(data[1]);
                        checksumSource += data[1];
                        break;
                    case "PAGE_SHIFT":
                        PageShift = float.Parse(data[1]);
                        checksumSource += data[1];
                        break;
                    case "NOTE":
                        checksumSource += data[1] + data[2] + data[3] + data[4];
                        var note = new Note(int.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]),
                            float.Parse(data[4]), false);
                        Notes.Add(int.Parse(data[1]), note);
                        if (note.duration > 0) note.type = OldNoteType.Hold;
                        break;
                    case "LINK":
                        var notesInChain = new List<Note>();
                        for (var i = 1; i < data.Length; i++)
                        {
                            if (data[i] != "LINK") checksumSource += data[i];
                            int id;
                            if (!int.TryParse(data[i], out id)) continue;
                            note = Notes[id];
                            note.type = OldNoteType.Chain;
                            notesInChain.Add(note);
                        }

                        for (var i = 0; i < notesInChain.Count - 1; i++)
                        {
                            notesInChain[i].connectedNote = notesInChain[i + 1];
                        }

                        notesInChain[0].isChainHead = true;
                        break;
                    case "OFFSET":
                        Offset = float.Parse(data[1]);
                        break;
                }
            }

            PageShift += PageDuration;
            
            // Calculate chronological note ids
            var noteList = Notes.Values.ToList();
            noteList.Sort((a, b) => a.time.CompareTo(b.time));
            ChronologicalIds = noteList.Select(note => note.id).ToList();

            return checksumSource;
        }
        
    }

}