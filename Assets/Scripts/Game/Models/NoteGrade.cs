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
    public static float GetScoreWeight(this NoteGrade grade, bool ranked)
    {
        if (!ranked)
        {
            switch (grade)
            {
                case NoteGrade.Perfect:
                    return 1f;
                case NoteGrade.Great:
                    return 1f;
                case NoteGrade.Good:
                    return 0.7f;
                case NoteGrade.Bad:
                    return 0.3f;
                case NoteGrade.Miss:
                    return 0f;
            }
        }
        else
        {
            switch (grade)
            {
                case NoteGrade.Perfect:
                    return 1f;
                case NoteGrade.Great:
                    return 0.9f;
                case NoteGrade.Good:
                    return 0.5f;
                case NoteGrade.Bad:
                    return 0.1f;
                case NoteGrade.Miss:
                    return 0f;
            }
        }

        return 0f;
    }

    public static float GetAccuracyWeight(this NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Perfect:
                return 1f;
            case NoteGrade.Great:
                return 0.7f;
            case NoteGrade.Good:
                return 0.3f;
            case NoteGrade.Bad:
                return 0f;
            case NoteGrade.Miss:
                return 0f;
        }

        return 0f;
    }
}