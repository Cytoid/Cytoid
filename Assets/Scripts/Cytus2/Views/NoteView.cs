using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{

    public class NoteView
    {

        public GameNote Note;
        protected CircleCollider2D Collider;

        public NoteView(GameNote note)
        {
            Note = note;
            Collider = note.gameObject.GetComponent<CircleCollider2D>();
            Collider.enabled = true;
            Collider.radius *= GameOptions.Instance.HitboxMultiplier;
        }

        public virtual void OnInit(ChartRoot chart, ChartNote note)
        {
        }

        public virtual void OnRender()
        {
        }
        
        public virtual void OnLateUpdate()
        {
        }

        public virtual void OnClear(NoteGrading grading)
        {
        }

        public virtual bool IsRendered()
        {
            return false;
        }

        public bool DoesCollide(Vector2 pos)
        {
            return Collider.OverlapPoint(pos);
        }

    }

}