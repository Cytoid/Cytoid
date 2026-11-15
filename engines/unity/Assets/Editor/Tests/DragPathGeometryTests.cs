using NUnit.Framework;
using UnityEngine;

public class DragPathGeometryTests
{
    [TestCase(30)]
    [TestCase(60)]
    [TestCase(120)]
    public void RemainingLineStartsAtTheHeadForEveryFrameRate(int fps)
    {
        var from = new Vector3(.1f, .5f);
        var to = new Vector3(.9f, .5f);
        for (var frame = 0; frame <= fps; frame++)
        {
            var time = frame / (float) fps;
            var lineStart = DragPathGeometry.LineStart(new Vector3(.1f, 2f), from, to, time, 0, 1);
            Assert.That(lineStart.x, Is.EqualTo(.1f + .8f * time).Within(1e-6f));
            Assert.That(lineStart.y, Is.EqualTo(.5f));
        }
    }

    [Test]
    public void BeforeStartTheLineFollowsTheActualApproachingNode()
    {
        var visual = new Vector3(.1f, 1.4f);
        var start = DragPathGeometry.LineStart(visual, new Vector3(.1f, 0),
            new Vector3(.9f, 0), -.1f, 0, 1);
        Assert.That(start, Is.EqualTo(visual));
    }

    [TestCase(.75f, .5f, .5f)]
    [TestCase(1f, .5f, 1f)]
    [TestCase(.25f, .5f, 0f)]
    [TestCase(1f, 1f, 0f)]
    [TestCase(.25f, 0f, .25f)]
    public void RevealIsRemappedToTheRemainingSegment(float reveal, float progress, float expected)
        => Assert.That(DragPathGeometry.RemainingReveal(reveal, progress), Is.EqualTo(expected));

    [TestCase(1f, 1f)]
    [TestCase(2f, 1f)]
    public void NonIncreasingEdgesCompleteWithoutNaN(float fromTime, float toTime)
    {
        Assert.That(DragPathGeometry.Progress(fromTime - .1f, fromTime, toTime), Is.Zero);
        Assert.That(DragPathGeometry.Progress(fromTime, fromTime, toTime), Is.EqualTo(1));
        var at = DragPathGeometry.HeadPosition(Vector3.zero, Vector3.one, fromTime, fromTime, toTime);
        Assert.That(at.x, Is.EqualTo(1));
    }

    [Test]
    public void DirectionUsesTheActualSegmentAndThenHonorsExplicitRotation()
    {
        var ovr = new ChartModel.Note.NoteOverride();
        Assert.That(DragPathGeometry.Rotation(Vector3.zero, Vector3.right, ovr).z, Is.EqualTo(-90));
        ovr.RotX = 15; ovr.RotZ = 45;
        var rotation = DragPathGeometry.Rotation(Vector3.zero, Vector3.up, ovr);
        Assert.That(rotation.x, Is.EqualTo(15));
        Assert.That(rotation.z, Is.EqualTo(45));
    }
}
