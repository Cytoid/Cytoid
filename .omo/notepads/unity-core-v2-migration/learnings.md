# learnings — unity-core-v2-migration

Cumulative memory for stateless subagents working on this plan.

## 2026-06-28 T0 — orchestration start
Atlas session opencode:ses_0f32b302affeTDCXz6xb574DV0 began executing plan.

## 2026-06-28 T3 — existing telemetry event shape
`GamePlayEvent` already uses v2 short wire fields (`t`, `f`, `p`, `x`, `y`), so `GamePlayEventRecorder.SnapshotAsWireObjects()` can wrap `Snapshot()` without remapping.
