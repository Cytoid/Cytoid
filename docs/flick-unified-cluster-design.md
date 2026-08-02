# Click / Flick Unified Hit-Cluster Design

> Status: implemented in `InputController` + `FlickNote`.
>
> Scope: Flick joins the existing 15ms Select cluster and rendered-X arbitration;
> no near-overlap epsilon special case in this change; Flick movement lifecycle
> keeps failed early swipes bound until a real clear or FingerUp.
>
> Target: `InputController` FingerDown select arbitration + `FlickNote` lifecycle.
>
> Primary cases: Click + Flick and Flick + Flick at the same or nearby note time.

## 1. Background

The previous select path applied note-time clustering and rendered-X ordering to
Click-like candidates, while Flick remained a `SpawnedNotes` / note-ID-order bind.
That left two failure modes when hitboxes overlap:

- a Click and Flick at the same or nearby time could be selected by list order
  instead of the finger's spatial intent;
- two nearby Flicks could bind to the wrong fingers, and asymmetric hitbox overlap
  could leave one Flick unbound even when two fingers are present.

Flick also had a lifecycle bug: after its displacement threshold was crossed,
`UpdateFingerPosition` treated the attempt as handled even if `TryClear` failed.
A too-early gesture could lose its binding without clearing the note.

## 2. Design summary

```text
- Flick joins the existing 15ms cluster + |Δx| select flow.
- No rendered-X near-overlap epsilon special case in this change.
- Flick movement: Pending / Cleared / Released (keep binding on failed TryClear).

Unchanged: Drag-first, DragCoHit 30ms, grade windows, one Down → one Select.
```

Flick on Down is still only a **reservation** (like Hold bind). Clear happens on
later movement or FingerUp. Relative to Click, Flick adds one displacement gate;
assignment uses the same cluster model.

Near-overlap epsilon / delayed Click / Flick-pool ideas are out of scope; revisit
only if device tests show micro-offset misbinds on stacked charts.

## 3. Goals

1. Put Click-like and Flick candidates into the same note-time clusters.
2. Use rendered horizontal distance as the primary in-cluster selector when notes
   are spatially distinguishable.
3. Cover exact-tick and pseudo-simultaneous Click + Flick and Flick + Flick.
4. Preserve monotonic note-time selection outside the cluster window.
5. One FingerDown allocates at most one Select note.
6. Same-frame multi-finger allocation skips notes already cleared or reserved.
7. Failed / too-early Flick threshold crossings must not report false success or
   drop the binding.
8. Keep judgment windows, grades, scoring, Drag ordering, and DragCoHit unchanged.
9. Do not introduce a near-overlap epsilon graph, deferred Click session, or Flick
   candidate pool in this change.

## 4. Non-goals

- Do not merge Drag, CDrag child, or DropDrag into select clustering.
- Do not change Click or Flick Perfect/Great windows.
- Do not change the Flick displacement threshold (`orthographicSize * 0.01`) or
  add velocity / direction detection.
- Do not allow one finger to clear both a Click and a Flick from one FingerDown.
- Do not batch all Began touches into a global minimum-cost assignment.
- Do not defer every Click to infer a later Flick.
- Do not add a player-facing cluster-window or epsilon setting.
- Do not ship a rendered-X near-overlap graph in this change.

## 5. Constants

| Name | Value | Role |
|------|-------|------|
| `NoteClusterGapSeconds` | `0.015` | Max effective-note-time span of one Select cluster |
| `DragCoHitWindowSeconds` | `0.030` | Max how much later a Select may be than an accepted Drag on the same Down |

This change does **not** introduce a near-overlap epsilon constant. Exact spatial
ties still fall through the normal comparator to `time → kind → id` when `|Δx|`
values are equal.

## 6. Candidate scope

| Kind | Note types | FingerDown | Completion |
|------|------------|------------|------------|
| Click-like | Click, CDrag head, DropClick | Immediate `OnTouch` / `TryClear` | Done on Down |
| Flick | Flick | Reserve + `StartFlicking` | Movement or FingerUp |
| Hold | Unheld Hold / LongHold | Bind + `UpdateFinger` | Existing Hold rules |

Never enter the unified Select cluster:

- Drag head, Drag child, CDrag child, DropDrag.

Those remain in `TouchableDragNotes` with list-order scan and DragCoHit gating of
Select.

## 7. Candidate collection

Each candidate records:

```text
note
kind
effectiveNoteTime = note.Model.start_time + note.JudgmentOffset
renderedCenterX   = collider.bounds.center.x if present else transform.position.x
```

Reject before reading `Model` when the note is null, collected, cleared, armed,
not emerged, not colliding with the FingerDown world position, blocked by
DragCoHit, or blocked by the existing cross-page early-selection rule.

Type-specific filters:

- Flick: finger not already in `FlickingNotes`; note not already reserved
  (`FlickingNotes.ContainsValue`);
- Hold: note not already holding; finger not already bound to a Hold.

**Important change vs the previous path:** Flick is **not** a list-order scan
boundary. Do not flush a partial Click/Hold cluster when a Flick is encountered.
Collect all eligible Select candidates first, then cluster.

