public class BaseChart
{

    public readonly string checksum;

    public BaseChart(string text)
    {
        var checksumSource = Parse(text);
        checksum = Checksum.From(checksumSource);
    }

    public virtual string Parse(string text)
    {
        return string.Empty;
    }

}