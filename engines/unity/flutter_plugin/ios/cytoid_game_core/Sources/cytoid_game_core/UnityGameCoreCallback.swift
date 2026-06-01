import Foundation

private let cytoidHostNativeOutboundNotification = Notification.Name("CytoidHostNativeOutboundMessage")
private let cytoidHostNativeOutboundJsonKey = "json"

enum UnityGameCoreCallback {
  private static weak var bridge: CytoidGameCoreBridge?
  private static var notificationObserver: NSObjectProtocol?

  static func register(_ bridge: CytoidGameCoreBridge) {
    self.bridge = bridge
    installNotificationObserverIfNeeded()
  }

  static func installNotificationObserverIfNeeded() {
    guard notificationObserver == nil else {
      return
    }

    notificationObserver = NotificationCenter.default.addObserver(
      forName: cytoidHostNativeOutboundNotification,
      object: nil,
      queue: .main
    ) { notification in
      guard let json = notification.userInfo?[cytoidHostNativeOutboundJsonKey] as? String else {
        return
      }
      bridge?.onUnityMessage(json)
    }
  }
}

#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
private func cytoidHostNativeMessageHandler(_ cString: UnsafePointer<CChar>?) {
  guard let cString else {
    return
  }

  UnityGameCoreCallback.deliverLegacyHandlerMessage(String(cString: cString))
}

extension UnityGameCoreCallback {
  static func installLegacyNativeHandlerIfNeeded() {
    guard let setHandler = resolveSetMessageHandler() else {
      return
    }

    setHandler(cytoidHostNativeMessageHandler)
  }

  fileprivate static func deliverLegacyHandlerMessage(_ json: String) {
    bridge?.onUnityMessage(json)
  }

  private static func resolveSetMessageHandler() -> (
    (@convention(c) ((@convention(c) (UnsafePointer<CChar>?) -> Void)?) -> Void)
  )? {
    guard let frameworkPath = UnityGameCoreRuntime.shared.loadedFrameworkPath else {
      return nil
    }

    guard let handle = dlopen(frameworkPath, RTLD_LAZY) else {
      return nil
    }

    guard let symbol = dlsym(handle, "CytoidHostNative_SetMessageHandler") else {
      return nil
    }

    return unsafeBitCast(
      symbol,
      to: (@convention(c) ((@convention(c) (UnsafePointer<CChar>?) -> Void)?) -> Void).self
    )
  }
}
#endif
