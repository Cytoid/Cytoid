using System.Collections.Generic;
using NUnit.Framework;

public class ChartUiEventTimelineTests
{
    [Test]
    public void MissingModelOrEventListPreservesInitializedTargetAlpha()
    {
        var missingModel = new ChartUiEventTimeline(null);
        var missingEvents = new ChartUiEventTimeline(new ChartModel
        {
            is_start_without_ui = true,
            event_order_list = null
        });

        Assert.That(missingModel.Evaluate(ChartUiTarget.Combo, 0).Alpha, Is.EqualTo(1));
        Assert.That(missingEvents.Evaluate(ChartUiTarget.Combo, 0).Alpha, Is.EqualTo(0));
    }

    [Test]
    public void InitialVisibilityAndCytoidSideTargetsAreResolvedIndependently()
    {
        var model = Model(true,
            Order(1, Event(ChartEventType.ShowUi, "2")),
            Order(2, Event(ChartEventType.ShowUi, "3")));
        var timeline = new ChartUiEventTimeline(model);

        Assert.That(timeline.Evaluate(ChartUiTarget.LeftUi, 0).Alpha, Is.EqualTo(0));
        Assert.That(timeline.Evaluate(ChartUiTarget.LeftUi, 1).Alpha, Is.EqualTo(1));
        Assert.That(timeline.Evaluate(ChartUiTarget.RightUi, 1).Alpha, Is.EqualTo(0));
        Assert.That(timeline.Evaluate(ChartUiTarget.RightUi, 2).Alpha, Is.EqualTo(1));
    }

