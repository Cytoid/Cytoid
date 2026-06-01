import Flutter
import Foundation

final class CytoidGameCoreBridge: NSObject, FlutterStreamHandler {
  private lazy var mockBridge: MockGameCoreBridge = {
    MockGameCoreBridge { [weak self] json in
      self?.emitEvent(json)
    }
  }()

  private var eventSink: FlutterEventSink?
  private var runtimeStarted = false
  private var engineReady = false
  private var surfaceVisible = false
  private var activePlayId: String?

  var engineMode: String {
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    return UnityGameCoreRuntime.shared.isFrameworkPresent ? "unity" : "mock"
#else
    return "mock"
#endif
  }

  private var shouldUseUnityRuntime: Bool {
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    return UnityGameCoreRuntime.shared.isFrameworkPresent
#else
    return false
#endif
  }

  func ensureRuntimeStarted() {
    runtimeStarted = true

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
    runtimeStarted = true

#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    if shouldUseUnityRuntime {
      DispatchQueue.main.async { [weak self] in
        guard let self else {
          return
        }

        let presented = UnityGameCoreRuntime.shared.presentExclusiveFullscreen()
        if presented {
          self.surfaceVisible = true
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

    surfaceVisible = true
    mockBridge.showGameSurface()
    result(nil)
  }

  func hideGameSurface() {
    surfaceVisible = false
#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
    if shouldUseUnityRuntime {
      UnityGameCoreRuntime.shared.dismissExclusiveFullscreen()
      return
    }
#endif
    mockBridge.hideGameSurface()
  }

  func onOutboundMessage(_ jsonString: String) {
    if let type = messageType(jsonString) {
      if type == "bridge.play.start" {
        activePlayId = messageId(jsonString)
      } else if type == "bridge.play.end" {
        activePlayId = nil
      }
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

    if messageType(jsonString) == "game.ready" {
      engineReady = true
      runtimeStarted = true
    }
    if isGameResultMessage(jsonString) {
      activePlayId = nil
    }
  }

  func runtimeStatus() -> [String: Any] {
    let state: String
    if activePlayId != nil {
      state = "busy"
    } else if engineReady {
      state = "ready"
    } else if runtimeStarted || surfaceVisible || shouldUseUnityRuntime {
      state = "starting"
    } else {
      state = "unavailable"
    }
    var payload: [String: Any] = [
      "state": state,
      "engine": engineMode,
    ]
    if let activePlayId {
      payload["activePlayId"] = activePlayId
    }
    return payload
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
    if isGameResultMessage(jsonString) || messageType(jsonString) == "game.play.ended" {
      activePlayId = nil
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
