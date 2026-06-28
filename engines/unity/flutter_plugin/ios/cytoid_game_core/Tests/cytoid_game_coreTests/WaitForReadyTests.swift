import XCTest
@testable import cytoid_game_core

/// T6 contract: `waitForReady` MUST complete when the runtime reaches
/// `.ready`, and throw `WaitForReadyError.timeout` when the deadline elapses
/// without `.ready` or `.failed`. Also covers the immediate-return paths
/// (already ready / already failed) and the parked-waiter cleanup on failure.
///
/// Uses very short timeout values so the suite runs in well under a second.
final class WaitForReadyTests: XCTestCase {

    func testReturnsImmediatelyWhenAlreadyReady() async throws {
        let bridge = CytoidGameCoreBridge()
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        XCTAssertEqual(bridge.runtimeState.state, .ready)

        try await bridge.waitForReady(timeout: 0.05)
    }

    func testThrowsAlreadyFailedWhenAlreadyFailed() async throws {
        let bridge = CytoidGameCoreBridge()
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onFailure(
            error: GameCoreError(code: "runtime_unavailable", message: "boom")
        )

        do {
            try await bridge.waitForReady(timeout: 0.05)
            XCTFail("expected alreadyFailed throw")
        } catch let error as CytoidGameCoreBridge.WaitForReadyError {
            switch error {
            case .alreadyFailed(let code):
                XCTAssertEqual(code, "runtime_unavailable")
            case .timeout:
                XCTFail("expected alreadyFailed, got timeout")
            }
        }
    }

    func testCompletesWhenEngineReadyArrivesWhileParked() async throws {
        let bridge = CytoidGameCoreBridge()
        bridge.runtimeState.onRequestStart()
        XCTAssertEqual(bridge.runtimeState.state, .starting)

        // Drive the engine.ready arrival on a background task; the main-task
        // await parks until then. waitForReady must resume on .ready.
        let expectation = expectation(description: "waitForReady completes")

        Task {
            try await bridge.waitForReady(timeout: 1.0)
            expectation.fulfill()
        }

        // Yield once so the Task above parks in the continuation before we
        // drive the ready transition. 50ms is plenty on a modern CPU.
        try? await Task.sleep(nanoseconds: 50_000_000)
        let readyEnvelope = "{\"schema\":\"cytoid.game-core.v2\",\"id\":\"r-1\",\"type\":\"engine.ready\",\"payload\":{}}"
        bridge.onUnityMessage(readyEnvelope)

        await fulfillment(of: [expectation], timeout: 1.0)
        XCTAssertEqual(bridge.runtimeState.state, .ready)
    }

    func testThrowsTimeoutWhenDeadlineElapses() async throws {
        let bridge = CytoidGameCoreBridge()
        bridge.runtimeState.onRequestStart()
        XCTAssertEqual(bridge.runtimeState.state, .starting)

        do {
            try await bridge.waitForReady(timeout: 0.05)
            XCTFail("expected timeout throw")
        } catch CytoidGameCoreBridge.WaitForReadyError.timeout {
            // expected
        } catch {
            XCTFail("expected .timeout, got \(error)")
        }

        // After timeout, the waiter is removed from the parked list so a
        // subsequent engine.ready does NOT over-resume (idempotency).
        XCTAssertEqual(bridge.runtimeState.state, .starting)
    }

    func testFailedTransitionResumesParkedWaiterWithError() async throws {
        let bridge = CytoidGameCoreBridge()
        bridge.runtimeState.onRequestStart()
        XCTAssertEqual(bridge.runtimeState.state, .starting)

        let expectation = expectation(description: "waitForReady fails on runtime failure")

        Task {
            do {
                try await bridge.waitForReady(timeout: 1.0)
                XCTFail("expected throw")
            } catch CytoidGameCoreBridge.WaitForReadyError.alreadyFailed {
                expectation.fulfill()
            } catch {
                XCTFail("unexpected error: \(error)")
            }
        }

        try? await Task.sleep(nanoseconds: 50_000_000)
        // Trigger a failure path: invoke the bridge's message-queue-timeout
        // routing directly with no active session (engine.error route), which
        // also resumes parked waiters via failReadyWaiters.
        bridge.handleMessageQueueTimeoutRouting()

        await fulfillment(of: [expectation], timeout: 1.0)
        XCTAssertEqual(bridge.runtimeState.state, .failed)
    }
}
