package org.cytoid.gamecore

/**
 * Trigger reasons for synthesizing a v2 `session.failed` envelope when an
 * active session is killed by a runtime-side event the engine itself cannot
 * report (v2 § Active-Session Runtime Failure).
 *
 * Each trigger maps to a `runtime_*` error code per the v2 spec table.
 *
 * - [GENERATION_CHANGE]: engine generation incremented while a session was
 *   active (the new engine instance does not know about the old session).
 *   Wired by T4 in `CytoidGameCoreBridge.onUnityMessage` after
 *   `RuntimeStateMachine.onEngineReady()`.
 * - [SURFACE_LOST]: the engine surface was destroyed during active play
 *   (Android Activity destroyed / iOS Unity window unloaded). Wired by T4 in
 *   the Android `onActivityDestroyed` callback and the iOS
 *   `unityDidUnload` callback.
 * - [UNREACHABLE]: the native bridge cannot deliver messages to the engine
 *   process. NOT wired by T4 — T5 (Android sendToUnity failure) and T6
 *   (iOS framework load failure during active session) implement that trigger
 *   by calling [CytoidGameCoreBridge.synthesizeRuntimeFailure] with
 *   [UNREACHABLE].
 *
 * The error code string literals (`runtime_recreated`, `runtime_surface_lost`,
 * `runtime_unreachable`) are the v2 wire contract — do NOT camelCase or rename.
 */
enum class RuntimeFailureTrigger(
    val errorCode: String,
    val defaultMessage: String,
) {
    GENERATION_CHANGE(
        errorCode = "runtime_recreated",
        defaultMessage = "Runtime recreated: engine generation incremented while a session was active.",
    ),

    UNREACHABLE(
        errorCode = "runtime_unreachable",
        defaultMessage = "Runtime unreachable: native bridge cannot deliver messages to the engine.",
    ),

    SURFACE_LOST(
        errorCode = "runtime_surface_lost",
        defaultMessage = "Runtime surface lost: engine surface was destroyed during active play.",
    ),
}
