import Flutter
import Foundation

final class CytoidGameCoreBridge: NSObject, FlutterStreamHandler {
  private lazy var mockBridge: MockGameCoreBridge = {
    MockGameCoreBridge { [weak self] json in
      self?.emitEvent(json)
    }
  }()

  private var eventSink: FlutterEventSink?

  // v2 runtime state. Replaces the v1 ad-hoc boolean tracking (startup
  // requested, engine acknowledgement, surface shown) with a single source
  // of truth that also tracks generation, activeSessionId, and lastError.
  // The flag→state migration table from the plan is encoded by the initial
  // .unavailable state plus the transition methods driven by lifecycle
  // events below.
  //
  // internal (not private): the GENERATION_CHANGE trigger fires only when
  // generation > 1, a state unreachable through the public bridge API
  // without a prior onFailure (T6 wires that). Tests drive the state
  // machine directly to set up that condition.
  internal let runtimeState = RuntimeStateMachine()

  // Testability seam for emitEvent(): isolated SwiftPM sandbox tests cannot
  // reach the real FlutterEventSink (the Flutter module isn't bootstrapped),
  // so the default eventSink path is unreachable. When non-nil, emitEvent()
  // calls this override directly with the JSON string — letting tests
  // capture synthesized envelopes without a real sink. Production leaves
  // this nil and uses the eventSink path.
  internal var emitOverride: ((String) -> Void)?

  var engineMode: String {
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    return UnityGameCoreRuntime.shared.isFrameworkPresent ? "unity" : "mock"
#else
    return "mock"
#endif
  }

  var mode: String { engineMode }

  private var shouldUseUnityRuntime: Bool {
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    return UnityGameCoreRuntime.shared.isFrameworkPresent
#else
    return false
#endif
  }

  func ensureRuntimeStarted() {
    runtimeState.onRequestStart()

    #if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    wireUnityRuntimeSurfaceLostHandlerIfNeeded()
    if shouldUseUnityRuntime {
      DispatchQueue.main.async {
        _ = UnityGameCoreRuntime.shared.loadIfNeeded()
      }
      return
    }
    #endif

    mockBridge.ensureRuntimeStarted()
  }

  func showGameSurface(result: @escaping FlutterResult) {
    runtimeState.onRequestStart()

    #if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    wireUnityRuntimeSurfaceLostHandlerIfNeeded()
    if shouldUseUnityRuntime {
      DispatchQueue.main.async { [weak self] in
        guard let self else {
          return
        }

        let presented = UnityGameCoreRuntime.shared.presentExclusiveFullscreen()
        if presented {
          result(nil)
          return
        }

        result(
          FlutterError(
            code: "unity_present_failed",
            message: "Unity failed to start in fullscreen mode.",
            details: nil
          )
        )
      }
      return
    }
#endif

    mockBridge.showGameSurface()
    result(nil)
  }

  func hideGameSurface() {
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    if shouldUseUnityRuntime {
      UnityGameCoreRuntime.shared.dismissExclusiveFullscreen()
      runtimeState.onSuspend()
      return
    }
#endif
    mockBridge.hideGameSurface()
    runtimeState.onSuspend()
  }

  func onOutboundMessage(_ jsonString: String) {
    let type = messageType(jsonString)

    // v1 fallback: bridge.play.start arrives without v2 session.started, so
    // treat it as ready→busy using the envelope id.
    if type == "bridge.play.start", let id = messageId(jsonString) {
      runtimeState.onSessionStarted(sessionId: id)
    } else if type == "session.start", let id = messageId(jsonString) {
      runtimeState.onSessionStarted(sessionId: id)
    } else if type == "bridge.play.end" || type == "session.cancel" {
      runtimeState.onSessionEnded()
    }

#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    if shouldUseUnityRuntime {
      UnityGameCoreRuntime.shared.sendMessage(jsonString)
      return
    }
#endif

