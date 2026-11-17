using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DragLineElement : MonoBehaviour
{
    private static readonly int MaterialEnd = Shader.PropertyToID("_End");
    private static readonly int MaterialStart = Shader.PropertyToID("_Start");

    /// <summary>The classic line's sorting layer; the skin-driven chain shares it so the
    /// layer stack (under flat note meshes, above hold lines) stays identical.
    /// Lazy: NameToID is barred from the MonoBehaviour constructor path, where a
    /// static initializer would run.</summary>
    private static int? dragLineSortingLayer;
    private static int DragLineSortingLayer =>
        dragLineSortingLayer ??= SortingLayer.NameToID("GameDragLine");

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

    // ------------------------------------------------------------------
    // Skin-driven chain (cytoid-design 6559409): when the flat skin is active
    // the connection draws from the source head's required chainLink slot — the
    // skin authors one periodic template along +y spanning 0..length (inputs:
    // length + the t sustain clock); this element keeps the engine-private
    // gameplay: endpoints, from-note-euler aim (Override.Rot* compat), the
    // lead/trail window (intro grow / outro retract) and the GameDragLine layer.
    // The classic sprite path stays for classic skins.
    // ------------------------------------------------------------------
    private FlatNoteMesh chainMesh;
    private readonly FlatMeshBuilder chainBuilder = new FlatMeshBuilder();
    private readonly SkinInputs.ChainOptions chainOptions = new SkinInputs.ChainOptions();
    private SkinPainter chainPainter;
    private SkinDrawContext chainContext;
    private bool usingFlatChain;
    private bool chainBroken;

    // Cached chainLink slot: CompiledSkin.Slot interpolates its lookup key, which
    // would allocate per visible link per frame. Invalidate on any skin swap or
    // pooled reuse (the kind may change between lives).
    private CompiledSkin cachedSlotSkin;
    private NoteKind cachedSlotKind;
    private CompiledSkinSlot cachedChainSlot;

    private CompiledSkinSlot ChainSlot(NoteKind kind)
    {
        var skin = FlatSkinRuntime.Current;
        if (!ReferenceEquals(cachedSlotSkin, skin) || cachedSlotKind != kind)
        {
            cachedSlotSkin = skin;
            cachedSlotKind = kind;
            cachedChainSlot = skin.Slot(kind, "chainLink");
        }
        return cachedChainSlot;
    }

    /// <summary>True when the flat path owns this link's visual (the sprite stays
    /// hidden). No classic fallback: a missing/unsupported chainLink slot leaves
    /// the chain undrawn (RenderFlatChain), as does a draw failure (chainBroken
    /// latch).</summary>
    private bool FlatChainOwnsVisual()
    {
        if (chainBroken) return true;
        return Game.Config.UseFlatNoteDesign;
    }

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
        if (chainMesh != null) chainMesh.Dispose();
        Destroy(gameObject);
    }

    public void SetData(ChartModel.Note fromNoteModel, ChartModel.Note toNoteModel)
    {
        IsCollected = false;
        chainBroken = false;

        FromNoteModel = fromNoteModel;
        ToNoteModel = toNoteModel;
        spriteRenderer.material.SetFloat(MaterialEnd, 0.0f);
        spriteRenderer.material.SetFloat(MaterialStart, 0.0f);
        usingFlatChain = FlatChainOwnsVisual();
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
        // Compatible with Override.Rot*: aim with the from-note euler, not from→to.
        transform.localEulerAngles = hasFromNote
            ? fromNote.transform.localEulerAngles
            : FromNoteModel.rotation;
        if (usingFlatChain)
        {
            // The skin mesh carries its own length (the template spans 0..length in
            // design px); no sprite tiling or y stretch on the shared transform.
            transform.localScale = Vector3.one;
            return;
        }
        spriteRenderer.material.mainTextureScale = new Vector2(1.0f, length / 0.16f);
        transform.localScale = new Vector3(1.0f, length / 0.16f);
    }

    static Vector3 VisualPosition(Note note)
    {
        if (note == null) return Vector3.zero;
        return note.StackVisualLocalPosition();
    }

    private void OnGameUpdate(Game _)
    {
        usingFlatChain = FlatChainOwnsVisual();
        UpdateTransform();

        // Compute windows first: the flat chain's lead/trail rect shares them with
        // the classic shader's _End/_Start clip.
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

        var hideNotes = Game.State.Mods.Contains(Mod.HideNotes);
        var flatChain = RenderFlatChain(hideNotes);

        spriteRenderer.enabled = !flatChain && !hideNotes;

        if (!flatChain)
        {
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

            if (outroRatio > 0f && outroRatio < 1f)
            {
                spriteRenderer.material.SetFloat(MaterialStart, outroRatio);
            }
        }

        if (outroRatio >= 1f)
        {
            spriteRenderer.material.SetFloat(MaterialStart, 1f);
            Collect();
            return;
        }
    }

    /// <summary>Draws the skin-driven chain when the flat skin owns the visual;
    /// returns true when it does (the sprite stays hidden).</summary>
    private bool RenderFlatChain(bool hideNotes)
    {
        if (!usingFlatChain || hideNotes)
        {
            if (chainMesh != null) chainMesh.SetVisible(false);
            return false;
        }
        if (chainBroken)
        {
            if (chainMesh != null) chainMesh.SetVisible(false);
            return true;
        }

        // Template-space window: lead grows from the source (intro), trail retracts
        // behind the scanner (outro) — the same gameplay the shader's _End/_Start clip.
        var lead = Mathf.Clamp01(1f - introRatio);
        var trail = Mathf.Clamp01(outroRatio);
        if (lead <= trail)
        {
            // Pre-window frames run here BEFORE the first DrawChainMesh created the
            // mesh. An NRE here would abort the shared onGameUpdate invocation and
            // freeze every note's LateUpdate behind this listener — never throw.
            if (chainMesh != null) chainMesh.SetVisible(false);
            return true;
        }

        var type = (NoteType) FromNoteModel.type;
        var kind = type == NoteType.CDragHead || type == NoteType.CDragChild
            ? NoteKind.ClickDragHead : NoteKind.DragHead;
        var slot = ChainSlot(kind);
        if (slot == null || slot.UsesUnsupported)
        {
            // No classic sprite fallback: the flat path owns the chain, and a
            // missing slot renders nothing. Null-checked: the mesh is created
            // lazily on the first successful draw (same guard as every other
            // early-out — an NRE would abort the shared onGameUpdate callback).
            FlatNoteRendererBase.WarnSlotMissingOnce(kind, "chainLink");
            if (chainMesh != null) chainMesh.SetVisible(false);
            return true;
        }

        try
        {
            DrawChainMesh(kind, slot, lead, trail);
        }
        catch (Exception e)
        {
            // Like note heads: a broken chain slot hides instead of falling back
            // mid-flight (the latch also pins the sprite off — no flapping).
            chainBroken = true;
            if (chainMesh != null) chainMesh.SetVisible(false);
            Debug.LogWarning($"[FlatSkinRuntime] {kind} chainLink draw failed, chain hidden: {e.Message}");
        }
        return true;
    }

    private void DrawChainMesh(NoteKind kind, CompiledSkinSlot slot, float lead, float trail)
    {
        if (chainMesh == null)
        {
            chainMesh = FlatNoteMesh.Create(transform, "FlatDragChain", false, 0);
            chainMesh.SetSortingLayer(DragLineSortingLayer);
            chainMesh.gameObject.layer = gameObject.layer;
            // Design px are y-down (FlatNoteMesh uploads (x, -y)); the template grows
            // along +y toward the destination — flip the child so template +y matches
            // the sprite's local +y the element rotation aims at the link.
            chainMesh.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
        // Classic stacks overlapping links by source id inside the GameDragLine
        // layer (spriteRenderer.sortingOrder = fromNoteModel.id); the mesh must
        // follow the id per pool reuse, not the creation-time 0.
        chainMesh.SetSortingOrder(FromNoteModel.id);

        // The chain rides the source head's scale so chain and head meet edge to edge;
        // before the head spawns, mirror the renderer's formula from the model.
        FlatNoteRendererBase fromFlat = hasFromNote && fromNote != null
            ? fromNote.Renderer as FlatNoteRendererBase : null;
        float worldPerPx;
        if (fromFlat != null) worldPerPx = fromFlat.LinkWorldScale;
        else
        {
            var baseScale = (float) Game.Chart.Model.size * Game.Config.GlobalNoteSizeMultiplier;
            if (FromNoteModel.size != double.MinValue) baseScale *= (float) FromNoteModel.size;
            worldPerPx = Game.Config.NoteTransformSizes[NoteType.Click] * baseScale / (float) FlatTokens.Unit;
        }
        if (worldPerPx <= 0f || length <= 0f || float.IsNaN(worldPerPx))
        {
            chainMesh.SetVisible(false);
            return;
        }

        // The chain takes the source head's palette and direction (alternative colour).
        var alternative = FromNoteModel.UseAlternativeColor();
        var family = kind == NoteKind.ClickDragHead ? FlatFamily.ClickDrag : FlatFamily.Drag;
        var palettes = FlatSkinRuntime.PaletteContextOf(Game, family,
            alternative ? FlatDirection.Down : FlatDirection.Up,
            FlatNoteRendererBase.ResolvePaletteSeed(FromNoteModel, Game),
            Game.Config.NoteRingColors[(NoteType) FromNoteModel.type][0]);
        chainContext ??= new SkinDrawContext();
        FlatSkinRuntime.FillHeadContext(chainContext, kind, palettes, alternative,
            Math.Max(1e-6, FromNoteModel.start_time - FromNoteModel.intro_time));

        chainMesh.SetWorldScale(worldPerPx);
        var lengthPx = length / worldPerPx;
        chainBuilder.Reset();
        chainOptions.Length = lengthPx;
        chainOptions.T = Math.Max(0.0, Game.Time - FromNoteModel.nextdraglinestarttime);
        // stage.ts parity: a generous symmetric band around the line — the window
        // cuts along y only.
        var half = (float) chainContext.Unit * 16f;
        chainPainter ??= new SkinPainter();
        chainPainter.DrawChainLinkSlot(slot, chainOptions, chainContext, new SkinSinks(chainBuilder),
            trail * lengthPx, lead * lengthPx, half);

        // The connection fades with its source head — AND with the head's approach.
        // The skin's enter template fades the head's own draw, but the link has no
        // enter animation and LinkOpacity carries no approach ramp (the engine-side
        // intro fade died with the imperative flat path), so a link pops at full
        // alpha the moment its window opens — while its endpoints may still sit at
        // pre-storyboard-override chart poses (you-are-the-miserable's final sea:
        // controllers teleport the nodes onto their slide rails ~38ms AFTER the
        // window opens; 16 full-alpha flash+teleport chains read as chains flying).
        // Classic parity: the sprite's alpha mirrors the source head's fill alpha —
        // EasedOpacity's ramp reaches full at HALF the approach (2×) and clamps at
        // the chart opacity, not at 1 — so it is still 0 at that teleport and the
        // pre-override pose never shows.
        var approach = FromNoteModel.start_time - FromNoteModel.intro_time;
        var maxOpacity = (float) Game.Chart.Model.opacity;
        if (FromNoteModel.opacity != double.MinValue) maxOpacity = (float) FromNoteModel.opacity;
        // Classic EasedOpacity's ramp: progress × 2, clamped AT the chart opacity
        // (not at 1). Renormalised to a 0..1 fraction of the link's full opacity.
        float approachFade;
        if (approach > 1e-6)
        {
            var ramp = (float) ((1.0 - (FromNoteModel.start_time - Game.Time) / approach) * 2.0);
            approachFade = maxOpacity > 1e-6f ? Mathf.Clamp01(ramp / maxOpacity) : 0f;
        }
        else approachFade = 1f;
        if (approachFade <= 0f)
        {
            // Entirely outside the visible envelope — skip the draw (sea-chart
            // chains otherwise rebuild 800-4300 verts per line per frame there).
            if (chainMesh != null) chainMesh.SetVisible(false);
            return;
        }

        // The connection fades with its source head.
        var opacity = (fromFlat != null ? fromFlat.LinkOpacity : FlatChainFallbackOpacity()) * approachFade;
        if (opacity < 1f)
            for (var i = 6; i < chainBuilder.Verts.Count; i += 7) chainBuilder.Verts[i] *= opacity;

        chainMesh.SetVisible(true);
        chainMesh.Upload(chainBuilder);
    }

    /// <summary>Source-head opacity before its renderer exists (chart/storyboard/global
    /// multipliers — UpdateComponentOpacity minus the skin's own fade).</summary>
    private float FlatChainFallbackOpacity()
    {
        var opacity = (float) Game.Chart.Model.opacity;
        if (FromNoteModel.opacity != double.MinValue) opacity = (float) FromNoteModel.opacity;
        return opacity * Game.Config.GlobalNoteOpacityMultiplier
               * (float) FromNoteModel.Override.OpacityMultiplier;
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
        if (chainMesh != null) chainMesh.SetVisible(false);
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
