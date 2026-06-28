import Flutter
import Foundation

final class CytoidGameCoreBridge: NSObject, FlutterStreamHandler {
  private lazy var mockBridge: MockGameCoreBridge = {
    MockGameCoreBridge { [weak self] json in
      // Route mock emissions through onUnityMessage so the OUTER runtimeState
      // sees engine.ready / session.started / session.result and waitForReady
      // resolves in mock mode. Emitting straight to emitEvent bypassed state.
      self?.onUnityMessage(json)
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

  // Testability seam for framework load (T6): when non-nil, replaces
  // `UnityGameCoreRuntime.shared.loadIfNeeded()` inside ensureRuntimeStarted
  // so SwiftPM-sandbox tests can simulate `runtime_unavailable` failures
  // without the real UnityFramework binary.
  internal var loadFrameworkOverride: (() -> Result<Void, Error>)?

  // v2 waitForReady continuations (T6). Parked until state -> .ready or .failed.
  private var readyWaiters: [ReadyWaiter] = []

  internal static let waitForReadyDefaultTimeout: TimeInterval = 30.0

  enum WaitForReadyError: Error, Equatable {
    case timeout
    case alreadyFailed(code: String)
  }

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

    switch attemptFrameworkLoad() {
    case .success:
      #if CYTOID_UNITY_FRAMEWORK_AVAILABLE
      wireUnityRuntimeSurfaceLostHandlerIfNeeded()
      wireMessageQueueTimeoutHandlerIfNeeded()
      if shouldUseUnityRuntime {
        return
      }
      #endif
      mockBridge.ensureRuntimeStarted()
    case .failure(let error):
      // Framework-load failure at startup is ALWAYS pre-session (no active
      // session at this point) → engine.error ONLY, never session.result.
      let path = (error as? FrameworkLoadError)?.pathDescription
        ?? (error as NSError).userInfo["frameworkPath"] as? String
      let details: [String: Any]? = path.map { ["frameworkPath": $0] }
      let coreError = GameCoreError(
        code: "runtime_unavailable",
        message: "UnityFramework load failed: \(error.localizedDescription)",
        details: details
      )
      runtimeState.onFailure(error: coreError)
      failReadyWaiters(.alreadyFailed(code: coreError.code))
      emitEngineError(coreError)
    }
  }

  func showGameSurface(result: @escaping FlutterResult) {
    // Atomic check-then-act: ensureRuntimeStarted runs synchronously, so the
    // .failed check below observes the post-load state with no interleaving.
    ensureRuntimeStarted()

    if runtimeState.state == .failed {
      let error = runtimeState.lastError
      result(
        FlutterError(
          code: error?.code ?? "runtime_unavailable",
          message: error?.message ?? "Runtime is in a failed state.",
          details: nil
        )
      )
      return
    }

    #if CYTOID_UNITY_FRAMEWORK_AVAILABLE
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

  private func attemptFrameworkLoad() -> Result<Void, Error> {
    if let loadFrameworkOverride {
      return loadFrameworkOverride()
    }
    #if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    wireUnityRuntimeSurfaceLostHandlerIfNeeded()
    wireMessageQueueTimeoutHandlerIfNeeded()
    if shouldUseUnityRuntime {
      return UnityGameCoreRuntime.shared.loadIfNeeded()
    }
    #endif
    return .success(())
  }

  // Non-session runtime failure envelope (v2 § engine.error). Active-session
  // failures route via synthesizeRuntimeFailure instead — never both.
  private func emitEngineError(_ error: GameCoreError) {
    let envelope: [String: Any] = [
      "schema": Self.protocolSchemaV2,
      "id": UUID().uuidString,
      "type": "engine.error",
      "payload": ["error": error.toMap()],
    ]
    guard
      let data = try? JSONSerialization.data(withJSONObject: envelope),
      let jsonString = String(data: data, encoding: .utf8)
    else { return }
    emitEvent(jsonString)
  }

  func hideGameSurface() {
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    if shouldUseUnityRuntime {
      UnityGameCoreRuntime.shared.dismissExclusiveFullscreen()
      return
    }
#endif
    mockBridge.hideGameSurface()
  }

  func onOutboundMessage(_ jsonString: String) {
    guard isProtocolV2Envelope(jsonString) else { return }

    let type = messageType(jsonString)

    if type == "session.start", let id = messageId(jsonString) {
      runtimeState.onSessionStarted(sessionId: id)
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
    guard isProtocolV2Envelope(jsonString) else { return }

    let type = messageType(jsonString)

    // GENERATION_CHANGE must run BEFORE the engine.ready envelope is
    // forwarded: the spec requires the prior session's `runtime_recreated`
    // session.failed to reach the host before the next engine.ready (v2 §
    // Active-Session Runtime Failure ordering).
    if type == "engine.ready" {
      let wasActiveSession = runtimeState.activeSessionId
      runtimeState.onEngineReady()
      if let wasActiveSession, runtimeState.generation > 1 {
        _ = synthesizeRuntimeFailure(trigger: .generationChange, sessionId: wasActiveSession)
      }
      resumeReadyWaiters()
    }

    emitEvent(jsonString)

    // v2 session.started: explicit ready→busy signal carries the sessionId.
    if type == "session.started", let id = messageId(jsonString) {
      runtimeState.onSessionStarted(sessionId: id)
    }
    if type == "session.failed" {
      // Apply the envelope's error → .failed. Synth already did onFailure
      // before emit; onSessionEnded here would downgrade busy/suspended →
      // ready and drop the error block from runtimeStatus().
      if let data = jsonString.data(using: .utf8),
         let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
         let payload = envelope["payload"] as? [String: Any],
         let error = GameCoreError.from(map: payload["error"]) {
        runtimeState.onFailure(error: error)
      } else {
        runtimeState.onSessionEnded()
      }
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
  private var messageQueueTimeoutHandlerInstalled = false

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

  // Install the message-queue timeout handler exactly once. Routes per the
  // v2 active-session routing rule: active → session.failed via T4 primitive
  // ONLY; no active session → engine.error ONLY. Never both.
  private func wireMessageQueueTimeoutHandlerIfNeeded() {
    guard !messageQueueTimeoutHandlerInstalled else { return }
    messageQueueTimeoutHandlerInstalled = true
    UnityGameCoreRuntime.shared.messageQueueTimeoutHandler = { [weak self] in
      self?.handleMessageQueueTimeoutRouting()
    }
  }
  #endif

  /// Route a message-queue timeout (framework load never completed within
  /// `MessageQueueTimeoutSeconds`). The active-session routing rule (v2 §
  /// Active-Session Runtime Failure) decides the envelope:
  ///   activeSessionId == nil → engine.error ONLY
  ///   activeSessionId != null → session.failed via T4 primitive ONLY
  /// Exposed internal so SwiftPM-sandbox tests can invoke it directly without
  /// the real UnityGameCoreRuntime.
  internal func handleMessageQueueTimeoutRouting() {
    // Capture BEFORE any state mutation (T4 handoff pattern).
    let activeSession = runtimeState.activeSessionId
    if let activeSession {
      _ = synthesizeRuntimeFailure(trigger: .unreachable, sessionId: activeSession)
    } else {
      let error = GameCoreError(
        code: "runtime_unavailable",
        message: "UnityFramework not loaded after pending message queue timeout."
      )
      runtimeState.onFailure(error: error)
      failReadyWaiters(.alreadyFailed(code: error.code))
      emitEngineError(error)
    }
  }

  /// Wait until the runtime reaches `.ready` (engine.ready) or fail. Returns
  /// immediately if already `.ready`; throws
  /// `WaitForReadyError.alreadyFailed` if already `.failed`; throws
  /// `WaitForReadyError.timeout` if `timeout` seconds elapse without either.
  func waitForReady(timeout: TimeInterval = waitForReadyDefaultTimeout) async throws {
    if runtimeState.state == .ready { return }
    if runtimeState.state == .failed {
      let code = runtimeState.lastError?.code ?? "runtime_unavailable"
      throw WaitForReadyError.alreadyFailed(code: code)
    }

    try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in
      let waiter = ReadyWaiter(continuation)
      readyWaiters.append(waiter)
      let delay = max(0, timeout)
      DispatchQueue.main.asyncAfter(deadline: .now() + delay) { [weak self] in
        self?.timeoutReadyWaiter(waiter)
      }
    }
  }

  private func resumeReadyWaiters() {
    let waiters = readyWaiters
    readyWaiters.removeAll()
    for waiter in waiters {
      waiter.continuation.resume(returning: ())
    }
  }

  private func failReadyWaiters(_ error: WaitForReadyError) {
    let waiters = readyWaiters
    readyWaiters.removeAll()
    for waiter in waiters {
      waiter.continuation.resume(throwing: error)
    }
  }

  // Single-waiter timeout. If the waiter was already resumed by
  // resumeReadyWaiters / failReadyWaiters, the firstIndex lookup misses and
  // this is a no-op — that is the idempotency seam.
  private func timeoutReadyWaiter(_ waiter: ReadyWaiter) {
    guard let index = readyWaiters.firstIndex(where: { $0 === waiter }) else { return }
    readyWaiters.remove(at: index)
    waiter.continuation.resume(throwing: WaitForReadyError.timeout)
  }

  // Class wrapping a continuation so we can use referential identity
  // (`===`) to find/remove a specific waiter when its timeout fires.
  private final class ReadyWaiter {
    let continuation: CheckedContinuation<Void, Error>
    init(_ continuation: CheckedContinuation<Void, Error>) {
      self.continuation = continuation
    }
  }

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
   * Synthesize a v2 `session.failed` envelope for an active session killed
   * by a runtime-side event the engine itself cannot report (v2 §
   * Active-Session Runtime Failure).
   *
   * Contract:
   *  - Idempotent: gated on `activeSessionId == sessionId`. If the session
   *    already terminated (activeSessionId is nil or a different id), this
   *    is a no-op and returns nil. At most one synthesized failure per session.
   *  - On success: transitions runtimeState to .failed via onFailure (which
   *    clears activeSessionId), emits the envelope via emitEvent, returns
   *    the JSON string.
   *  - Active-session failures use `session.failed`, NEVER `engine.error`.
   *  - Payload is minimal: `{sessionId, error, timestamp}`. The `outcome`
   *    field MUST be absent (runtime death is not a gameplay result).
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
      "error": error.toMap(),
      "timestamp": Int(Date().timeIntervalSince1970 * 1000),
    ]
    let envelope: [String: Any] = [
      "schema": Self.protocolSchemaV2,
      "id": sessionId,
      "type": "session.failed",
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

  private static let protocolSchemaV2 = "cytoid.game-core.v2"

  private func isProtocolV2Envelope(_ jsonString: String) -> Bool {
    guard
      let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
    else {
      return false
    }

    return envelope["schema"] as? String == Self.protocolSchemaV2
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
    guard isProtocolV2Envelope(jsonString) else { return }

    let type = messageType(jsonString)
    if type == "session.result" {
      let resultId = messageId(jsonString)
      let outcomeKind = outcomeKind(jsonString)
      let isRejected = outcomeKind == "rejected"
      if resultId == nil || resultId == runtimeState.activeSessionId {
        if !isRejected {
          runtimeState.onSessionEnded()
        }
      }
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

  private func outcomeKind(_ jsonString: String) -> String? {
    guard
      let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
      let payload = envelope["payload"] as? [String: Any],
      let outcome = payload["outcome"] as? [String: Any]
    else {
      return nil
    }
    return outcome["kind"] as? String
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
