using System;

[Serializable]
public class Note
{

    public int id;
    public float time;
    public float x;
    public float duration;

    public NoteType type = NoteType.Single;

    [NonSerialized]
    public Note connectedNote;
    public bool isChainHead;

    public Note(int id, float time, float x, float duration, bool isChainHead)
    {
        this.id = id;
        this.time = time;
        this.x = x;
        this.duration = duration;
        this.isChainHead = isChainHead;
    }
    
}

public enum NoteType
{
    Single, Chain, Hold
}