using System.Linq.Expressions;
using UnityEngine;

public class NoteRenderer
{
    public Note Note { get; }
    public Game Game => Note.Game;

    protected CircleCollider2D Collider;
    protected float InitialColliderRadius;

    public NoteRenderer(Note note)
    {
        Note = note;
        Collider = note.gameObject.GetComponent<CircleCollider2D>();
        Collider.enabled = true;
        InitialColliderRadius = Collider.radius;
    }

    public virtual void OnLateUpdate()
    {
        Collider.radius = InitialColliderRadius * Note.Game.Config.NoteHitboxMultiplier /
                          Note.Game.Config.ChartNoteSizeMultiplier;
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