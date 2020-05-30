using System;
using Cytoid.Storyboard.Notes;
using UniRx.Async;
using UnityEngine;

namespace Cytoid.Storyboard.Sprites
{
    public class NoteControllerRenderer : StoryboardComponentRenderer<NoteController, NoteControllerState>
    {
        
        public ChartModel.Note Note { get; private set; }

        public override Transform Transform => notePlaceholderTransform;
        
        public override bool IsOnCanvas => false;

        private Transform notePlaceholderTransform;
        private GameObject noteGameObject;
        
        public NoteControllerRenderer(StoryboardRenderer mainRenderer, NoteController component) : base(mainRenderer, component)
        {
        }

        public override StoryboardRendererEaser<NoteControllerState> CreateEaser() => new NoteControllerEaser(this);

        public override async UniTask Initialize()
        {
            if (Component.ParentId != null) throw new InvalidOperationException($"Storyboard: NoteController {Component.Id} cannot have a parent");
            
            var note = Component.States[0].Note;
            if (note == null) throw new ArgumentNullException();
            if (!MainRenderer.Game.Chart.Model.note_map.ContainsKey(note.Value)) throw new ArgumentException($"Storyboard: Note {note.Value} does not exist");
            
            Note = MainRenderer.Game.Chart.Model.note_map[note.Value];

            // TODO: Optimize this? Don't generate transforms if not in use
            notePlaceholderTransform = new GameObject("NoteControllerPlaceholder_" + Note.id).transform;
            Clear();
        }

        public override void Clear()
        {
            notePlaceholderTransform.localPosition = Vector3.zero;
        }

        public override void Dispose()
        {
            if (notePlaceholderTransform != null)
            {
                UnityEngine.Object.Destroy(notePlaceholderTransform.gameObject);
            }
            notePlaceholderTransform = null;
            noteGameObject = null;
        }
        
        public override void Update(NoteControllerState fromState, NoteControllerState toState)
        {
            base.Update(fromState, toState);
            if (noteGameObject == null && MainRenderer.Game.Notes.ContainsKey(Note.id))
            {
                noteGameObject = MainRenderer.Game.Notes[Note.id].gameObject;
            }
            if (noteGameObject != null && !MainRenderer.Game.Notes.ContainsKey(Note.id))
            {
                noteGameObject = null;
            }
            if (noteGameObject == null)
            {
                notePlaceholderTransform.position = Vector3.zero;
                return;
            }
            notePlaceholderTransform.position = noteGameObject.transform.position;
        }

    }
}