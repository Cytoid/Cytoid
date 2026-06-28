import XCTest
@testable import cytoid_game_core

/// T6 contract: `UnityGameCoreRuntime`'s message-queue timeout (30s default,
/// overridable for tests via `messageQueueTimeoutSeconds`) MUST route via
/// `CytoidGameCoreBridge.handleMessageQueueTimeoutRouting` according to the
/// v2 active-session routing rule:
///
///   activeSessionId == nil  → `engine.error` ONLY (code = `runtime_unavailable`)
///   activeSessionId != nil  → `session.failed` via T4 primitive ONLY
///                              (code = `runtime_unreachable`)
///
/// Never both. The bridge's handler is invoked directly here — the runtime's
/// real timer firing is verified by code inspection; in the SwiftPM sandbox
/// the runtime module (inside `#if CYTOID_UNITY_FRAMEWORK_AVAILABLE`) does
/// not compile.
final class MessageQueueTimeoutTests: XCTestCase {

    func testNoActiveSessionEmitsEngineErrorOnly() throws {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        // No active session — pre-condition.
        XCTAssertNil(bridge.runtimeState.activeSessionId)

        bridge.handleMessageQueueTimeoutRouting()

        XCTAssertEqual(captured.values.count, 1, "exactly one engine.error envelope")
        let envelope = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(captured.values[0].utf8)) as? [String: Any]
        )
        XCTAssertEqual(envelope["type"] as? String, "engine.error")
        XCTAssertEqual(envelope["schema"] as? String, "cytoid.game-core.v2")
        let payload = try XCTUnwrap(envelope["payload"] as? [String: Any])
        let error = try XCTUnwrap(payload["error"] as? [String: Any])
        XCTAssertEqual(error["code"] as? String, "runtime_unavailable")
        XCTAssertNil(payload["outcome"], "engine.error must NOT carry an outcome.kind")

        XCTAssertEqual(bridge.runtimeState.state, .failed)
        XCTAssertEqual(bridge.runtimeState.lastError?.code, "runtime_unavailable")
    }

    func testActiveSessionDelegatesToT4PrimitiveOnly() throws {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        // Drive the state machine to BUSY with an active session.
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "session-active")
        XCTAssertEqual(bridge.runtimeState.state, .busy)
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "session-active")

        bridge.handleMessageQueueTimeoutRouting()

        XCTAssertEqual(captured.values.count, 1, "exactly one session.failed envelope")
        let envelope = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(captured.values[0].utf8)) as? [String: Any]
        )
        XCTAssertEqual(envelope["type"] as? String, "session.failed")
        XCTAssertEqual(envelope["id"] as? String, "session-active")
        let payload = try XCTUnwrap(envelope["payload"] as? [String: Any])
        XCTAssertEqual(payload["sessionId"] as? String, "session-active")
        XCTAssertNil(payload["outcome"], "session.failed payload MUST NOT carry an outcome")
        let error = try XCTUnwrap(payload["error"] as? [String: Any])
        XCTAssertEqual(error["code"] as? String, "runtime_unreachable")
        let timestamp = try XCTUnwrap(payload["timestamp"] as? NSNumber)
        XCTAssertGreaterThan(
            timestamp.intValue,
            0,
            "timestamp must be a non-zero epoch-millis value"
        )

        // After T4 synthesis: state is .failed, activeSessionId cleared.
        XCTAssertEqual(bridge.runtimeState.state, .failed)
        XCTAssertNil(bridge.runtimeState.activeSessionId)
    }

    func testRoutesEngineErrorOnlyOncePerFailureCycle() {
        // Idempotency: after a non-session timeout routes to engine.error,
        // state is .failed and activeSessionId stays nil. A second
        // handleMessageQueueTimeoutRouting call still has no active session
        // and emits another engine.error — proving the routing does NOT
        // accidentally synthesize a session.result for a phantom session.
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.handleMessageQueueTimeoutRouting()
        bridge.handleMessageQueueTimeoutRouting()

        XCTAssertEqual(captured.values.count, 2, "both calls route to engine.error")
        for json in captured.values {
            let envelope = try? JSONSerialization.jsonObject(with: Data(json.utf8)) as? [String: Any]
            XCTAssertEqual(envelope?["type"] as? String, "engine.error")
        }
    }

    func testActiveSessionRoutesViaPrimitiveExactlyOncePerSession() {
        // Idempotency: the T4 primitive's gate (activeSessionId == sessionId)
        // ensures a second invocation after the first failure (which clears
        // activeSessionId) is a no-op — no second session.failed.
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "session-once")

        bridge.handleMessageQueueTimeoutRouting()
        let firstCount = captured.values.count
        XCTAssertEqual(firstCount, 1)

        bridge.handleMessageQueueTimeoutRouting()
        // After the first routing, activeSessionId is cleared (T4's onFailure),
        // so the second call routes to engine.error instead of duplicating
        // the session.failed.
        XCTAssertEqual(captured.values.count, 2)
        let secondEnvelope = try? JSONSerialization.jsonObject(
            with: Data(captured.values[1].utf8)
        ) as? [String: Any]
        XCTAssertEqual(secondEnvelope?["type"] as? String, "engine.error")
    }

    // MARK: - Helpers

    private final class CapturedEmits {
        private(set) var values: [String] = []
        func append(_ value: String) { values.append(value) }
    }
}
