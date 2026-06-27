import Foundation

/// Native runtime lifecycle state machine shared by `CytoidGameCoreBridge` and
/// `MockGameCoreBridge`. Encodes the v2 host protocol transitions:
///
///   unavailable → starting          (host requested startup)
///   starting    → ready             (engine.ready / game.ready fallback)
///   ready       → busy              (session.started; sets activeSessionId)
///   busy        → ready             (session.result; clears activeSessionId)
///   ready|busy  → suspended         (app backgrounded; saves prior state)
///   suspended   → prior state       (resume; single-slot memory, no stack)
///   any         → failed            (unrecoverable; sets lastError)
///
/// Pause during `starting` stays `starting` — no transition.
///
/// The machine owns only the lifecycle bookkeeping. Downstream tasks (T4/T5/T6)
/// are responsible for synthesising `session.result` with
/// `outcome.kind = "runtimeFailed"` when `onFailure` fires with a non-null
/// `activeSessionId`; see `.omo/notepads/v2-host-impl/decisions.md`.
public final class RuntimeStateMachine {
    public private(set) var state: RuntimeState = .unavailable
    public private(set) var generation: Int = 0
    public private(set) var activeSessionId: String?
    public private(set) var lastError: GameCoreError?

    // Single-slot resume memory. NOT a stack. Cleared on resume or failure.
    private var priorStateForResume: RuntimeState?

    public init() {}

    /// v2 runtime snapshot, with conditional optionality matching the spec:
    /// required keys `engine`, `mode`, `state`, `generation` always present;
    /// `activeSessionId` only when `state = busy`; `error` only when
    /// `state = failed`.
    public func snapshot(engine: String, mode: String) -> [String: Any] {
        var map: [String: Any] = [
            "engine": engine,
            "mode": mode,
            "state": state.wireName,
            "generation": generation,
        ]
        if state == .busy {
            // busy implies activeSessionId != nil by construction — the only
            // path into busy is onSessionStarted(id), which sets both.
            guard let activeSessionId else {
                preconditionFailure("RuntimeStateMachine: busy state requires activeSessionId")
            }
            map["activeSessionId"] = activeSessionId
        }
        if state == .failed, let lastError {
            map["error"] = lastError.toMap()
        }
        return map
    }

    /// unavailable → starting. Idempotent if already past unavailable.
    public func onRequestStart() {
        if state == .unavailable {
            state = .starting
        }
    }

    /// starting → ready (or failed → ready on recovery). Bumps `generation`
    /// when entering ready from starting or failed, per v2 § Active-Session
    /// Runtime Failure ("next engine.ready MUST carry an incremented generation").
    public func onEngineReady() {
        if state == .starting || state == .failed {
            generation += 1
        }
        state = .ready
        lastError = nil
    }

    /// ready → busy. Sets `activeSessionId`. No-op outside ready.
    public func onSessionStarted(sessionId: String) {
        if state == .ready {
            activeSessionId = sessionId
            state = .busy
        }
    }

    /// busy → ready. Clears `activeSessionId`. No-op outside busy.
    public func onSessionEnded() {
        if state == .busy {
            activeSessionId = nil
            state = .ready
        }
    }

    /// ready|busy → suspended. Saves the prior state in `priorStateForResume`
    /// so `onResume` can restore it. Pause during starting stays starting —
    /// no transition, no resume slot.
    public func onSuspend() {
        switch state {
        case .ready, .busy:
            priorStateForResume = state
            state = .suspended
        case .unavailable, .starting, .suspended, .failed:
            break
        }
    }

    /// suspended → prior state (single-slot memory). No-op outside suspended.
    public func onResume() {
        guard state == .suspended, let prior = priorStateForResume else { return }
        state = prior
        priorStateForResume = nil
    }

    /// any → failed. Sets `lastError`, clears `activeSessionId` and the resume
    /// slot. Active-session failure routing (whether to emit `session.result`
    /// vs `engine.error`) is decided by the bridge based on the pre-failure
    /// value of `activeSessionId`; see `decisions.md` T3 entry.
    public func onFailure(error: GameCoreError) {
        lastError = error
        activeSessionId = nil
        priorStateForResume = nil
        state = .failed
    }

    /// Test-only: directly clear `activeSessionId` without changing state.
    /// Used by the bridge when a suspended session is cancelled by backgrounding
    /// (per T3 lifecycle rule: clear on suspended only if cancelled). Does NOT
    /// affect state — the runtime stays suspended until resumed.
    public func clearActiveSessionForCancellation() {
        activeSessionId = nil
    }
}
