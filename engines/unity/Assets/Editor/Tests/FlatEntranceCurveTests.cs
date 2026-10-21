using System;
using System.Reflection;
using NUnit.Framework;

public class FlatEntranceCurveTests
{
    private static readonly MethodInfo SharedCurve = typeof(FlatNoteRendererBase).GetMethod(
        "SharedSizeCurve", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo Scale = typeof(FlatNoteRendererBase).GetMethod(
        "ScaleAt", BindingFlags.Static | BindingFlags.NonPublic);

    [TestCase(SizeKind.Click)]
    [TestCase(SizeKind.DragHead)]
    [TestCase(SizeKind.DragChild)]
    public void SharedCurvePreservesProgressAndPerNoteInitialScale(SizeKind kind)
    {
        var shared = (Func<double, double>) SharedCurve.Invoke(null, new object[] {kind});
        Assert.That(SharedCurve.Invoke(null, new object[] {kind}), Is.SameAs(shared));
        var tokens = FlatTokens.SizeCurve(kind);
        var fresh = FlatEase.RiseSettleCurve(tokens, FlatTokens.SizeCurveShape);
        for (var i = -1000; i <= 11000; i++)
        {
            var progress = i / 10000.0;
            if (BitConverter.DoubleToInt64Bits(shared(progress)) != BitConverter.DoubleToInt64Bits(fresh(progress)))
                Assert.Fail("Curve differs at progress " + progress);
        }
        foreach (var progress in new[] {-0.1f, 0f, 0.2f, 0.7f, 0.95f, 1f, 1.1f})
        foreach (var initial in new[] {-0.5f, 0f, 0.3f, 1f, 1.8f})
        {
            var value = fresh(progress);
            var expected = (float) (initial == 1f ? value :
                initial + (1 - initial) * ((value - tokens.Spawn) / (1 - tokens.Spawn)));
            var actual = (float) Scale.Invoke(null, new object[] {progress, shared, tokens.Spawn, initial});
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
