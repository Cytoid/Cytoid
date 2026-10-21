using System;
using System.Reflection;
using NUnit.Framework;

public class FlatColorQuantizationTests
{
    private static readonly MethodInfo Quantize = typeof(FlatPalette).GetMethod(
        "QuantizedRgb", BindingFlags.Static | BindingFlags.NonPublic, null, new[] {typeof(Rgba)}, null);

    [Test]
    public void QuantizationMatchesHexRoundTripAtEveryChannelBoundary()
    {
        Assert.That(Quantize, Is.Not.Null);
        for (var i = 0; i < 255; i++)
        {
            var boundary = (float) ((i + 0.5) / 255);
            var bits = BitConverter.ToInt32(BitConverter.GetBytes(boundary), 0);
            for (var offset = -2; offset <= 2; offset++)
            {
                var channel = BitConverter.ToSingle(BitConverter.GetBytes(bits + offset), 0);
                AssertQuantization(new Rgba(channel, 1 - channel, channel, 0.3f));
            }
        }
        AssertQuantization(new Rgba(0, 1, 0.5f, 0));
    }

    [TestCase("perfect", "#5BC0EB")]
    [TestCase("great", "#FDE74C")]
    [TestCase("good", "#9BC53D")]
    [TestCase("bad", "#E55934")]
    [TestCase("miss", "#6B6F7A")]
    public void GradeColorsKeepTheirExactChannels(string grade, string hex)
    {
        AssertColor(FlatTokens.GradeColor(grade), FlatColor.ParseHex(hex));
    }

    [Test]
    public void UnknownGradeStillThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FlatTokens.GradeColor("unknown"));
    }

    private static void AssertQuantization(Rgba input)
    {
        var expected = FlatColor.ParseHex(FlatColor.ToHex(input));
        var actual = (Rgba) Quantize.Invoke(null, new object[] {input});
        AssertColor(actual, expected);
    }

    private static void AssertColor(Rgba actual, Rgba expected)
    {
        Assert.That(actual.R, Is.EqualTo(expected.R));
        Assert.That(actual.G, Is.EqualTo(expected.G));
        Assert.That(actual.B, Is.EqualTo(expected.B));
        Assert.That(actual.A, Is.EqualTo(expected.A));
    }
}
