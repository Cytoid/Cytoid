using UnityEngine;
using Object = UnityEngine.Object;

public class ClassicDragHeadNoteRenderer : ClassicNoteRenderer
{
    public DragHeadNote DragHeadNote => (DragHeadNote) Note;
    
    protected SpriteMask SpriteMask;
    protected SpriteRenderer CDragFill;

    private readonly Vector3 ringBaseLocal;
    private readonly Vector3 fillBaseLocal;
    private readonly Quaternion ringBaseRotation;
    private readonly Quaternion fillBaseRotation;
    private readonly Vector3 noteIdBaseLocal;
    private readonly Vector3 maskBaseLocal;
    private readonly Vector3 maskBaseScale;
    private GameObject indicator;
    private SpriteRenderer indicatorRing;
    private SpriteRenderer indicatorFill;
    private float? judgmentRotationZ;
    private float? approachRotationZ;
    private bool clearedSuccessfully;
    private bool positionCallbackRegistered;
    
    public ClassicDragHeadNoteRenderer(DragHeadNote dragHeadNote) : base(dragHeadNote)
    {
        SpriteMask = Note.transform.GetComponentInChildren<SpriteMask>();
        CDragFill = Note.transform.Find("NoteRing/CDragFill")?.GetComponent<SpriteRenderer>();
        // Capture the pristine prefab pose once, rather than a pooled note's last offset.
        ringBaseLocal = Ring.transform.localPosition;
        fillBaseLocal = Fill.transform.localPosition;
        ringBaseRotation = Ring.transform.localRotation;
        fillBaseRotation = Fill.transform.localRotation;
        if (DisplayNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
        maskBaseLocal = SpriteMask.transform.localPosition;
        maskBaseScale = SpriteMask.transform.localScale;
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        Fill.sortingOrder = Ring.sortingOrder + 1;
        if (CDragFill != null) CDragFill.sortingOrder = Fill.sortingOrder + 1;
        SpriteMask.frontSortingOrder = Note.Model.id + 1;
        SpriteMask.backSortingOrder = Note.Model.id - 2;
        if (DragHeadNote.IsCDrag)
        {
            judgmentRotationZ = null;
            clearedSuccessfully = false;
            Game.onGameLateUpdate.RemoveListener(UpdateCDragHeadPosition);
            positionCallbackRegistered = false;
            if (indicator == null)
            {
                indicator = new GameObject("CDragIndicator");
                indicator.transform.SetParent(Note.transform, false);
                var prefab = GameObjectProvider.Instance.dragHeadNotePrefab.transform;
                indicatorRing = CreateIndicatorSprite(prefab.Find("NoteRing"));
                indicatorFill = CreateIndicatorSprite(prefab.Find("NoteFill"));
                // Let the mask follow the indicator independently of the anchored Ring.
                SpriteMask.transform.SetParent(Note.transform, false);
            }
            indicatorRing.sortingOrder = Ring.sortingOrder;
            indicatorFill.sortingOrder = indicatorRing.sortingOrder + 1;
            // The head freezes its judged direction and the ID stays upright; the indicator follows the chain.
            indicator.transform.localRotation = Quaternion.identity;
            // The travelling mask only cuts the drag-line layer, never the anchored head.
            Ring.maskInteraction = Fill.maskInteraction = SpriteMaskInteraction.None;
            if (CDragFill != null) CDragFill.maskInteraction = SpriteMaskInteraction.None;
            indicatorRing.enabled = indicatorFill.enabled = false;
        }
    }

    private SpriteRenderer CreateIndicatorSprite(Transform source)
    {
        // Copy only the prefab's visual data, without its gameplay or mask children.
        var sprite = new GameObject(source.name).AddComponent<SpriteRenderer>();
        sprite.transform.SetParent(indicator.transform, false);
        sprite.transform.localPosition = source.localPosition;
        sprite.transform.localScale = source.localScale;
        sprite.transform.localRotation = source.localRotation;
        var original = source.GetComponent<SpriteRenderer>();
        sprite.sprite = original.sprite;
        sprite.sharedMaterial = original.sharedMaterial;
        sprite.sortingLayerID = original.sortingLayerID;
        sprite.maskInteraction = original.maskInteraction;
        sprite.enabled = false;
        return sprite;
    }

    protected override void Render()
    {
        base.Render();
        if (!DragHeadNote.IsCDrag) return;

        // Note registers its movement after OnNoteLoaded. Register here so this callback
        // runs after that movement; an Update-only offset would lag by one rendered frame.
        if (!positionCallbackRegistered)
        {
            Game.onGameLateUpdate.AddListener(UpdateCDragHeadPosition);
            positionCallbackRegistered = true;
        }
        UpdateCDragHeadPosition(Game);
    }

    private void UpdateCDragHeadPosition(Game _)
    {
        if (Note.IsCollected || !DragHeadNote.IsCDrag) return;
        var transform = Note.transform;
        var scale = transform.localScale;
        if (scale.x == 0 || scale.y == 0 || scale.z == 0 ||
            float.IsNaN(scale.sqrMagnitude) || float.IsInfinity(scale.sqrMagnitude)) return;

        // Both positions are in the note parent's space. Convert their difference to
        // child space, cancelling the travelling parent's rotation AND scale.
        var rotation = Quaternion.Inverse(transform.localRotation);
        var offset = rotation * (Note.Model.CalculatePosition(Game.Chart) - transform.localPosition);
        offset = new Vector3(offset.x / scale.x, offset.y / scale.y, offset.z / scale.z);
        Ring.transform.localPosition = ringBaseLocal + offset;
        Fill.transform.localPosition = fillBaseLocal + offset;
        // The chain departs at start_time; capture the approach direction (toward the
        // first node) there so an unjudged or missed head keeps pointing along its own
        // approach instead of turning with the travelling indicator.
        if (approachRotationZ == null && Game.Time >= Note.Model.start_time)
            approachRotationZ = transform.localEulerAngles.z;
        // Follow the approach direction until judgment, then preserve it as the parent turns.
        var headRotation = Quaternion.Euler(0, 0,
            (judgmentRotationZ ?? approachRotationZ ?? transform.localEulerAngles.z) - transform.localEulerAngles.z);
        Ring.transform.localRotation = headRotation * ringBaseRotation;
        Fill.transform.localRotation = headRotation * fillBaseRotation;
        // CDragFill is nested under Ring: it inherits the offset and counter-rotation.
        if (DisplayNoteId && NoteId.gameObject.activeSelf)
        {
            NoteId.transform.localPosition = noteIdBaseLocal + offset;
            NoteId.transform.localEulerAngles = new Vector3(0, 0, -transform.localEulerAngles.z);
        }
        // Remain on the travelling transform even when the head is missed or unjudged.
        SpriteMask.transform.localPosition = maskBaseLocal;
    }

    protected override void UpdateComponentStates()
    {
        if (DragHeadNote.IsCDrag)
        {
            if (!Note.IsCleared) clearedSuccessfully = false;
            var chainActive = DragHeadNote.IsChainEndPending(Game);
            var chainVisible = Game.Time >= Note.Model.intro_time && chainActive;
            var hidden = Game.State.Mods.Contains(Mod.HideNotes);
            // Preserve Classic's head visibility window, including its existing miss
            // presentation, but never re-show a successfully cleared head.
            var headVisible = !hidden && !clearedSuccessfully && chainVisible;
            Ring.enabled = Fill.enabled = headVisible;
            if (CDragFill != null) CDragFill.enabled = headVisible;
            // An early-tapped head shows the indicator immediately — the transform is
            // still parked at the head position until start_time.
            indicatorRing.enabled = indicatorFill.enabled =
                !hidden && chainActive && (clearedSuccessfully || Game.Time >= Note.Model.start_time);
            SpriteMask.enabled = headVisible || indicatorRing.enabled;
            if (DisplayNoteId)
            {
                // The anchored head's ID has the click lifecycle, independent of the chain.
                NoteId.gameObject.SetActive(!hidden && DragHeadNote.IsHeadNoteIdActive(Game));
            }
            return;
        }
        if (!Note.IsCleared)
        {
            SpriteMask.enabled = Game.Time >= Note.Model.intro_time;
        }
        if (DragHeadNote.IsChainActive(Game))
        {
            if (Game.State.Mods.Contains(Mod.HideNotes))
            {
                Ring.enabled = false;
                Fill.enabled = false;
                if (CDragFill != null) CDragFill.enabled = false;
            }
            else
            {
                Ring.enabled = true;
                Fill.enabled = true;
                if (CDragFill != null) CDragFill.enabled = true;
                if (DisplayNoteId)
                {
                    NoteId.gameObject.SetActive(true);
                    NoteId.transform.localEulerAngles = new Vector3(0, 0, -Note.transform.localEulerAngles.z);
                }
            }
        }
        else
        {
            Ring.enabled = false;
            Fill.enabled = false;
            if (CDragFill != null) CDragFill.enabled = false;
            if (DisplayNoteId) NoteId.gameObject.SetActive(false);
        }
    }

    protected override void UpdateTransformScale()
    {
        if (DragHeadNote.IsCDrag)
        {
            if (clearedSuccessfully)
            {
                var size = BaseTransformSize * Note.Model.Override.SizeMultiplier;
                Note.transform.SetLocalScaleXY(size, size);
            }
            else base.UpdateTransformScale();
            var ratio = Game.Config.NoteTransformSizes[NoteType.DragHead] / Game.Config.NoteTransformSizes[Note.Type];
            indicator.transform.SetLocalScaleXY(ratio, ratio);
            SpriteMask.transform.localScale = maskBaseScale * (Game.Time >= Note.Model.start_time ? ratio : 1f);
        }
        else
        {
            var size = BaseTransformSize * Note.Model.Override.SizeMultiplier;
            
            var minSize = Note.Model.initial_scale;
            var timeScale = Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);
            var timeScaledSize = size * minSize + size * (1 - minSize) * timeScale;

            Note.transform.SetLocalScaleXY(timeScaledSize, timeScaledSize);
        }
    }