## 8. Time clustering

Unchanged algorithm:

1. Sort candidates by `effectiveNoteTime` asc, then note id asc.
2. From the earliest remaining candidate, grow one cluster while
   `candidate.effectiveNoteTime - clusterMinTime ≤ 0.015`.
3. No adjacent-gap chain expansion (`0/10/20` → `[0,10]` then `[20]`).
4. Process clusters earliest-first. A later cluster is considered only if every
   candidate in the earlier cluster rejects the FingerDown (soft fallthrough).

## 9. In-cluster ordering

Single comparator for all Select candidates in the cluster:

```text
|touchWorldX - renderedCenterX| ascending
effectiveNoteTime ascending
kind priority ascending
note id ascending
```

Kind priority:

```text
Click / CDrag head / DropClick = 0
Flick                         = 1
Hold / LongHold               = 2
```

Notes:

- Spatially separable notes: finger `|Δx|` decides.
- Exact equal `|Δx|` (true coincident centers + same touch distance): fall through
  to time → kind → id (Click before Flick before Hold).
- Near-but-not-equal centers (e.g. `|Δcx| = 0.01` with finger slightly closer to
  Flick): **Flick can win** — accepted in this change; add a near-overlap rule
  later only if device feel is wrong.
- Swipe never re-picks: settle only `FlickingNotes[finger]`.

## 10. Deferred: near-overlap ideas (not in this change)

| Idea | Status |
|------|--------|
| Epsilon connected components; ignore micro `|Δx|`; Down kind/id | Out of scope |
| Short window: watch swipe, then C vs F with Down timestamp | Out of scope |
| Group Flick pool; assign on swipe start X | Not recommended |

## 11. FingerDown arbitration

```text
acceptedDrag = FindAcceptedDrag(pressedPosition)          // unchanged
candidates   = collectUnifiedSelectCandidates(...)        // includes Flick

for cluster in clustersByEffectiveNoteTime(candidates):
    ordered = orderClusterByRenderedDistance(cluster, touchWorldX)
    for candidate in ordered:
        if became invalid / reserved: continue
        if Click-like and OnTouch: return Consumed
        if Flick and reserveFlick: StartFlicking; return Consumed
        if Hold and bindHold: return Consumed

return NotConsumed
```

Settlement interaction with Drag (unchanged):

```text
if acceptedDrag != null:
    if acceptedSelect != null: acceptedDrag.OnTouchDeferred(...)
    else:                      acceptedDrag.OnTouch(...)
AcceptSelect(acceptedSelect, ...)
```

A reserved Flick counts as `acceptedSelect != null`, so Drag+Flick within the
30ms CoHit window still co-hits: Drag arms, Flick reserves. Flick still needs a
later displacement to clear.

## 12. Same-frame multi-finger

Keep event-by-event FingerDown dispatch (no frame-wide assignment).

After finger 1 accepts:

- Click-like → `IsCleared`
- Flick → inserted into `FlickingNotes`
- Hold → inserted into `HoldingNotes`

Finger 2 uses the same per-frame list snapshot but **revalidates** every
candidate (skip cleared / reserved / holding / collected). Goal: concurrent
Flick finger tracking rises with real contacts; fixes dual-Flick and asymmetric
overlap misses.

Greedy, not globally optimal. Three-way ties can still depend on dispatch order.

## 13. Flick lifecycle — required

### 13.1 Why it is necessary

Previously `FlickNote.UpdateFingerPosition` did:

```text
if (|swipe| > threshold):
    TryClear()      // may no-op when grade is None (too early / too late-not-yet-Miss)
    return true     // InputController ALWAYS removes FlickingNotes[finger]
```

So a displacement that **failed to clear** still **dropped the binding**. Effects:

1. **Early swipe kills the reservation.** Player Downs early (legal), twitches or
   swipes before Great/Perfect → `TryClear` returns false → finger unbound →
   further movement and even FingerUp no longer see this Flick → note Misses
   despite the finger still being on it.
2. **Unified clustering makes this worse if left unfixed.** Correct Flick reserves
   become more common; without this lifecycle fix those reserves are still one
   early micro-swipe away from being discarded.
3. **False “success” breaks the Hold-like contract.** Down means “this finger owns
   this Flick until clear or release.” Returning true on a failed `TryClear`
   violates that: ownership ends without a judgment.
4. **Not a substitute for grade windows.** This does not widen Perfect/Great or
   Miss (still +0.3s). It only keeps the finger→note link so a *later* in-window
   swipe can still call `TryClear`.

Resetting the swipe origin after a failed threshold cross blocks the exploit
“flick far early, hold still, wait until the window arrives and clear with no
new motion”; the player must produce another threshold-crossing movement inside
the window.

### 13.2 State machine

Replace boolean “handled” with:

```text
Pending   — keep binding
Cleared   — remove binding after successful clear
Released  — remove binding after FingerUp / Cancel (after one final TryClear)
```

Movement (always against the **single** reserved note from Down):

