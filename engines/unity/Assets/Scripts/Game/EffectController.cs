using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EffectController : MonoBehaviour
{
    public Game game;
    public GameObject effectParent;

    public NoteRippleEffect noteRippleEffect;

    public ParticleSystem clearFx;
    public ParticleSystem clearDragFx;
    public ParticleSystem missFx;
    public ParticleSystem holdFx;

    public Transform EffectParentTransform { get; private set; }
    
    private float clearEffectSizeMultiplier;

    /// <summary>Outer diameter at ring spawn; matches legacy FlatFX ripple preset.</summary>
    const float ClearRingStartDiameter = 1f;

    private void Awake()
    {
        EffectParentTransform = effectParent.transform;
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
    }

    public void OnGameLoaded()
    {
        clearEffectSizeMultiplier = Context.Player.Settings.ClearEffectsSize;
    }

    public void PlayRippleEffect(Vector3 position)
    {
        noteRippleEffect.PlayRing(
            position,
            lifetime: 2f,
            sectorCount: 96,
            startColor: Color.white.WithAlpha(1),
            endColor: Color.white.WithAlpha(0),
            startDiameter: ClearRingStartDiameter,
            endDiameter: 6f,
            startThickness: 0.666f,
            endThickness: 0.111f);
    }

    public void PlayClearEffect(NoteRenderer noteRenderer, NoteGrade grade, float timeUntilEnd)
    {
        PlayClearEffect(noteRenderer, grade, timeUntilEnd, Context.Player.Settings.DisplayEarlyLateIndicators);
    }

    public void PlayClearEffect(NoteRenderer noteRenderer, NoteGrade grade, float timeUntilEnd, bool earlyLateIndicator)
    {
        if (game.State.Mode == GameMode.GlobalCalibration)
        {
            return;
        }
        
        var color = game.Config.NoteGradeEffectColors[grade];
        var at = noteRenderer.Note.transform.position;
        if (noteRenderer.Note.Type == NoteType.Hold || noteRenderer.Note.Type == NoteType.LongHold)
        {
            if (noteRenderer.Note.Model.style == 1)
            {
                at = new Vector3(at.x, Scanner.Instance.transform.position.y, at.z);
            }
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
        
        var isDragType = noteRenderer.Note.Type == NoteType.DragHead || noteRenderer.Note.Type == NoteType.DragChild || 
                     noteRenderer.Note.Type == NoteType.CDragChild;
        
        var scale = noteRenderer.Note.Model.Override.SizeMultiplier;
        if (noteRenderer.Note.Model.size != double.MinValue)
        {
            scale *= (float) noteRenderer.Note.Model.size;
        }
        scale *= (float) noteRenderer.Game.Chart.Model.size;
        var endDiameter = (isDragType ? 4f : 5f) * noteRenderer.Game.Config.GlobalNoteSizeMultiplier * (1 + clearEffectSizeMultiplier) * scale;

        noteRippleEffect.PlayRing(
            at,
            lifetime: 0.4f / speed,
            sectorCount: noteRenderer.Note.Type == NoteType.Flick ? 4 : 24,
            startColor: color.WithAlpha(1),
            endColor: color.WithAlpha(0),
            startDiameter: ClearRingStartDiameter,
            endDiameter: endDiameter,
            startThickness: 1.333f,
            endThickness: 0.333f);

        if (grade == NoteGrade.Miss)
        {
            var fx = game.ObjectPool.SpawnEffect(Effect.Miss, at);
            fx.Stop();

            var mainModule = fx.main;
            mainModule.simulationSpeed = 0.3f;
            mainModule.duration /= 0.3f;
            mainModule.startColor = game.Config.NoteGradeEffectColors[grade];

            if (isDragType) fx.transform.localScale = new Vector3(2, 2, 2);

            fx.Play();
            AwaitAndCollect(Effect.Miss, fx);
        }
        else
        {
            var clearEffect = isDragType ? Effect.ClearDrag : Effect.Clear;

            var fx = game.ObjectPool.SpawnEffect(clearEffect, at);
            fx.Stop();

            if (!isDragType)
            {
                var t = fx.transform.GetChild(0);
                var early = t.GetChild(0);
                var late = t.GetChild(1);
                if (earlyLateIndicator)
                {
                    if (grade != NoteGrade.Perfect)
                    {
                        t.gameObject.SetActive(true);
                        if (timeUntilEnd > 0)
                        {
                            early.gameObject.SetActive(true);
                            late.gameObject.SetActive(false);
                        }
                        else
                        {
                            early.gameObject.SetActive(false);
                            late.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        t.gameObject.SetActive(false);
                    }
                }
                else
                {
                    t.gameObject.SetActive(false);
                }
            }

            var mainModule = fx.main;
            mainModule.simulationSpeed = speed;
            mainModule.duration /= speed;
            mainModule.startColor = color.WithAlpha(1);

            if (isDragType) fx.transform.localScale = new Vector3(3f, 3f, 3f);

            fx.Play();
            AwaitAndCollect(clearEffect, fx);
        }
    }

    public void PlayClassicHoldEffect(ClassicNoteRenderer noteRenderer)
    {
        var fx = game.ObjectPool.SpawnEffect(Effect.Hold, new Vector3(0, 0, -0.2f), noteRenderer.Note.gameObject.transform);
        fx.Stop();

        var mainModule = fx.main;
        mainModule.startColor = noteRenderer.Fill.color;

        fx.Play();
        AwaitAndCollect(Effect.Hold, fx);
    }
    
    private async void AwaitAndCollect(Effect effect, ParticleSystem particle)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(particle.main.duration));
        if (this == null) return;
        game.ObjectPool.CollectEffect(effect, particle);
    }

    public ParticleSystem GetPrefab(Effect effect)
    {
        switch (effect)
        {
            case Effect.Clear:
                return clearFx;
            case Effect.ClearDrag:
                return clearDragFx;
            case Effect.Miss:
                return missFx;
            case Effect.Hold:
                return holdFx;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
        }
    }
    
    public enum Effect
    {
        Clear, ClearDrag, Miss, Hold
    }
}
