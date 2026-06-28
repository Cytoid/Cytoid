import Foundation

final class MockGameCoreBridge {
  private static let protocolSchemaV2 = "cytoid.game-core.v2"
  private let emit: (String) -> Void
  private let hostReadyDelay: TimeInterval = 0.3
  private let resultDelay: TimeInterval = 1.0

  // Protocol smoke fake: it never emits session.telemetry; results always carry
  // a false/0/0 telemetry summary, including sessions with auto-class mods.
  private let runtimeState = RuntimeStateMachine()
  private var pendingResultWorkItem: DispatchWorkItem?

  init(emit: @escaping (String) -> Void) {
    self.emit = emit
  }

  func ensureRuntimeStarted() {
    let isFirstStart = runtimeState.state == .unavailable
    guard isFirstStart || runtimeState.state == .starting else { return }
    runtimeState.onRequestStart()
    if isFirstStart {
      DispatchQueue.main.asyncAfter(deadline: .now() + hostReadyDelay) { [weak self] in self?.emitHostReady() }
    }
  }

  func showGameSurface() {
    if runtimeState.state == .suspended { runtimeState.onResume() }
    ensureRuntimeStarted()
  }

  func hideGameSurface() {
    runtimeState.onSuspend()
  }

  func onOutboundMessage(_ jsonString: String) {
    guard let data = jsonString.data(using: .utf8),
      let envelope = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
      envelope["schema"] as? String == Self.protocolSchemaV2,
      let type = envelope["type"] as? String else { return }
    switch type {
    case "session.start": handleSessionStart(envelope)
    case "session.cancel": handleSessionCancel(envelope)
    case "health.check": handleHealthCheck(envelope)
    case "settings.apply": handleSettingsApply(envelope)
    default: break
    }
  }

  private func handleSessionStart(_ envelope: [String: Any]) {
    guard let sessionId = envelope["id"] as? String else { return }
    guard let launchPayload = envelope["payload"] as? [String: Any] else {
      emitSessionResult(sessionId: sessionId, payload: rejectedResultPayload(
        sessionId: sessionId, mode: "unknown", mods: [], usedAutoMod: false,
        message: "session.start payload must be an object"))
      return
    }

    let mode = launchPayload["mode"] as? String
    let mods = stringArray(launchPayload["mods"])
    let usedAutoMod = containsAutoMod(mods)
    guard let mode, isSupportedMode(mode) else {
      emitSessionResult(sessionId: sessionId, payload: rejectedResultPayload(
        sessionId: sessionId, mode: mode ?? "unknown", mods: mods, usedAutoMod: usedAutoMod,
        message: "session.start mode is missing or unsupported"))
      return
    }

    runtimeState.onSessionStarted(sessionId: sessionId)
    emitSampleLogs(sessionId: sessionId)
    emit(["schema": Self.protocolSchemaV2, "id": sessionId, "type": "session.started", "payload": [
      "sessionId": sessionId, "mode": mode, "generation": runtimeState.generation,
    ]])

    let resultPayload = buildDefaultResult(
      sessionId: sessionId, mode: mode, mods: mods, usedAutoMod: usedAutoMod, launchPayload: launchPayload)
    let workItem = DispatchWorkItem { [weak self] in
      guard let self else { return }
      guard self.runtimeState.activeSessionId == sessionId else { return }
      self.runtimeState.onSessionEnded()
      self.emitSessionResult(sessionId: sessionId, payload: resultPayload)
    }
    pendingResultWorkItem = workItem
    DispatchQueue.main.asyncAfter(deadline: .now() + resultDelay, execute: workItem)
  }

  private func buildDefaultResult(
    sessionId: String, mode: String, mods: [String], usedAutoMod: Bool, launchPayload: [String: Any]
  ) -> [String: Any] {
    var payload = resultBase(
      sessionId: sessionId, mode: mode, mods: mods,
      outcome: ["kind": isCalibrationMode(mode) ? "calibration" : "completed"], usedAutoMod: usedAutoMod)
    if isCalibrationMode(mode) {
      payload["calibration"] = ["baseNoteOffset": 0.0, "levelNoteOffset": 0.0]
      return payload
    }
    payload["level"] = (launchPayload["level"] as? [String: Any]) ?? [
      "id": "mock-level", "title": "Mock Level", "difficulty": "mock", "difficultyLevel": 1,
    ]
    if mode == "tier" {
      let tierResult = buildMockTierResult(tier: launchPayload["tier"] as? [String: Any] ?? [:])
      payload["tier"] = tierResult.tier
      payload["score"] = scorePayload(maxCombo: tierResult.combo)
    } else {
      payload["score"] = scorePayload(maxCombo: 1_234)
    }
    return payload
  }

  private func buildMockTierResult(tier: [String: Any]) -> (tier: [String: Any], combo: Int) {
    let maxHealth = (tier["maxHealth"] as? NSNumber)?.doubleValue ?? 1_000
    let initialHealth = (tier["initialHealth"] as? NSNumber)?.doubleValue ?? maxHealth
    let combo = ((tier["initialCombo"] as? NSNumber)?.intValue ?? 0) + 50
    return (tier: [
      "tierId": tier["tierId"] as? String ?? "mock-tier",
      "stageIndex": (tier["stageIndex"] as? NSNumber)?.intValue ?? 0,
      "stageCount": (tier["stageCount"] as? NSNumber)?.intValue ?? 1,
      "health": max(initialHealth * 0.85, 0),
      "maxHealth": maxHealth,
      "combo": combo,
    ], combo: combo)
  }

