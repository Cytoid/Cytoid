import Foundation
import MachO
import UIKit

#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
import UnityFramework

final class UnityGameCoreRuntime: NSObject, UnityFrameworkListener {
  static let shared = UnityGameCoreRuntime()

  private var unityFramework: UnityFramework?
  private var isEmbedded = false
  private var isExclusivePresented = false
  private var pendingOutboundMessages: [String] = []
  private(set) var loadedFrameworkPath: String?

  var isFrameworkPresent: Bool {
    resolveFrameworkPath() != nil
  }

  var isExclusiveSessionActive: Bool {
    isExclusivePresented && isEmbedded && unityFramework != nil
  }

  @discardableResult
  func loadIfNeeded() -> Bool {
    if unityFramework != nil {
      return true
    }

    guard let frameworkPath = resolveFrameworkPath() else {
      NSLog("[CytoidGameCore] UnityFramework.framework not found.")
      return false
    }

    guard let bundle = Bundle(path: frameworkPath) else {
      NSLog("[CytoidGameCore] Unable to open UnityFramework bundle at \(frameworkPath).")
      return false
    }

    if !bundle.isLoaded {
      bundle.load()
    }

    guard
      let principalClass = bundle.principalClass as? UnityFramework.Type,
      let instance = principalClass.getInstance()
    else {
      NSLog("[CytoidGameCore] UnityFramework principal class unavailable.")
      return false
    }

    loadedFrameworkPath = frameworkPath
    unityFramework = instance
    instance.setDataBundleId("com.unity3d.framework")
    UnityGameCoreCallback.installLegacyNativeHandlerIfNeeded()
    return true
  }

  @discardableResult
  private func startEmbeddedIfNeeded(displaySize: CGSize) -> Bool {
    guard displaySize.width > 0, displaySize.height > 0 else {
      return false
    }

    if isEmbedded {
      return unityFramework != nil
    }

    guard loadIfNeeded(), let unityFramework else {
      return false
    }

    let executeHeader = #dsohandle.assumingMemoryBound(to: MachHeader.self)
    unityFramework.setExecuteHeader(executeHeader)
    unityFramework.runEmbedded(
      withArgc: CommandLine.argc,
      argv: CommandLine.unsafeArgv,
      appLaunchOpts: nil
    )

    unityFramework.register(self)
    isEmbedded = true
    UnityGameCoreCallback.installLegacyNativeHandlerIfNeeded()
    flushPendingMessages()
    return true
  }

  @discardableResult
  func presentExclusiveFullscreen() -> Bool {
    let bounds = UIScreen.main.bounds
    let landscapeSize = CGSize(
      width: max(bounds.width, bounds.height),
      height: min(bounds.width, bounds.height)
    )

    guard startEmbeddedIfNeeded(displaySize: landscapeSize), let unityFramework else {
      return false
    }

    unityFramework.showUnityWindow()
    unityFramework.pause(false)

    if let appWindow = unityFramework.appController()?.window {
      appWindow.frame = bounds
      appWindow.windowLevel = UIWindow.Level.normal + 1
      appWindow.isHidden = false
      appWindow.isUserInteractionEnabled = true
      appWindow.makeKeyAndVisible()
    }

    isExclusivePresented = true
    return true
  }

  func dismissExclusiveFullscreen() {
    guard isExclusivePresented, let unityFramework else {
      return
    }

    unityFramework.pause(true)
    hideUnityWindow()
    restoreFlutterWindow()
    isExclusivePresented = false
  }

  func sendMessage(_ json: String) {
    if !isEmbedded {
      pendingOutboundMessages.append(json)
      return
    }

    deliverMessage(json)
  }

  func unityDidUnload(_ notification: Notification!) {
    unityFramework = nil
    isEmbedded = false
    isExclusivePresented = false
    loadedFrameworkPath = nil
  }

  private func hideUnityWindow() {
    guard let appWindow = unityFramework?.appController()?.window else {
      return
    }

    appWindow.isHidden = true
    appWindow.isUserInteractionEnabled = false
  }

  private func restoreFlutterWindow() {
    let unityWindow = unityFramework?.appController()?.window
    guard
      let scene = UIApplication.shared.connectedScenes
        .compactMap({ $0 as? UIWindowScene })
        .first(where: {
          $0.activationState == .foregroundActive || $0.activationState == .foregroundInactive
        })
    else {
      return
    }

    for window in scene.windows where window !== unityWindow {
      window.windowLevel = .normal
      window.makeKeyAndVisible()
      return
    }
  }

  private func deliverMessage(_ json: String) {
    unityFramework?.sendMessageToGO(
      withName: "GameBridge",
      functionName: "OnBridgeMessage",
      message: json
    )
  }

  private func flushPendingMessages() {
    guard isEmbedded, !pendingOutboundMessages.isEmpty else {
      return
    }

    let messages = pendingOutboundMessages
    pendingOutboundMessages.removeAll()
    for message in messages {
      deliverMessage(message)
    }
  }

  private func resolveFrameworkPath() -> String? {
    if let bundledPath = Bundle.main.path(
      forResource: "UnityFramework",
      ofType: "framework",
      inDirectory: "Frameworks"
    ) {
      return bundledPath
    }

    return nil
  }
}
#endif
