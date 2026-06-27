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
  private let runtimeState = RuntimeStateMachine()

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
      runtimeState.onEngineReady()
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

  /**
   * v2 runtime snapshot. Conditional optionality per spec:
   * required keys `engine`, `mode`, `state`, `generation` always present;
   * `activeSessionId` only when `state = busy`; `error` only when
   * `state = failed`.
   */
  func runtimeStatus() -> [String: Any] {
    return runtimeState.snapshot(engine: engineMode, mode: mode)
  }

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
