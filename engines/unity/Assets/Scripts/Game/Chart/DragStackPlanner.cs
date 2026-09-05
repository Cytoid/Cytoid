using System;
using System.Collections.Generic;
using System.Text;
using Cytoid.Storyboard;

/// <summary>
/// Builds visual/collider drag stacks after chart load and storyboard parse.
///
/// Eligibility (all must hold; otherwise the note keeps the per-note path):
/// <list type="number">
/// <item>Type is <see cref="NoteType.DragChild"/> or <see cref="NoteType.CDragChild"/>.
/// Heads travel along their own chain and cannot share a host.</item>
/// <item>Full visual/collision equivalence: type, start time, intro time, chart x, approach rate,
/// size, opacity, hitbox, colors, style, page index, scan direction, and is_forward.</item>
/// <item>No storyboard note controller. A non-empty signature (parsed controller,
/// including trigger-spawned) keeps the per-note path so a later destroy/spawn
/// cannot desync a shared host. Chart ids are unchanged either way.</item>
/// <item>Outgoing chain destinations match: every member has no next, or every next
/// note shares the same visual/storyboard key. Origins whose next notes diverge
/// stay independent so shared rotation cannot point a line at the wrong child.</item>
/// <item>At least two notes share the same key.</item>
/// </list>
/// Drag-line sharing uses stack identity of both endpoints, not static chart x/time,
/// so independently storyboard-moved endpoints never share a line.
/// </summary>
public sealed class DragStackPlan
{
    public Dictionary<int, int> NoteIdToStackId { get; } = new Dictionary<int, int>();
    public Dictionary<int, List<int>> StackMembers { get; } = new Dictionary<int, List<int>>();
    public int MaxSamePageDragStackHostCount { get; set; }
    public int MaxSamePageDragLineCount { get; set; }
}

public static class DragStackPlanner
{
    public const string UncontrolledSignature = "";
    public const string TriggerSignaturePrefix = "trigger:";

    public static DragStackPlan Build(
        ChartModel model,
        IReadOnlyDictionary<int, string> storyboardSignatures = null)
    {
        var plan = new DragStackPlan();
        if (model?.note_list == null || model.note_list.Count == 0) return plan;

        var pageCount = model.page_list != null ? model.page_list.Count : 0;
        var buckets = new Dictionary<EquivalenceKey, List<int>>();

        foreach (var note in model.note_list)
        {
            if (note == null) continue;
            var type = (NoteType) note.type;
            if (type != NoteType.DragChild && type != NoteType.CDragChild) continue;

            string signature;
            if (storyboardSignatures == null ||
                !storyboardSignatures.TryGetValue(note.id, out signature) ||
                signature == null)
            {
                signature = UncontrolledSignature;
            }

            // Any NoteController contact opts out. Identical controllers used to stack;
            // a later trigger destroy of one controller would freeze the primary.
            if (signature != UncontrolledSignature) continue;

            var key = EquivalenceKey.From(note, signature);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = new List<int>();
                buckets[key] = list;
            }

            list.Add(note.id);
        }

        var nextStackId = 1;
        var pageHostCounts = pageCount > 0 ? new int[pageCount] : Array.Empty<int>();
        foreach (var pair in buckets)
        {
            var ids = pair.Value;
            if (ids.Count < 2) continue;
            if (!NextDestinationsMatch(ids, model, storyboardSignatures)) continue;

            ids.Sort();
            var stackId = nextStackId++;
            plan.StackMembers[stackId] = ids;
            var first = model.note_map != null && model.note_map.TryGetValue(ids[0], out var firstNote)
                ? firstNote
                : FindNote(model, ids[0]);
            var pageIndex = first != null ? first.page_index : 0;
            if (pageIndex >= 0 && pageIndex < pageHostCounts.Length)
            {
                pageHostCounts[pageIndex]++;
            }

            foreach (var id in ids)
            {
                plan.NoteIdToStackId[id] = stackId;
            }
        }