```text
|Δx| < threshold     → Pending
|Δx| ≥ threshold
    TryClear ok      → Cleared; unbind
    TryClear fail    → Pending; reset FlickingStartPosition / StartTime to current;
                       require another threshold-crossing movement
```

FingerUp / Cancel:

```text
final TryClear at release position on the reserved note only
unbind regardless of result
```

Threshold value unchanged: `orthographicSize * 0.01`.

Collect / pause must still drop `FlickingNotes` entries for the note / all
fingers as today.

## 14. Drag + Flick

| Topic | Rule |
|-------|------|
| Bucket | Drag never enters the Select cluster |
| Gate | Flick uses the same `IsEligibleSelectAfterDrag` (≤ 30ms later) |
| Co-hit | Same Down may arm Drag and reserve Flick |
| Regression | Drag+Flick (and Drag+Click) at 29 / 30 / 31 ms |

Joining the cluster must not change Drag-first settlement order.

## 15. Behavior matrix

| Combination | Path | One finger | Two fingers |
|-------------|------|------------|-------------|
| Click + Click, separable | Cluster | Closest Click | Nearest remaining |
| Click + Flick, separable | Cluster | Closest note | Nearest remaining |
| Click + Flick, exact same \|Δx\| | Tiebreak | time→kind→id (Click first) | 2nd gets remainder |
| Flick + Flick, separable | Cluster | Closest Flick | Nearest unreserved |
| Flick + Flick, exact same \|Δx\| | Tiebreak | Lower id if time ties | Reservation gives the other |
| Flick + Drag, any X | DragCoHit | May co-hit if ≤30ms | Independent Downs |
| Gap 16ms+ | Cluster | Earlier viable cluster wins | Same |

## 16. Compatibility

Unchanged:

- grade windows, Early/Late, score;
- Drag list-order + DragCoHit 30ms;
- one Down → one Select;
- Auto / AutoFlick / AutoHold / practice rules;
- 15ms cluster span;
- Flick swipe threshold magnitude.

Intentionally changed:

- Flick participates in unified clustering and `|Δx|`;
- same-frame reserved/cleared notes cannot be taken again;
- early Flick threshold miss no longer false-success unbind.

Selected note **ids** on overlapping charts may change even when grades do not.
Update play-event fixtures accordingly.

## 17. Test matrix

### Cluster + main path

- Click + Flick and Flick + Flick at note gaps 0, 10, 15, 16, 30 ms.
- `0/10/20` chain → second cluster at 20.
- `JudgmentOffset` in `effectiveNoteTime`.
- Finger closer to higher id; ids L→R and R→L.
- Collider center ≠ raw chart X.
- Exact coincident centers: kind/id tiebreak (Click before Flick).

### Multi-finger + lifecycle

- Two Downs both orders; asymmetric overlap.
- Too-early swipe keeps binding; later swipe inside window clears.
- Stationary after failed early swipe does not auto-clear.
- FingerUp / Cancel / pause / collect release bindings.
- After failed early threshold: InputController must **not** remove `FlickingNotes[finger]`.

### Regression

- Click-only cluster ordering.
- Drag + Click / Drag + Flick at 29, 30, 31 ms.
- Ranked vs practice Flick windows; 30 / 60 / 120 Hz sampling.

## 18. Acceptance criteria

1. Spatially separable same-cluster notes + matching fingers → two allocations
   independent of note-id order.
2. One finger → exactly one closest viable Select.
3. Candidate >15ms from cluster min cannot beat a viable earlier cluster.
4. Click+Flick / Flick+Flick with distinct rendered X use distance, not id.
5. Second same-frame finger cannot take an already allocated note.
6. Flick unbinds on movement only after real clear, or on release/cancel.
7. Too-early Flick cannot become an automatic clear while stationary, and cannot
   drop the binding on a failed `TryClear`.
8. Click-only, grade windows, scoring, DragCoHit unchanged.

## 19. Implementation stages

1. Flick lifecycle `Pending / Cleared / Released` (+ start-point reset) — **do first**.
2. Unified Select collector (Flick into `hitCandidates`; remove list-order boundary).
3. Extend `SelectTypePriority` (Click 0 / Flick 1 / Hold 2); cluster sort includes Flick.
4. Same-frame revalidation hardening.
5. EditMode tests: clustering, reservation, lifecycle false-success.
6. Device multi-touch checks before release enablement.

## 20. Code touchpoints

| Area | File / symbol |
|------|----------------|
| Select Down | `InputController.FindAcceptedSelect`, `OrderHitCandidatesByNoteTimeClusters`, `SelectTypePriority`, `AcceptSelect` |
| Lifecycle | `FlickNote.UpdateFingerPosition` (+ result enum or equivalent); `OnFingerUpdate` must honor Pending |
| Drag | `FindAcceptedDrag`, `IsEligibleSelectAfterDrag` — logic preserved |

```text
OrderCluster(touchWorldX, candidates):
  sort by |touchX - renderedCenterX|, effectiveNoteTime, kind, id
  emit in that order (existing 15ms cluster loop unchanged)
```

## 21. Open item

- If stacked charts show frequent micro-offset misbinds, consider a near-overlap
  epsilon rule without changing the unified cluster path or lifecycle.