    [Test]
    public void TickZeroEventsDoNotChangeThePreSongState()
    {
        var model = Model(true, Order(0, Event(ChartEventType.ShowUi, "0")));
        var timeline = new ChartUiEventTimeline(model);

        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, -0.001f).Alpha, Is.EqualTo(0));
        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, 0).Alpha, Is.EqualTo(1));
    }

    [Test]
    public void FadeInterruptionStartsFromTheCurrentCylheimAlpha()
    {
        var model = Model(false,
            Order(0, Event(ChartEventType.FadeOutUi, "0")),
            Order(0.65f, Event(ChartEventType.FadeInUi, "0")));
        var timeline = new ChartUiEventTimeline(model);

        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, 0.65f).Alpha, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, 1.3f).Alpha, Is.EqualTo(0.75f).Within(0.0001f));
    }

    [Test]
    public void SameTimeEventsUseTheirOriginalSequence()
    {
        var model = Model(false,
            Order(1,
                Event(ChartEventType.HideUi, "1"),
                Event(ChartEventType.ShowUi, "1")));
        var timeline = new ChartUiEventTimeline(model);

        Assert.That(timeline.Evaluate(ChartUiTarget.Score, 1).Alpha, Is.EqualTo(1));
    }

    [Test]
    public void AnimationOutKeepsItsDelayedHideAfterAnIntermediateShow()
    {
        var model = Model(false,
            Order(0, Event(ChartEventType.AnimationOutUi, "7")),
            Order(0.2f, Event(ChartEventType.ShowUi, "7")));
        var timeline = new ChartUiEventTimeline(model);

        var during = timeline.Evaluate(ChartUiTarget.ProgressBar, 0.3f);
        Assert.That(during.Alpha, Is.EqualTo(1));
        Assert.That(during.AnimationKind, Is.EqualTo(ChartUiAnimationKind.Out));
        Assert.That(timeline.Evaluate(ChartUiTarget.ProgressBar, 0.5f).Alpha, Is.EqualTo(0));
    }

    [Test]
    public void LaterShowWinsWhenItMatchesAnimationOutsDelayedHideTime()
    {
        var model = Model(false,
            Order(0, Event(ChartEventType.AnimationOutUi, "7")),
            Order(0.5f, Event(ChartEventType.ShowUi, "7")));
        var timeline = new ChartUiEventTimeline(model);

        Assert.That(timeline.Evaluate(ChartUiTarget.ProgressBar, 0.5f).Alpha, Is.EqualTo(1));
    }

    [Test]
    public void NewAnimationImmediatelyOverridesAndRestartsThePreviousAnimation()
    {
        var model = Model(false,
            Order(0, Event(ChartEventType.AnimationInUi, "4")),
            Order(0.2f, Event(ChartEventType.AnimationOutUi, "4")));
        var timeline = new ChartUiEventTimeline(model);

        var state = timeline.Evaluate(ChartUiTarget.Scanline, 0.2f);
        Assert.That(state.AnimationKind, Is.EqualTo(ChartUiAnimationKind.Out));
        Assert.That(state.AnimationProgress, Is.EqualTo(0));
        Assert.That(state.AnimationAlpha, Is.EqualTo(1));

        state = timeline.Evaluate(ChartUiTarget.Scanline, 0.7f);
        Assert.That(state.AnimationProgress, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(state.AnimationAlpha, Is.EqualTo(1));
    }

    [Test]
    public void AnimationLayersUseTargetSpecificSampling()
    {
        var model = Model(false,
            Order(0,
                Event(ChartEventType.AnimationInUi, "0"),
                Event(ChartEventType.AnimationOutUi, "4"),
                Event(ChartEventType.AnimationInUi, "5")));
        var timeline = new ChartUiEventTimeline(model);

        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, 0).AnimationAlpha, Is.EqualTo(0));
        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, 0.033f).AnimationAlpha, Is.EqualTo(1));
        Assert.That(timeline.Evaluate(ChartUiTarget.Combo, 0.066f).AnimationAlpha, Is.EqualTo(0));
        Assert.That(timeline.Evaluate(ChartUiTarget.Scanline, 0.5f).AnimationAlpha, Is.EqualTo(1));
        Assert.That(timeline.Evaluate(ChartUiTarget.BoundaryLine, 0.06665f).AnimationAlpha,
            Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(timeline.Evaluate(ChartUiTarget.BoundaryLine, 0.1333f).AnimationAlpha, Is.EqualTo(1));
    }

    [Test]
    public void EventCursorCanMoveBackwardAndSkipsEventsAtTheSeekTime()
    {
        var events = new List<ChartModel.EventOrder>
        {
            Order(1),
            Order(2),
            Order(3)
        };

        Assert.That(ChartEventCursor.FindFirstAfter(events, 2), Is.EqualTo(2));
        Assert.That(ChartEventCursor.FindFirstAfter(events, 0.5f), Is.EqualTo(0));
        Assert.That(ChartEventCursor.FindFirstAfter(events, 4), Is.EqualTo(3));
    }

    [Test]
    public void EmptyTargetsApplyToAllSupportedTargetsButSpectrumRemainsIgnored()
    {
        var model = Model(true, Order(0, Event(ChartEventType.ShowUi, "")));
        var timeline = new ChartUiEventTimeline(model);

        foreach (var target in new[]
                 {
                     ChartUiTarget.Combo, ChartUiTarget.Score, ChartUiTarget.LeftUi,
                     ChartUiTarget.RightUi, ChartUiTarget.Scanline, ChartUiTarget.BoundaryLine,
                     ChartUiTarget.ProgressBar
                 })
            Assert.That(timeline.Evaluate(target, 0).Alpha, Is.EqualTo(1), target.ToString());

        Assert.That(timeline.Evaluate(ChartUiTarget.Spectrum, 0).Alpha, Is.EqualTo(1));
    }

    [Test]
    public void InvalidTargetsWarnWhileSpectrumIsSilent()
    {
        var warnings = new List<string>();
        var model = Model(false, Order(0, Event(ChartEventType.HideUi, "6,nope,9")));
        _ = new ChartUiEventTimeline(model, warnings.Add);

        Assert.That(warnings, Has.Count.EqualTo(2));
    }

    private static ChartModel Model(bool startsWithoutUi, params ChartModel.EventOrder[] orders)
    {
        return new ChartModel
        {
            is_start_without_ui = startsWithoutUi,
            event_order_list = new List<ChartModel.EventOrder>(orders)
        };
    }

    private static ChartModel.EventOrder Order(float time, params ChartModel.ChartEvent[] events)
    {
        return new ChartModel.EventOrder
        {
            time = time,
            event_list = new List<ChartModel.ChartEvent>(events)
        };
    }

    private static ChartModel.ChartEvent Event(ChartEventType type, string args)
    {
        return new ChartModel.ChartEvent {type = (int) type, args = args};
    }
}
