public class PlayResult
{

    public bool Ranked;
    public double Score;
    public double Tp;
    public int MaxCombo;
    public int PerfectCount;
    public int GreatCount;
    public int GoodCount;
    public int BadCount;
    public int MissCount;

    public int TotalCount
    {
        get { return PerfectCount + GreatCount + GoodCount + BadCount + MissCount; }
    }

}