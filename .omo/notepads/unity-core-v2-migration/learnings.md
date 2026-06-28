# learnings — unity-core-v2-migration

Cumulative memory for stateless subagents working on this plan.

## 2026-06-28 T0 — orchestration start
Atlas session opencode:ses_0f32b302affeTDCXz6xb574DV0 began executing plan.

## 2026-06-28 T3 — existing telemetry event shape
`GamePlayEvent` already uses v2 short wire fields (`t`, `f`, `p`, `x`, `y`), so `GamePlayEventRecorder.SnapshotAsWireObjects()` can wrap `Snapshot()` without remapping.

## 2026-06-28 T5 — pending settings and full session.start snapshots
The v2 `session.start.settings` full-snapshot requirement means router pending settings should not synthesize a missing `settings` object anymore; `settings.apply` can still store a flat patch for local application, but `session.start` validation remains owned by `ExternalGameContentProvider.FlattenLaunchSettings(..., requireFullSnapshot: true)`.
