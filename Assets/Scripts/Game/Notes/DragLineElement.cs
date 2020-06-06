using UniRx.Async;
using UnityEngine;

public class DragLineElement : MonoBehaviour
{
    private Game Game { get; set; }
    
    private SpriteRenderer spriteRenderer;
    
    private ChartModel.Note fromNoteModel;
    private ChartModel.Note toNoteModel;

    private bool hasFromNote;
    private Note fromNote;
    private bool hasToNote;
    private Note toNote;
    
    private float introRatio;
    private float outroRatio;

    private float length;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Game game)
    {
        Game = game;
    }

    public void Dispose()
    {
        Destroy(gameObject);
    }

    public async void SetData(ChartModel.Note fromNoteModel, ChartModel.Note toNoteModel)
    {
        this.fromNoteModel = fromNoteModel;
        this.toNoteModel = toNoteModel;
        spriteRenderer.material.SetFloat("_End", 0.0f);
        spriteRenderer.material.SetFloat("_Start", 0.0f);
        await UniTask.DelayFrame(0);
        UpdateTransform();
        spriteRenderer.sortingOrder = fromNoteModel.id;
        Game.onGameUpdate.AddListener(OnGameUpdate);
    }

    private void UpdateTransform()
    {
        if (Game.SpawnedNotes.ContainsKey(fromNoteModel.id))
        {
            if (!hasFromNote)
            {
                hasFromNote = true;
                fromNote = Game.SpawnedNotes[fromNoteModel.id];
            }
        }
        else
        {
            if (hasFromNote)
            {
                hasFromNote = false;
                fromNote = null;
            }
        }
        if (Game.SpawnedNotes.ContainsKey(toNoteModel.id))
        {
            if (!hasToNote)
            {
                hasToNote = true;
                toNote = Game.SpawnedNotes[toNoteModel.id];
            }
        }
        else
        {
            if (hasToNote)
            {
                hasToNote = false;
                toNote = null;
            }
        }

        var fromNotePosition = hasFromNote ? (fromNote is DragHeadNote dragHeadNote ? dragHeadNote.OriginalPosition : fromNote.transform.localPosition) : fromNoteModel.position;
        var toNotePosition = hasToNote ? toNote.transform.localPosition : toNoteModel.position;
        
        var transform = this.transform;
        transform.localPosition = fromNotePosition;
        length = Vector3.Distance(
            fromNotePosition, 
            toNotePosition
        );
        spriteRenderer.material.mainTextureScale = new Vector2(1.0f, length / 0.16f);
        transform.localEulerAngles = hasFromNote ? fromNote.transform.localEulerAngles : fromNoteModel.rotation;
        transform.localScale = new Vector3(1.0f, length / 0.16f);
    }

    private void OnGameUpdate(Game _)
    {
        UpdateTransform();
        
        spriteRenderer.enabled = !Game.State.Mods.Contains(Mod.HideNotes);

        if (Game is PlayerGame)
        {
            spriteRenderer.enabled = outroRatio < 1;
        }
        else
        {
            if (outroRatio >= 1)
            {
                Collect();
                return;
            }
        }

        if (Game.SpawnedNotes.ContainsKey(fromNoteModel.id))
        {
            var note = Game.SpawnedNotes[fromNoteModel.id];
            if (!note.IsCleared)
            {
                if (note.Renderer is ClassicNoteRenderer classicNoteRenderer)
                {
                    var fill = classicNoteRenderer.Fill;
                    spriteRenderer.color = spriteRenderer.color.WithAlpha(fill.enabled ? fill.color.a : 0);
                }
                else
                {
                    var f = 1 - note.TimeUntilStart / (note.Model.start_time - note.Model.intro_time);
                    f = Mathf.Clamp01(f);
                    spriteRenderer.color = Color.white.WithAlpha(f);
                }
            }
        }

        var time = Game.Time;
        introRatio = (fromNoteModel.nextdraglinestoptime - time) /
                     (fromNoteModel.nextdraglinestoptime - fromNoteModel.nextdraglinestarttime);
        outroRatio = (time - fromNoteModel.start_time) / (toNoteModel.start_time - fromNoteModel.start_time);

        if (introRatio > 0 && introRatio < 1)
        {
            spriteRenderer.material.SetFloat("_End", 1.0f - introRatio);
        }
        else if (introRatio <= 0)
        {
            spriteRenderer.material.SetFloat("_End", 1.0f);
        }
        else
        {
            spriteRenderer.material.SetFloat("_End", 0.0f);
        }

        if (outroRatio > 0 && outroRatio < 1)
        {
            spriteRenderer.material.SetFloat("_Start", outroRatio);
        }
    }

    public void Collect()
    {
        fromNoteModel = default;
        toNoteModel = default;
        hasFromNote = default;
        fromNote = default;
        hasToNote = default;
        toNote = default;
        introRatio = default;
        outroRatio = default;
        length = default;
        Game.ObjectPool.CollectDragLine(this);
        Game.onGameUpdate.RemoveListener(OnGameUpdate);
    }
}