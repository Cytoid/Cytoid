import Foundation

/// Typed failure reason for `UnityGameCoreRuntime.loadIfNeeded`. Lives
/// outside the `CYTOID_UNITY_FRAMEWORK_AVAILABLE` flag so SwiftPM sandbox
/// tests can simulate framework-load failures without the real binary. The
/// bridge converts these into `engine.error` envelopes (no active session)
/// or routes via T4's `synthesizeRuntimeFailure(.unreachable, …)`
/// (active session, emits `session.failed`). `pathDescription` surfaces into
/// `engine.error.payload.error.details.frameworkPath` for host-side
/// diagnostics.
enum FrameworkLoadError: Error {
  case frameworkNotFound
  case bundleOpenFailed(path: String)
  case principalClassUnavailable(path: String)

  var pathDescription: String? {
    switch self {
    case .frameworkNotFound: return nil
    case .bundleOpenFailed(let path): return path
    case .principalClassUnavailable(let path): return path
    }
  }
}

extension FrameworkLoadError: LocalizedError {
  var errorDescription: String? {
    switch self {
    case .frameworkNotFound:
      return "UnityFramework.framework not found in the app bundle's Frameworks directory."
    case .bundleOpenFailed(let path):
      return "Unable to open UnityFramework bundle at \(path)."
    case .principalClassUnavailable(let path):
      return "UnityFramework principal class unavailable at \(path)."
    }
  }
}

#if CYTOID_UNITY_FRAMEWORK_AVAILABLE
import MachO
import UIKit
import UnityFramework

final class UnityGameCoreRuntime: NSObject, UnityFrameworkListener {
  static let shared = UnityGameCoreRuntime()

  /// Fixed timeout (v2 spec): if `sendMessage` queues an outbound message
  /// for this many seconds without the framework loading and the engine
  /// embedding, the runtime fires `messageQueueTimeoutHandler` so the bridge
  /// can emit `engine.error` (no active session) or call T4's
  /// `synthesizeRuntimeFailure(.unreachable, …)` (active session, emits
  /// `session.failed`). Internal `var` so isolated SwiftPM tests can shorten
  /// the deadline; production callers never write to it.
  internal var messageQueueTimeoutSeconds: TimeInterval = 30.0

  private var unityFramework: UnityFramework?
  private var isEmbedded = false
  private var isExclusivePresented = false
  private var pendingOutboundMessages: [String] = []
  private(set) var loadedFrameworkPath: String?

  // Timestamp of the first still-pending outbound message. Cleared on flush
  // or when the queue drains via embed. Nil whenever the queue is empty.
  private var pendingMessagesEnqueuedAt: Date?
  private var messageQueueTimer: DispatchSourceTimer?

  // SURFACE_LOST notification seam (v2 § Active-Session Runtime Failure):
  // invoked from unityDidUnload BEFORE the runtime clears its own state, so
  // the bridge can capture the live activeSessionId and synthesize a
  // session.result with error.code = "runtime_surface_lost". The bridge
  // installs this once; production code never reaches unityDidUnload without
  // the bridge having wired it (the runtime is only loaded via bridge calls).
  var surfaceLostHandler: (() -> Void)?

  // Framework-load failure / pending-queue timeout seam (T6). The runtime
  // owns only the timing + detection; the bridge owns routing (engine.error
  // vs session.result via T4 primitive) based on the live `activeSessionId`.
  // Fired at most once per queue cycle; cleared when the queue flushes.
  var messageQueueTimeoutHandler: (() -> Void)?

  var isFrameworkPresent: Bool {
    resolveFrameworkPath() != nil
  }

  var isExclusiveSessionActive: Bool {
    isExclusivePresented && isEmbedded && unityFramework != nil
  }

  /// Load (or reuse) `UnityFramework`. Returns `Result<Void, Error>` so the
  /// bridge can convert each failure mode into a structured v2 envelope
  /// (replaces the silent `NSLog` + `Bool` paths T6 removed).
  @discardableResult
  func loadIfNeeded() -> Result<Void, Error> {
    if unityFramework != nil {
      return .success(())
    }

    guard let frameworkPath = resolveFrameworkPath() else {
      return .failure(FrameworkLoadError.frameworkNotFound)
    }

    guard let bundle = Bundle(path: frameworkPath) else {
      return .failure(FrameworkLoadError.bundleOpenFailed(path: frameworkPath))
    }

    if !bundle.isLoaded {
      bundle.load()
    }

    guard
      let principalClass = bundle.principalClass as? UnityFramework.Type,
      let instance = principalClass.getInstance()
    else {
      return .failure(FrameworkLoadError.principalClassUnavailable(path: frameworkPath))
    }

    loadedFrameworkPath = frameworkPath
    unityFramework = instance
    instance.setDataBundleId("com.unity3d.framework")
    UnityGameCoreCallback.installLegacyNativeHandlerIfNeeded()
    return .success(())
  }

  @discardableResult
  private func startEmbeddedIfNeeded(displaySize: CGSize) -> Bool {
    guard displaySize.width > 0, displaySize.height > 0 else {
      return false
    }

    if isEmbedded {
      return unityFramework != nil
    }

    // startEmbeddedIfNeeded's core flow is unchanged (per T6 MUST NOT): on
    // load failure it still returns false. Only loadIfNeeded's error
    // reporting changed; here we discard the typed error and preserve the
    // historical boolean contract for presentExclusiveFullscreen.
    guard case .success = loadIfNeeded(), let unityFramework else {
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
      if pendingOutboundMessages.isEmpty {
        pendingMessagesEnqueuedAt = Date()
        scheduleMessageQueueTimeout()
      }
      pendingOutboundMessages.append(json)
      return
    }

    deliverMessage(json)
  }

  func unityDidUnload(_ notification: Notification!) {
    // Notify the bridge BEFORE clearing local state so it can capture the
    // live activeSessionId from its own runtimeState and synthesize the
    // SURFACE_LOST envelope. The handler is a one-shot notification; the
    // bridge's synthesizeRuntimeFailure is idempotent so duplicate calls
    // are safe.
    surfaceLostHandler?()
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

    cancelMessageQueueTimer()
    pendingMessagesEnqueuedAt = nil

    let messages = pendingOutboundMessages
    pendingOutboundMessages.removeAll()
    for message in messages {
      deliverMessage(message)
    }
  }

  private func scheduleMessageQueueTimeout() {
    cancelMessageQueueTimer()
    let timer = DispatchSource.makeTimerSource(queue: .main)
    timer.schedule(deadline: .now() + messageQueueTimeoutSeconds)
    timer.setEventHandler { [weak self] in
      self?.handleMessageQueueTimeout()
    }
    timer.resume()
    messageQueueTimer = timer
  }

  private func cancelMessageQueueTimer() {
    messageQueueTimer?.cancel()
    messageQueueTimer = nil
  }

    private func handleMessageQueueTimeout() {
        cancelMessageQueueTimer()
        guard !isEmbedded, !pendingOutboundMessages.isEmpty else { return }
        let handler = messageQueueTimeoutHandler
        // Drop the queued messages: they were never delivered and must not be
        // flushed later if the runtime eventually embeds (stale dispatch).
        pendingOutboundMessages.removeAll()
        pendingMessagesEnqueuedAt = nil
        handler?()
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
