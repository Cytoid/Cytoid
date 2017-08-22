using System;

public enum NoteRanking
{
    Undetermined,
    Perfect,
    Excellent,
    Good,
    Bad,
    Miss
}

public static class RankExtensions
{

    public static float ScoreWeight(this NoteRanking ranking)
    {
        switch (ranking)
        {
            case NoteRanking.Perfect: 
                return 1f;
            case NoteRanking.Excellent:
                return 1f;
            case NoteRanking.Good:
                return 0.7f;
            case NoteRanking.Bad:
                return 0.3f;
            case NoteRanking.Miss:
                return 0f;
        }
        return 0f;
    }
    
    public static float TpWeight(this NoteRanking ranking)
    {
        switch (ranking)
        {
            case NoteRanking.Perfect: 
                return 1f;
            case NoteRanking.Excellent:
                return 0.7f;
            case NoteRanking.Good:
                return 0.3f;
            case NoteRanking.Bad:
                return 0f;
            case NoteRanking.Miss:
                return 0f;
        }
        return 0f;
    }
    
}