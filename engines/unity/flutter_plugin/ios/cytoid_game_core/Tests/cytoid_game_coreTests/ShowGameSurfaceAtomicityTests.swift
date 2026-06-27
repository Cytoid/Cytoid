import XCTest
import Flutter
@testable import cytoid_game_core

/// T6 contract: `showGameSurface` MUST be atomic — first call
/// `ensureRuntimeStarted()` synchronously, then short-circuit with a
/// structured `FlutterError` when state == `.failed`, otherwise proceed to
/// the mock / Unity present path. The atomicity contract is single check-then-
/// act: between observing `.failed` and reporting the error, no other code
/// can change the observed state.
final class ShowGameSurfaceAtomicityTests: XCTestCase {

    func testShortCircuitsWithStructuredErrorWhenStateIsFailed() {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        // Pre-fail the runtime via a load override so ensureRuntimeStarted
        // transitions state to .failed before showGameSurface checks it.
        bridge.loadFrameworkOverride = {
            .failure(FrameworkLoadError.frameworkNotFound)
        }

        var resultValue: Any?
        bridge.showGameSurface { value in resultValue = value }

        XCTAssertNotNil(resultValue, "showGameSurface must short-circuit (call result)")
        let error = try? XCTUnwrap(resultValue as? FlutterError)
        XCTAssertEqual(error?.code, "runtime_unavailable")
        XCTAssertEqual(bridge.runtimeState.state, .failed)

        // Exactly one engine.error envelope emitted by ensureRuntimeStarted's
        // failure path; showGameSurface's short-circuit adds no further emit.
        XCTAssertEqual(captured.values.count, 1)
    }

    func testShortCircuitsWhenFailurePreExists() {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }

        // Pre-fail the state machine directly (simulating a prior failure
        // such as a message-queue timeout). Then drive showGameSurface with
        // a load override that would succeed — the short-circuit must take
        // precedence over the load attempt's success.
        let priorError = GameCoreError(code: "runtime_unavailable", message: "pre-existing failure")
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onFailure(error: priorError)

        bridge.loadFrameworkOverride = { .success(()) }

        var resultValue: Any?
        bridge.showGameSurface { value in resultValue = value }

        let error = try? XCTUnwrap(resultValue as? FlutterError)
        XCTAssertEqual(error?.code, "runtime_unavailable")
        XCTAssertEqual(error?.message, "pre-existing failure")
        XCTAssertEqual(bridge.runtimeState.state, .failed)
    }

    func testProceedsNormallyWhenLoadSucceeds() {
        let bridge = CytoidGameCoreBridge()
        bridge.loadFrameworkOverride = { .success(()) }

        var resultValue: Any?
        bridge.showGameSurface { value in resultValue = value }

        // Mock path: present succeeds, result is nil. State ends in .starting
        // until engine.ready arrives.
        XCTAssertNil(resultValue, "mock path must resolve with nil on success")
        XCTAssertEqual(bridge.runtimeState.state, .starting)
    }

    func testAtomicCheckThenActObservesPostLoadState() {
        // Verifies the atomicity invariant directly: showGameSurface calls
        // ensureRuntimeStarted first, then re-reads state. A failed load
        // inside ensureRuntimeStarted must transition state before the
        // short-circuit check — i.e. no interleaving.
        let bridge = CytoidGameCoreBridge()
        bridge.loadFrameworkOverride = {
            .failure(FrameworkLoadError.principalClassUnavailable(path: "/Y.framework"))
        }

        var resultValue: Any?
        bridge.showGameSurface { value in resultValue = value }

        // The state the short-circuit observed must be .failed (the load
        // failure's effect), proving the load ran before the check.
        XCTAssertEqual(bridge.runtimeState.state, .failed)
        let error = try? XCTUnwrap(resultValue as? FlutterError)
        XCTAssertEqual(error?.code, "runtime_unavailable")
    }

    // MARK: - Helpers

    private final class CapturedEmits {
        private(set) var values: [String] = []
        func append(_ value: String) { values.append(value) }
    }
}
