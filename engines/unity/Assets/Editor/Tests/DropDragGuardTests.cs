using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

public class DropDragGuardTests
{
    [TestCase(4, 3.29670f, 3.62637f, 3.288872f, -3.28f, -3.276845f, -2.788882f)]
    [TestCase(24, 8.90109f, 9.56043f, 8.896207f, 0f, -0.234283f, -2.913804f)]
    [TestCase(46, 13.84614f, 14.50548f, 13.850166f, -0.656f, -0.534167f, -2.564023f)]
    [TestCase(47, 14.17581f, 14.50548f, 14.100184f, -1.968f, -1.921124f, -2.707683f)]
    public void RecordedFalseClicksAreBlocked(int id, float dragTime, float clickTime,
        float now, float landingX, float x, float y)
    {
        var guard = new DropDragGuard(id, dragTime, dragTime, 0, 1.3f,
            new Vector2(landingX, -3.288333f));
        Assert.That(DropNoteIsolation.BlocksSelect(guard, clickTime, 2.726251f,
            new Vector2(x, y), now, 0), Is.True);
    }

    [Test]
    public void OpeningClickRemainsAvailableForItsRealPress()
    {
        var guard = new DropDragGuard(4, 3.29670f, 3.29670f, 0, 1.3f,
            new Vector2(-3.28f, -3.288333f));
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 3.62637f, 2.726251f,
            guard.Landing, 3.638888f, 0), Is.False);
    }

    [Test]
    public void MidpointAndCoHitBoundariesProtectOnlyTheDragSide()
    {
        var guard = Guard(10f);
        Assert.That(Block(guard, 10.030f, 10f), Is.False, "30ms co-hit");
        Assert.That(Block(guard, 10.060f, 10.029f), Is.True);
        Assert.That(Block(guard, 10.060f, 10.030f), Is.True, "midpoint tie belongs to Drag");
        Assert.That(Block(guard, 10.060f, 10.031f), Is.False);
        Assert.That(Block(guard, 10.060f, 10.060f), Is.False, "on-time Click");
    }

    [Test]
    public void GuardUsesLandingRadiusAndSuccessfulLateWindow()
    {
        var guard = Guard(10f);
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 11f, 2f, new Vector2(2f, 0), 10f, 0), Is.True);
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 11f, 2f, new Vector2(2.001f, 0), 10f, 0), Is.False);
        Assert.That(Block(guard, 11f, 10.199f), Is.True);
        Assert.That(Block(Guard(0f), 1f, 0.200f), Is.False, "exact representable 200ms boundary");
        Assert.That(Block(guard, 11f, 10.201f), Is.False);
        Assert.That(Block(guard, 11f, 10.250f), Is.False, "300ms Miss threshold is not guard expiry");
    }

    [TestCase(-0.1f)]
    [TestCase(0.1f)]
    public void OffsetChangesMidpointButRawTimeStillControlsAdmission(float offset)
    {
        var guard = new DropDragGuard(1, 10f, 10f + offset, 0, 1f,
            Vector2.zero);
        Assert.That(Block(guard, 11f, 9.689f), Is.False);
        Assert.That(Block(guard, 11f, 9.691f), Is.True);
        var clickTime = guard.EffectiveTime + 0.060f;
        Assert.That(Block(guard, clickTime, guard.EffectiveTime + 0.020f), Is.True);
        Assert.That(Block(guard, clickTime, guard.EffectiveTime + 0.040f), Is.False);
    }

    [Test]
    public void FuturePageUsesItsOwnHalfDuration()
    {
        var guard = new DropDragGuard(1, 10f, 10f, 1, 0.2f,
            Vector2.zero);
        Assert.That(Block(guard, 11f, 9.89f), Is.False);
        Assert.That(Block(guard, 11f, 9.91f), Is.True);
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 11f, 2f, Vector2.zero, 9.89f, 1), Is.True);
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void ActualFreshDragGateAllowsOnTimeClickAndKeepsOrdinaryDragPolicy(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.060f);
            fixture.Time(10f);
            Assert.That(fixture.Eligible(click, drag), Is.False);
            fixture.Time(10.060f);
            Assert.That(drag.CanHandleTouch(), Is.True, "fresh Drag remains a real accepted candidate");
            Assert.That(fixture.Eligible(click, drag), Is.True, "old unconditional 30ms gate must be replaced");
            SetProperty(drag, nameof(Note.Type), NoteType.DragHead);
            Assert.That(fixture.Eligible(click, drag), Is.False, "ordinary Drag policy is unchanged");
        }
    }

    [Test]
    public void ArmedDragStillProtectsEvenThoughItIsAbsentFromTouchableList()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(4, 3.29670f);
            var click = fixture.Click(6, 3.62637f);
            SetProperty(drag, nameof(Note.IsArmed), true);
            fixture.Time(3.288872f);
            CollectionAssert.DoesNotContain(fixture.Input.TouchableDragNotes, drag);
            CollectionAssert.Contains(fixture.Input.ArmedDragNotes, drag,
                "armed drags stay tracked for in-flight chain detection");
            Assert.That(drag.CanHandleTouch(), Is.False);
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Time(3.62637f);
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [Test]
    public void SameFrameClearSurvivesPoolReuseAndPauseButResetsOnRetry()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(46, 13.84614f);
            var click = fixture.Click(49, 14.50548f);
            fixture.Time(13.850166f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            // Reproduce Collect resetting Model and reusing the same object at another landing.
            fixture.Game.SpawnedNotes.Remove(46);
            SetProperty(drag, nameof(Note.Model), null);
            drag.transform.position = new Vector3(100, 100, 0);
            fixture.Input.OnGamePaused(fixture.Game);
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Input.DisableInput();
            fixture.Input.EnableInput();
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [Test]
    public void MissDoesNotCaptureAGuardAndExplicitResetClearsSuccessfulGuards()
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.6f);
            fixture.Time(10.01f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Miss, 0, 0));
            fixture.Game.SpawnedNotes.Remove(1);
            Assert.That(fixture.Eligible(click, null), Is.True);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Input.ResetDropGuards();
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [TestCase(0.005f)]
    [TestCase(0.020f)]
    public void BackwardClockCorrectionKeepsAnUnexpiredClearedGuard(float correction)
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.6f);
            fixture.Time(10.03f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            fixture.Game.SpawnedNotes.Remove(1);
            Assert.That(fixture.Eligible(click, null), Is.False);

            fixture.Time(10.03f - correction);
            Assert.That(fixture.Eligible(click, null), Is.False, "DSP correction is not a new play");
            fixture.Time(10.201f);
            Assert.That(fixture.Eligible(click, null), Is.True, "normal expiry still applies");
        }
    }

    [Test]
    public void NewPlayClearsGuardsThroughTheGameStartedEvent()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.3f);
            fixture.Time(10.01f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            fixture.Game.SpawnedNotes.Remove(1);
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Game.onGameStarted.Invoke(fixture.Game);
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void TwoRealDownsInOneFrameCannotJudgeALaterClickAfterDragPoolReuse(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.33f);
            var reusedModel = fixture.Model(3, 20f, NoteType.DropDrag);
            fixture.PrepareJudgments(mode);
            fixture.Time(10.01f);
            var frame = Time.frameCount;

            fixture.Down(0);
            Assert.That(fixture.Game.State.IsJudged(1), Is.True, "first Down must really settle the Drag");
            Assert.That(fixture.Game.State.IsJudged(2), Is.False, "guard must be built before Select selection");
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(1));

            drag.Collect();
            Assert.That(drag.Model, Is.Null, "real Collect resets the pooled model");
            var reused = fixture.Game.ObjectPool.SpawnNote(reusedModel);
            Assert.That(reused, Is.SameAs(drag), "the actual pool reuses the old object");
            reused.transform.position = new Vector3(100, 100, 0);

            // Do not refresh the per-frame touchable lists or yield between Downs.
            fixture.Down(1);
            Assert.That(Time.frameCount, Is.EqualTo(frame));
            Assert.That(fixture.Game.State.IsJudged(2), Is.False, "cleared snapshot must survive pool reuse");
            Assert.That(click.IsCleared, Is.False);
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(1));

            fixture.Time(10.33f);
            fixture.Down(2);
            Assert.That(fixture.Game.State.IsJudged(2), Is.True, "the later real Click remains available");
            Assert.That(fixture.Game.State.GetJudgement(2).Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    // GameTouchInput dispatches FingerUpdate only from Moved/Stationary phases -- never on
    // the press frame. These tests lock the press/settle contract at the settle level using
    // the timings of a recorded replay over two consecutive DropDrag landings (20ms apart,
    // beyond DragStackBatchGapSeconds, so the pair can never co-batch): one Down settles
    // exactly ONE note -- the representative; the second drag is recorded by the next
    // stationary contact update and resolves at its own trigger point, never at the
    // finger's release. A port over an event layer that co-dispatches Down+Update on the
    // press frame (e.g. LeanTouch UpdateEvents) regresses both and settles two notes from
    // one tap.
    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void StationaryUpdateRecordsSecondConsecutiveDropDragForItsTriggerPoint(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var first = fixture.Drag(56, 5.056f);
            var second = fixture.Drag(58, 5.076f); // 20ms apart: never co-batched
            first.transform.position = Vector3.zero;
            second.transform.position = Vector3.zero;
            fixture.PrepareJudgments(mode);

            fixture.Time(5.251f); // 195ms / 175ms late: both inside the late Perfect window
            fixture.Down(0);
            Assert.That(fixture.Game.State.IsJudged(56), Is.True, "the Down settles the first drag");
            Assert.That(fixture.Game.State.GetJudgement(56).Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(fixture.Game.State.IsJudged(58), Is.False,
                "a Down must not settle the second consecutive drag");
            Assert.That(second.IsArmed, Is.False, "and must not arm it either");

            fixture.Time(5.259f); // next stationary contact frame (120Hz)
            fixture.Update(0);
            Assert.That(second.IsArmed, Is.True,
                "the first stationary contact update records the second drag");
            Assert.That(fixture.Game.State.IsJudged(58), Is.False,
                "a recorded contact settles nothing by itself");

            fixture.Up(0, Vector3.zero);
            Assert.That(fixture.Game.State.IsJudged(58), Is.False,
                "a recorded DropDrag never settles on release");

            second.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(58), Is.True,
                "the recording resolves at its own trigger point");
            Assert.That(fixture.Game.State.GetJudgement(58).Grade, Is.EqualTo(NoteGrade.Perfect),
                "same grade a press-frame judgment would have given");
        }
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void EarlySecondConsecutiveDropDragArmsOnContactUpdateAndResolvesAtTriggerPoint(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var first = fixture.Drag(44, 4.125f);
            var second = fixture.Drag(47, 4.287f);
            first.transform.position = Vector3.zero;
            second.transform.position = Vector3.zero;
            fixture.PrepareJudgments(mode);

            fixture.Time(4.284f); // 159ms late / 2.7ms early
            fixture.Down(0);
            Assert.That(fixture.Game.State.GetJudgement(44).Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(fixture.Game.State.IsJudged(47), Is.False,
                "a Down must not settle the still-early second drag");

            fixture.Time(4.285f); // contact while still before the trigger point
            fixture.Update(0);
            Assert.That(second.IsArmed, Is.True, "contact before start arms the note");
            Assert.That(fixture.Game.State.IsJudged(47), Is.False);

            second.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(47), Is.False,
                "armed note resolves only at its trigger point");
            fixture.Time(4.292f);
            second.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(47), Is.True);
            Assert.That(fixture.Game.State.GetJudgement(47).Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    // Cyanine record 21347999 (C2v3-restored, engine timings): tapping note 14
    // (DropClick 5.934) recorded the overlapping note 15 (DropDrag 6.264) because the
    // Select won that Down; the next tap at 6.2641 then made the still-early note 17
    // (6.429) the first colliding drag and the representative slot judged it Perfect
    // 164ms before its trigger point. Contract: a press settles exactly one note --
    // the representative, immediately, with its contact grade -- UNLESS an earlier
    // colliding drag is already armed: that press lands on an in-flight chain and
    // continues the recording instead, so 17 resolves at its own trigger point.
    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void PressOnInFlightChainRecordsTheNextNoteInsteadOfSettlingIt(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var armed = fixture.Drag(15, 10.0f);
            var early = fixture.Drag(17, 10.164f); // 164ms after the in-front note
            armed.transform.position = Vector3.zero;
            early.transform.position = Vector3.zero;
            SetProperty(armed, nameof(Note.IsArmed), true); // the "14" tap recorded it
            fixture.PrepareJudgments(mode);

            fixture.Time(10.0f);
            fixture.Down(0);
            Assert.That(early.IsArmed, Is.True,
                "a press onto an in-flight chain records the next note");
            Assert.That(fixture.Game.State.IsJudged(17), Is.False,
                "it must not settle at tap time while the series' first is still recorded");
            Assert.That(fixture.Game.State.IsJudged(15), Is.False,
                "the already-armed in-front note is not consumed by this Down");

            early.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(17), Is.False,
                "recorded note resolves only at its trigger point");
            fixture.Time(10.17f);
            early.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.GetJudgement(17).Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    // These tests drive the production OnFingerDown chain (guard filtering, batch
    // collection and settlement) at arbitrary judgement-world positions instead of
    // calling internal filter methods, so the real input path is covered.
    [Test]
    public void RealFingerDownSettlesDragAndBlocksGuardedClickUntilMidpoint()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.5f);
            fixture.PrepareJudgments(GameMode.Practice);
            fixture.Time(10.06f);
            fixture.Down(0);
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.True,
                "fresh DropDrag settles through the real OnFingerDown chain");
            Assert.That(fixture.Game.State.Judgements[1].Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(click.IsCleared, Is.False, "guarded false Click must not clear");
            fixture.Time(10.1f);
            fixture.Down(1);
            Assert.That(click.IsCleared, Is.False, "captured guard still blocks before the midpoint");
            fixture.Time(10.26f);
            fixture.Down(2);
            Assert.That(fixture.Game.State.Judgements[2].IsJudged, Is.True,
                "Click settles through the real chain after the midpoint");
            Assert.That(fixture.Game.State.Judgements[2].Grade, Is.EqualTo(NoteGrade.Good));
        }
    }

    [Test]
    public void RealFingerDownDefersDropDragToItsTriggerPointWhenClickWinsSameDown()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.3f);
            fixture.PrepareJudgments(GameMode.Practice);
            fixture.Time(10.16f);
            var finger = fixture.Down(0);
            Assert.That(fixture.Game.State.Judgements[2].IsJudged, Is.True,
                "Click past the midpoint clears on the same Down");
            Assert.That(fixture.Game.State.Judgements[2].Grade, Is.EqualTo(NoteGrade.Great));
            Assert.That(drag.IsArmed, Is.True,
                "the drag under a Select-won Down is recorded, not judged");
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.False);
            fixture.Update(finger);
            Assert.That(drag.IsArmed, Is.True,
                "a contact update must not change the recording");
            fixture.Up(finger, Vector3.zero);
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.False,
                "DropDrag never settles on release");
            Assert.That(drag.IsArmed, Is.True);
            drag.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.True,
                "a recorded DropDrag resolves only at its own trigger point");
            Assert.That(fixture.Game.State.Judgements[1].Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void RealFingerDownArmedDragGuardCoversOnlyLandingRadius()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(4, 3.29670f);
            var click = fixture.Click(6, 3.62637f);
            drag.transform.position = new Vector2(-3.28f, -3.288333f);
            click.transform.position = new Vector2(-2f, -2.8f);
            SetProperty(drag, nameof(Note.IsArmed), true);
            fixture.PrepareJudgments(GameMode.Practice);
            fixture.Time(3.288872f);
            fixture.Down(0, drag.transform.position);
            Assert.That(click.IsCleared, Is.False,
                "armed DropDrag landing guard blocks the colliding Click before the midpoint");
            Assert.That(drag.IsCleared, Is.False, "armed DropDrag stays pending for its own judgment");
            fixture.Down(1, new Vector2(-1.2f, -2.4f));
            Assert.That(fixture.Game.State.Judgements[6].IsJudged, Is.True,
                "guard does not extend beyond the landing radius");
            Assert.That(fixture.Game.State.Judgements[6].Grade, Is.EqualTo(NoteGrade.Good));
            Assert.That(fixture.Game.State.Judgements[4].IsJudged, Is.False,
                "armed DropDrag was not consumed by the Click's Down");
        }
    }

    [Test]
    public void EarlyPressWithinWindowSettlesImmediatelyWithoutAnInFlightRecording()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var early = fixture.Drag(17, 10.164f);
            early.transform.position = Vector3.zero;
            fixture.PrepareJudgments(GameMode.Practice);

            fixture.Time(10.13f); // 34ms early -- within the press settle window
            fixture.Down(0);
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.True,
                "a near-on-time press settles its representative immediately");
            Assert.That(fixture.Game.State.Judgements[17].Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(early.IsArmed, Is.False);
        }
    }

    [Test]
    public void FarEarlyOrdinaryDragPressStillSettlesImmediately()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.DragChild(17, 10.164f);
            drag.transform.position = Vector3.zero;
            fixture.PrepareJudgments(GameMode.Practice);

            fixture.Time(10.0f); // 164ms early: travelling drags keep the instant feel
            fixture.Down(0);
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.True,
                "ordinary drags settle at the press regardless of the early window");
            Assert.That(fixture.Game.State.Judgements[17].Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(drag.IsArmed, Is.False);
        }
    }

    // Cyanine record 21349071: a stationary hold over the 14/15 hitbox overlap recorded
    // 15 (0.301s early, just under admission); 15 tick-resolved at its trigger; the
    // player's press for 15 -- 33ms later, landing inside 17's hitbox as well -- found
    // 15 gone and settled 17 immediately, 116ms above the line. Presses farther than
    // PressSettleEarlyWindowSeconds ahead of the trigger now record instead.
    [Test]
    public void Record21349071PressForConsumedNeighbourRecordsTheNextNote()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var n15 = fixture.Drag(15, 10.0f);
            var n17 = fixture.Drag(17, 10.2827f);
            n15.transform.position = new Vector3(2.0f, -3.513f);
            n17.transform.position = new Vector3(1.5f, -3.513f);
            foreach (var n in new Note[] {n15, n17})
                n.GetComponent<CircleCollider2D>().radius = 1.037f;
            fixture.PrepareJudgments(GameMode.Practice);

            // Stationary hold 0.30s early records 15 (within admission, past the window).
            fixture.Time(9.700f);
            fixture.Down(0, new Vector3(2.2f, -3.513f));
            Assert.That(n15.IsArmed, Is.True, "the early hold records 15");
            Assert.That(fixture.Game.State.Judgements[15].IsJudged, Is.False);

            // 15 resolves at its own trigger.
            fixture.Time(10.0f);
            n15.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[15].Grade, Is.EqualTo(NoteGrade.Perfect));
            fixture.Up(0, new Vector3(2.2f, -3.513f));

            // The press meant for 15 lands inside 17's hitbox too -- 17 is 250ms early,
            // past the settle window: record it, do not pop it above the line.
            fixture.Time(10.033f);
            fixture.Down(1, new Vector3(2.3f, -3.513f));
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.False,
                "a far-early press must not settle 17 immediately");
            Assert.That(n17.IsArmed, Is.True, "it records 17 for its own trigger point");

            n17.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.False);
            fixture.Time(10.2827f);
            n17.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[17].Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    // Cyanine record 21348400 (chartBackup-restored types: 28/30 and 46/47 are DropDrag
    // pairs 330ms apart with overlapping hitboxes). A stationary 83ms tap whose point
    // falls inside BOTH hitboxes settles the tapped note on the press; the hold's
    // contact updates record the neighbour; the lift must NOT settle it -- it resolves
    // at its own trigger point.
    [Test]
    public void Record21348400StationaryTapRecordsNeighbourForItsTriggerPoint()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var tapped = fixture.Drag(46, 13.84614f);
            tapped.transform.position = new Vector3(-0.656f, -3.288333f);
            var neighbour = fixture.Drag(47, 14.17581f); // 329.67ms later
            neighbour.transform.position = new Vector3(-1.968f, -3.288333f);
            var tapPos = new Vector3(-1.056f, -3.306f); // inside both hitboxes
            fixture.PrepareJudgments(GameMode.Practice);

            fixture.Time(13.869368f); // 23.2ms late for 46
            var finger = fixture.Down(0, tapPos);
            Assert.That(fixture.Game.State.Judgements[46].IsJudged, Is.True,
                "the tapped note settles on the press");
            Assert.That(fixture.Game.State.Judgements[46].Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(fixture.Game.State.Judgements[47].IsJudged, Is.False);

            foreach (var t in new[] {13.886056f, 13.902716f, 13.919378f, 13.936026f, 13.952677f})
            {
                fixture.Time(t);
                fixture.Update(finger, tapPos); // stationary hold
            }
            Assert.That(neighbour.IsArmed, Is.True,
                "the hold records the neighbour once it enters its early window");
            Assert.That(fixture.Game.State.Judgements[47].IsJudged, Is.False);

            fixture.Up(finger, tapPos);
            Assert.That(fixture.Game.State.Judgements[47].IsJudged, Is.False,
                "the lift must not settle a recorded DropDrag");
            Assert.That(neighbour.IsArmed, Is.True);

            neighbour.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[47].IsJudged, Is.False,
                "before its trigger point even the tick settles nothing");
            fixture.Time(14.175810f);
            neighbour.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[47].Grade, Is.EqualTo(NoteGrade.Perfect),
                "the recording resolves at its own trigger point");
        }
    }

    // Cyanine record 21348471 (chartBackup geometry: 14 DropClick and 17 DropDrag share
    // landing x=0.6 -> +1.344; 15 DropDrag at +2.688; 16 DropClick at -1.344; real player
    // hitbox radius 1.037). Replays the exact touch sequence: the 14 tap (10.9ms early,
    // held stationary), the two-finger 15/16 pair tap (9.5ms late), then the habitual
    // 17 tap (5.3ms early). Verdict under current code: 17 stays virgin through the
    // 14-tap hold (0.31s admission) and through the 15/16 pair (1.50 units away); it
    // settles only at its own tap.
    [Test]
    public void Record21348471DropPairDoesNotTouchTheSharedLandingNeighbour()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var n14 = fixture.Click(14, 5.934060f);
            var n15 = fixture.Drag(15, 6.263730f);
            var n16 = fixture.Click(16, 6.263730f);
            var n17 = fixture.Drag(17, 6.428565f);
            n14.transform.position = new Vector3(1.344f, -3.513f);
            n15.transform.position = new Vector3(2.688f, -3.513f);
            n16.transform.position = new Vector3(-1.344f, -3.513f);
            n17.transform.position = new Vector3(1.344f, -3.513f);
            foreach (var n in new Note[] {n14, n15, n16, n17})
                n.GetComponent<CircleCollider2D>().radius = 1.037f;
            fixture.PrepareJudgments(GameMode.Practice);

            // Tap 14: down 5.923239 at (+1.125, -3.350), stationary hold, up 6.023261.
            fixture.Time(5.923239f);
            var f0 = fixture.Down(0, new Vector3(1.125f, -3.350f));
            foreach (var t in new[] {5.939917f, 5.956583f, 5.973256f, 5.989934f, 6.006602f})
            {
                fixture.Time(t);
                fixture.Update(f0, new Vector3(1.125f, -3.350f));
            }
            fixture.Time(6.023261f);
            fixture.Up(f0, new Vector3(1.125f, -3.350f));
            Assert.That(fixture.Game.State.Judgements[14].IsJudged, Is.True, "14 settles at its own tap");
            Assert.That(n17.IsArmed, Is.False,
                "the hold at 14/17's shared landing must not record 17 (0.505s early, past admission)");
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.False);

            // Two-finger 15/16 pair tap: down 6.273268, stationary, up 6.339934.
            fixture.Time(6.273268f);
            var pair0 = fixture.Down(1, new Vector3(-1.481f, -3.475f));
            var pair1 = fixture.Down(2, new Vector3(2.844f, -3.413f));
            foreach (var t in new[] {6.289921f, 6.306594f, 6.323265f})
            {
                fixture.Time(t);
                fixture.Update(pair0, new Vector3(-1.481f, -3.475f));
                fixture.Update(pair1, new Vector3(2.844f, -3.413f));
            }
            fixture.Time(6.339934f);
            fixture.Up(pair0, new Vector3(-1.481f, -3.475f));
            fixture.Up(pair1, new Vector3(2.844f, -3.413f));
            Assert.That(fixture.Game.State.Judgements[15].IsJudged, Is.True, "15 settles at its own tap");
            Assert.That(fixture.Game.State.Judgements[16].IsJudged, Is.True, "16 settles at its own tap");
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.False,
                "the pair tap (1.50 / 2.83 units from 17) must not settle 17");
            Assert.That(n17.IsArmed, Is.False, "nor record it");

            // Habitual 17 tap: down 6.423265 at (+1.425, -3.387), 5.3ms before trigger.
            fixture.Time(6.423265f);
            var late = fixture.Down(3, new Vector3(1.425f, -3.387f));
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.True,
                "17 settles at its own tap with its contact grade");
            Assert.That(fixture.Game.State.Judgements[17].Grade, Is.EqualTo(NoteGrade.Perfect));
            fixture.Time(6.439950f);
            fixture.Update(late, new Vector3(1.425f, -3.387f));
            fixture.Time(6.489913f);
            fixture.Up(late, new Vector3(1.425f, -3.387f));
        }
    }

    // Cyanine record 21348526: a press at (+3.444, -3.444) sits inside BOTH 15's
    // (landing +2.688, 9.7ms past due) and 18's (landing +4.032, 0.320s early -- past
    // admission) hitboxes. The press settles 15 immediately (the only due colliding
    // drag); 18 arms one frame later once admission opens (0.303s early); the swipe
    // continues left and records 17 (6.356, 0.072s early); 17/18 resolve at their own
    // trigger points. Locks the overlap-zone press timeline.
    [Test]
    public void Record21348526PressInsideOverlapZoneSettlesTheDueNeighbour()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var n14 = fixture.Click(14, 5.934060f);
            var n15 = fixture.Drag(15, 6.263730f);
            var n16 = fixture.Click(16, 6.263730f);
            var n17 = fixture.Drag(17, 6.428565f);
            var n18 = fixture.Drag(18, 6.593400f);
            n14.transform.position = new Vector3(1.344f, -3.513f);
            n15.transform.position = new Vector3(2.688f, -3.513f);
            n16.transform.position = new Vector3(-1.344f, -3.513f);
            n17.transform.position = new Vector3(1.344f, -3.513f);
            n18.transform.position = new Vector3(4.032f, -3.513f);
            foreach (var n in new Note[] {n14, n15, n16, n17, n18})
                n.GetComponent<CircleCollider2D>().radius = 1.037f;
            fixture.PrepareJudgments(GameMode.Practice);

            // 14 tap: down 5.956738, stationary, up 6.040033.
            fixture.Time(5.956738f);
            var f0 = fixture.Down(0, new Vector3(1.481f, -3.438f));
            foreach (var t in new[] {5.973415f, 5.990064f, 6.006731f, 6.023381f})
            {
                fixture.Time(t);
                fixture.Update(f0, new Vector3(1.481f, -3.438f));
            }
            fixture.Time(6.040033f);
            fixture.Update(f0, new Vector3(1.481f, -3.438f));
            fixture.Up(f0, new Vector3(1.481f, -3.438f));
            Assert.That(fixture.Game.State.Judgements[14].IsJudged, Is.True);
            Assert.That(n17.IsArmed, Is.False, "17 stays virgin through the 14 tap hold");

            // Swipe press at 6.273385 inside the 15/18 hitbox overlap.
            fixture.Time(6.273385f);
            var sw = fixture.Down(1, new Vector3(3.444f, -3.444f));
            Assert.That(fixture.Game.State.Judgements[15].IsJudged, Is.True,
                "15 (9.7ms past due, 0.76 units away) settles at the press");
            Assert.That(fixture.Game.State.Judgements[15].Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(n18.IsArmed, Is.False,
                "18 (0.320s early) is past admission at the press");

            // Admission opens one frame later: the stationary hold records 18.
            fixture.Time(6.290023f);
            fixture.Update(sw, new Vector3(3.444f, -3.444f));
            Assert.That(n18.IsArmed, Is.True, "18 records once within admission");

            // Concurrent 16 tap on the other finger.
            fixture.Time(6.306675f);
            fixture.Update(sw, new Vector3(3.444f, -3.444f));
            var tap = fixture.Down(2, new Vector3(-1.188f, -3.844f));
            Assert.That(fixture.Game.State.Judgements[16].IsJudged, Is.True);

            // Swipe left across 15's landing toward 14/17's shared landing.
            var path = new[] {
                Tuple.Create(6.323325f, new Vector3(2.979f, -3.305f)),
                Tuple.Create(6.339987f, new Vector3(2.663f, -3.312f)),
                Tuple.Create(6.356643f, new Vector3(2.259f, -3.321f)),
                Tuple.Create(6.373300f, new Vector3(1.876f, -3.349f)),
                Tuple.Create(6.389969f, new Vector3(1.573f, -3.368f)),
                Tuple.Create(6.406677f, new Vector3(1.334f, -3.372f)),
                Tuple.Create(6.423412f, new Vector3(1.334f, -3.372f)),
            };
            foreach (var step in path)
            {
                fixture.Time(step.Item1);
                fixture.Update(sw, step.Item2);
                fixture.Update(tap, new Vector3(-1.188f, -3.844f));
                n17.TickStackFollowerJudgment();
                n18.TickStackFollowerJudgment();
            }
            Assert.That(n17.IsArmed, Is.True, "the swipe records 17 (0.072s early at 6.3566)");
            Assert.That(fixture.Game.State.Judgements[17].IsJudged, Is.False);

            fixture.Time(6.428565f);
            n17.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[17].Grade, Is.EqualTo(NoteGrade.Perfect),
                "17 resolves at its own trigger point");
            fixture.Time(6.593400f);
            n18.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[18].Grade, Is.EqualTo(NoteGrade.Perfect),
                "18 resolves at its own trigger point");
        }
    }

    [Test]
    public void FingerReleaseSettlesRecordedDragsBeforeTheirTriggerPoints()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.DragChild(1, 10.0f);
            drag.transform.position = Vector3.zero;
            fixture.PrepareJudgments(GameMode.Practice);

            fixture.Time(9.9f); // 100ms early: within admission and the drag grade window
            var finger = fixture.Down(0, new Vector3(-6f, 0f, 0f)); // press beside it
            fixture.Update(finger, Vector3.zero); // swipe over it records the contact
            Assert.That(drag.IsArmed, Is.True, "swipe contact records without settling");
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.False);

            fixture.Up(finger, Vector3.zero); // lift before the 10.0 trigger point
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.True,
                "release settles what the finger recorded");
            Assert.That(fixture.Game.State.Judgements[1].Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    [Test]
    public void DropDragReleaseKeepsRecordingUntilItsTriggerPoint()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(1, 10.0f);
            var sibling = fixture.DragChild(2, 10.0f);
            sibling.transform.position = new Vector3(-3f, 0f, 0f);
            drag.transform.position = Vector3.zero;
            fixture.PrepareJudgments(GameMode.Practice);

            fixture.Time(9.9f);
            fixture.Down(0, new Vector3(6f, 0f, 0f)); // this finger records only the DropDrag
            fixture.Down(1, new Vector3(-6f, 0f, 0f)); // this finger records only the DragChild
            fixture.Update(0, Vector3.zero);
            fixture.Update(1, new Vector3(-3f, 0f, 0f));
            Assert.That(drag.IsArmed, Is.True);
            Assert.That(sibling.IsArmed, Is.True);

            fixture.Up(0, Vector3.zero);
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.False,
                "DropDrag never settles on release");
            Assert.That(drag.IsArmed, Is.True);
            drag.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.False,
                "before its trigger point even the tick settles nothing");

            fixture.Up(1, new Vector3(-3f, 0f, 0f));
            Assert.That(fixture.Game.State.Judgements[2].IsJudged, Is.True,
                "ordinary drags still settle on release");
            Assert.That(fixture.Game.State.Judgements[2].Grade, Is.EqualTo(NoteGrade.Perfect));

            fixture.Time(10.0f);
            drag.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.True,
                "a recorded DropDrag resolves only at its own trigger point");
            Assert.That(fixture.Game.State.Judgements[1].Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    [Test]
    public void FingerReleaseSettlesOnlyThatFingersRecordedDrags()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var theirs = fixture.DragChild(1, 10.0f);
            theirs.transform.position = Vector3.zero;
            var mine = fixture.DragChild(2, 10.0f);
            mine.transform.position = new Vector3(-3f, 0f, 0f);
            fixture.PrepareJudgments(GameMode.Practice);

            fixture.Time(9.9f);
            fixture.Down(0, new Vector3(6f, 0f, 0f));
            fixture.Down(1, new Vector3(-6f, 0f, 0f));
            fixture.Update(0, Vector3.zero); // finger 0 records note 1
            fixture.Update(1, new Vector3(-3f, 0f, 0f)); // finger 1 records note 2
            Assert.That(theirs.IsArmed, Is.True);
            Assert.That(mine.IsArmed, Is.True);

            fixture.Up(0, Vector3.zero);
            Assert.That(fixture.Game.State.Judgements[1].IsJudged, Is.True,
                "finger 0's release settles its own recording");
            Assert.That(fixture.Game.State.Judgements[2].IsJudged, Is.False,
                "finger 1's recording must wait for finger 1");
            Assert.That(mine.IsArmed, Is.True);

            fixture.Up(1, new Vector3(-3f, 0f, 0f));
            Assert.That(fixture.Game.State.Judgements[2].IsJudged, Is.True);
            Assert.That(fixture.Game.State.Judgements[2].Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    [Test]
    public void RecordV2JudgeCarriesPressAttribution()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(1, 10.0f);
            drag.transform.position = Vector3.zero;
            fixture.PrepareJudgments(GameMode.Practice);
            var recorder = AttachRecorder(fixture);

            fixture.Time(10.01f);
            var finger = fixture.Down(0);

            var judge = SingleJudge(recorder.RecordedEvents);
            Assert.That(judge.note, Is.EqualTo(1));
            Assert.That(judge.grade, Is.EqualTo("perfect"));
            Assert.That(judge.cause, Is.EqualTo(GamePlayRecordWire.CausePress));
            Assert.That(judge.finger, Is.EqualTo(finger));
            Assert.That(judge.hasInput, Is.True, "the judge back-references the down event");
            Assert.That(judge.input, Is.EqualTo(0), "sequence 0 is the down touch");
        }
    }

    [Test]
    public void RecordV2AttributionAcrossArmReleaseAndTriggerTick()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var dropDrag = fixture.Drag(3, 10.0f);            // far-early press records, tick settles
            dropDrag.transform.position = new Vector3(6f, 0f, 0f);
            var swipeA = fixture.DragChild(1, 10.0f);         // swipe-recorded, release settles
            swipeA.transform.position = Vector3.zero;
            var swipeB = fixture.DragChild(2, 10.15f); // 0.23s early at the swipe: within DragChild admission+window
            swipeB.transform.position = new Vector3(-3f, 0f, 0f);
            fixture.PrepareJudgments(GameMode.Practice);
            var recorder = AttachRecorder(fixture);

            fixture.Time(9.9f);
            var finger = fixture.Down(0, new Vector3(6f, 0f, 0f)); // press records dropDrag (0.1s early)
            fixture.Update(finger, Vector3.zero);                  // moving swipe records swipeA
            fixture.Time(9.92f);
            fixture.Update(finger, new Vector3(-3f, 0f, 0f));      // ...and swipeB
            fixture.Time(9.95f);
            fixture.Up(finger, new Vector3(-3f, 0f, 0f));          // release settles both bindings
            fixture.Time(10.0f);
            dropDrag.TickStackFollowerJudgment();                   // trigger tick settles dropDrag

            var judges = recorder.RecordedEvents.OfType<GamePlayJudgeEvent>().ToList();
            Assert.That(judges.Count, Is.EqualTo(3));

            var released = judges.Where(j => j.cause == GamePlayRecordWire.CauseRelease).ToList();
            Assert.That(released.Select(j => j.note).OrderBy(n => n), Is.EqualTo(new[] {1, 2}),
                "the release settles every note this finger recorded");
            Assert.That(released.All(j => j.finger == finger && j.hasInput), Is.True,
                "both batch members share the release attribution and input back-reference");

            var ticked = judges.Single(j => j.cause == GamePlayRecordWire.CauseTriggerTick);
            Assert.That(ticked.note, Is.EqualTo(3));
            Assert.That(ticked.finger, Is.Null, "a timer-driven settlement has no direct finger");
            Assert.That(ticked.hasInput, Is.False);

            var arms = recorder.RecordedEvents.OfType<GamePlayArmEvent>().ToList();
            Assert.That(arms.Count, Is.EqualTo(3), "dropDrag, swipeA, swipeB each record once");
            var dropArm = arms.Single(a => a.note == 3);
            Assert.That(dropArm.finger, Is.EqualTo(finger));
            Assert.That(dropArm.hasInput, Is.True);
            Assert.That(dropArm.input, Is.EqualTo(0), "the arm back-references the press");
            CollectionAssert.AreEqual(new[] {2L, 4L},
                arms.Where(a => a.note != 3).OrderBy(a => a.s).Select(a => a.input),
                "swipe arms reference their own update events");
        }
    }

    /// <summary>Attaches a live recorder to the fixture game.</summary>
    private static GamePlayRecordRecorder AttachRecorder(InputFixture fixture)
    {
        var recorder = new GamePlayRecordRecorder(fixture.Game);
        SetProperty(fixture.Game, nameof(Game.PlayRecordRecorder), recorder);
        return recorder;
    }

    private static GamePlayJudgeEvent SingleJudge(IReadOnlyList<GamePlayRecordEvent> events)
    {
        return events.OfType<GamePlayJudgeEvent>().Single();
    }

    private static DropDragGuard Guard(float time) =>
        new DropDragGuard(1, time, time, 0, 1f, Vector2.zero);

    private static bool Block(DropDragGuard guard, float clickTime, float now) =>
        DropNoteIsolation.BlocksSelect(guard, clickTime, 2f, Vector2.zero, now, 0);

    internal static void SetProperty(object instance, string name, object value)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property == null) continue;
            property.SetValue(instance, value);
            return;
        }
        throw new System.MissingMemberException(name);
    }

    internal sealed class InputFixture : System.IDisposable
    {
        public readonly Game Game;
        public readonly InputController Input;
        private readonly List<GameObject> objects = new List<GameObject>();
        private RenderTexture cameraTarget;
        private LocalPlayerSettings previousSettings;
        private bool settingsOverridden;

        public InputFixture(GameMode mode)
        {
            var go = Object("DropGuardInputTest", false);
            Game = go.AddComponent<Game>();
            var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
            chart.Model = new ChartModel();
            chart.Model.page_list.Add(new ChartModel.Page {start_time = 0, end_time = 30});
            SetProperty(Game, nameof(Game.Chart), chart);
            SetProperty(Game, nameof(Game.IsLoaded), true);
            // Grading needs only Mode; the constructor requires fully initialized level metadata.
            var state = (GameState) FormatterServices.GetUninitializedObject(typeof(GameState));
            state.IsPlaying = true;
            typeof(GameState).GetField("<Mode>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(state, mode);
            SetProperty(Game, nameof(Game.State), state);
            Game.ObjectPool = new ObjectPool(Game);
            Input = go.AddComponent<InputController>();
            Input.game = Game;
            Game.inputController = Input;
            Game.effectController = go.AddComponent<EffectController>();
            Game.effectController.game = Game;
            Game.camera = Object("DropGuardCamera", true).AddComponent<Camera>();
            Game.camera.orthographic = true;
            Game.camera.transform.position = new Vector3(0, 0, -10);
            // Fixed-size render target so world<->screen mapping of off-centre taps is
            // deterministic in edit mode.
            Game.camera.orthographicSize = 5f;
            cameraTarget = new RenderTexture(256, 256, 0);
            Game.camera.targetTexture = cameraTarget;
            Invoke("Awake");
            Input.EnableInput();
        }

        public DropDragGuardTestDrag Drag(int id, float time) => Make<DropDragGuardTestDrag>(id, time, NoteType.DropDrag);
        public DropDragGuardTestDragChild DragChild(int id, float time) => Make<DropDragGuardTestDragChild>(id, time, NoteType.DragChild);
        public DropDragGuardTestClick Click(int id, float time) => Make<DropDragGuardTestClick>(id, time, NoteType.DropClick);

        private T Make<T>(int id, float time, NoteType type) where T : Note
        {
            var go = Object(type.ToString(), true);
            go.AddComponent<CircleCollider2D>().radius = 2f;
            var note = go.AddComponent<T>();
            SetProperty(note, nameof(Note.Game), Game);
            SetProperty(note, nameof(Note.Type), type);
            SetProperty(note, nameof(Note.Model), Model(id, time, type));
            SetProperty(note, nameof(Note.Page), Game.Chart.Model.page_list[0]);
            note.MissThreshold = type.GetDefaultMissThreshold();
            note.Renderer = new NoteRenderer(note);
            Game.SpawnedNotes[id] = note;
            return note;
        }

        public ChartModel.Note Model(int id, float time, NoteType type)
        {
            var model = new ChartModel.Note {id = id, type = (int) type, start_time = time, end_time = time, page_index = 0};
            Game.Chart.Model.note_list.Add(model);
            Game.Chart.Model.note_map[id] = model;
            return model;
        }

        public void PrepareJudgments(GameMode mode)
        {
            // Real pool spawning reads the player's judgment offset; restore it after the test.
            previousSettings = Context.Player.Settings;
            settingsOverridden = true;
            SetProperty(Context.Player, nameof(Player.Settings), new LocalPlayerSettings());
            SetProperty(Game, nameof(Game.Level), Level.FromExternal(new LevelMeta()));
            SetProperty(Game, nameof(Game.Difficulty), Difficulty.Extreme);
            var state = new GameState(Game, mode, new HashSet<Mod>()) {IsPlaying = true};
            SetProperty(Game, nameof(Game.State), state);
        }

        /// <summary>
        /// Raises the real OnFingerDown chain for an explicit finger at world zero
        /// (camera center) without refreshing per-frame touchable lists, so multiple
        /// Downs can be issued within one frame as in gameplay.
        /// </summary>
        public int Down(int finger)
        {
            Down(finger, Vector3.zero);
            return finger;
        }

        /// <summary>Down for an explicit finger at an arbitrary judgement-world position.</summary>
        public int Down(int finger, Vector3 worldPosition)
        {
            Physics2D.SyncTransforms();
            Invoke("OnFingerDown", new GameFinger
            {
                Index = finger,
                ScreenPosition = Game.camera.WorldToScreenPoint(worldPosition)
            });
            return finger;
        }

        /// <summary>
        /// Raises the real OnFingerUpdate chain for a finger resting at world zero, as
        /// GameTouchInput does on every Moved/Stationary frame after the press frame.
        /// </summary>
        public void Update(int finger)
        {
            Update(finger, Vector3.zero);
        }

        /// <summary>Update for an explicit finger at an arbitrary judgement-world position.</summary>
        public void Update(int finger, Vector3 worldPosition)
        {
            Physics2D.SyncTransforms();
            Invoke("OnFingerUpdate", new GameFinger
            {
                Index = finger,
                ScreenPosition = Game.camera.WorldToScreenPoint(worldPosition)
            });
        }

        /// <summary>Up for an explicit finger at an arbitrary judgement-world position.</summary>
        public void Up(int finger, Vector3 worldPosition)
        {
            Physics2D.SyncTransforms();
            Invoke("OnFingerUp", new GameFinger
            {
                Index = finger,
                ScreenPosition = Game.camera.WorldToScreenPoint(worldPosition)
            });
        }

        public void Time(float time)
        {
            SetProperty(Game, nameof(Game.Time), time);
            Input.OnGameUpdate(Game);
        }

        public bool Eligible(Note click, Note acceptedDrag)
        {
            Invoke("BuildDropGuardsForDown");
            return (bool) Invoke("IsEligibleSelectAfterDrag", click, acceptedDrag, Vector2.zero);
        }

        private object Invoke(string name, params object[] args) =>
            typeof(InputController).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Input, args);

        private GameObject Object(string name, bool active)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            objects.Add(go);
            return go;
        }

        public void Dispose()
        {
            Input.DisableInput();
            // Drain pending Clear -> AwaitAndCollect callbacks safely even after pool reuse.
            foreach (var go in objects)
            {
                var note = go.GetComponent<Note>();
                if (note != null && note.Model != null) note.Collect();
            }
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
            if (cameraTarget != null)
            {
                cameraTarget.Release();
                UnityEngine.Object.DestroyImmediate(cameraTarget);
            }
            if (settingsOverridden) SetProperty(Context.Player, nameof(Player.Settings), previousSettings);
        }
    }
}

// Minimal renderers avoid loading gameplay art; inherited admission and grading stay real.
public class DropDragGuardTestDrag : DropDragNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}

public class DropDragGuardTestDragChild : DragChildNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}

public class DropDragGuardTestClick : DropClickNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}
