using System;

public enum NoteGrading
{
    Undetermined,
    Perfect,
    Great,
    Good,
    Bad,
    Miss
}

public static class RankExtensions
{

    public static float ScoreWeight(this NoteGrading grading, bool ranked)
    {
        if (!ranked)
        {
            switch (grading)
            {
                case NoteGrading.Perfect:
                    return 1f;
                case NoteGrading.Great:
                    return 1f;
                case NoteGrading.Good:
                    return 0.7f;
                case NoteGrading.Bad:
                    return 0.3f;
                case NoteGrading.Miss:
                    return 0f;
            }
        }
        else
        {
            switch (grading)
            {
                case NoteGrading.Perfect:
                    return 1f;
                case NoteGrading.Great:
                    return 0.9f;
                case NoteGrading.Good:
                    return 0.5f;
                case NoteGrading.Bad:
                    return 0.1f;
                case NoteGrading.Miss:
                    return 0f;
            }
        }
        return 0f;
    }
    
    public static float TpWeight(this NoteGrading grading)
    {
        switch (grading)
        {
            case NoteGrading.Perfect: 
                return 1f;
            case NoteGrading.Great:
                return 0.7f;
            case NoteGrading.Good:
                return 0.3f;
            case NoteGrading.Bad:
                return 0f;
            case NoteGrading.Miss:
                return 0f;
        }
        return 0f;
    }
    
}