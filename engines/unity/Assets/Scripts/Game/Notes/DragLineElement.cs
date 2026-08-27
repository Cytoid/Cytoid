using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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

    /// <summary>Shared-geometry key assigned by <see cref="ObjectPool"/>; 0 when unset.</summary>
    public long GeometryKey { get; set; }

    private readonly HashSet<int> geometryFromIds = new HashSet<int>();

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

    public void AddGeometryRef(int fromNoteId)
    {
        geometryFromIds.Add(fromNoteId);
    }

    public List<int> DrainGeometryRefs()
    {
        var ids = new List<int>(geometryFromIds);
        geometryFromIds.Clear();
        return ids;
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
        Game.onGameUpdate.RemoveListener(OnGameUpdate);
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

        Vector3 fromNotePosition;
        if (!hasFromNote)
        {
            fromNotePosition = FromNoteModel.CalculatePosition(Game.Chart);
        }
        else if (fromNote is DragHeadNote dragHeadNote)
        {
            // OriginalPosition is only written while Time < start_time; unset (default)
            // would anchor the line at the world origin and leave a visible stub.
            fromNotePosition = dragHeadNote.OriginalPosition != default
                ? dragHeadNote.OriginalPosition
                : VisualPosition(dragHeadNote);
        }
        else
        {
            fromNotePosition = VisualPosition(fromNote);
        }

        var toNotePosition = hasToNote
            ? VisualPosition(toNote)
            : ToNoteModel.CalculatePosition(Game.Chart);
        
        var transform = this.transform;
        transform.localPosition = fromNotePosition;
        length = Vector3.Distance(
            fromNotePosition, 
            toNotePosition
        );
        spriteRenderer.material.mainTextureScale = new Vector2(1.0f, length / 0.16f);
        // Compatible with Override.Rot*: aim with the from-note euler, not from→to.
        transform.localEulerAngles = hasFromNote
            ? fromNote.transform.localEulerAngles
            : FromNoteModel.rotation;
        transform.localScale = new Vector3(1.0f, length / 0.16f);
    }

    static Vector3 VisualPosition(Note note)
    {
        if (note == null) return Vector3.zero;
        return note.StackVisualLocalPosition();
    }

    private void OnGameUpdate(Game _)
    {
        UpdateTransform();
        
        spriteRenderer.enabled = !Game.State.Mods.Contains(Mod.HideNotes);

        if (Game.SpawnedNotes.ContainsKey(FromNoteModel.id))
        {
            var note = Game.SpawnedNotes[FromNoteModel.id];
            if (!note.IsCleared)
            {
                // Followers disable Fill; resolve opacity from the stack primary when present.
                var visual = note;
                if (note.IsDragStackFollower && note.DragStack?.Primary != null)
                    visual = note.DragStack.Primary;

                if (visual.Renderer is ClassicNoteRenderer classicNoteRenderer)
                {
                    var fill = classicNoteRenderer.Fill;
                    if (fill != null && fill.enabled)
                    {
                        spriteRenderer.color = spriteRenderer.color.WithAlpha(fill.color.a);
                    }
                    else
                    {
                        spriteRenderer.color = Color.white.WithAlpha(FallbackAlpha(note));
                    }
                }
                else
                {
                    spriteRenderer.color = Color.white.WithAlpha(FallbackAlpha(note));
                }
            }
        }

        var time = Game.Time;
        var introDuration = FromNoteModel.nextdraglinestoptime - FromNoteModel.nextdraglinestarttime;
        if (introDuration > 0)
        {
            introRatio = (FromNoteModel.nextdraglinestoptime - time) / introDuration;
        }
        else
        {
            introRatio = time < FromNoteModel.nextdraglinestarttime ? 1.0f : 0.0f;
        }

        // Compute outro before Collect so completion happens on the same frame
        // (avoids a one-frame remnant when _Start is never pulled to 1).
        var outroDuration = ToNoteModel.start_time - FromNoteModel.start_time;
        if (outroDuration > 0f)
        {
            outroRatio = (time - FromNoteModel.start_time) / outroDuration;
        }
        else
        {
            // Simultaneous or reverse edge: finish once at/after from start (no NaN).
            outroRatio = time < FromNoteModel.start_time ? 0f : 1f;
        }

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

        if (outroRatio >= 1f)
        {
            spriteRenderer.material.SetFloat(MaterialStart, 1f);
            Collect();
            return;
        }

        if (outroRatio > 0f)
        {
            spriteRenderer.material.SetFloat(MaterialStart, outroRatio);
        }
    }

    static float FallbackAlpha(Note note)
    {
        var denom = note.Model.start_time - note.Model.intro_time;
        return denom > 0f ? Mathf.Clamp01(1f - note.TimeUntilStart / denom) : 1f;
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
        GeometryKey = default;
        geometryFromIds.Clear();
    }
}
