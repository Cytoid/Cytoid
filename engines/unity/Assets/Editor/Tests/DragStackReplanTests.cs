using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Re-planning coverage for <see cref="Chart.ApplyDragStacks"/>: the initialize-time
/// tables must be fully replaceable so <see cref="Game.RebuildDragStacks"/> can react
/// to storyboard lifecycle changes (trigger spawn/destroy of note controllers).
/// The pure Game/ObjectPool migration is Play-Mode-only; see
/// CytoidDragStackReplanEditor for the editor demonstration.
/// </summary>
public class DragStackReplanTests
{
    [Test]
    public void ReplanRemovesStaleStackEntries()
    {
        var chart = NewChart();

        chart.ApplyDragStacks(null);
        Assert.That(chart.NoteIdToDragStackId.Count, Is.EqualTo(4));
        Assert.That(chart.MaxSamePageDragLineCount, Is.EqualTo(1));

        // Storyboard lifecycle changed: note 1 now runs a live note controller, so the
        // stale {1,2} entry must drop. The untouched {3,4} pair keeps its stack.
        chart.ApplyDragStacks(new Dictionary<int, string> {{1, "dx=0.1"}});

        Assert.That(chart.NoteIdToDragStackId.ContainsKey(1), Is.False);
        Assert.That(chart.NoteIdToDragStackId.ContainsKey(2), Is.False);
        Assert.That(chart.NoteIdToDragStackId[3], Is.EqualTo(chart.NoteIdToDragStackId[4]));
        // The 1->3 and 2->4 edges no longer share endpoints, so both need a line.
        Assert.That(chart.MaxSamePageDragLineCount, Is.EqualTo(2));
    }

    [Test]
    public void ReplanPicksUpNewlyStackableNotes()
    {
        var chart = NewChart();

        // Notes 1 and 2 are controller-touched at plan time, so only {3,4} stacks.
        chart.ApplyDragStacks(new Dictionary<int, string> {{1, "dx=0.1"}, {2, "dx=0.1"}});
        Assert.That(chart.NoteIdToDragStackId.Keys, Is.EquivalentTo(new[] {3, 4}));

        // Both controllers are destroyed later; the replan may stack the pair again.
        chart.ApplyDragStacks(null);

        Assert.That(chart.NoteIdToDragStackId.Count, Is.EqualTo(4));
        Assert.That(chart.NoteIdToDragStackId[1], Is.EqualTo(chart.NoteIdToDragStackId[2]));
        Assert.That(chart.MaxSamePageDragLineCount, Is.EqualTo(1));
    }

    [Test]
    public void ReplanStackIdsAreRenumberedFromScratch()
    {
        var chart = NewChart();

        chart.ApplyDragStacks(new Dictionary<int, string> {{1, "dx=0.1"}});
        Assert.That(chart.NoteIdToDragStackId.Keys, Is.EquivalentTo(new[] {3, 4}));

        // Full dissolve + rebuild after the controller goes away.
        chart.ApplyDragStacks(null);

        Assert.That(chart.NoteIdToDragStackId.Count, Is.EqualTo(4));
        Assert.That(chart.DragStackMembers.Count, Is.EqualTo(2));
        Assert.That(chart.DragStackMembers[chart.NoteIdToDragStackId[1]], Is.EqualTo(new List<int> {1, 2}));
        Assert.That(chart.DragStackMembers[chart.NoteIdToDragStackId[3]], Is.EqualTo(new List<int> {3, 4}));
    }

    static Chart NewChart()
    {
        const string json = @"
{
  ""time_base"": 480,
  ""tempo_list"": [{""tick"": 0, ""value"": 60000000}],
  ""page_list"": [{""start_tick"": 0, ""end_tick"": 960, ""scan_line_direction"": 1}],
  ""note_list"": [
    {""id"": 1, ""type"": 4, ""page_index"": 0, ""tick"": 240, ""x"": 0.5, ""next_id"": 3},
    {""id"": 2, ""type"": 4, ""page_index"": 0, ""tick"": 240, ""x"": 0.5, ""next_id"": 4},
    {""id"": 3, ""type"": 4, ""page_index"": 0, ""tick"": 480, ""x"": 0.5},
    {""id"": 4, ""type"": 4, ""page_index"": 0, ""tick"": 480, ""x"": 0.5}
  ],
  ""event_order_list"": [],
  ""display_boundaries"": true,
  ""display_background"": true,
  ""horizontal_margin"": 5,
  ""vertical_margin"": 3,
  ""restrict_play_area_aspect_ratio"": false,
  ""skip_music_on_completion"": false
}";
        return new Chart(json, false, false, true, false, 1f, 5f);
    }
}
