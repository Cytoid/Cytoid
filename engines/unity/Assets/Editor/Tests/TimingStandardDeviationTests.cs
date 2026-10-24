using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;

public class TimingStandardDeviationTests
{
    [Test]
    public void MatchesKnownPopulationDeviation()
    {
        var state = CompletedState(-0.02, -0.01, 0, 0.01, 0.02);

        Assert.That(state.AverageTimingError, Is.EqualTo(0).Within(1e-12));
        Assert.That(state.StandardTimingError, Is.EqualTo(Math.Sqrt(0.0002)).Within(1e-12));
    }

    [Test]
    public void LargeConstantChartHasZeroDeviation()
    {
        var state = CompletedState(new double[16000]);

        Assert.That(state.StandardTimingError, Is.EqualTo(0));
    }

    [Test]
    public void StillRequiresCompletedState()
    {
        var state = CompletedState(0);
        state.IsCompleted = false;

        Assert.Throws<InvalidOperationException>(() => { var value = state.StandardTimingError; });
    }

    private static GameState CompletedState(params double[] errors)
    {
        var state = (GameState) FormatterServices.GetUninitializedObject(typeof(GameState));
        var judgements = new Dictionary<int, NoteJudgement>();
        for (var i = 0; i < errors.Length; i++)
        {
            judgements[i] = new NoteJudgement {IsJudged = true, Error = errors[i]};
        }
        typeof(GameState).GetProperty(nameof(GameState.Judgements)).SetValue(state, judgements);
        state.IsCompleted = true;
        return state;
    }
}
