import XCTest
@testable import cytoid_game_core

/// T6 contract: `ensureRuntimeStarted` MUST
///  (a) on framework load failure → transition state to `.failed`, set
///      `lastError`, and emit exactly one `engine.error` envelope with
///      `error.code = "runtime_unavailable"` and a message naming the
///      framework path;
///  (b) on framework load success → leave state in `.starting` until
///      `engine.ready` arrives, after which state transitions to `.ready`.
///
/// Uses the bridge's `loadFrameworkOverride` test seam — no real
/// UnityFramework binary is required. Mirrors the SwiftPM sandbox convention
/// used by T4's `RuntimeFailureSynthesisTests`.
final class EnsureRuntimeStartedContractTests: XCTestCase {

    func testLoadFailureTransitionsToFailedAndEmitsEngineError() throws {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        bridge.loadFrameworkOverride = {
            .failure(FrameworkLoadError.bundleOpenFailed(path: "/App/Frameworks/UnityFramework.framework"))
        }

        bridge.ensureRuntimeStarted()

        XCTAssertEqual(bridge.runtimeState.state, .failed)
        let lastError = try XCTUnwrap(bridge.runtimeState.lastError)
        XCTAssertEqual(lastError.code, "runtime_unavailable")
        XCTAssertTrue(
            lastError.message.contains("UnityFramework.framework"),
            "engine.error message should name the framework path; was: \(lastError.message)"
        )

        XCTAssertEqual(captured.values.count, 1, "exactly one engine.error envelope")

        let envelope = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(captured.values[0].utf8)) as? [String: Any]
        )
        XCTAssertEqual(envelope["type"] as? String, "engine.error")
        XCTAssertEqual(envelope["schema"] as? String, "cytoid.game-core.v2")

        let payload = try XCTUnwrap(envelope["payload"] as? [String: Any])
        let error = try XCTUnwrap(payload["error"] as? [String: Any])
        XCTAssertEqual(error["code"] as? String, "runtime_unavailable")
        let message = try XCTUnwrap(error["message"] as? String)
        XCTAssertTrue(message.contains("UnityFramework.framework"))

        let details = try XCTUnwrap(error["details"] as? [String: Any])
        XCTAssertEqual(details["frameworkPath"] as? String, "/App/Frameworks/UnityFramework.framework")
    }

    func testLoadSuccessKeepsStartingUntilEngineReady() throws {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }
        bridge.loadFrameworkOverride = { .success(()) }

        bridge.ensureRuntimeStarted()
        XCTAssertEqual(bridge.runtimeState.state, .starting, "success must leave state in starting")
        XCTAssertEqual(captured.values.count, 0, "no engine.error before failure")

        // Simulate the engine.ready arrival — the bridge drives onEngineReady
        // and resumes any parked waitForReady waiters.
        let readyEnvelope = "{\"schema\":\"cytoid.game-core.v2\",\"id\":\"ready-1\",\"type\":\"engine.ready\",\"payload\":{\"engine\":\"mock\"}}"
        bridge.onUnityMessage(readyEnvelope)

        XCTAssertEqual(bridge.runtimeState.state, .ready)
        XCTAssertEqual(bridge.runtimeState.generation, 1)
    }

    func testLoadFailureWithCustomErrorSurfacesNSErrorPath() throws {
        // An override returning a non-FrameworkLoadError (e.g. a test-thrown
        // NSError with frameworkPath in userInfo) still surfaces the path
        // into the engine.error envelope's details.frameworkPath.
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        let nsError = NSError(
            domain: "CytoidTest",
            code: 99,
            userInfo: [NSLocalizedDescriptionKey: "Bundle missing", "frameworkPath": "/X.framework"]
        )
        bridge.loadFrameworkOverride = { .failure(nsError) }

        bridge.ensureRuntimeStarted()

        XCTAssertEqual(bridge.runtimeState.state, .failed)
        let envelope = try XCTUnwrap(
            JSONSerialization.jsonObject(with: Data(captured.values[0].utf8)) as? [String: Any]
        )
        let payload = try XCTUnwrap(envelope["payload"] as? [String: Any])
        let error = try XCTUnwrap(payload["error"] as? [String: Any])
        XCTAssertEqual(error["code"] as? String, "runtime_unavailable")
        let details = try XCTUnwrap(error["details"] as? [String: Any])
        XCTAssertEqual(details["frameworkPath"] as? String, "/X.framework")
    }

    // MARK: - Helpers

    private final class CapturedEmits {
        private(set) var values: [String] = []
        func append(_ value: String) { values.append(value) }
    }
}
