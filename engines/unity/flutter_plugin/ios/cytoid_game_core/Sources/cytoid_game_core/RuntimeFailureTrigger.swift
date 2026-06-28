import Foundation

/// Trigger reasons for synthesizing a v2 `session.failed` envelope when an
/// active session is killed by a runtime-side event the engine itself cannot
/// report (v2 § Active-Session Runtime Failure).
///
/// Each trigger maps to a `runtime_*` error code per the v2 spec table.
///
/// - `.generationChange`: engine generation incremented while a session was
///   active (the new engine instance does not know about the old session).
///   Wired by T4 in `CytoidGameCoreBridge.onUnityMessage` after
///   `RuntimeStateMachine.onEngineReady()`.
/// - `.surfaceLost`: the engine surface was destroyed during active play
///   (iOS Unity window unloaded). Wired by T4 in the iOS `unityDidUnload`
///   callback.
/// - `.unreachable`: the native bridge cannot deliver messages to the engine
///   process. NOT wired by T4 — T6 (iOS framework load failure during active
///   session) implements that trigger by calling
///   `CytoidGameCoreBridge.synthesizeRuntimeFailure` with `.unreachable`.
///
/// The error code string literals (`runtime_recreated`, `runtime_surface_lost`,
/// `runtime_unreachable`) are the v2 wire contract — do NOT camelCase or rename.
public enum RuntimeFailureTrigger {
    case generationChange
    case unreachable
    case surfaceLost

    public var errorCode: String {
        switch self {
        case .generationChange: return "runtime_recreated"
        case .unreachable: return "runtime_unreachable"
        case .surfaceLost: return "runtime_surface_lost"
        }
    }

    public var defaultMessage: String {
        switch self {
        case .generationChange:
            return "Runtime recreated: engine generation incremented while a session was active."
        case .unreachable:
            return "Runtime unreachable: native bridge cannot deliver messages to the engine."
        case .surfaceLost:
            return "Runtime surface lost: engine surface was destroyed during active play."
        }
    }
}
