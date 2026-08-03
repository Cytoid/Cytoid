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

    /// <summary>
    /// Max clear FX (ripple + particles) for one co-located drag-stack settle
    /// (input batch or deferred armed co-clear). Does not cap Click/Flick/Hold/Auto.
    /// </summary>
    public const int MaxClearFxPerDragBatch = 3;

    /// <summary>
    /// Max hit sounds (and gated haptics) for one co-located drag-stack settle.
    /// </summary>
    public const int MaxHitSoundsPerDragBatch = 1;

    /// <summary>
    /// Active only while a drag-stack budget is open (<see cref="BeginClearBatch"/> or
    /// <see cref="EnsureDragStackBudget"/>). Cap &lt; 0 means uncapped.
    /// </summary>
    private int batchDepth;
    private int softBudgetFrame = -1;
    private int batchFxCap = -1;
    private int batchFxUsed;
    private int batchSoundCap = -1;
    private int batchSoundUsed;

    private void Awake()
    {
        EffectParentTransform = effectParent.transform;
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
    }

    public void OnGameLoaded()
    {
        clearEffectSizeMultiplier = Context.Player.Settings.ClearEffectsSize;
    }

    private void ExpireSoftBudgetIfStale()
    {
        if (softBudgetFrame < 0 || batchDepth > 0) return;
        if (softBudgetFrame == Time.frameCount) return;
        softBudgetFrame = -1;
        batchFxCap = -1;
        batchSoundCap = -1;
    }

    /// <summary>
    /// Caps clear FX / hit sounds for one stacked-drag settle batch only.
    /// Nested calls are not supported; always pair with <see cref="EndClearBatch"/>.
    /// </summary>
    public void BeginClearBatch(int maxFx, int maxSounds)
    {
        batchDepth++;
        if (batchDepth > 1) return;
        softBudgetFrame = -1;
        batchFxCap = maxFx;
        batchFxUsed = 0;
        batchSoundCap = maxSounds;
        batchSoundUsed = 0;
    }

    public void EndClearBatch()
    {
        if (batchDepth <= 0) return;
        batchDepth--;
        if (batchDepth > 0) return;
        batchFxCap = -1;
        batchSoundCap = -1;
    }

    /// <summary>
    /// Opens the same drag-stack caps for the rest of this frame when deferred armed
    /// stack notes co-clear outside <see cref="BeginClearBatch"/>. No-op if a hard
    /// batch is already active or the soft budget was already opened this frame.
    /// </summary>
    public void EnsureDragStackBudget(int maxFx, int maxSounds)
    {
        ExpireSoftBudgetIfStale();
        if (batchDepth > 0) return;
        if (softBudgetFrame == Time.frameCount) return;
        softBudgetFrame = Time.frameCount;
        batchFxCap = maxFx;
        batchFxUsed = 0;
        batchSoundCap = maxSounds;
        batchSoundUsed = 0;
    }

    /// <returns>
    /// False only when a drag-stack budget is active and its FX quota is exhausted.
    /// Outside a drag-stack budget, always true.
    /// </returns>
    public bool TryConsumeClearFx()
    {
        ExpireSoftBudgetIfStale();
        if (batchFxCap < 0) return true;
        if (batchFxUsed >= batchFxCap) return false;
        batchFxUsed++;
        return true;
    }

    /// <returns>
    /// False only when a drag-stack budget is active and its hit-sound quota is exhausted.
    /// Outside a drag-stack budget, always true.
    /// </returns>
    public bool TryConsumeHitSound()
    {
        ExpireSoftBudgetIfStale();
        if (batchSoundCap < 0) return true;
        if (batchSoundUsed >= batchSoundCap) return false;
        batchSoundUsed++;
        return true;
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

        if (!TryConsumeClearFx()) return;
        
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
