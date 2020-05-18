public enum NoteType
{
    Click = 0,
    Hold = 1,
    LongHold = 2,
    DragHead = 3,
    DragChild = 4,
    Flick = 5,
    CDragHead = 6,
    CDragChild = 7,
}

public static class NoteTypeExtensions
{
    public static float GetDefaultMissThreshold(this NoteType type)
    {
        return type == NoteType.DragChild || type == NoteType.CDragChild ? 0.15f : 0.3f;
    }
}