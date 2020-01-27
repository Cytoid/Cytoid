using System.Linq.Expressions;
using UnityEngine;

public class NoteRenderer
{
    public Note Note { get; }
    public Game Game => Note.Game;

    protected CircleCollider2D Collider;

    public NoteRenderer(Note note)
    {
        Note = note;
        Collider = note.gameObject.GetComponent<CircleCollider2D>();
        Collider.enabled = true;
    }

    public virtual void OnLateUpdate()
    {
        Collider.radius = Note.Game.Config.NoteHitboxSizes[Note.Type] / Note.Game.Config.NoteSizeMultiplier;
        Render();
    }

    protected virtual void Render() => Expression.Empty();

    public virtual void OnNoteLoaded() => Expression.Empty();

    public virtual void OnClear(NoteGrade grade) => Expression.Empty();

    public bool DoesCollide(Vector2 pos)
    {
        return Collider.OverlapPoint(pos);
    }

    public virtual void Cleanup() => Expression.Empty();
    
}