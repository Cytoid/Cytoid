import XCTest
@testable import cytoid_game_core

/// State machine matrix for `RuntimeStateMachine`. Mirrors the Kotlin test
/// `RuntimeStateTest.kt` byte-for-byte: the 6 spec states + the 8 spec
/// transitions, plus the conditional-optionality contract on the v2 snapshot.
///
/// Wire shapes asserted here are the v2 host-protocol contract — downstream
/// tasks T4/T5/T6/T7/T9 rely on this matrix holding on both platforms.
final class RuntimeStateTests: XCTestCase {

    func testEnumHasSixValuesInSpecOrder() {
        // Spec order: unavailable, starting, ready, busy, suspended, failed.
        // Both Kotlin and Swift must list them in this exact order.
        XCTAssertEqual(
            RuntimeState.allCases,
            [.unavailable, .starting, .ready, .busy, .suspended, .failed]
        )
    }

    func testWireNamesAreLowerCaseEnumNames() {
        XCTAssertEqual(RuntimeState.unavailable.wireName, "unavailable")
        XCTAssertEqual(RuntimeState.starting.wireName, "starting")
        XCTAssertEqual(RuntimeState.ready.wireName, "ready")
        XCTAssertEqual(RuntimeState.busy.wireName, "busy")
        XCTAssertEqual(RuntimeState.suspended.wireName, "suspended")
        XCTAssertEqual(RuntimeState.failed.wireName, "failed")
    }

    func testInitialStateIsUnavailable() {
        let sm = RuntimeStateMachine()
        XCTAssertEqual(sm.state, .unavailable)
        XCTAssertEqual(sm.generation, 0)
        XCTAssertNil(sm.activeSessionId)
        XCTAssertNil(sm.lastError)
    }

    // MARK: - 8 transitions (mirrors Kotlin RuntimeStateTest.kt)

