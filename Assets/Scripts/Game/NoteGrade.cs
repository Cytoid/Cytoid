public enum NoteGrade
{
    Perfect = 4,
    Great = 3,
    Good = 2,
    Bad = 1,
    Miss = 0,
    None = -1
}

public static class NoteGradeExtensions
{
    public static double GetScoreWeight(this NoteGrade grade, bool ranked)
    {
        if (!ranked)
        {
            switch (grade)
            {
                case NoteGrade.Perfect:
                    return 1.0;
                case NoteGrade.Great:
                    return 1.0;
                case NoteGrade.Good:
                    return 0.7;
                case NoteGrade.Bad:
                    return 0.3;
                case NoteGrade.Miss:
                    return 0;
            }
        }
        else
        {
            switch (grade)
            {
                case NoteGrade.Perfect:
                    return 1.0;
                case NoteGrade.Great:
                    return 0.9;
                case NoteGrade.Good:
                    return 0.5;
                case NoteGrade.Bad:
                    return 0.1;
                case NoteGrade.Miss:
                    return 0f;
            }
        }

        return 0f;
    }

    public static double GetAccuracyWeight(this NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Perfect:
                return 1.0;
            case NoteGrade.Great:
                return 0.7;
            case NoteGrade.Good:
                return 0.3;
            case NoteGrade.Bad:
                return 0;
            case NoteGrade.Miss:
                return 0;
        }

        return 0f;
    }
}