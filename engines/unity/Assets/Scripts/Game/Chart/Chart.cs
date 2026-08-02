using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class Chart
{
    public ChartModel Model { get; set; }
    
    public int CurrentPageId { get; set; }
    public int CurrentNoteId { get; set; }
    public int CurrentEventId { get; set; }
    
    public float MusicOffset { get; }
    public bool DisplayBoundaries { get; }
    public bool DisplayBackground { get; }
    public int HorizontalMargin { get; }
    public int VerticalMargin { get; }
    public bool RestrictPlayAreaAspectRatio { get; }
    public bool SkipMusicOnCompletion { get; }
    public bool IsHorizontallyInverted { get; }
    public bool IsVerticallyInverted { get; }
    public bool UseScannerSmoothing { get; set; }

    public Dictionary<NoteType, int> MaxSamePageNoteCountByType { get; } = new Dictionary<NoteType, int>();
    public int MaxSamePageNoteCount { get; }
    public int MaxSamePageNonDragTypeNoteCount { get; }
    public int MaxSamePageDragTypeNoteCount { get; }
    public int MaxSamePageHoldTypeNoteCount { get; }

    /// <summary>note id → drag stack id (only multi-note stacks, size ≥ 2).</summary>
    public Dictionary<int, int> NoteIdToDragStackId { get; } = new Dictionary<int, int>();

    /// <summary>stack id → note ids sorted ascending (list-order / DragCoHit order).</summary>
    public Dictionary<int, List<int>> DragStackMembers { get; } = new Dictionary<int, List<int>>();

    /// <summary>Peak distinct drag-stack hosts on one page (for pool sizing).</summary>
    public int MaxSamePageDragStackHostCount { get; private set; }
    
    private readonly float baseSize;
    private readonly float horizontalRatio;
    private readonly float verticalOffset;
    private readonly float verticalRatio;
    private readonly float screenRatio;

    public Chart(
        string text,
        bool isHorizontallyInverted,
        bool isVerticallyInverted,
        bool useScannerSmoothing,
        bool useExperimentalNoteAr,
        float approachRateMultiplier,
        float cameraOrthographicSize)
    {
        IsHorizontallyInverted = isHorizontallyInverted;
        IsVerticallyInverted = isVerticallyInverted;
        UseScannerSmoothing = useScannerSmoothing;
        
        baseSize = cameraOrthographicSize;
        screenRatio = 1.0f * UnityEngine.Screen.width / UnityEngine.Screen.height;
        
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
        
        // Cytoid chart parameters
        MusicOffset = (float) Model.music_offset;
        DisplayBoundaries = Model.display_boundaries ?? Context.Player.Settings.DisplayBoundaries;
        DisplayBackground = Model.display_background ?? true;
        HorizontalMargin = Model.horizontal_margin ?? Context.Player.Settings.HorizontalMargin;
        VerticalMargin = Model.vertical_margin ?? Context.Player.Settings.VerticalMargin;
        RestrictPlayAreaAspectRatio = Model.restrict_play_area_aspect_ratio ?? Context.Player.Settings.RestrictPlayAreaAspectRatio;
        SkipMusicOnCompletion = Model.skip_music_on_completion ?? Context.Player.Settings.SkipMusicOnCompletion;

        // Apply aspect ratio restriction if enabled
        if (RestrictPlayAreaAspectRatio)
        {
            const float maxRatio = 16f / 9f;  // 16:9
            const float minRatio = 4f / 3f;   // 4:3
            
            if (screenRatio > maxRatio)
            {
                screenRatio = maxRatio;
            }
            else if (screenRatio < minRatio)
            {
                screenRatio = minRatio;
            }
        }
        
        var height = cameraOrthographicSize * 2.0f;
        var width = height * screenRatio;
        
        const float topRatio = 0.0966666f;
        const float bottomRatio = 0.07f;
        
        horizontalRatio = 0.8f + (5 - HorizontalMargin - 1) * 0.02f;
        verticalRatio = 1 - width * (topRatio + bottomRatio) / height + (3 - VerticalMargin) * 0.05f;
        verticalOffset = -(width * (topRatio - (topRatio + bottomRatio) / 2.0f));

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
            PositionFunction.BakeArgs(page);
        }

        var pageNoteCountsByType = new Dictionary<NoteType, int[]>();
        foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType)))
        {
            pageNoteCountsByType[type] = new int[Model.page_list.Count];
        }

        var pageNoteCounts = new int[Model.page_list.Count];
        var pageDragTypeNoteCounts = new int[Model.page_list.Count];
        var pageNonDragTypeNoteCounts = new int[Model.page_list.Count];
        var pageHoldTypeNoteCounts = new int[Model.page_list.Count];
        for (var i = 0; i < Model.note_list.Count; i++)
        {
            var note = Model.note_list[i];
            var type = (NoteType) note.type;
            Model.note_map[note.id] = note;
            var page = Model.page_list[note.page_index];
            pageNoteCountsByType[(NoteType) note.type][note.page_index]++;
            pageNoteCounts[note.page_index]++;
            
            var isDragType = type == NoteType.DragHead || type == NoteType.DragChild || type == NoteType.CDragChild;
            var isHoldType = type == NoteType.Hold || type == NoteType.LongHold;

            if (isDragType) pageDragTypeNoteCounts[note.page_index]++;
            else pageNonDragTypeNoteCounts[note.page_index]++;
            if (isHoldType) pageHoldTypeNoteCounts[note.page_index]++;
            
            note.direction = page.scan_line_direction;
            var speed = note.page_index == 0 ? 1.0f : CalculateNoteSpeed(note);

            var modSpeed = 1f;
            modSpeed *= approachRateMultiplier;
            speed *= modSpeed;
            speed *= (float) note.approach_rate;

            switch (type)
            {
                case NoteType.Click:
                case NoteType.Hold:
                case NoteType.LongHold:
                case NoteType.Flick:
                case NoteType.CDragHead:
                    note.initial_scale = useExperimentalNoteAr ? 0.1f : 0.4f;
                    break;
                case NoteType.DragHead:
                case NoteType.DragChild:
                case NoteType.CDragChild:
                    note.initial_scale = useExperimentalNoteAr ? 0.4f : 0.7f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (useExperimentalNoteAr)
            {
                speed /= 1.5f;
            }

            // Final speed (mods × AR × experimental) must stay positive/finite for intro/scale.
            const float minNoteSpeed = 1e-4f;
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed <= minNoteSpeed)
                speed = 1f;

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

            note.holdlength = GetHoldScreenLength(page, (float) note.tick, (float) (note.tick + note.hold_tick));

            if (note.type == (int) NoteType.DragHead || note.type == (int) NoteType.DragChild || 
                note.type == (int) NoteType.CDragHead || note.type == (int) NoteType.CDragChild)
                note.intro_time = note.start_time - 1.175f / speed;
            else
                note.intro_time = note.start_time - 1.367f / speed;
        }

        foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType)))
        {
            MaxSamePageNoteCountByType[type] = pageNoteCountsByType[type].Max();
        }
        MaxSamePageNoteCount = pageNoteCounts.Max();
        MaxSamePageDragTypeNoteCount = pageDragTypeNoteCounts.Max();
        MaxSamePageNonDragTypeNoteCount = pageNonDragTypeNoteCounts.Max();
        MaxSamePageHoldTypeNoteCount = pageHoldTypeNoteCounts.Max();

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

        BuildDragStacks();
    }

    /// <summary>
    /// Groups co-located drag notes that share type, start time, and chart x into stacks.
    /// Only multi-member groups are recorded — singles keep the legacy per-note path.
    /// </summary>
    private void BuildDragStacks()
    {
        NoteIdToDragStackId.Clear();
        DragStackMembers.Clear();

        var buckets = new Dictionary<long, List<int>>();
        foreach (var note in Model.note_list)
        {
            var type = (NoteType) note.type;
            // CDragHead is Select-consumed (one per Down); do not stack-share its collider.
            if (type != NoteType.DragHead && type != NoteType.DragChild &&
                type != NoteType.CDragChild)
            {
                continue;
            }

            var key = MakeDragStackKey(type, note.start_time, note.x);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = new List<int>();
                buckets[key] = list;
            }

            list.Add(note.id);
        }

        var nextStackId = 1;
        var pageHostCounts = new int[Model.page_list.Count];
        foreach (var pair in buckets)
        {
            var ids = pair.Value;
            if (ids.Count < 2) continue;

            ids.Sort();
            var stackId = nextStackId++;
            DragStackMembers[stackId] = ids;
            var pageIndex = Model.note_map[ids[0]].page_index;
            if (pageIndex >= 0 && pageIndex < pageHostCounts.Length)
            {
                pageHostCounts[pageIndex]++;
            }

            foreach (var id in ids)
            {
                NoteIdToDragStackId[id] = stackId;
            }
        }

        MaxSamePageDragStackHostCount = pageHostCounts.Length > 0 ? pageHostCounts.Max() : 0;
    }

    private static long MakeDragStackKey(NoteType type, float startTime, double x)
    {
        var t = (long) Mathf.Round(startTime * 1000f);
        var xq = (long) Mathf.Round((float) x * 10000f);
        return ((long) (int) type << 48) ^ (t << 20) ^ (xq & 0xFFFFF);
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

    float PageDisplayYToScreenY(float yDisplay)
    {
        return verticalRatio * (yDisplay * baseSize) + verticalOffset;
    }

    static float GetPageProgressByTick(ChartModel.Page page, float tick)
    {
        var span = (float) (page.end_tick - page.start_tick);
        if (span <= 0f) return 0f;
        return (tick - (float) page.start_tick) / span;
    }

    static float GetPageProgressByTime(ChartModel.Page page, float time)
    {
        var span = page.end_time - page.start_time;
        if (span <= 0f) return 0f;
        return (time - page.start_time) / span;
    }

    public int FindPageIndexByTime(float time)
    {
        for (var i = 0; i < Model.page_list.Count; i++)
        {
            if (time < Model.page_list[i].end_time) return i;
        }

        return Model.page_list.Count;
    }

    public int FindPageIndexByTick(float tick)
    {
        for (var i = 0; i < Model.page_list.Count; i++)
        {
            if (tick <= Model.page_list[i].end_tick) return i;
        }

        return Model.page_list.Count;
    }

    float GetNoteScreenYAtTick(ChartModel.Page page, float tick)
    {
        var t = GetPageProgressByTick(page, tick);
        return PageDisplayYToScreenY(PositionFunction.EvaluateDisplayY(page, t));
    }

    /// <summary>
    /// Legacy past-last-page extrapolation: continue from the end edge with flipped scan direction.
    /// </summary>
    float GetScreenYPastPageEnd(ChartModel.Page page, float chronologicalProgressPastStart)
    {
        var past = chronologicalProgressPastStart - 1f;
        return PageDisplayYToScreenY(
            PositionFunction.EvaluateDisplayY(page, past, -page.scan_line_direction));
    }

    float GetHoldScreenLength(ChartModel.Page page, float startTick, float endTick)
    {
        var t0 = GetPageProgressByTick(page, startTick);
        var t1 = GetPageProgressByTick(page, endTick);
        var y0 = PositionFunction.EvaluateDisplayY(page, t0);
        var y1 = PositionFunction.EvaluateDisplayY(page, t1);
        return verticalRatio * baseSize * Mathf.Abs(y1 - y0);
    }

    public float GetNoteScreenY(ChartModel.Note note)
    {
        return GetNoteScreenYAtTick(Model.page_list[note.page_index], (float) note.tick);
    }

    /// <summary>
    /// Legacy chart Y for Storyboard overrides (<see cref="ChartModel.Note.CalculatePosition"/>).
    /// Intentionally linear in tick progress; not PageFunction-aware.
    /// </summary>
    public float GetNoteChartY(ChartModel.Note note)
    {
        var page = Model.page_list[note.page_index];
        return (float) (page.scan_line_direction * ((note.tick - page.start_tick) * 1.0f /
                                                    (page.end_tick - page.start_tick)));
    }

    public float ConvertChartTickToScreenY(float tick)
    {
        var pageId = FindPageIndexByTick(tick);
        if (pageId == Model.page_list.Count)
            return GetScreenYPastPageEnd(Model.page_list[pageId - 1], GetPageProgressByTick(Model.page_list[pageId - 1], tick));

        return GetNoteScreenYAtTick(Model.page_list[pageId], tick);
    }

    public float GetScannerPositionY(float time, bool useScannerSmoothing)
    {
        var pageId = FindPageIndexByTime(time);
        if (pageId == Model.page_list.Count)
        {
            var page = Model.page_list[pageId - 1];
            var progress = useScannerSmoothing
                ? GetPageProgressByTick(page, ConvertToTick(time))
                : GetPageProgressByTime(page, time);
            return GetScreenYPastPageEnd(page, progress);
        }

        var current = Model.page_list[pageId];
        var pageProgress = useScannerSmoothing
            ? GetPageProgressByTick(current, ConvertToTick(time))
            : GetPageProgressByTime(current, time);
        return PageDisplayYToScreenY(PositionFunction.EvaluateDisplayY(current, pageProgress));
    }

    public float GetScanlinePosition01(float percentage)
    {
        return ConvertChartYToScreenY(percentage);
    }

    /// <summary>
    /// Full play-area top/bottom (always ±baseSize), independent of the per-page
    /// PositionFunction band.
    /// </summary>
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
                    pageDuration = NumberUtils.ParseFloat(data[1]);
                    break;
                case "PAGE_SHIFT":
                    pageShift = NumberUtils.ParseFloat(data[1]);
                    break;
                case "NOTE":
                    var note = new LegacyNote(NumberUtils.ParseInt(data[1]), NumberUtils.ParseFloat(data[2]), NumberUtils.ParseFloat(data[3]),
                        NumberUtils.ParseFloat(data[4]), false);
                    tmpNotes.Add(NumberUtils.ParseInt(data[1]), note);
                    if (note.Duration > 0) note.Type = LegacyNoteType.Hold;
                    break;
                case "LINK":
                    var notesInChain = new List<LegacyNote>();
                    for (var i = 1; i < data.Length; i++)
                    {
                        int id;
                        if (!NumberUtils.TryParseInt(data[i], out id)) continue;
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
