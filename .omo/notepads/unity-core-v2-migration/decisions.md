# decisions — unity-core-v2-migration

Cumulative memory for stateless subagents working on this plan.

## 2026-06-28 T0 — orchestration start
Atlas session opencode:ses_0f32b302affeTDCXz6xb574DV0 began executing plan.

## 2026-06-28 T3 — calibration cancel terminal ownership
Chose the suppress-calibration-emission variant for host-driven `session.cancel`: `Game.AbortExternalSession(bool emitCalibrationResult = true)` keeps existing direct abort behavior by default, while the router can call `AbortExternalSession(false)` before emitting the single `session.result(outcome.kind="cancelled")` terminal envelope.
