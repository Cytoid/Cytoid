using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

public class DropNoteChartInitializationTests
{
    [Test]
    public void DropNotesInitializeForBothApproachModes(
        [Values(NoteType.DropClick, NoteType.DropDrag)] NoteType type,
        [Values(false, true)] bool experimentalAr)
    {
        var chart = Load(type, experimentalAr: experimentalAr);
        var note = chart.Model.note_list[0];

        Assert.That(chart.Model.note_map[0], Is.SameAs(note));
        Assert.That(chart.MaxSamePageNoteCountByType[type], Is.EqualTo(1));
        Assert.That(note.start_time, Is.EqualTo(3.5f).Within(0.0001f));
        Assert.That(note.end_time, Is.EqualTo(note.start_time));
    }

    [Test]
    public void DropIntroUsesFivePageTickSpansAcrossTempoChanges(
        [Values(NoteType.DropClick, NoteType.DropDrag)] NoteType type,
        [Values(false, true)] bool experimentalAr,
        [Values(0.75f, 1f, 1.5f)] float approachRate)
    {
        var note = Load(type, experimentalAr: experimentalAr, approachRate: approachRate)
            .Model.note_list[0];

        // Hit tick 2640 is at 3.5s; five 480-tick pages earlier is tick 240 at 0.25s.
        // Subtracting five times the hit page's duration would incorrectly yield -1.5s.
        Assert.That(note.intro_time, Is.EqualTo(0.25f).Within(0.0001f));
    }

    [TestCase(NoteType.DropClick)]
    [TestCase(NoteType.DropDrag)]
    public void DropIntroCanPrecedeChartStart(NoteType type)
    {
        var note = Load(type, pageIndex: 0, tick: 240).Model.note_list[0];

        Assert.That(note.start_time, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(note.intro_time, Is.EqualTo(-2.25f).Within(0.0001f));
    }

    [Test]
    public void VerticalFlipMirrorsDropDirectionAndAlternativeColor(
        [Values(NoteType.DropClick, NoteType.DropDrag)] NoteType type,
        [Values(0, 1)] int direction,
        [Values(false, true)] bool flip)
    {
        var note = Load(type, direction: direction, flip: flip).Model.note_list[0];
        var expectedDirection = flip ? 1 - direction : direction;

        Assert.That(note.NoteDirection, Is.EqualTo(expectedDirection));
        Assert.That(note.UseAlternativeColor(), Is.EqualTo(expectedDirection == 1));
    }

    [TestCase(NoteType.DropClick, 1, 0.94f)]
    [TestCase(NoteType.DropClick, -1, 1.06f)]
    [TestCase(NoteType.DropDrag, 1, 0.94f)]
    [TestCase(NoteType.DropDrag, -1, 1.06f)]
    public void DropTintFollowsScanDirection(NoteType type, int scanDirection, float expectedTint)
    {
        var note = Load(type, scanDirection: scanDirection).Model.note_list[0];

        Assert.That(note.tint, Is.EqualTo(expectedTint).Within(0.0001f));
    }

    [TestCase(NoteType.Click)]
    [TestCase(NoteType.Hold)]
    [TestCase(NoteType.LongHold)]
    [TestCase(NoteType.DragHead)]
    [TestCase(NoteType.DragChild)]
    [TestCase(NoteType.Flick)]
    [TestCase(NoteType.CDragHead)]
    [TestCase(NoteType.CDragChild)]
    public void VerticalFlipKeepsOtherNoteDirections(NoteType type)
    {
        var note = Load(type, flip: true).Model.note_list[0];

        Assert.That(note.NoteDirection, Is.EqualTo(0));
    }

    // Same configuration Context assigns to JsonConvert.DefaultSettings at startup.
    private static readonly JsonSerializerSettings RuntimeJsonSettings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> {new UnityColorConverter()},
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    private static Chart Load(
        NoteType type, bool experimentalAr = false, float approachRate = 1f,
        int direction = 0, bool flip = false, int pageIndex = 5, double tick = 2640,
        int scanDirection = -1)
    {
        // Supply all setting-backed fields so these constructor tests need no initialized player.
        var model = new ChartModel
        {
            time_base = 480,
            display_boundaries = false,
            horizontal_margin = 2,
            vertical_margin = 2,
            restrict_play_area_aspect_ratio = false,
            skip_music_on_completion = false,
            tempo_list = new List<ChartModel.Tempo>
            {
                new ChartModel.Tempo {tick = 0, value = 500000},
                new ChartModel.Tempo {tick = 1920, value = 1000000}
            },
            note_list = new List<ChartModel.Note>
            {
                new ChartModel.Note
                {
                    id = 0, type = (int) type, page_index = pageIndex,
                    tick = tick, x = 0.5, next_id = -1, NoteDirection = direction
                }
            }
        };
        for (var i = 0; i < 8; i++)
        {
            model.page_list.Add(new ChartModel.Page
            {
                start_tick = i * 480, end_tick = (i + 1) * 480,
                scan_line_direction = scanDirection
            });
        }

        // Context assigns JsonConvert.DefaultSettings during runtime startup, which
        // EditMode tests never execute. Mirror that production configuration here:
        // without ReferenceLoopHandling.Ignore, serializing ChartModel's Vector3
        // fields throws on the self-referencing getter-only `normalized` property.
        return new Chart(JsonConvert.SerializeObject(model, RuntimeJsonSettings), false, flip, true,
            experimentalAr, approachRate, 5f);
    }
}