    mockBridge.onOutboundMessage(jsonString)
  }

  func onUnityMessage(_ jsonString: String) {
    emitEvent(jsonString)

    let type = messageType(jsonString)
    // v2 engine.ready or v1 fallback game.ready both complete the
    // starting→ready transition.
    if type == "engine.ready" || type == "game.ready" {
      // GENERATION_CHANGE trigger (v2 § Active-Session Runtime Failure):
      // if a session was active AND generation is now >1, the prior
      // session belongs to a stale engine instance. Capture before
      // onEngineReady (which doesn't clear activeSessionId, but the
      // capture makes the intent explicit and survives future edits).
      let wasActiveSession = runtimeState.activeSessionId
      runtimeState.onEngineReady()
      if let wasActiveSession, runtimeState.generation > 1 {
        _ = synthesizeRuntimeFailure(trigger: .generationChange, sessionId: wasActiveSession)
      }
    }
    // v2 session.started: explicit ready→busy signal carries the sessionId.
    if type == "session.started", let id = messageId(jsonString) {
      runtimeState.onSessionStarted(sessionId: id)
    }
    if type == "session.result" || isGameResultMessage(jsonString) {
      runtimeState.onSessionEnded()
    }
  }

  func onAppWillResignActive() {
    runtimeState.onSuspend()
  }

  func onAppDidBecomeActive() {
    runtimeState.onResume()
  }

  #if CYTOID_UNITY_FRAMEWORK_AVAILABLE
  private var surfaceLostHandlerInstalled = false

  // Install the SURFACE_LOST notification handler on UnityGameCoreRuntime
  // exactly once. Subsequent calls are no-ops, so re-entry through
  // ensureRuntimeStarted / showGameSurface is safe.
  private func wireUnityRuntimeSurfaceLostHandlerIfNeeded() {
    guard !surfaceLostHandlerInstalled else { return }
    surfaceLostHandlerInstalled = true
    UnityGameCoreRuntime.shared.surfaceLostHandler = { [weak self] in
      guard let self else { return }
      let activeSession = self.runtimeState.activeSessionId
      guard let activeSession else { return }
      _ = self.synthesizeRuntimeFailure(trigger: .surfaceLost, sessionId: activeSession)
    }
  }
  #endif

  /**
   * v2 runtime snapshot. Conditional optionality per spec:
   * required keys `engine`, `mode`, `state`, `generation` always present;
   * `activeSessionId` only when `state = busy`; `error` only when
   * `state = failed`.
   */
  func runtimeStatus() -> [String: Any] {
    return runtimeState.snapshot(engine: engineMode, mode: mode)
  }

  /**
   * Synthesize a v2 `session.result` envelope with
   * `outcome.kind = "runtimeFailed"` for an active session killed by a
   * runtime-side event the engine itself cannot report (v2 § Active-Session
   * Runtime Failure).
   *
   * Contract:
   *  - Idempotent: gated on `activeSessionId == sessionId`. If the session
   *    already terminated (activeSessionId is nil or a different id), this
   *    is a no-op and returns nil. At most one synthesized result per session.
   *  - On success: transitions runtimeState to .failed via onFailure (which
   *    clears activeSessionId), emits the envelope via emitEvent, returns
   *    the JSON string.
   *  - Active-session failures use `session.result`, NEVER `engine.error`.
   *
   * Returns the emitted JSON envelope string, or nil if the gate suppressed
   * the synthesis (idempotency).
   */
  @discardableResult
  func synthesizeRuntimeFailure(
    trigger: RuntimeFailureTrigger,
    sessionId: String
  ) -> String? {
    guard let currentSessionId = runtimeState.activeSessionId else { return nil }
    guard currentSessionId == sessionId else { return nil }

    let error = GameCoreError(
      code: trigger.errorCode,
      message: trigger.defaultMessage
    )

    let payload: [String: Any] = [
      "sessionId": sessionId,
      "outcome": ["kind": "runtimeFailed"],
      "error": error.toMap(),
    ]
    let envelope: [String: Any] = [
      "v": Self.protocolVersionV2,
      "id": sessionId,
      "type": "session.result",
      "payload": payload,
    ]

    // onFailure clears activeSessionId AFTER transitioning to .failed;
    // we already captured it above, so order is safe. This is the
    // idempotency seam: a second call sees activeSessionId == nil.
    runtimeState.onFailure(error: error)

    guard
      let data = try? JSONSerialization.data(withJSONObject: envelope),
      let jsonString = String(data: data, encoding: .utf8)
    else {
      return nil
    }

    emitEvent(jsonString)
    return jsonString
  }

  private static let protocolVersionV2 = 2

  private func isGameResultMessage(_ jsonString: String) -> Bool {
    guard
      let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
      let type = envelope["type"] as? String
    else {
      return false
    }

    return type == "game.play.result"
  }

  private func messageType(_ jsonString: String) -> String? {
    guard
      let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
    else {
      return nil
    }

    return envelope["type"] as? String
  }

  private func messageId(_ jsonString: String) -> String? {
    guard
      let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
    else {
      return nil
    }

    return envelope["id"] as? String
  }

  private func emitEvent(_ jsonString: String) {
    let type = messageType(jsonString)
    if type == "session.result" || type == "game.play.result" || type == "game.play.ended" {
      runtimeState.onSessionEnded()
    }

    if let emitOverride {
      emitOverride(jsonString)
      return
    }

    if Thread.isMainThread {
      eventSink?(jsonString)
      return
    }

    DispatchQueue.main.async { [weak self] in
      self?.eventSink?(jsonString)
    }
  }

  func onListen(withArguments arguments: Any?, eventSink events: @escaping FlutterEventSink) -> FlutterError? {
    eventSink = events
    return nil
  }

  func onCancel(withArguments arguments: Any?) -> FlutterError? {
    eventSink = nil
    return nil
  }
}