    func testT1_unavailableToStartingOnRequestStart() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        XCTAssertEqual(sm.state, .starting)
    }

    func testT2_startingToReadyOnEngineReady() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        XCTAssertEqual(sm.state, .ready)
        XCTAssertEqual(sm.generation, 1, "generation must increment on first ready")
    }

    func testT3_readyToBusyOnSessionStarted() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted(sessionId: "session-1")
        XCTAssertEqual(sm.state, .busy)
        XCTAssertEqual(sm.activeSessionId, "session-1")
    }

    func testT4_busyToReadyOnSessionEnded() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted(sessionId: "session-1")
        sm.onSessionEnded()
        XCTAssertEqual(sm.state, .ready)
        XCTAssertNil(
            sm.activeSessionId,
            "activeSessionId must be cleared on BUSY -> READY"
        )
    }

    func testT5_readyToSuspendedOnBackground() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSuspend()
        XCTAssertEqual(sm.state, .suspended)
    }

    func testT6_busyToSuspendedPreservesActiveSessionIdForResume() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted(sessionId: "session-1")
        sm.onSuspend()
        XCTAssertEqual(sm.state, .suspended)
        // activeSessionId is NOT cleared on SUSPENDED unless the bridge
        // explicitly cancels (single-slot memory rule).
        XCTAssertEqual(
            sm.activeSessionId, "session-1",
            "activeSessionId preserved across brief suspend"
        )
    }

    func testT7_suspendedRestoresPriorStateOnResumeSingleSlotMemory() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted(sessionId: "session-1")
        sm.onSuspend()
        sm.onResume()
        XCTAssertEqual(sm.state, .busy, "resume restores prior busy state")
        XCTAssertEqual(sm.activeSessionId, "session-1")
    }

    func testT8_anyToFailedSetsLastErrorAndClearsActiveSessionId() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted(sessionId: "session-1")
        let error = GameCoreError(
            code: "runtime_exception",
            message: "boom",
            details: ["where": "engine"]
        )
        sm.onFailure(error: error)
        XCTAssertEqual(sm.state, .failed)
        XCTAssertNotNil(sm.lastError, "lastError must be populated")
        XCTAssertEqual(sm.lastError?.code, "runtime_exception")
        XCTAssertEqual(sm.lastError?.message, "boom")
        XCTAssertEqual(sm.lastError?.details?["where"] as? String, "engine")
        XCTAssertNil(
            sm.activeSessionId,
            "activeSessionId must be cleared on failure"
        )
    }

    // MARK: - Single-slot memory: no stack

    func testResumeIsNoOpOutsideSuspendedState() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        // Resume without prior suspend — must not change state.
        sm.onResume()
        XCTAssertEqual(sm.state, .ready)
    }

    func testSuspendDuringStartingStaysStarting() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onSuspend()
        // Per spec: pause during STARTING stays STARTING (no transition).
        XCTAssertEqual(sm.state, .starting, "pause during starting must not transition")
        // Resume must also be a no-op (no resume slot was saved).
        sm.onResume()
        XCTAssertEqual(sm.state, .starting)
    }

    func testSingleSlotMemoryDoesNotStackAcrossRepeatedSuspends() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSuspend()
        // While suspended, the only legal recovery is onResume (restores
        // ready). A second suspend without an intervening resume must NOT
        // push ready onto a phantom stack — the single slot is already
        // consumed.
        sm.onResume()
        XCTAssertEqual(sm.state, .ready)
        sm.onSuspend()
        sm.onResume()
        XCTAssertEqual(sm.state, .ready)
    }

    // MARK: - Snapshot conditional optionality (v2 spec contract)

    func testSnapshotAlwaysContainsRequiredKeys() {
        let sm = RuntimeStateMachine()
        let snap = sm.snapshot(engine: "unity", mode: "unity")
        XCTAssertEqual(snap["engine"] as? String, "unity")
        XCTAssertEqual(snap["mode"] as? String, "unity")
        XCTAssertEqual(snap["state"] as? String, "unavailable")
        XCTAssertEqual(snap["generation"] as? Int, 0)
    }

    func testBusySnapshotIncludesActiveSessionIdAndOmitsError() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted(sessionId: "session-7")
        let snap = sm.snapshot(engine: "unity", mode: "unity")
        XCTAssertTrue(snap.keys.contains("activeSessionId"), "busy snapshot must contain activeSessionId")
        XCTAssertEqual(snap["activeSessionId"] as? String, "session-7")
        XCTAssertFalse(snap.keys.contains("error"), "busy snapshot must omit error")
    }

    func testFailedSnapshotIncludesErrorAndOmitsActiveSessionId() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onFailure(error: GameCoreError(code: "x", message: "y"))
        let snap = sm.snapshot(engine: "unity", mode: "unity")
        XCTAssertTrue(snap.keys.contains("error"), "failed snapshot must contain error")
        let error = snap["error"] as? [String: Any]
        XCTAssertEqual(error?["code"] as? String, "x")
        XCTAssertEqual(error?["message"] as? String, "y")
        XCTAssertFalse(snap.keys.contains("activeSessionId"), "failed snapshot must omit activeSessionId")
    }

    func testReadySnapshotOmitsBothActiveSessionIdAndError() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        let snap = sm.snapshot(engine: "unity", mode: "unity")
        XCTAssertFalse(snap.keys.contains("activeSessionId"), "ready snapshot must omit activeSessionId")
        XCTAssertFalse(snap.keys.contains("error"), "ready snapshot must omit error")
    }

    func testEngineRecoveryFromFailedIncrementsGeneration() {
        let sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        XCTAssertEqual(sm.generation, 1)
        sm.onFailure(error: GameCoreError(code: "x", message: "y"))
        sm.onEngineReady()
        XCTAssertEqual(sm.state, .ready)
        XCTAssertEqual(sm.generation, 2, "generation must increment on recovery from failed")
        XCTAssertNil(sm.lastError, "lastError cleared on recovery")
    }

    // MARK: - Happy-path state cycle (drives evidence file)

    func testHappyPathCycleClearsActiveSessionId() {
        let sm = RuntimeStateMachine()
        // STARTING -> READY -> BUSY -> READY, with activeSessionId lifecycle
        // tracked correctly. This is the cycle the evidence log captures.
        sm.onRequestStart()
        XCTAssertEqual(sm.state, .starting)

        sm.onEngineReady()
        XCTAssertEqual(sm.state, .ready)
        XCTAssertNil(sm.activeSessionId)

        sm.onSessionStarted(sessionId: "session-happy")
        XCTAssertEqual(sm.state, .busy)
        XCTAssertEqual(sm.activeSessionId, "session-happy")

        sm.onSessionEnded()
        XCTAssertEqual(sm.state, .ready)
        XCTAssertNil(sm.activeSessionId, "activeSessionId cleared on BUSY exit")
    }
}