  private func handleSessionCancel(_ envelope: [String: Any]) {
    guard let sessionId = envelope["id"] as? String else { return }
    guard runtimeState.activeSessionId == sessionId else { return }
    pendingResultWorkItem?.cancel()
    pendingResultWorkItem = nil
    let payload = envelope["payload"] as? [String: Any] ?? [:]
    let mode = payload["mode"] as? String ?? "ranked"
    let mods = stringArray(payload["mods"])
    runtimeState.onSessionEnded()
    emitSessionResult(sessionId: sessionId, payload: resultBase(
      sessionId: sessionId, mode: mode, mods: mods,
      outcome: ["kind": "cancelled", "reason": payload["reason"] as? String ?? "unknown"],
      usedAutoMod: containsAutoMod(mods)))
  }

  private func handleHealthCheck(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else { return }
    var payload: [String: Any] = [
      "engine": "mock", "generation": runtimeState.generation, "state": runtimeState.state.wireName,
    ]
    if let activeSessionId = runtimeState.activeSessionId { payload["activeSessionId"] = activeSessionId }
    emit(["schema": Self.protocolSchemaV2, "id": id, "type": "health.ok", "payload": payload])
  }

  private func handleSettingsApply(_ envelope: [String: Any]) {
    guard let id = envelope["id"] as? String else { return }
    let payload = envelope["payload"] as? [String: Any] ?? [:]
    emit(["schema": Self.protocolSchemaV2, "id": id, "type": "settings.applied", "payload": [
      "applied": true,
      "appliedFields": payload.keys.sorted(),
      "deferredFields": [],
      "rejectedFields": [],
      "errors": [],
    ]])
  }

  private func emitSampleLogs(sessionId: String) {
    let samples: [(String, String, String?)] = [
      ("debug", "Mock runtime started session", nil),
      ("warning", "Mock Unity: storyboard texture cache miss", nil),
      ("error", "Mock Unity: Unity artifact not mounted", "MockGameCoreBridge.swift:handleSessionStart"),
    ]
    DispatchQueue.main.asyncAfter(deadline: .now() + 0.6) { [weak self] in
      let now = Self.epochMilliseconds()
      let logs = samples.map { sample -> [String: Any] in
        var entry: [String: Any] = [
          "level": sample.0, "message": sample.1, "timestamp": now, "sessionId": sessionId,
        ]
        if let stackTrace = sample.2 { entry["stackTrace"] = stackTrace }
        return entry
      }
      self?.emit(["schema": Self.protocolSchemaV2, "id": UUID().uuidString, "type": "logs.batch", "payload": [
        "reason": "trigger", "triggerLevel": "error", "timestamp": now, "truncated": false, "logs": logs,
      ]])
    }
  }

  private func emitHostReady() {
    runtimeState.onEngineReady()
    emit(["schema": Self.protocolSchemaV2, "id": UUID().uuidString, "type": "engine.ready", "payload": [
      "engine": "mock",
      "engineVersion": "cytoid_game_core",
      "generation": runtimeState.generation,
      "display": ["targetFrameRate": 60, "screenRefreshRate": 60],
    ]])
  }

  private func emitSessionResult(sessionId: String, payload: [String: Any]) {
    emit(["schema": Self.protocolSchemaV2, "id": sessionId, "type": "session.result", "payload": payload])
  }

  private func rejectedResultPayload(
    sessionId: String, mode: String, mods: [String], usedAutoMod: Bool, message: String
  ) -> [String: Any] {
    var payload = resultBase(
      sessionId: sessionId, mode: mode, mods: mods, outcome: ["kind": "rejected"], usedAutoMod: usedAutoMod)
    payload["error"] = ["code": "invalid_payload", "message": message]
    return payload
  }

  private func resultBase(
    sessionId: String, mode: String, mods: [String], outcome: [String: Any], usedAutoMod: Bool
  ) -> [String: Any] {
    [
      "sessionId": sessionId,
      "mode": mode,
      "mods": mods,
      "outcome": outcome,
      "flags": ["usedAutoMod": usedAutoMod],
      "telemetry": ["available": false, "eventsRecorded": 0, "bytes": 0],
      "timestamp": Self.epochMilliseconds(),
    ]
  }

  private func scorePayload(maxCombo: Int) -> [String: Any] {
    [
      "score": 950_000,
      "accuracy": 0.97,
      "maxCombo": maxCombo,
      "gradeCounts": ["perfect": 1_000, "great": 20, "good": 0, "bad": 0, "miss": 0],
      "early": 0,
      "late": 0,
      "averageTimingError": 0.0,
      "standardTimingError": 0.0,
    ]
  }

  private func stringArray(_ value: Any?) -> [String] {
    (value as? [Any])?.compactMap { $0 as? String } ?? []
  }

  private func containsAutoMod(_ mods: [String]) -> Bool {
    let autoMods = Set(["auto", "autodrag", "autohold", "autoflick"])
    return mods.contains { autoMods.contains($0.replacingOccurrences(of: "_", with: "").lowercased()) }
  }

  private func isSupportedMode(_ mode: String) -> Bool {
    mode == "ranked" || mode == "practice" || mode == "tier" || isCalibrationMode(mode)
  }

  private func isCalibrationMode(_ mode: String) -> Bool {
    mode == "calibration" || mode == "globalCalibration"
  }

  private func emit(_ envelope: [String: Any]) {
    guard let data = try? JSONSerialization.data(withJSONObject: envelope),
      let json = String(data: data, encoding: .utf8) else { return }
    DispatchQueue.main.async { [emit] in emit(json) }
  }

  private static func epochMilliseconds() -> Int {
    Int(Date().timeIntervalSince1970 * 1_000)
  }
}
