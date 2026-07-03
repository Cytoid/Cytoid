import XCTest
@testable import cytoid_game_core

/// Regression coverage for the rejected-`session.result` state-machine bug,
/// mirroring `RejectedSessionResultTransitionTest.kt` on iOS.
///
/// Before the fix, `CytoidGameCoreBridge.emitEvent` skipped
/// `runtimeState.onSessionEnded()` whenever `outcome.kind == "rejected"`,
/// leaving the runtime stuck in busy with a stale `activeSessionId`. The
/// v2 § session.result contract treats `session.result` as terminal for ALL
/// outcome kinds, including `rejected`.
///
/// The host enters busy optimistically at `session.start` time, so a rejected
/// result MUST transition busy → ready so the next session can launch.
final class RejectedSessionResultTransitionTests: XCTestCase {

    func testRejectedSessionResultForActiveSessionTransitionsBusyToReady() throws {
        let bridge = makeBridge()
        bridge.emitOverride = { _ in /* forward-only */ }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S1")
        XCTAssertEqual(bridge.runtimeState.state, .busy)
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "S1")

        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.result",\
             "payload":{"sessionId":"S1","outcome":{"kind":"rejected"},\
             "error":{"code":"level_not_found","message":"missing"}}}
            """
        )

        XCTAssertEqual(
            bridge.runtimeState.state,
            .ready,
            "rejected session.result must transition busy → ready"
        )
        XCTAssertNil(
            bridge.runtimeState.activeSessionId,
            "rejected session.result must clear activeSessionId"
        )
    }

    func testRuntimeIsReusableAfterRejectedSessionResult() {
        let bridge = makeBridge()
        bridge.emitOverride = { _ in /* forward-only */ }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S1")
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.result",\
             "payload":{"sessionId":"S1","outcome":{"kind":"rejected"},\
             "error":{"code":"level_not_found","message":"missing"}}}
            """
        )
        XCTAssertEqual(bridge.runtimeState.state, .ready)

        bridge.runtimeState.onSessionStarted(sessionId: "S2")
        XCTAssertEqual(
            bridge.runtimeState.state,
            .busy,
            "second session must be launchable after a rejected first"
        )
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "S2")
    }

    func testRejectedSessionResultForDifferentSessionIdDoesNotDisturbActiveSession() {
        let bridge = makeBridge()
        bridge.emitOverride = { _ in /* forward-only */ }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S-active")
        XCTAssertEqual(bridge.runtimeState.state, .busy)

        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S-stale","type":"session.result",\
             "payload":{"sessionId":"S-stale","outcome":{"kind":"rejected"},\
             "error":{"code":"level_not_found","message":"missing"}}}
            """
        )

        XCTAssertEqual(
            bridge.runtimeState.state,
            .busy,
            "stale-id rejected result must not disturb the active session"
        )
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "S-active")
    }

    func testRejectedSessionResultEnvelopeIsStillForwardedToEventSink() throws {
        let bridge = makeBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S1")
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.result",\
             "payload":{"sessionId":"S1","outcome":{"kind":"rejected"},\
             "error":{"code":"level_not_found","message":"missing"}}}
            """
        )

        XCTAssertEqual(captured.values.count, 1, "rejected session.result must still be forwarded")
        let parsed = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(captured.values.first!.utf8)) as? [String: Any]
        )
        XCTAssertEqual(parsed["type"] as? String, "session.result")
        let payload = try XCTUnwrap(parsed["payload"] as? [String: Any])
        let outcome = try XCTUnwrap(payload["outcome"] as? [String: Any])
        XCTAssertEqual(outcome["kind"] as? String, "rejected")
    }

    // MARK: - Helpers

    private func makeBridge() -> CytoidGameCoreBridge {
        CytoidGameCoreBridge()
    }

    private final class CapturedEmits {
        private(set) var values: [String] = []
        func append(_ value: String) { values.append(value) }
    }
}