    protected override void UpdateFillScale()
    {
        if (DragHeadNote.IsCDrag)
        {
            if (clearedSuccessfully) Fill.transform.SetLocalScaleXY(1f, 1f);
            else base.UpdateFillScale();
        }
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        if (DragHeadNote.IsCDrag)
        {
            CDragFill.color = Ring.color;
            // Match a real Classic drag head, including this note's colour overrides.
            indicatorRing.color = Ring.color;
            indicatorFill.color = Fill.color;
        }
    }

    public override void OnClear(NoteGrade grade)
    {
        if (DragHeadNote.IsCDrag)
        {
            // A miss freezes the head at its approach direction, not wherever the
            // travelling transform was pointing when the miss threshold fired.
            judgmentRotationZ = grade == NoteGrade.Miss
                ? (approachRotationZ ?? Note.transform.localEulerAngles.z)
                : Note.transform.localEulerAngles.z;
            clearedSuccessfully = grade != NoteGrade.Miss;
            if (DisplayNoteId) NoteId.gameObject.SetActive(false);
        }
        base.OnClear(grade);
        if (DragHeadNote.IsCDrag && clearedSuccessfully)
        {
            Ring.enabled = Fill.enabled = false;
            if (CDragFill != null) CDragFill.enabled = false;
        }
    }

    public override void OnCollect()
    {
        base.OnCollect();
        if (indicator == null) return;
        Game.onGameLateUpdate.RemoveListener(UpdateCDragHeadPosition);
        positionCallbackRegistered = false;
        judgmentRotationZ = null;
        approachRotationZ = null;
        clearedSuccessfully = false;
        indicatorRing.enabled = indicatorFill.enabled = false;
        Ring.transform.localPosition = ringBaseLocal;
        Fill.transform.localPosition = fillBaseLocal;
        Ring.transform.localRotation = ringBaseRotation;
        Fill.transform.localRotation = fillBaseRotation;
        if (DisplayNoteId)
        {
            NoteId.transform.localPosition = noteIdBaseLocal;
            NoteId.transform.localRotation = Quaternion.identity;
        }
        SpriteMask.transform.localPosition = maskBaseLocal;
        SpriteMask.transform.localScale = maskBaseScale;
    }

    public override void Dispose()
    {
        base.Dispose();
        Game.onGameLateUpdate.RemoveListener(UpdateCDragHeadPosition);
        if (indicator != null) Object.Destroy(indicator);
        Object.Destroy(SpriteMask);
        if (CDragFill != null) Object.Destroy(CDragFill);
    }
}
