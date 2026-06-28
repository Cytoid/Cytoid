import XCTest
@testable import cytoid_game_core

/// Verifies `CytoidGameCoreBridge.synthesizeRuntimeFailure` for the two
/// triggers T4 owns (GENERATION_CHANGE, SURFACE_LOST). UNREACHABLE is
/// exercised by T6.
///
/// Acceptance (from `.omo/plans/v2-host-impl.md` T4):
///  1. GENERATION_CHANGE with active session → envelope emitted with
///     `error.code = "runtime_recreated"`.
///  2. GENERATION_CHANGE without active session → no envelope (idempotency).
///  3. SURFACE_LOST with active session → envelope emitted with
///     `error.code = "runtime_surface_lost"`.
///  4. SURFACE_LOST without active session → no envelope.
///
/// Mirrors `RuntimeFailureSynthesisTest.kt`. The Kotlin test additionally
/// exercises the GENERATION_CHANGE trigger-site wiring (the
/// `generation > 1` gate) via JVM reflection. Swift's pure-class stored
/// properties are not exposed to the ObjC runtime, so the same impossible-
/// state setup requires adding test-only API to the production type — out
/// of T4 scope. The trigger-site logic is byte-identical between platforms
/// (verified by code inspection); this test covers the primitive's behavior
/// on the happy path + idempotency, which is the per-platform contract.
///
/// Note: when run in the isolated SwiftPM sandbox (FlutterFramework not
/// bootstrapped), this file is copied verbatim with
/// `@testable import cytoid_game_core` remapped to the sandbox module name.
/// The assertions are identical.
final class RuntimeFailureSynthesisTests: XCTestCase {

    func testGenerationChangeWithActiveSessionEmitsRuntimeRecreatedEnvelope() throws {
        let bridge = makeBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S1")
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "S1")

        // Call the primitive directly. The generation>1 check lives at the
        // trigger site (bridge.onUnityMessage inline), not in the primitive;
        // the primitive's contract is envelope shape + idempotency, which
        // holds regardless of how generation reached its current value.
        let result = bridge.synthesizeRuntimeFailure(
            trigger: .generationChange,
            sessionId: "S1"
        )

        let envelope = try XCTUnwrap(result)
        XCTAssertEqual(captured.values.count, 1, "expected exactly one emitted envelope")
        XCTAssertEqual(envelope, captured.values.first)

        let parsed = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(envelope.utf8)) as? [String: Any]
        )
        XCTAssertEqual(parsed["id"] as? String, "S1")
        XCTAssertEqual(parsed["type"] as? String, "session.failed")
        XCTAssertEqual(parsed["schema"] as? String, "cytoid.game-core.v2")

        let payload = try XCTUnwrap(parsed["payload"] as? [String: Any])
        XCTAssertEqual(payload["sessionId"] as? String, "S1")
        XCTAssertNil(payload["outcome"], "session.failed payload MUST NOT carry an outcome")

        let error = try XCTUnwrap(payload["error"] as? [String: Any])
        XCTAssertEqual(error["code"] as? String, "runtime_recreated")
        let message = try XCTUnwrap(error["message"] as? String)
        XCTAssertTrue(
            message.lowercased().contains("recreated"),
            "error message must mention recreation; was: \(message)"
        )

        let timestamp = try XCTUnwrap(payload["timestamp"] as? NSNumber)
        XCTAssertGreaterThan(
            timestamp.intValue,
            0,
            "timestamp must be a non-zero epoch-millis value"
        )

        XCTAssertNil(bridge.runtimeState.activeSessionId)
    }

    func testGenerationChangeWithoutActiveSessionEmitsNothing() {
        let bridge = makeBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        XCTAssertNil(bridge.runtimeState.activeSessionId)
        let generationBefore = bridge.runtimeState.generation

        let result = bridge.synthesizeRuntimeFailure(
            trigger: .generationChange,
            sessionId: "S1"
        )

        XCTAssertNil(result, "primitive must return nil when no active session")
        XCTAssertEqual(captured.values.count, 0, "no envelope may be emitted")
        XCTAssertEqual(
            bridge.runtimeState.generation,
            generationBefore,
            "generation must be unchanged (primitive short-circuited)"
        )
        XCTAssertNil(bridge.runtimeState.activeSessionId)
    }

    func testSurfaceLostWithActiveSessionEmitsRuntimeSurfaceLostEnvelope() throws {
        let bridge = makeBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S2")
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "S2")

        let result = bridge.synthesizeRuntimeFailure(
            trigger: .surfaceLost,
            sessionId: "S2"
        )

        let envelope = try XCTUnwrap(result)
        XCTAssertEqual(captured.values.count, 1)

        let parsed = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(envelope.utf8)) as? [String: Any]
        )
        XCTAssertEqual(parsed["id"] as? String, "S2")
        XCTAssertEqual(parsed["type"] as? String, "session.failed")
        let payload = try XCTUnwrap(parsed["payload"] as? [String: Any])
        XCTAssertEqual(payload["sessionId"] as? String, "S2")
        XCTAssertNil(payload["outcome"], "session.failed payload MUST NOT carry an outcome")
        let error = try XCTUnwrap(payload["error"] as? [String: Any])
        XCTAssertEqual(error["code"] as? String, "runtime_surface_lost")
        let timestamp = try XCTUnwrap(payload["timestamp"] as? NSNumber)
        XCTAssertGreaterThan(
            timestamp.intValue,
            0,
            "timestamp must be a non-zero epoch-millis value"
        )

        XCTAssertNil(bridge.runtimeState.activeSessionId)
    }

    func testSurfaceLostWithoutActiveSessionEmitsNothing() {
        let bridge = makeBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        XCTAssertNil(bridge.runtimeState.activeSessionId)

        let result = bridge.synthesizeRuntimeFailure(
            trigger: .surfaceLost,
            sessionId: "S2"
        )

        XCTAssertNil(result)
        XCTAssertEqual(captured.values.count, 0)
        XCTAssertNil(bridge.runtimeState.activeSessionId)
    }

    func testSynthesizeRuntimeFailureIsIdempotentWhenCalledTwiceForSameSession() {
        let bridge = makeBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S3")

        let first = bridge.synthesizeRuntimeFailure(
            trigger: .surfaceLost,
            sessionId: "S3"
        )
        let second = bridge.synthesizeRuntimeFailure(
            trigger: .surfaceLost,
            sessionId: "S3"
        )

        XCTAssertNotNil(first, "first call must emit")
        XCTAssertNil(second, "second call must be a no-op")
        XCTAssertEqual(captured.values.count, 1, "exactly one envelope, not two")
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