        plan.MaxSamePageDragStackHostCount = pageHostCounts.Length > 0 ? Max(pageHostCounts) : 0;
        plan.MaxSamePageDragLineCount = CountMaxSamePageDragLines(model, plan.NoteIdToStackId, pageCount);
        return plan;
    }

    /// <summary>
    /// Share key for coincident drag-line geometry. Uses stack ids when both
    /// endpoints are stacked; otherwise the concrete note id so storyboard-divergent
    /// endpoints never collapse onto one <see cref="DragLineElement"/>.
    /// </summary>
    public static long MakeDragLineShareKey(
        ChartModel.Note from,
        ChartModel.Note to,
        IReadOnlyDictionary<int, int> noteIdToStackId)
    {
        if (from == null || to == null) return 0;
        unchecked
        {
            long hash = 17;
            hash = hash * 31 + from.type;
            hash = hash * 31 + to.type;
            hash = hash * 31 + EndpointIdentity(from.id, noteIdToStackId);
            hash = hash * 31 + EndpointIdentity(to.id, noteIdToStackId);
            return hash;
        }
    }

    public static Dictionary<int, string> SignaturesFromNoteControllers(
        IEnumerable<NoteController> controllers)
    {
        var result = new Dictionary<int, string>();
        if (controllers == null) return result;

        var byNote = new Dictionary<int, List<NoteController>>();
        foreach (var controller in controllers)
        {
            if (controller == null) continue;
            var noteId = ResolveNoteId(controller);
            if (noteId == null) continue;
            if (!byNote.TryGetValue(noteId.Value, out var list))
            {
                list = new List<NoteController>();
                byNote[noteId.Value] = list;
            }

            list.Add(controller);
        }

        foreach (var pair in byNote)
        {
            var noteId = pair.Key;
            var list = pair.Value;
            var triggerControlled = false;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].IsManuallySpawned())
                {
                    triggerControlled = true;
                    break;
                }
            }

            if (triggerControlled)
            {
                result[noteId] = TriggerSignaturePrefix + noteId;
                continue;
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var builder = new StringBuilder();
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0) builder.Append("||");
                AppendControllerFingerprint(builder, list[i]);
            }

            result[noteId] = builder.ToString();
        }

        return result;
    }

    static int EndpointIdentity(int noteId, IReadOnlyDictionary<int, int> noteIdToStackId)
    {
        if (noteIdToStackId != null && noteIdToStackId.TryGetValue(noteId, out var stackId) && stackId > 0)
        {
            return stackId;
        }

        // Unstacked notes keep a distinct identity in the negative range.
        return -noteId - 1;
    }

    static int CountMaxSamePageDragLines(
        ChartModel model,
        IReadOnlyDictionary<int, int> noteIdToStackId,
        int pageCount)
    {
        if (pageCount <= 0) return 0;
        var perPage = new HashSet<long>[pageCount];
        for (var i = 0; i < pageCount; i++) perPage[i] = new HashSet<long>();

        foreach (var note in model.note_list)
        {
            if (note == null || note.next_id <= 0) continue;
            var type = (NoteType) note.type;
            if (type != NoteType.DragHead && type != NoteType.DragChild &&
                type != NoteType.CDragHead && type != NoteType.CDragChild)
            {
                continue;
            }

            if (model.note_map == null || !model.note_map.TryGetValue(note.next_id, out var to) || to == null)
            {
                continue;
            }

            var pageIndex = note.page_index;
            if (pageIndex < 0 || pageIndex >= pageCount) continue;
            perPage[pageIndex].Add(MakeDragLineShareKey(note, to, noteIdToStackId));
        }

        var max = 0;
        for (var i = 0; i < pageCount; i++)
        {
            if (perPage[i].Count > max) max = perPage[i].Count;
        }

        return max;
    }

    static bool NextDestinationsMatch(
        List<int> ids,
        ChartModel model,
        IReadOnlyDictionary<int, string> storyboardSignatures)
    {
        var haveFirst = false;
        var firstIsNone = false;
        var firstKey = default(EquivalenceKey);

        for (var i = 0; i < ids.Count; i++)
        {
            var note = LookupNote(model, ids[i]);
            ChartModel.Note next = null;
            var isNone = note == null || note.next_id <= 0 ||
                         (next = LookupNote(model, note.next_id)) == null;

            if (!haveFirst)
            {
                firstIsNone = isNone;
                if (!isNone) firstKey = EquivalenceKey.From(next, SignatureOf(next.id, storyboardSignatures));
                haveFirst = true;
                continue;
            }

            if (isNone != firstIsNone) return false;
            if (isNone) continue;
            if (!firstKey.Equals(EquivalenceKey.From(next, SignatureOf(next.id, storyboardSignatures))))
            {
                return false;
            }
        }

        return true;
    }

    static string SignatureOf(int noteId, IReadOnlyDictionary<int, string> storyboardSignatures)
    {
        if (storyboardSignatures == null ||
            !storyboardSignatures.TryGetValue(noteId, out var signature) ||
            signature == null)
        {
            return UncontrolledSignature;
        }

        return signature;
    }

    static ChartModel.Note LookupNote(ChartModel model, int id)
    {
        if (model.note_map != null && model.note_map.TryGetValue(id, out var mapped) && mapped != null)
        {
            return mapped;
        }

        return FindNote(model, id);
    }

    static ChartModel.Note FindNote(ChartModel model, int id)
    {
        for (var i = 0; i < model.note_list.Count; i++)
        {
            if (model.note_list[i] != null && model.note_list[i].id == id) return model.note_list[i];
        }

        return null;
    }

    static int Max(int[] values)
    {
        var max = 0;
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] > max) max = values[i];
        }

        return max;
    }

    static int? ResolveNoteId(NoteController controller)
    {
        if (controller.States == null) return null;
        for (var i = 0; i < controller.States.Count; i++)
        {
            var note = controller.States[i]?.Note;
            if (note != null) return note;
        }

        return null;
    }

    static void AppendControllerFingerprint(StringBuilder builder, NoteController controller)
    {
        var states = controller.States;
        if (states == null) return;
        for (var i = 0; i < states.Count; i++)
        {
            if (i > 0) builder.Append(';');
            AppendStateFingerprint(builder, states[i]);
        }
    }

    static void AppendStateFingerprint(StringBuilder builder, NoteControllerState state)
    {
        if (state == null)
        {
            builder.Append("null");
            return;
        }

        builder.Append(state.Time.ToString("R"));
        builder.Append('|').Append(Flag(state.OverrideX)).Append(Unit(state.X));
        builder.Append('|').Append(Num(state.XMultiplier)).Append(',').Append(Num(state.XOffset));
        builder.Append('|').Append(Flag(state.OverrideY)).Append(Unit(state.Y));
        builder.Append('|').Append(Num(state.YMultiplier)).Append(',').Append(Num(state.YOffset));
        builder.Append('|').Append(Flag(state.OverrideZ)).Append(Unit(state.Z));
        builder.Append('|').Append(Flag(state.OverrideRotX)).Append(Num(state.RotX));
        builder.Append('|').Append(Flag(state.OverrideRotY)).Append(Num(state.RotY));
        builder.Append('|').Append(Flag(state.OverrideRotZ)).Append(Num(state.RotZ));
        builder.Append('|').Append(Flag(state.OverrideRingColor)).Append(Color(state.RingColor));
        builder.Append('|').Append(Flag(state.OverrideFillColor)).Append(Color(state.FillColor));
        builder.Append('|').Append(Num(state.OpacityMultiplier));
        builder.Append('|').Append(Num(state.SizeMultiplier));
        builder.Append('|').Append(Num(state.HitboxMultiplier));
        builder.Append('|').Append(state.HoldDirection);
        builder.Append('|').Append(state.Style);
        builder.Append('|').Append(state.Easing);
        builder.Append('|').Append(Flag(state.Destroy));
    }

    static string Flag(bool? value) => value == null ? "_" : (value.Value ? "1" : "0");

    static string Num(float? value) => value == null ? "_" : value.Value.ToString("R");

    static string Unit(UnitFloat value)
    {
        if (value == null) return "_";
        return value.Value.ToString("R") + ":" + (int) value.Unit + ":" + (value.ScaleToCanvas ? 1 : 0) + ":" +
               (value.Span ? 1 : 0);
    }

    static string Color(Cytoid.Storyboard.Color value)
    {
        if (value == null) return "_";
        return value.R.ToString("R") + "," + value.G.ToString("R") + "," + value.B.ToString("R") + "," +
               value.A.ToString("R");
    }

    readonly struct EquivalenceKey : IEquatable<EquivalenceKey>
    {
        public readonly NoteType Type;
        public readonly int StartTimeMs;
        public readonly int IntroTimeMs;
        public readonly int Xq;
        public readonly int ApproachRateQ;
        public readonly int SizeQ;
        public readonly int OpacityQ;
        public readonly int HitboxQ;
        public readonly int Style;
        public readonly int Direction;
        public readonly int PageIndex;
        public readonly bool IsForward;
        public readonly string RingColor;
        public readonly string FillColor;
        public readonly string StoryboardSignature;

        EquivalenceKey(
            NoteType type,
            int startTimeMs,
            int introTimeMs,
            int xq,
            int approachRateQ,
            int sizeQ,
            int opacityQ,
            int hitboxQ,
            int style,
            int direction,
            int pageIndex,
            bool isForward,
            string ringColor,
            string fillColor,
            string storyboardSignature)
        {
            Type = type;
            StartTimeMs = startTimeMs;
            IntroTimeMs = introTimeMs;
            Xq = xq;
            ApproachRateQ = approachRateQ;
            SizeQ = sizeQ;
            OpacityQ = opacityQ;
            HitboxQ = hitboxQ;
            Style = style;
            Direction = direction;
            PageIndex = pageIndex;
            IsForward = isForward;
            RingColor = ringColor ?? "";
            FillColor = fillColor ?? "";
            StoryboardSignature = storyboardSignature ?? UncontrolledSignature;
        }

        public static EquivalenceKey From(ChartModel.Note note, string signature)
        {
            return new EquivalenceKey(
                (NoteType) note.type,
                (int) Math.Round(note.start_time * 1000.0),
                (int) Math.Round(note.intro_time * 1000.0),
                (int) Math.Round(note.x * 10000.0),
                Quantize(note.approach_rate),
                Quantize(note.size),
                Quantize(note.opacity),
                Quantize(note.hitbox),
                note.style,
                note.direction,
                note.page_index,
                note.is_forward,
                note.ring_color,
                note.fill_color,
                signature);
        }

        static int Quantize(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return int.MinValue;
            return (int) Math.Round(value * 10000.0);
        }

        public bool Equals(EquivalenceKey other)
        {
            return Type == other.Type &&
                   StartTimeMs == other.StartTimeMs &&
                   IntroTimeMs == other.IntroTimeMs &&
                   Xq == other.Xq &&
                   ApproachRateQ == other.ApproachRateQ &&
                   SizeQ == other.SizeQ &&
                   OpacityQ == other.OpacityQ &&
                   HitboxQ == other.HitboxQ &&
                   Style == other.Style &&
                   Direction == other.Direction &&
                   PageIndex == other.PageIndex &&
                   IsForward == other.IsForward &&
                   RingColor == other.RingColor &&
                   FillColor == other.FillColor &&
                   StoryboardSignature == other.StoryboardSignature;
        }

        public override bool Equals(object obj) => obj is EquivalenceKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int) Type;
                hash = (hash * 397) ^ StartTimeMs;
                hash = (hash * 397) ^ IntroTimeMs;
                hash = (hash * 397) ^ Xq;
                hash = (hash * 397) ^ ApproachRateQ;
                hash = (hash * 397) ^ SizeQ;
                hash = (hash * 397) ^ OpacityQ;
                hash = (hash * 397) ^ HitboxQ;
                hash = (hash * 397) ^ Style;
                hash = (hash * 397) ^ Direction;
                hash = (hash * 397) ^ PageIndex;
                hash = (hash * 397) ^ (IsForward ? 1 : 0);
                hash = (hash * 397) ^ (RingColor != null ? RingColor.GetHashCode() : 0);
                hash = (hash * 397) ^ (FillColor != null ? FillColor.GetHashCode() : 0);
                hash = (hash * 397) ^ (StoryboardSignature != null ? StoryboardSignature.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
