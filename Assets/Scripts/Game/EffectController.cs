using System;
using Cysharp.Threading.Tasks;
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

    public Transform EffectParentTransform { get; private set; }
    
    private float clearEffectSizeMultiplier;

    private void Awake()
    {
        EffectParentTransform = effectParent.transform;
        // game.onGameUpdate.AddListener(_ => OnGameUpdate());
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
    }

    public void OnGameUpdate()
    {
        var settings = flatFx.settings[1];
        settings.lifetime = 0.4f / 1;
        settings.sectorCount = 100;
        settings.start.innerColor = settings.start.outerColor = game.Config.NoteGradeEffectColors[NoteGrade.Perfect].WithAlpha(1);
        settings.end.innerColor = settings.end.outerColor = game.Config.NoteGradeEffectColors[NoteGrade.Perfect].WithAlpha(0);
        settings.end.size = 5f;
        settings.start.thickness = 1.333f;
        settings.end.thickness = 0.333f;
        flatFx.AddEffect(new Vector2(0, 0), 1);
    }

    public void OnGameLoaded()
    {
        clearEffectSizeMultiplier = Context.Player.Settings.ClearEffectsSize;
    }

    public void PlayRippleEffect(Vector3 position)
    {
        var settings = flatFx.settings[1];
        settings.lifetime = 2;
        settings.sectorCount = 96;
        settings.start.innerColor = settings.start.outerColor = Color.white.WithAlpha(1);
        settings.end.innerColor = settings.end.outerColor = Color.white.WithAlpha(0);
        settings.end.size = 6;
        settings.start.thickness = 0.666f;
        settings.end.thickness = 0.111f;
        flatFx.AddEffect(position, 1);
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
        
        var settings = flatFx.settings[1];
        settings.lifetime = 0.4f / speed;
        settings.sectorCount = noteRenderer.Note.Type == NoteType.Flick ? 4 : 24;
        settings.start.innerColor = settings.start.outerColor = color.WithAlpha(1);
        settings.end.innerColor = settings.end.outerColor = color.WithAlpha(0);
        var scale = noteRenderer.Note.Model.Override.SizeMultiplier;
        if (noteRenderer.Note.Model.size != double.MinValue)
        {
            scale *= (float) noteRenderer.Note.Model.size / (float) noteRenderer.Game.Chart.Model.size;
        }
        settings.end.size = (isDragType ? 4f : 5f) * noteRenderer.Game.Config.GlobalNoteSizeMultiplier * (1 + clearEffectSizeMultiplier) * scale;
        settings.start.thickness = 1.333f;
        settings.end.thickness = 0.333f;
        flatFx.AddEffect(at, 1);

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