using System;
using UnityEngine;

public class EffectController : MonoBehaviour
{
    public Game game;
    public GameObject effectParent;

    public FlatFX flatFx;

    public ParticleSystem clearFx;
    public ParticleSystem clearDragFx;
    public ParticleSystem missFx;
    public ParticleSystem holdFx;

    private float clearEffectSizeMultiplier;

    private void Awake()
    {
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
    }

    public void OnGameLoaded()
    {
        clearEffectSizeMultiplier = Context.Player.Settings.ClearEffectsSize;
    }

    public void PlayClearEffect(NoteRenderer noteRenderer, NoteGrade grade, float timeUntilEnd)
    {
        PlayClearEffect(noteRenderer, grade, timeUntilEnd, Context.Player.Settings.DisplayEarlyLateIndicators);
    }

    public void PlayClearEffect(NoteRenderer noteRenderer, NoteGrade grade, float timeUntilEnd, bool earlyLateIndicator)
    {
        var color = game.Config.NoteGradeEffectColors[grade];
        var at = noteRenderer.Note.transform.position;
        if (noteRenderer.Note.Type == NoteType.Hold || noteRenderer.Note.Type == NoteType.LongHold)
        {
            at = new Vector3(at.x, Scanner.Instance.transform.position.y, at.z);
        }
        
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
            case NoteGrade.Miss:
                speed = 0.3f;
                break;
        }
        
        var settings = flatFx.settings[1];
        settings.lifetime = 0.4f / speed;
        settings.sectorCount = noteRenderer.Note.Type == NoteType.Flick ? 4 : 100;
        settings.start.innerColor = settings.start.outerColor = color.WithAlpha(1);
        settings.end.innerColor = settings.end.outerColor = color.WithAlpha(0);
        settings.end.size = (noteRenderer.Note.Type == NoteType.DragHead || noteRenderer.Note.Type == NoteType.DragChild ||
                             noteRenderer.Note.Type == NoteType.CDragHead || noteRenderer.Note.Type == NoteType.CDragChild
                                ? 4f
                                : 5f) * noteRenderer.Game.Config.NoteSizeMultiplier * (1 + clearEffectSizeMultiplier);
        settings.start.thickness = 1.333f;
        settings.end.thickness = 0.333f;
        flatFx.AddEffect(at, 1);
        
        var isDrag = noteRenderer is ClassicDragChildNoteRenderer || noteRenderer is ClassicDragHeadNoteRenderer;
        if (noteRenderer is ClassicDragHeadNoteRenderer dragHeadNoteRenderer &&
            dragHeadNoteRenderer.DragHeadNote.IsCDrag)
        {
            isDrag = false;
        }

        var clearFx = isDrag ? clearDragFx : this.clearFx;

        if (grade == NoteGrade.Miss)
        {
            var fx = Instantiate(missFx, at, Quaternion.identity);
            fx.transform.SetParent(effectParent.transform, true);
            fx.Stop();

            var mainModule = fx.main;
            mainModule.simulationSpeed = 0.3f;
            mainModule.duration /= 0.3f;
            mainModule.startColor = game.Config.NoteGradeEffectColors[grade];

            if (noteRenderer.Note.Type == NoteType.DragHead || noteRenderer.Note.Type == NoteType.DragChild ||
                noteRenderer.Note.Type == NoteType.CDragHead || noteRenderer.Note.Type == NoteType.CDragChild)
                fx.transform.localScale = new Vector3(2, 2, 2);

            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
        else
        {
            var fx = Instantiate(clearFx, at, Quaternion.identity);
            fx.transform.SetParent(effectParent.transform, true);
            fx.Stop();

            if (!isDrag)
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

            var mainModule = fx.main;
            mainModule.simulationSpeed = speed;
            mainModule.duration /= speed;
            mainModule.startColor = color.WithAlpha(1);

            if (noteRenderer.Note.Type == NoteType.DragHead || noteRenderer.Note.Type == NoteType.DragChild ||
                noteRenderer.Note.Type == NoteType.CDragHead || noteRenderer.Note.Type == NoteType.CDragChild)
                fx.transform.localScale = new Vector3(3f, 3f, 3f);

            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }
    }

    public void PlayClassicHoldEffect(ClassicNoteRenderer noteRenderer)
    {
        var fx = Instantiate(holdFx, noteRenderer.Note.transform);

        var fxTransform = fx.transform;
        fxTransform.SetParent(effectParent.transform, true);
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