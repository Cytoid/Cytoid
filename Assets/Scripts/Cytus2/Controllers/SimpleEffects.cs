using Cytus2.Models;
using Cytus2.Views;
using UnityEngine;

namespace Cytus2.Controllers
{
    public class SimpleEffects : SingletonMonoBehavior<SimpleEffects>
    {
        public ParticleSystem ClearFx;
        public ParticleSystem ClearDragFx;
        public ParticleSystem MissFx;
        public ParticleSystem HoldFx;

        private SimpleVisualOptions options;

        protected override void Awake()
        {
            base.Awake();
            options = SimpleVisualOptions.Instance;
        }

        public void PlayClearFx(SimpleNoteView noteView, NoteGrading grading, float timeUntilComplete,
            bool earlyLateIndicator)
        {
            if (grading == NoteGrading.Undetermined) return;
            
            var at = noteView.Note.Note.position;
            var clearFx = ClearFx;
            if (noteView is DragChildNoteView || noteView is DragHeadNoteView)
            {
                clearFx = ClearDragFx;
            }

            if (noteView.Note.Note.type == NoteType.Hold)
            {
                at = new Vector3(at.x, ScannerView.Instance.transform.position.y, at.z);
            }

            if (grading == NoteGrading.Miss)
            {
                var fx = Instantiate(MissFx, at, Quaternion.identity);
                fx.Stop();

                var mainModule = fx.main;
                mainModule.simulationSpeed = 0.3f;
                mainModule.duration = mainModule.duration / 0.3f;
                mainModule.startColor = options.MissColor;

                if (noteView.Note.Note.type == NoteType.DragHead || noteView.Note.Note.type == NoteType.DragChild)
                    fx.transform.localScale = new Vector3(2, 2, 2);

                fx.Play();
                Destroy(fx.gameObject, fx.main.duration);
            }
            else
            {
                var fx = Instantiate(clearFx, at, Quaternion.identity);
                fx.Stop();

                if (!(noteView is DragChildNoteView) && !(noteView is DragHeadNoteView))
                {
                    if (earlyLateIndicator)
                    {
                        if (grading != NoteGrading.Perfect)
                        {
                            fx.transform.GetChild(0).GetChild(timeUntilComplete > 0 ? 1 : 0).gameObject
                                .SetActive(false);
                        }
                        else
                        {
                            fx.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                            fx.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        fx.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                        fx.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                    }
                }

                var speed = 1f;
                var color = options.PerfectColor;
                switch (grading)
                {
                    case NoteGrading.Great:
                        speed = 0.9f;
                        color = options.GreatColor;
                        break;
                    case NoteGrading.Good:
                        speed = 0.7f;
                        color = options.GoodColor;
                        break;
                    case NoteGrading.Bad:
                        speed = 0.5f;
                        color = options.BadColor;
                        break;
                }

                var mainModule = fx.main;
                mainModule.simulationSpeed = speed;
                mainModule.duration = mainModule.duration / speed;
                mainModule.startColor = color.WithAlpha(1);

                if (noteView.Note.Note.type == NoteType.DragHead || noteView.Note.Note.type == NoteType.DragChild)
                    fx.transform.localScale = new Vector3(3f, 3f, 3f);

                fx.Play();
                Destroy(fx.gameObject, fx.main.duration);
            }
        }

        public void PlayHoldFx(SimpleNoteView noteView)
        {
            var fx = Instantiate(HoldFx, noteView.Note.transform);

            var newPos = fx.transform.position;
            newPos.z -= 0.2f;
            fx.transform.position = newPos;

            fx.Stop();

            var mainModule = fx.main;
            mainModule.startColor = noteView.Fill.color;

            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
        
    }
}