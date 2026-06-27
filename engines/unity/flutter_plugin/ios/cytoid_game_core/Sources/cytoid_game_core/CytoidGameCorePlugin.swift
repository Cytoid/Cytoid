import Flutter
import UIKit

public class CytoidGameCorePlugin: NSObject, FlutterPlugin {
  private static let methodChannelName = "cytoid/game_core"
  private static let eventChannelName = "cytoid/game_core/events"
  private static let waitForReadyChannelName = "cytoid_game_core/waitForReady"

  private let bridge = CytoidGameCoreBridge()

  public static func register(with registrar: FlutterPluginRegistrar) {
    let instance = CytoidGameCorePlugin()

    let methodChannel = FlutterMethodChannel(
      name: methodChannelName,
      binaryMessenger: registrar.messenger()
    )
    registrar.addMethodCallDelegate(instance, channel: methodChannel)

    let waitForReadyChannel = FlutterMethodChannel(
      name: waitForReadyChannelName,
      binaryMessenger: registrar.messenger()
    )
    registrar.addMethodCallDelegate(instance, channel: waitForReadyChannel)

    let eventChannel = FlutterEventChannel(
      name: eventChannelName,
      binaryMessenger: registrar.messenger()
    )
    eventChannel.setStreamHandler(instance.bridge)

    UnityGameCoreCallback.register(instance.bridge)
  }

  public func handle(_ call: FlutterMethodCall, result: @escaping FlutterResult) {
    switch call.method {
    case "send":
      guard let json = call.arguments as? String else {
        result(
          FlutterError(
            code: "invalid_argument",
            message: "Expected envelope JSON string for send.",
            details: nil
          )
        )
        return
      }
      bridge.onOutboundMessage(json)
      result(nil)
    case "getEngineMode":
      result(bridge.engineMode)
    case "queryRuntimeStatus":
      result(bridge.runtimeStatus())
    case "ensureRuntimeStarted":
      bridge.ensureRuntimeStarted()
      result(nil)
    case "showGameSurface":
      bridge.showGameSurface(result: result)
    case "hideGameSurface":
      bridge.hideGameSurface()
      result(nil)
    case "waitForReady":
      // Optional per-call timeout in seconds. Falls back to the v2
      // recommended default (30s) when omitted.
      let timeout: TimeInterval
      if let value = call.arguments as? Double {
        timeout = value
      } else if let value = call.arguments as? Int {
        timeout = TimeInterval(value)
      } else {
        timeout = CytoidGameCoreBridge.waitForReadyDefaultTimeout
      }
      Task {
        do {
          try await bridge.waitForReady(timeout: timeout)
          result(nil)
        } catch let error as CytoidGameCoreBridge.WaitForReadyError {
          switch error {
          case .timeout:
            result(FlutterError(code: "waitForReadyTimeout", message: "Runtime did not reach ready state within \(timeout)s.", details: nil))
          case .alreadyFailed(let code):
            result(FlutterError(code: code, message: "Runtime is in a failed state.", details: nil))
          }
        } catch {
          result(FlutterError(code: "waitForReadyFailed", message: "\(error)", details: nil))
        }
      }
    default:
      result(FlutterMethodNotImplemented)
    }
  }
}
