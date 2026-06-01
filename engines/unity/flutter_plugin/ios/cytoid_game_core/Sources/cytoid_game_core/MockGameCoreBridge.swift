import Foundation

final class MockGameCoreBridge {
  private let emit: (String) -> Void
  private let hostReadyDelay: TimeInterval = 0.3
  private var runtimeStarted = false
  private var surfaceVisible = false
  private var activePlayId: String?

  init(emit: @escaping (String) -> Void) {
    self.emit = emit
  }

  func ensureRuntimeStarted() {
    if runtimeStarted {
      return
    }
    runtimeStarted = true
    DispatchQueue.main.asyncAfter(deadline: .now() + hostReadyDelay) { [weak self] in
      self?.emitHostReady()
    }
  }

  func showGameSurface() {
    ensureRuntimeStarted()
    surfaceVisible = true
  }

  func hideGameSurface() {
    surfaceVisible = false
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
    activePlayId = id
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
      self?.activePlayId = nil
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
    activePlayId = nil
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
    var payload: [String: Any] = [
      "state": activePlayId == nil ? (runtimeStarted || surfaceVisible ? "ready" : "unavailable") : "busy",
      "engine": "mock",
    ]
    if let activePlayId {
      payload["activePlayId"] = activePlayId
    }
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
