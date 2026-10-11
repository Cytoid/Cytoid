using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class GamePlayRecordTests
{
    [Test]
    public void Codec_RoundTripsMetadataAndExactEvents()
    {
        var snapshot = CreateSnapshot(
            new GamePlayTouchEvent {s = 0, t = -1000, p = "down", finger = 3, x = -12, y = 34},
            new GamePlayControlEvent {s = 1, t = 2500, p = "pause"},
            new GamePlayControlEvent {s = 2, t = 2500, p = "resume"});

        var lines = DecodeLines(GamePlayRecordCodec.EncodeGzip(snapshot, 1892, 21195107));
        var metadata = JObject.Parse(lines[0]);

        Assert.That((string) metadata["format"], Is.EqualTo("cytoid.play-record"));
        Assert.That((int) metadata["version"], Is.EqualTo(2));
        Assert.That((long) metadata["recordId"], Is.EqualTo(21195107));
        Assert.That((long) metadata["chartId"], Is.EqualTo(1892));
        Assert.That((int) metadata["eventCount"], Is.EqualTo(3));
        Assert.That((string) metadata["coordinates"]["space"], Is.EqualTo("judgement_world"));
        Assert.That(lines[1], Is.EqualTo("{\"s\":0,\"t\":-1000,\"p\":\"down\",\"finger\":3,\"x\":-12,\"y\":34}"));
        Assert.That(lines[2], Is.EqualTo("{\"s\":1,\"t\":2500,\"p\":\"pause\"}"));
        Assert.That(lines[3], Is.EqualTo("{\"s\":2,\"t\":2500,\"p\":\"resume\"}"));
    }

    [Test]
    public void Codec_GoldenJudgeAndArmShapes()
    {
        var snapshot = CreateSnapshot(
            new GamePlayArmEvent {s = 5, t = 5962658, p = "arm", note = 15, finger = 0, input = 100, hasInput = true},
            new GamePlayJudgeEvent
            {
                s = 6, t = 6312706, p = "judge", note = 17, finger = 1, grade = "perfect",
                cause = "press", input = 99, hasInput = true
            },
            new GamePlayJudgeEvent
            {
                s = 7, t = 6400000, p = "judge", note = 18, finger = null, grade = "miss", cause = "trigger_tick"
            });

        var lines = DecodeLines(GamePlayRecordCodec.EncodeGzip(snapshot, 1892, 21195107));

        Assert.That(lines[1], Is.EqualTo(
            "{\"s\":5,\"t\":5962658,\"p\":\"arm\",\"note\":15,\"finger\":0,\"input\":100}"));
        Assert.That(lines[2], Is.EqualTo(
            "{\"s\":6,\"t\":6312706,\"p\":\"judge\",\"note\":17,\"finger\":1,\"grade\":\"perfect\",\"cause\":\"press\",\"input\":99}"));
        Assert.That(lines[3], Is.EqualTo(
            "{\"s\":7,\"t\":6400000,\"p\":\"judge\",\"note\":18,\"finger\":null,\"grade\":\"miss\",\"cause\":\"trigger_tick\"}"));
    }

    [Test]
    public void GradeMapping_CoversEveryEnumValueAndFailsClosed()
    {
        Assert.That(GamePlayRecordWire.ToWireGrade(NoteGrade.Miss), Is.EqualTo("miss"));
        Assert.That(GamePlayRecordWire.ToWireGrade(NoteGrade.Bad), Is.EqualTo("bad"));
        Assert.That(GamePlayRecordWire.ToWireGrade(NoteGrade.Good), Is.EqualTo("good"));
        Assert.That(GamePlayRecordWire.ToWireGrade(NoteGrade.Great), Is.EqualTo("great"));
        Assert.That(GamePlayRecordWire.ToWireGrade(NoteGrade.Perfect), Is.EqualTo("perfect"));
        Assert.That(GamePlayRecordWire.ToWireGrade(NoteGrade.None), Is.EqualTo("none"));
        Assert.That(GamePlayRecordWire.ToWireGrade((NoteGrade) 99), Is.EqualTo("unknown"));
    }

    [Test]
    public void RecorderPolicy_IsOptInAndStandardOrPracticeOnly()
    {
        Assert.That(GamePlayRecordRecorder.ShouldCollect(false, GameMode.Standard), Is.False);
        Assert.That(GamePlayRecordRecorder.ShouldCollect(true, GameMode.Standard), Is.True);
        Assert.That(GamePlayRecordRecorder.ShouldCollect(true, GameMode.Practice), Is.True);
        Assert.That(GamePlayRecordRecorder.ShouldCollect(true, GameMode.Tier), Is.False);
        Assert.That(GamePlayRecordRecorder.ShouldCollect(true, GameMode.Calibration), Is.False);
        Assert.That(GamePlayRecordRecorder.ShouldCollect(true, GameMode.GlobalCalibration), Is.False);
    }

    private static GamePlayRecordSnapshot CreateSnapshot(params GamePlayRecordEvent[] events)
    {
        return new GamePlayRecordSnapshot
        {
            ContentHash = "hash",
            ClientVersion = "test",
            UnityVersion = "test",
            Mode = "standard",
            Mods = Array.Empty<string>(),
            TimeBase = 480,
            HitboxSettings = new Dictionary<string, int>(),
            HitboxSizes = new Dictionary<string, int>(),
            Events = events
        };
    }

    private static string[] DecodeLines(byte[] payload)
    {
        using (var input = new MemoryStream(payload))
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        using (var reader = new StreamReader(gzip, Encoding.UTF8))
        {
            return reader.ReadToEnd().TrimEnd('\n').Split('\n');
        }
    }
}
