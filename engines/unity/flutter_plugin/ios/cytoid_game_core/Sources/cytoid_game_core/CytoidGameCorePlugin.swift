import Flutter
import UIKit

public class CytoidGameCorePlugin: NSObject, FlutterPlugin {
  private static let methodChannelName = "cytoid/game_core"
  private static let eventChannelName = "cytoid/game_core/events"

  private let bridge = CytoidGameCoreBridge()

  public static func register(with registrar: FlutterPluginRegistrar) {
    let instance = CytoidGameCorePlugin()

    let methodChannel = FlutterMethodChannel(
      name: methodChannelName,
      binaryMessenger: registrar.messenger()
    )
    registrar.addMethodCallDelegate(instance, channel: methodChannel)

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
    default:
      result(FlutterMethodNotImplemented)
    }
  }
}
