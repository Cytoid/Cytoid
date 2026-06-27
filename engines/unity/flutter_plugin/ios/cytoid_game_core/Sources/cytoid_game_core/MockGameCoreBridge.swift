import Foundation

final class MockGameCoreBridge {
  private let emit: (String) -> Void
  private let hostReadyDelay: TimeInterval = 0.3

  // v2 runtime state mirror. The mock bridge keeps its own state machine so
  // bridge.status responses reflect the same lifecycle as the real bridge,
  // satisfying "Mock runtimes must implement the same protocol semantics".
  private let runtimeState = RuntimeStateMachine()

  init(emit: @escaping (String) -> Void) {
    self.emit = emit
  }

  func ensureRuntimeStarted() {
    // Only schedule emitHostReady on the unavailable→starting transition;
    // repeated calls during startup (e.g. showGameSurface re-entering) must
    // not stack a second delayed ready emission.
    let isFirstStart = runtimeState.state == .unavailable
    guard isFirstStart || runtimeState.state == .starting else {
      return
    }
    runtimeState.onRequestStart()
    if isFirstStart {
      DispatchQueue.main.asyncAfter(deadline: .now() + hostReadyDelay) { [weak self] in
        self?.emitHostReady()
      }
    }
  }

  func showGameSurface() {
    // Resume a suspended mock so handleGameStart can re-enter busy;
    // ensureRuntimeStarted alone no-ops outside starting.
    if runtimeState.state == .suspended {
      runtimeState.onResume()
    }
    ensureRuntimeStarted()
  }

  func hideGameSurface() {
    runtimeState.onSuspend()
  }

  func onOutboundMessage(_ jsonString: String) {
    guard
      let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
      let type = envelope["type"] as? String
    else {
      return
    }

    switch type {
    case "bridge.status":
      handleStatus(envelope)
    case "bridge.ping":
      handlePing(envelope)
    case "bridge.play.start":
      handleGameStart(envelope)
    case "bridge.settings.update":
      handleSettingsUpdate(envelope)
    case "bridge.play.end":
      handleSessionEnd(envelope)
    default:
      break
    }
  }

  private func handlePing(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else {
      return
    }

    let payload = envelope["payload"] as? [String: Any] ?? [:]
    emit([
      "v": 1,
      "id": id,
      "type": "game.pong",
      "payload": payload,
    ])
  }

  private func handleGameStart(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else {
      return
    }
    runtimeState.onSessionStarted(sessionId: id)
    emitSampleGameLogs(playId: id)

    let launchPayload = envelope["payload"] as? [String: Any] ?? [:]
    let gameMode = (launchPayload["gameMode"] as? String) ?? ""
    let tierPlay = launchPayload["tierPlay"] as? [String: Any]
    let resultPayload: [String: Any]
    if gameMode.caseInsensitiveCompare("Tier") == .orderedSame, let tierPlay {
      resultPayload = buildMockTierResult(tierPlay: tierPlay)
    } else {
      resultPayload = [
        "completed": false,
        "failed": true,
        "usedAutoMod": false,
        "error": "Unity artifact not mounted",
        "timestamp": ISO8601DateFormatter().string(from: Date()),
      ]
    }

    DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) { [weak self] in
      self?.runtimeState.onSessionEnded()
      self?.emit([
        "v": 1,
        "id": id,
        "type": "game.play.result",
        "payload": resultPayload,
      ])
    }
  }

  private func buildMockTierResult(tierPlay: [String: Any]) -> [String: Any] {
    let maxHealth = (tierPlay["maxHealth"] as? NSNumber)?.doubleValue ?? 1000
    let initialHealth = (tierPlay["initialHealth"] as? NSNumber)?.doubleValue ?? maxHealth
    let initialCombo = (tierPlay["initialCombo"] as? NSNumber)?.intValue ?? 0
    let finalHealth = max(initialHealth * 0.85, 0)
    let endingCombo = initialCombo + 50

    return [
      "completed": true,
      "failed": false,
      "usedAutoMod": false,
      "gameMode": "Tier",
      "timestamp": ISO8601DateFormatter().string(from: Date()),
      "levelId": "mock-level",
      "score": 950000,
      "accuracy": 0.97,
      "maxCombo": endingCombo,
      "tierPlay": [
        "tierId": tierPlay["tierId"] as Any,
        "stageIndex": (tierPlay["stageIndex"] as? NSNumber)?.intValue ?? 0,
        "finalHealth": finalHealth,
        "maxHealth": maxHealth,
        "endingCombo": endingCombo,
      ],
    ]
  }

  private func handleSessionEnd(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else {
      return
    }
    NSLog("[CytoidGameCore] bridge.play.end received")
    runtimeState.onSessionEnded()
    emit([
      "v": 1,
      "id": id,
      "type": "game.play.ended",
      "payload": ["ended": true],
    ])
  }

  private func handleSettingsUpdate(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else {
      return
    }
    emit([
      "v": 1,
      "id": id,
      "type": "game.settings.updated",
      "payload": ["applied": true],
    ])
  }

  private func handleStatus(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else {
      return
    }
    // v2 runtime-status snapshot shape (engine/mode/state/generation +
    // conditional activeSessionId/error), replacing the legacy
    // {state, engine, activePlayId} payload.
    let payload = runtimeState.snapshot(engine: "mock", mode: "mock")
    emit([
      "v": 1,
      "id": id,
      "type": "game.status",
      "payload": payload,
    ])
  }

  private func emitSampleGameLogs(playId: String) {
    let samples: [(String, String, String?)] = [
      ("log", "Mock game runtime started play", nil),
      ("warning", "Mock Unity: storyboard texture cache miss", nil),
      ("error", "Mock Unity: Unity artifact not mounted", "MockGameCoreBridge.swift:handleGameStart"),
    ]

    DispatchQueue.main.asyncAfter(deadline: .now() + 0.6) { [weak self] in
      let logs = samples.map { sample -> [String: Any] in
        var entry: [String: Any] = [
          "level": sample.0,
          "message": sample.1,
          "timestamp": ISO8601DateFormatter().string(from: Date()),
          "playId": playId,
        ]
        if let stackTrace = sample.2 {
          entry["stackTrace"] = stackTrace
        }
        return entry
      }

      self?.emit([
        "v": 1,
        "id": UUID().uuidString,
        "type": "game.logs.batch",
        "payload": [
          "reason": "trigger",
          "triggerLevel": "error",
          "timestamp": ISO8601DateFormatter().string(from: Date()),
          "truncated": false,
          "logs": logs,
        ],
      ])
    }
  }

  private func emitHostReady() {
    runtimeState.onEngineReady()
    emit([
      "v": 1,
      "id": UUID().uuidString,
      "type": "game.ready",
      "payload": [
        "initialized": true,
        "engine": "mock",
        "engineVersion": "cytoid_game_core",
      ],
    ])
  }

  private func emit(_ envelope: [String: Any]) {
    guard
      let data = try? JSONSerialization.data(withJSONObject: envelope),
      let json = String(data: data, encoding: .utf8)
    else {
      return
    }

    DispatchQueue.main.async { [emit] in
      emit(json)
    }
  }
}
