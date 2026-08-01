using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ChartEventPresentationTimelineTests
{
    [Test]
    public void MessageParsesContentAndRgbColor()
    {
        var timeline = Timeline(Order(0, Event(ChartEventType.Message, "Hello,#0A141E")));

        var state = timeline.Evaluate(1);

        Assert.That(state.Kind, Is.EqualTo(ChartEventPresentationKind.Message));
        Assert.That(state.Content, Is.EqualTo("Hello"));
        AssertColor(state.TextColor, new Color32(10, 20, 30, 255));
        AssertColor(state.ScanlineColor, new Color32(10, 20, 30, 255));
    }

    [Test]
    public void InvalidOrMissingColorWarnsAndFallsBackToWhite()
    {
        var warnings = new List<string>();
        var timeline = new ChartEventPresentationTimeline(
            Model(Order(0, Event(ChartEventType.Message, "Hello,not-a-color"))),
            warnings.Add);

        Assert.That(warnings, Has.Count.EqualTo(1));
        AssertColor(timeline.Evaluate(1).ScanlineColor, Color.white);
    }

    [Test]
    public void EmptyMessageStillColorsScanlineWithoutShowingText()
    {
        var state = Timeline(Order(0, Event(ChartEventType.Message, ",#FF0000"))).Evaluate(1);

        Assert.That(state.IsActive, Is.True);
        Assert.That(state.IsTextVisible, Is.False);
        AssertColor(state.ScanlineColor, Color.red);
    }

    [Test]
    public void SamplesCytoidTextAnimationAndFiveSecondColorCurve()
    {
        var timeline = Timeline(Order(10, Event(ChartEventType.Message, "Hello,#000000")));

        Assert.That(timeline.Evaluate(10).TextAlpha, Is.EqualTo(0).Within(0.0001f));
        AssertColor(timeline.Evaluate(10).TextColor, Color.black);
        AssertColor(timeline.Evaluate(10).ScanlineColor, Color.white);
        Assert.That(timeline.Evaluate(10.2f).TextAlpha, Is.EqualTo(0.875f).Within(0.001f));
        Assert.That(timeline.Evaluate(10.4f).TextAlpha, Is.EqualTo(1).Within(0.0001f));
        Assert.That(timeline.Evaluate(11).TextAlpha, Is.EqualTo(1).Within(0.0001f));
        Assert.That(timeline.Evaluate(11.3f).TextAlpha, Is.EqualTo(0.125f).Within(0.001f));
        Assert.That(timeline.Evaluate(11.5f).TextAlpha, Is.EqualTo(0).Within(0.0001f));
        Assert.That(timeline.Evaluate(11.5f).LetterSpacing,
            Is.EqualTo(ChartEventPresentationTimeline.MaxLetterSpacing).Within(0.0001f));
        AssertColor(timeline.Evaluate(10.5f).ScanlineColor, Color.gray);
        AssertColor(timeline.Evaluate(12).ScanlineColor, Color.black);
        AssertColor(timeline.Evaluate(14.5f).ScanlineColor, Color.gray);
        Assert.That(timeline.Evaluate(15).IsActive, Is.False);
        AssertColor(timeline.Evaluate(15).ScanlineColor, Color.white);
    }

    [Test]
    public void MessageUsesIndependentInQuintSpacingWhileSpeedEventsKeepOutCirc()
    {
        var message = Timeline(Order(0, Event(ChartEventType.Message, "Custom,#FFFFFF")));
        var speedUp = Timeline(Order(0, Event(ChartEventType.SpeedUp, "R")));
        var speedDown = Timeline(Order(0, Event(ChartEventType.SpeedDown, "G")));

        Assert.That(message.Evaluate(0).LetterSpacing, Is.EqualTo(0).Within(0.0001f));
        Assert.That(speedUp.Evaluate(0).LetterSpacing, Is.EqualTo(0).Within(0.0001f));
        Assert.That(speedDown.Evaluate(0).LetterSpacing,
            Is.EqualTo(ChartEventPresentationTimeline.MaxLetterSpacing).Within(0.0001f));

        Assert.That(message.Evaluate(0.75f).LetterSpacing,
            Is.EqualTo(ChartEventPresentationTimeline.MaxLetterSpacing / 32).Within(0.0001f));
        Assert.That(message.Evaluate(0.75f).LetterSpacing,
            Is.LessThan(speedUp.Evaluate(0.75f).LetterSpacing));
        Assert.That(speedDown.Evaluate(0.75f).LetterSpacing,
            Is.EqualTo(ChartEventPresentationTimeline.MaxLetterSpacing -
                       speedUp.Evaluate(0.75f).LetterSpacing).Within(0.0001f));
    }

    [Test]
    public void InterruptedEventStartsFromPreviouslySampledColor()
    {
        var timeline = Timeline(
            Order(0, Event(ChartEventType.Message, "First,#FF0000")),
            Order(0.5f, Event(ChartEventType.Message, "Second,#0000FF")));

        var atInterruption = timeline.Evaluate(0.5f);
        Assert.That(atInterruption.Content, Is.EqualTo("Second"));
        AssertColor(atInterruption.ScanlineColor, new Color(1, 0.5f, 0.5f));
        AssertColor(timeline.Evaluate(1).ScanlineColor, new Color(0.5f, 0.25f, 0.75f));
    }

    [Test]
    public void SameTimeLastAuthoredEventWinsWithoutResettingTransition()
    {
        var timeline = Timeline(Order(
            0,
            Event(ChartEventType.Message, "First,#FF0000"),
            Event(ChartEventType.Message, "Second,#0000FF")));

        var state = timeline.Evaluate(0);
        Assert.That(state.Content, Is.EqualTo("Second"));
        AssertColor(state.ScanlineColor, Color.white);
        AssertColor(timeline.Evaluate(1).ScanlineColor, Color.blue);
    }

    [Test]
    public void SpeedAndMessageEventsShareOneInterruptibleTrack()
    {
        var timeline = Timeline(
            Order(0, Event(ChartEventType.SpeedUp, "R")),
            Order(0.25f, Event(ChartEventType.SpeedDown, "G")),
            Order(0.5f, Event(ChartEventType.Message, "Custom,#FFFFFF")));

        Assert.That(timeline.Evaluate(0).Kind, Is.EqualTo(ChartEventPresentationKind.SpeedUp));
        Assert.That(timeline.Evaluate(0.25f).Kind, Is.EqualTo(ChartEventPresentationKind.SpeedDown));
        Assert.That(timeline.Evaluate(0.5f).Kind, Is.EqualTo(ChartEventPresentationKind.Message));
    }

    [Test]
    public void BackwardSeekResamplesTheEarlierEvent()
    {
        var timeline = Timeline(
            Order(1, Event(ChartEventType.Message, "First,#FF0000")),
            Order(3, Event(ChartEventType.Message, "Second,#0000FF")));

        Assert.That(timeline.Evaluate(3).Content, Is.EqualTo("Second"));
        Assert.That(timeline.Evaluate(1.5f).Content, Is.EqualTo("First"));
        Assert.That(timeline.Evaluate(0).IsActive, Is.False);
    }

    private static ChartEventPresentationTimeline Timeline(params ChartModel.EventOrder[] orders) =>
        new ChartEventPresentationTimeline(Model(orders));

    private static ChartModel Model(params ChartModel.EventOrder[] orders) => new ChartModel
    {
        event_order_list = new List<ChartModel.EventOrder>(orders)
    };

    private static ChartModel.EventOrder Order(float time, params ChartModel.ChartEvent[] events) =>
        new ChartModel.EventOrder
        {
            time = time,
            event_list = new List<ChartModel.ChartEvent>(events)
        };

    private static ChartModel.ChartEvent Event(ChartEventType type, string args) =>
        new ChartModel.ChartEvent {type = (int) type, args = args};

    private static void AssertColor(Color actual, Color expected)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
    }
}
