using UnityEngine;

public class EffectController : MonoBehaviour
{
    public Game game;
    
    public ParticleSystem clearFx;
    public ParticleSystem clearDragFx;
    public ParticleSystem missFx;
    public ParticleSystem holdFx;

    public void PlayClearEffect(NoteRenderer noteRenderer, NoteGrade grade, float timeUntilEnd)
    {
        PlayClearEffect(noteRenderer, grade, timeUntilEnd, Context.LocalPlayer.ShowEarlyLateIndicator);
    }
    
    public void PlayClearEffect(NoteRenderer noteRenderer, NoteGrade grade, float timeUntilEnd, bool earlyLateIndicator)
    {
        var at = noteRenderer.Note.Model.position;
        var clearFx = this.clearFx;
        if (noteRenderer is DragChildNoteRenderer || noteRenderer is DragHeadNoteRenderer)
        {
            clearFx = clearDragFx;
        }

        if (noteRenderer.Note.Type == NoteType.Hold)
        {
            at = new Vector3(at.x, ScannerElement.Instance.transform.position.y, at.z);
        }

        if (grade == NoteGrade.Miss)
        {
            var fx = Instantiate(missFx, at, Quaternion.identity);
            fx.Stop();

            var mainModule = fx.main;
            mainModule.simulationSpeed = 0.3f;
            mainModule.duration /= 0.3f;
            mainModule.startColor = game.Config.NoteGradeEffectColors[grade];

            if (noteRenderer.Note.Type == NoteType.DragHead || noteRenderer.Note.Type == NoteType.DragChild)
                fx.transform.localScale = new Vector3(2, 2, 2);

            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
        else
        {
            var fx = Instantiate(clearFx, at, Quaternion.identity);
            fx.Stop();

            if (!(noteRenderer is DragChildNoteRenderer) && !(noteRenderer is DragHeadNoteRenderer))
            {
                if (earlyLateIndicator)
                {
                    if (grade != NoteGrade.Perfect)
                    {
                        fx.transform.GetChild(0).GetChild(timeUntilEnd > 0 ? 1 : 0).gameObject
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
            
            var color = game.Config.NoteGradeEffectColors[grade];
            var speed = 1f;
            switch (grade)
            {
                case NoteGrade.Great:
                    speed = 0.9f;
                    break;
                case NoteGrade.Good:
                    speed = 0.7f;
                    break;
                case NoteGrade.Bad:
                    speed = 0.5f;
                    break;
            }

            var mainModule = fx.main;
            mainModule.simulationSpeed = speed;
            mainModule.duration /= speed;
            mainModule.startColor = color.WithAlpha(1);

            if (noteRenderer.Note.Type == NoteType.DragHead || noteRenderer.Note.Type == NoteType.DragChild)
                fx.transform.localScale = new Vector3(3f, 3f, 3f);

            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
    }

    public void PlayClassicHoldEffect(ClassicNoteRenderer noteRenderer)
    {
        var fx = Instantiate(holdFx, noteRenderer.Note.transform);

        var fxTransform = fx.transform;
        var newPos = fxTransform.position;
        newPos.z -= 0.2f;
        fxTransform.position = newPos;

        fx.Stop();

        var mainModule = fx.main;
        mainModule.startColor = noteRenderer.Fill.color;

        fx.Play();
        Destroy(fx.gameObject, fx.main.duration);
    }
}