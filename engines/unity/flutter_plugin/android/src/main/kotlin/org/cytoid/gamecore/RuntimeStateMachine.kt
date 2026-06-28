package org.cytoid.gamecore

import androidx.annotation.VisibleForTesting

/**
 * Native runtime lifecycle state machine shared by [CytoidGameCoreBridge] and
 * [MockGameCoreBridge]. Encodes the v2 host protocol transitions:
 *
 *   UNAVAILABLE → STARTING            (host requested startup)
 *   STARTING    → READY               (engine.ready)
 *   READY       → BUSY                (session.started; sets activeSessionId)
 *   BUSY        → READY               (session.result; clears activeSessionId)
 *   READY|BUSY  → SUSPENDED           (app backgrounded; saves prior state)
 *   SUSPENDED   → prior state         (resume; single-slot memory, no stack)
 *   any         → FAILED              (unrecoverable; sets lastError)
 *
 * Pause during STARTING stays STARTING — no transition.
 *
 * The machine owns only the lifecycle bookkeeping. Downstream tasks (T4/T5/T6)
 * are responsible for synthesising `session.failed` when [onFailure] fires
 * with a non-null [activeSessionId]; see `.omo/notepads/v2-host-impl/decisions.md`.
 */
class RuntimeStateMachine {
    @Volatile
    var state: RuntimeState = RuntimeState.UNAVAILABLE
        private set

    @Volatile
    var generation: Int = 0
        private set

    @Volatile
    var activeSessionId: String? = null
        private set

    @Volatile
    var lastError: GameCoreError? = null
        private set

    // Single-slot resume memory. NOT a stack. Cleared on resume or failure.
    @Volatile
    private var priorStateForResume: RuntimeState? = null

    /**
     * v2 runtime snapshot, with conditional optionality matching the spec:
     * required keys `engine`, `mode`, `state`, `generation` always present;
     * `activeSessionId` only when `state = busy`; `error` only when
     * `state = failed`.
     */
    fun snapshot(engine: String, mode: String): Map<String, Any?> = buildMap {
        put("engine", engine)
        put("mode", mode)
        put("state", state.wireName)
        put("generation", generation)
        if (state == RuntimeState.BUSY) {
            // BUSY implies activeSessionId != null by construction — the only
            // path into BUSY is onSessionStarted(id), which sets both.
            put("activeSessionId", checkNotNull(activeSessionId) {
                "RuntimeStateMachine: BUSY state requires activeSessionId"
            })
        }
        if (state == RuntimeState.FAILED) {
            lastError?.let { put("error", it.toMap()) }
        }
    }

    /** UNAVAILABLE → STARTING. Idempotent if already past UNAVAILABLE. */
    @VisibleForTesting
    fun onRequestStart() {
        if (state == RuntimeState.UNAVAILABLE) {
            state = RuntimeState.STARTING
        }
    }

    /**
     * STARTING → READY (or FAILED → READY on recovery). Bumps [generation]
     * when entering READY, per v2 § Active-Session Runtime Failure ("next
     * engine.ready MUST carry an incremented generation"). No-op from any
     * other state: a duplicate or late ready arriving while BUSY or
     * SUSPENDED must not reset state and corrupt the active session or the
     * suspend invariant.
     */
    @VisibleForTesting
    fun onEngineReady() {
        if (state != RuntimeState.STARTING && state != RuntimeState.FAILED) {
            return
        }
        generation += 1
        state = RuntimeState.READY
        lastError = null
    }

    /** READY → BUSY. Sets [activeSessionId]. No-op outside READY. */
    @VisibleForTesting
    fun onSessionStarted(sessionId: String) {
        if (state == RuntimeState.READY) {
            activeSessionId = sessionId
            state = RuntimeState.BUSY
        }
    }

    /** BUSY → READY. Clears [activeSessionId]. No-op outside BUSY/SUSPENDED. */
    @VisibleForTesting
    fun onSessionEnded() {
        when (state) {
            RuntimeState.BUSY -> {
                activeSessionId = null
                state = RuntimeState.READY
            }
            RuntimeState.SUSPENDED -> {
                // Session ended while suspended (e.g. session.cancel / session.result
                // arrived during backgrounding): clear the active session and
                // neutralize the resume target so onResume lands in READY, not
                // stale BUSY with a dead sessionId.
                activeSessionId = null
                priorStateForResume = RuntimeState.READY
            }
            else -> Unit
        }
    }

    /**
     * READY|BUSY → SUSPENDED. Saves the prior state in [priorStateForResume]
     * so [onResume] can restore it. Pause during STARTING stays STARTING —
     * no transition, no resume slot.
     */
    @VisibleForTesting
    fun onSuspend() {
        when (state) {
            RuntimeState.READY, RuntimeState.BUSY -> {
                priorStateForResume = state
                state = RuntimeState.SUSPENDED
            }
            else -> Unit
        }
    }

    /** SUSPENDED → prior state (single-slot memory). No-op outside SUSPENDED. */
    @VisibleForTesting
    fun onResume() {
        val prior = priorStateForResume
        if (state == RuntimeState.SUSPENDED && prior != null) {
            state = prior
            priorStateForResume = null
        }
    }

    /**
     * any → FAILED. Sets [lastError], clears [activeSessionId] and the resume
     * slot. Active-session failure routing (whether to emit `session.result`
     * vs `engine.error`) is decided by the bridge based on the pre-failure
     * value of [activeSessionId]; see `decisions.md` T3 entry.
     */
    @VisibleForTesting
    fun onFailure(error: GameCoreError) {
        lastError = error
        activeSessionId = null
        priorStateForResume = null
        state = RuntimeState.FAILED
    }

    /**
     * Test-only: directly clear [activeSessionId] without changing state.
     * Used by the bridge when a SUSPENDED session is cancelled by backgrounding
     * (per T3 lifecycle rule: clear on SUSPENDED only if cancelled). Does NOT
     * affect state — the runtime stays SUSPENDED until resumed.
     */
    @VisibleForTesting
    fun clearActiveSessionForCancellation() {
        activeSessionId = null
    }

    /**
     * Reset to [RuntimeState.UNAVAILABLE]. Called when the runtime surface is
     * destroyed without an active session (e.g. the OS reclaimed the Unity
     * Activity). The host MUST call `startRuntime()` again before launching a
     * new session.
     *
     * Does NOT reset [generation]: the next `onEngineReady` bumps it naturally
     * from STARTING. [activeSessionId] is already null by contract (the caller
     * only invokes this when no session is active). [lastError] and the resume
     * slot are cleared to give the host a clean slate on restart.
     */
    @VisibleForTesting
    fun reset() {
        state = RuntimeState.UNAVAILABLE
        lastError = null
        priorStateForResume = null
    }
}
