using System.Linq;
using NUnit.Framework;

public class TmpRichTextSanitizerTests
{
    [Test]
    public void KeepsOnlyWhitelistedTagsAndNormalizesQuotedValues()
    {
        var result = TmpRichTextSanitizer.Sanitize(
            "<b>B</b><i>I</i><u>U</u><s>S</s>" +
            "<size=\"150%\">Size</size><color='#3366CC'>Color</color>");

        Assert.That(result, Is.EqualTo(
            "<b>B</b><i>I</i><u>U</u><s>S</s>" +
            "<size=42>Size</size><color=#3366CC>Color</color>"));
    }

    [Test]
    public void NormalizesAndClampsEverySupportedSizeForm()
    {
        var result = TmpRichTextSanitizer.Sanitize(
            "<size=1000>A</size>|<size=10%>B</size>|<size=9em>C</size>|" +
            "<size=+100>D</size>|<size=-10>E</size>");

        Assert.That(result, Is.EqualTo(
            "<size=72>A</size>|<size=14>B</size>|<size=70>C</size>|" +
            "<size=72>D</size>|<size=18>E</size>"));
    }

    [Test]
    public void RemovesInvalidSizeWrappersButKeepsTheirText()
    {
        var result = TmpRichTextSanitizer.Sanitize(
            "<size=0>Zero</size><size=-100>Negative</size><size=NaN>Nan</size>");

        Assert.That(result, Is.EqualTo("ZeroNegativeNan"));
    }

    [Test]
    public void RemovesUnsupportedAndMalformedTagsButKeepsText()
    {
        var result = TmpRichTextSanitizer.Sanitize(
            "<sprite=1><link=unsafe>Text</link><b class=x>Bold</b>");

        Assert.That(result, Is.EqualTo("TextBold"));
    }

    [Test]
    public void LimitsNestingAndTextLengthAndBalancesOutput()
    {
        var nested = string.Concat(Enumerable.Repeat("<b>", 10)) +
                     new string('x', TmpRichTextSanitizer.MaxTextLength + 20) +
                     string.Concat(Enumerable.Repeat("</b>", 10));

        var result = TmpRichTextSanitizer.Sanitize(nested);

        Assert.That(result.Count(c => c == '<'),
            Is.EqualTo(TmpRichTextSanitizer.MaxNestingDepth * 2));
        Assert.That(result.Count(c => c == 'x'), Is.EqualTo(TmpRichTextSanitizer.MaxTextLength));
        Assert.That(result, Does.EndWith(string.Concat(
            Enumerable.Repeat("</b>", TmpRichTextSanitizer.MaxNestingDepth))));
    }
}
