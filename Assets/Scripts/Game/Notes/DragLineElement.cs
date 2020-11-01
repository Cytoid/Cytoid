using Cysharp.Threading.Tasks;
using UnityEngine;

public class DragLineElement : MonoBehaviour
{
    private static readonly int MaterialEnd = Shader.PropertyToID("_End");
    private static readonly int MaterialStart = Shader.PropertyToID("_Start");
    
    private Game Game { get; set; }
    
    private SpriteRenderer spriteRenderer;
    
    public bool IsCollected { get; private set; }
    public ChartModel.Note FromNoteModel { get; private set; }
    public ChartModel.Note ToNoteModel { get; private set; }

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

    public void SetData(ChartModel.Note fromNoteModel, ChartModel.Note toNoteModel)
    {
        IsCollected = false;
        
        FromNoteModel = fromNoteModel;
        ToNoteModel = toNoteModel;
        spriteRenderer.material.SetFloat(MaterialEnd, 0.0f);
        spriteRenderer.material.SetFloat(MaterialStart, 0.0f);
        UpdateTransform();
        spriteRenderer.sortingOrder = fromNoteModel.id;
        Game.onGameUpdate.AddListener(OnGameUpdate);
    }

    private void UpdateTransform()
    {
        if (Game.SpawnedNotes.ContainsKey(FromNoteModel.id))
        {
            if (!hasFromNote)
            {
                hasFromNote = true;
                fromNote = Game.SpawnedNotes[FromNoteModel.id];
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
        if (Game.SpawnedNotes.ContainsKey(ToNoteModel.id))
        {
            if (!hasToNote)
            {
                hasToNote = true;
                toNote = Game.SpawnedNotes[ToNoteModel.id];
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

        var fromNotePosition = hasFromNote ? (fromNote is DragHeadNote dragHeadNote ? dragHeadNote.OriginalPosition : fromNote.transform.localPosition) : FromNoteModel.CalculatePosition(Game.Chart);
        var toNotePosition = hasToNote ? toNote.transform.localPosition : ToNoteModel.CalculatePosition(Game.Chart);
        
        var transform = this.transform;
        transform.localPosition = fromNotePosition;
        length = Vector3.Distance(
            fromNotePosition, 
            toNotePosition
        );
        spriteRenderer.material.mainTextureScale = new Vector2(1.0f, length / 0.16f);
        transform.localEulerAngles = hasFromNote ? fromNote.transform.localEulerAngles : FromNoteModel.rotation;
        transform.localScale = new Vector3(1.0f, length / 0.16f);
    }

    private void OnGameUpdate(Game _)
    {
        UpdateTransform();
        
        spriteRenderer.enabled = !Game.State.Mods.Contains(Mod.HideNotes);

        if (outroRatio >= 1)
        {
            Collect();
            return;
        }

        if (Game.SpawnedNotes.ContainsKey(FromNoteModel.id))
        {
            var note = Game.SpawnedNotes[FromNoteModel.id];
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
        introRatio = (FromNoteModel.nextdraglinestoptime - time) /
                     (FromNoteModel.nextdraglinestoptime - FromNoteModel.nextdraglinestarttime);
        outroRatio = (time - FromNoteModel.start_time) / (ToNoteModel.start_time - FromNoteModel.start_time);

        if (introRatio > 0 && introRatio < 1)
        {
            spriteRenderer.material.SetFloat(MaterialEnd, 1.0f - introRatio);
        }
        else if (introRatio <= 0)
        {
            spriteRenderer.material.SetFloat(MaterialEnd, 1.0f);
        }
        else
        {
            spriteRenderer.material.SetFloat(MaterialEnd, 0.0f);
        }

        if (outroRatio > 0 && outroRatio < 1)
        {
            spriteRenderer.material.SetFloat(MaterialStart, outroRatio);
        }
    }

    public void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;
        
        Game.ObjectPool.CollectDragLine(this);
        Game.onGameUpdate.RemoveListener(OnGameUpdate);
        FromNoteModel = default;
        ToNoteModel = default;
        hasFromNote = default;
        fromNote = default;
        hasToNote = default;
        toNote = default;
        introRatio = default;
        outroRatio = default;
        length = default;
    }
}