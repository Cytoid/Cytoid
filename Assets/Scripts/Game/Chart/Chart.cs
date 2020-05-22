using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class Chart
{
    private readonly float baseSize;
    private readonly float horizontalRatio;
    private readonly float verticalOffset;
    private readonly float verticalRatio;
    private readonly float screenRatio;
    
    public ChartModel Model { get; set; }
    
    public int CurrentTempoId { get; set; }
    public int CurrentPageId { get; set; }
    public int CurrentNoteId { get; set; }
    public int CurrentEventId { get; set; }
    
    public float MusicOffset { get; }

    public bool IsHorizontallyInverted { get; }
    public bool IsVerticallyInverted { get; }
    public bool UseScannerSmoothing { get; set; }

    public Chart(
        string text,
        bool isHorizontallyInverted,
        bool isVerticallyInverted,
        bool useScannerSmoothing,
        float approachRateMultiplier,
        float cameraOrthographicSize,
        float horizontalRatio,
        float verticalRatio,
        float verticalOffset)
    {
        IsHorizontallyInverted = isHorizontallyInverted;
        IsVerticallyInverted = isVerticallyInverted;
        UseScannerSmoothing = useScannerSmoothing;

        Model = new ChartModel();
        try
        {
            // New format
            Model = JsonConvert.DeserializeObject<ChartModel>(text);
        }
        catch (JsonReaderException)
        {
            // Legacy format
            Model = FromLegacyChart(text);
        }

        baseSize = cameraOrthographicSize;
        this.horizontalRatio = horizontalRatio;
        // verticalOffset = -baseSize * 0.04f;
        this.verticalRatio = verticalRatio;
        this.verticalOffset = verticalOffset;

        screenRatio = 1.0f * UnityEngine.Screen.width / UnityEngine.Screen.height;
        
        // Convert tick to absolute time
        foreach (var eventOrder in Model.event_order_list) eventOrder.time = ConvertToTime(eventOrder.tick);

        for (var index = 0; index < Model.page_list.Count; index++)
        {
            var page = Model.page_list[index];
            page.start_time = ConvertToTime((float) page.start_tick);
            page.end_time = ConvertToTime((float) page.end_tick);

            if (index != 0)
            {
                page.actual_start_tick = (float) Model.page_list[index - 1].end_tick;
                page.actual_start_time = Model.page_list[index - 1].end_time;
            }
            else
            {
                page.actual_start_tick = 0;
                page.actual_start_time = 0;
            }

            if (isVerticallyInverted) page.scan_line_direction = page.scan_line_direction == 1 ? -1 : 1;
        }

        for (var i = 0; i < Model.note_list.Count; i++)
        {
            var note = Model.note_list[i];
            Model.note_map[note.id] = note;
            var page = Model.page_list[note.page_index];

            note.direction = page.scan_line_direction;
            note.speed = note.page_index == 0 ? 1.0f : CalculateNoteSpeed(note);
            note.speed *= (float) note.approach_rate;

            var modSpeed = 1f;
            modSpeed *= approachRateMultiplier;
            note.speed *= modSpeed;

            note.start_time = ConvertToTime((float) note.tick);
            note.end_time = ConvertToTime((float) (note.tick + note.hold_tick));

            note.position = new Vector3(
                ConvertChartXToScreenX((float) note.x),
                GetNoteScreenY(note)
            );
            note.y = GetNoteChartY(note);

            note.end_position = new Vector3(
                note.position.x,
                ConvertChartTickToScreenY((float) (note.tick + note.hold_tick))
            );

            note.holdlength = (float) (verticalRatio * 2.0f * baseSize *
                                       note.hold_tick /
                                       (page.end_tick -
                                        page.start_tick));

            if (note.type == (int) NoteType.DragHead || note.type == (int) NoteType.DragChild || 
                note.type == (int) NoteType.CDragHead || note.type == (int) NoteType.CDragChild)
                note.intro_time = note.start_time - 1.175f / note.speed;
            else
                note.intro_time = note.start_time - 1.367f / note.speed;
        }

        foreach (var note in Model.note_list)
            switch ((NoteType) note.type)
            {
                case NoteType.Click:
                    note.tint = note.direction == 1 ? 0.94f : 1.06f;
                    break;
                case NoteType.Hold:
                    note.tint = note.direction == 1 ? 0.94f : 1.06f;
                    break;
                case NoteType.LongHold:
                    note.tint = note.direction == 1 ? 0.94f : 1.06f;
                    break;
                case NoteType.DragHead:
                case NoteType.CDragHead:
                    note.tint = note.direction == 1 ? 0.94f : 1.06f;
                    if (note.next_id > 0 && Model.note_map.ContainsKey(note.next_id))
                    {
                        note.nextdraglinestarttime = note.intro_time - 0.133f;
                        note.nextdraglinestoptime = Model.note_map[note.next_id].intro_time - 0.132f;
                    }

                    break;
                case NoteType.DragChild:
                case NoteType.CDragChild:
                    note.tint = note.direction == 1 ? 0.94f : 1.06f;
                    if (note.next_id > 0 && Model.note_map.ContainsKey(note.next_id))
                    {
                        note.nextdraglinestarttime = note.intro_time - 0.133f;
                        note.nextdraglinestoptime = Model.note_map[note.next_id].intro_time - 0.132f;
                    }

                    break;
                case NoteType.Flick:
                    note.tint = note.direction == 1 ? 1.00f : 1.30f;
                    break;
            }

        /*foreach (var note in Model.note_list)
        {
            if (note.next_id <= 0 || !Model.note_map.ContainsKey(note.next_id)) continue;

            var noteThis = note;
            var noteNext = Model.note_map[note.next_id];

            if (noteThis.position == noteNext.position)
                noteThis.rotation = 0;
            else if (Math.Abs(noteThis.position.y - noteNext.position.y) < 0.000001)
                noteThis.rotation = noteThis.position.x > noteNext.position.x ? -90 : 90;
            else if (Math.Abs(noteThis.position.x - noteNext.position.x) < 0.000001)
                noteThis.rotation = noteThis.position.y > noteNext.position.y ? 180 : 0;
            else
                noteThis.rotation =
                    Mathf.Atan((noteNext.position.x - noteThis.position.x) /
                               (noteNext.position.y - noteThis.position.y)) / Mathf.PI * 180f +
                    (noteNext.position.y > noteThis.position.y ? 0 : 180);
        }*/

        MusicOffset = (float) Model.music_offset;
    }

    private float ConvertToTime(float tick)
    {
        double result = 0;

        var currentTick = 0f;
        var currentTimeZone = 0;

        for (var i = 1; i < Model.tempo_list.Count; i++)
        {
            if (Model.tempo_list[i].tick >= tick) break;
            result += (Model.tempo_list[i].tick - currentTick) * 1e-6 * Model.tempo_list[i - 1].value /
                      (float) Model.time_base;
            currentTick = (float) Model.tempo_list[i].tick;
            currentTimeZone++;
        }

        result += (tick - currentTick) * 1e-6 * Model.tempo_list[currentTimeZone].value / (float) Model.time_base;
        return (float) result;
    }

    private int ConvertToTick(float time)
    {
        var currentTime = 0.0;
        var currentTick = 0.0;
        int i;
        for (i = 1; i < Model.tempo_list.Count; i++)
        {
            var delta = (Model.tempo_list[i].tick - Model.tempo_list[i - 1].tick) / Model.time_base *
                        Model.tempo_list[i - 1].value * 1e-6;
            if (currentTime + delta < time)
            {
                currentTime += delta;
                currentTick = Model.tempo_list[i].tick;
            }
            else
            {
                break;
            }
        }

        return Mathf.RoundToInt((float) (currentTick +
                                         (time - currentTime) / Model.tempo_list[i - 1].value * 1e6f *
                                         Model.time_base));
    }

    public float CalculateNoteSpeed(ChartModel.Note note)
    {
        var page = Model.page_list[note.page_index];
        var previousPage = Model.page_list[note.page_index - 1];
        var pageRatio = (float) (
            1.0f * (note.tick - page.actual_start_tick) /
            (page.end_tick -
             page.actual_start_tick));
        var tempo =
            (page.end_time -
             page.actual_start_time) * pageRatio +
            (previousPage.end_time -
             previousPage.actual_start_time) * (1.367f - pageRatio);
        return tempo >= 1.367f ? 1.0f : 1.367f / tempo;
    }

    public float ConvertChartXToScreenX(float x)
    {
        return (x * 2 * horizontalRatio - horizontalRatio) * baseSize * screenRatio * (IsHorizontallyInverted ? -1 : 1);
    }
    
    public float ConvertChartYToScreenY(float y)
    {
        return verticalRatio *
               (-baseSize + 2.0f * baseSize * y)
               + verticalOffset;
    }

    public float GetNoteScreenY(ChartModel.Note note)
    {
        var page = Model.page_list[note.page_index];
        return (float) (
            verticalRatio * page.scan_line_direction *
            (-baseSize + 2.0f *
                baseSize *
                (note.tick - page.start_tick) * 1.0f /
                (page.end_tick - page.start_tick))
            + verticalOffset);
    }
    
    public float GetNoteChartY(ChartModel.Note note)
    {
        var page = Model.page_list[note.page_index];
        return (float) (page.scan_line_direction * ((note.tick - page.start_tick) * 1.0f /
                                                    (page.end_tick - page.start_tick)));
    }

    public float ConvertChartTickToScreenY(float tick)
    {
        var targetPageId = 0;
        while (targetPageId < Model.page_list.Count && tick > Model.page_list[targetPageId].end_tick)
            targetPageId++;

        if (targetPageId == Model.page_list.Count)
            return (float) (
                -verticalRatio * Model.page_list[targetPageId - 1].scan_line_direction *
                (-baseSize + 2.0f *
                 baseSize *
                 (tick - Model.page_list[targetPageId - 1].end_tick) *
                 1.0f / (Model.page_list[targetPageId - 1].end_tick -
                         Model.page_list[targetPageId - 1].start_tick))
                + verticalOffset);

        return (float) (
            verticalRatio * Model.page_list[targetPageId].scan_line_direction *
            (-baseSize + 2.0f *
             baseSize * (tick - Model.page_list[targetPageId].start_tick) *
             1.0f / (Model.page_list[targetPageId].end_tick - Model.page_list[targetPageId].start_tick))
            + verticalOffset);
    }

    public float GetScannerPositionY(float time, bool useScannerSmoothing)
    {
        CurrentPageId = 0;
        while (CurrentPageId < Model.page_list.Count && time > Model.page_list[CurrentPageId].end_time)
            CurrentPageId++;
        if (CurrentPageId == Model.page_list.Count)
        {
            if (useScannerSmoothing)
                return (float) (-verticalRatio * Model.page_list[CurrentPageId - 1].scan_line_direction *
                                (-baseSize + 2.0f *
                                 baseSize *
                                 (ConvertToTick(time) - Model.page_list[CurrentPageId - 1].end_tick) *
                                 1.0f / (Model.page_list[CurrentPageId - 1].end_tick -
                                         Model.page_list[CurrentPageId - 1].start_tick))
                                + verticalOffset);
            return -verticalRatio * Model.page_list[CurrentPageId - 1].scan_line_direction *
                   (-baseSize + 2.0f *
                    baseSize *
                    (time - Model.page_list[CurrentPageId - 1].end_time) *
                    1.0f / (Model.page_list[CurrentPageId - 1].end_time -
                            Model.page_list[CurrentPageId - 1].start_time))
                   + verticalOffset;
        }

        if (useScannerSmoothing)
            return (float) (verticalRatio * Model.page_list[CurrentPageId].scan_line_direction *
                            (-baseSize + 2.0f *
                             baseSize *
                             (ConvertToTick(time) - Model.page_list[CurrentPageId].start_tick) *
                             1.0f / (Model.page_list[CurrentPageId].end_tick -
                                     Model.page_list[CurrentPageId].start_tick))
                            + verticalOffset);
        return verticalRatio * Model.page_list[CurrentPageId].scan_line_direction *
               (-baseSize + 2.0f *
                baseSize *
                (time - Model.page_list[CurrentPageId].start_time) *
                1.0f / (Model.page_list[CurrentPageId].end_time - Model.page_list[CurrentPageId].start_time))
               + verticalOffset;
    }

    public float GetScanlinePosition01(float percentage)
    {
        return ConvertChartYToScreenY(percentage);
    }

    public float GetBoundaryPosition(bool bottom)
    {
        return verticalRatio * (bottom ? 1 : -1) *
               -baseSize
               + verticalOffset;
    }

    public ChartModel FromLegacyChart(string text)
    {
        // Parse

        var pageDuration = 0f;
        var pageShift = 0f;
        var tmpNotes = new Dictionary<int, LegacyNote>();

        foreach (var line in text.Split('\n'))
        {
            var data = line.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
            if (data.Length == 0) continue;
            var type = data[0];
            switch (type)
            {
                case "PAGE_SIZE":
                    pageDuration = float.Parse(data[1]);
                    break;
                case "PAGE_SHIFT":
                    pageShift = float.Parse(data[1]);
                    break;
                case "NOTE":
                    var note = new LegacyNote(int.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]),
                        float.Parse(data[4]), false);
                    tmpNotes.Add(int.Parse(data[1]), note);
                    if (note.Duration > 0) note.Type = LegacyNoteType.Hold;
                    break;
                case "LINK":
                    var notesInChain = new List<LegacyNote>();
                    for (var i = 1; i < data.Length; i++)
                    {
                        int id;
                        if (!int.TryParse(data[i], out id)) continue;
                        note = tmpNotes[id];
                        note.Type = LegacyNoteType.Drag;

                        if (!notesInChain.Contains(note)) notesInChain.Add(note);
                    }

                    for (var i = 0; i < notesInChain.Count - 1; i++)
                        notesInChain[i].ConnectedNote = notesInChain[i + 1];

                    notesInChain[0].IsChainHead = true;
                    break;
            }
        }

        pageShift += pageDuration;

        // Calculate chronological note ids
        var sortedNotes = tmpNotes.Values.ToList();
        sortedNotes.Sort((a, b) => a.Time.CompareTo(b.Time));
        var chronologicalIds = sortedNotes.Select(note => note.OriginalId).ToList();

        var notes = new Dictionary<int, LegacyNote>();

        // Recalculate note ids from original ids
        var newId = 0;
        foreach (var noteId in chronologicalIds)
        {
            tmpNotes[noteId].Id = newId;
            notes[newId] = tmpNotes[noteId];
            newId++;
        }

        // Reset chronological ids
        chronologicalIds.Clear();
        for (var i = 0; i < tmpNotes.Count; i++) chronologicalIds.Add(i);

        // Convert

        const int timeBase = 480;

        var root = new ChartModel();
        root.time_base = 480;

        var tempo = new ChartModel.Tempo();
        tempo.tick = 0;
        var tempoValue = (long) (pageDuration * 1000000f);
        tempo.value = tempoValue;
        root.tempo_list = new List<ChartModel.Tempo> {tempo};

        if (pageShift < 0) pageShift = pageShift + 2 * pageDuration;

        var pageShiftTickOffset = pageShift / pageDuration * timeBase;

        var noteList = new List<ChartModel.Note>();
        var page = 0;
        foreach (var note in notes.Values)
        {
            var obj = new ChartModel.Note();
            obj.id = note.Id;
            switch (note.Type)
            {
                case LegacyNoteType.Click:
                    obj.type = (int) NoteType.Click;
                    break;
                case LegacyNoteType.Drag:
                    obj.type = note.IsChainHead ? (int) NoteType.DragHead : (int) NoteType.DragChild;
                    break;
                case LegacyNoteType.Hold:
                    obj.type = (int) NoteType.Hold;
                    break;
            }

            obj.x = note.X;
            var ti = note.Time * timeBase * 1000000 / tempoValue + pageShiftTickOffset;
            obj.tick = ti;
            obj.hold_tick = note.Duration * timeBase * 1000000 / tempoValue;
            page = Mathf.FloorToInt(ti / timeBase);
            obj.page_index = page;
            if (note.Type == LegacyNoteType.Drag)
                obj.next_id = note.ConnectedNote != null ? note.ConnectedNote.Id : -1;
            else
                obj.next_id = 0;

            noteList.Add(obj);
        }

        root.note_list = noteList;

        var pageList = new List<ChartModel.Page>();
        var direction = false;
        var t = 0;
        for (var i = 0; i <= page; i++)
        {
            var obj = new ChartModel.Page();
            obj.scan_line_direction = direction ? 1 : -1;
            direction = !direction;
            obj.start_tick = t;
            t += timeBase;
            obj.end_tick = t;
            pageList.Add(obj);
        }

        root.page_list = pageList;

        root.music_offset = pageShiftTickOffset / timeBase / 1000000 * tempoValue;
        return root;
    }

    public class LegacyNote
    {
        public LegacyNote ConnectedNote;
        public float Duration;
        public int Id;
        public bool IsChainHead;
        public int OriginalId;
        public float Time;

        public LegacyNoteType Type = LegacyNoteType.Click;
        public float X;

        public LegacyNote(int originalId, float time, float x, float duration, bool isChainHead)
        {
            OriginalId = originalId;
            Time = time;
            X = x;
            Duration = duration;
            IsChainHead = isChainHead;
        }
    }

    public enum LegacyNoteType
    {
        Click,
        Drag,
        Hold
    }
}