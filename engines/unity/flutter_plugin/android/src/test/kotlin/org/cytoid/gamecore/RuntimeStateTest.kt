package org.cytoid.gamecore

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * State machine matrix for [RuntimeStateMachine]. Mirrors the iOS test
 * `RuntimeStateTests.swift` byte-for-byte: the 6 spec states + the 8 spec
 * transitions, plus the conditional-optionality contract on the v2 snapshot.
 *
 * Wire shapes asserted here are the v2 host-protocol contract — downstream
 * tasks T4/T5/T6/T7/T9 rely on this matrix holding on both platforms.
 */
class RuntimeStateTest {

    @Test
    fun `state enum has 6 values in spec order`() {
        // Spec order: UNAVAILABLE, STARTING, READY, BUSY, SUSPENDED, FAILED.
        // Both Kotlin and Swift must list them in this exact order.
        assertEquals(
            listOf(
                RuntimeState.UNAVAILABLE,
                RuntimeState.STARTING,
                RuntimeState.READY,
                RuntimeState.BUSY,
                RuntimeState.SUSPENDED,
                RuntimeState.FAILED,
            ),
            RuntimeState.entries,
        )
    }

    @Test
    fun `wire names are lower-case enum names`() {
        assertEquals("unavailable", RuntimeState.UNAVAILABLE.wireName)
        assertEquals("starting", RuntimeState.STARTING.wireName)
        assertEquals("ready", RuntimeState.READY.wireName)
        assertEquals("busy", RuntimeState.BUSY.wireName)
        assertEquals("suspended", RuntimeState.SUSPENDED.wireName)
        assertEquals("failed", RuntimeState.FAILED.wireName)
    }

    @Test
    fun `initial state is unavailable`() {
        val sm = RuntimeStateMachine()
        assertEquals(RuntimeState.UNAVAILABLE, sm.state)
        assertEquals(0, sm.generation)
        assertNull(sm.activeSessionId)
        assertNull(sm.lastError)
    }

    // --- 8 transitions (mirrors iOS RuntimeStateTests.swift) ---

    @Test
    fun `T1 unavailable to starting on request start`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        assertEquals(RuntimeState.STARTING, sm.state)
    }

    @Test
    fun `T2 starting to ready on engine ready`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        assertEquals(RuntimeState.READY, sm.state)
        assertEquals("generation must increment on first ready", 1, sm.generation)
    }

    @Test
    fun `T3 ready to busy on session started`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("session-1")
        assertEquals(RuntimeState.BUSY, sm.state)
        assertEquals("session-1", sm.activeSessionId)
    }

    @Test
    fun `T4 busy to ready on session ended`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("session-1")
        sm.onSessionEnded()
        assertEquals(RuntimeState.READY, sm.state)
        assertNull(
            "activeSessionId must be cleared on BUSY -> READY",
            sm.activeSessionId,
        )
    }
    @Test
    fun `T5 ready to suspended on background`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSuspend()
        assertEquals(RuntimeState.SUSPENDED, sm.state)
    }

    @Test
    fun `T6 busy to suspended preserves activeSessionId for resume`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("session-1")
        sm.onSuspend()
        assertEquals(RuntimeState.SUSPENDED, sm.state)
        // activeSessionId is NOT cleared on SUSPENDED unless the bridge
        // explicitly cancels (single-slot memory rule).
        assertEquals(
            "activeSessionId preserved across brief suspend",
            "session-1",
            sm.activeSessionId,
        )
    }

    @Test
    fun `T7 suspended restores prior state on resume single-slot memory`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("session-1")
        sm.onSuspend()
        sm.onResume()
        assertEquals(
            "resume restores prior busy state",
            RuntimeState.BUSY,
            sm.state,
        )
        assertEquals("session-1", sm.activeSessionId)
    }

    @Test
    fun `T8 any to failed sets lastError and clears activeSessionId`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("session-1")
        val error = GameCoreError(
            code = "runtime_exception",
            message = "boom",
            details = mapOf("where" to "engine"),
        )
        sm.onFailure(error)
        assertEquals(RuntimeState.FAILED, sm.state)
        assertNotNull("lastError must be populated", sm.lastError)
        assertEquals("runtime_exception", sm.lastError?.code)
        assertEquals("boom", sm.lastError?.message)
        assertEquals("engine", sm.lastError?.details?.get("where"))
        assertNull(
            "activeSessionId must be cleared on failure",
            sm.activeSessionId,
        )
    }

    // --- Single-slot memory: no stack ---

    @Test
    fun `resume is no-op outside suspended state`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        // Resume without prior suspend — must not change state.
        sm.onResume()
        assertEquals(RuntimeState.READY, sm.state)
    }

    @Test
    fun `suspend during starting stays starting`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onSuspend()
        // Per spec: pause during STARTING stays STARTING (no transition).
        assertEquals(
            "pause during starting must not transition",
            RuntimeState.STARTING,
            sm.state,
        )
        // Resume must also be a no-op (no resume slot was saved).
        sm.onResume()
        assertEquals(RuntimeState.STARTING, sm.state)
    }

    @Test
    fun `single-slot memory does not stack across repeated suspends`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSuspend()
        // While suspended, the only legal recovery is onResume (restores
        // READY). A second suspend without an intervening resume must NOT
        // push READY onto a phantom stack — the single slot is already
        // consumed.
        sm.onResume()
        assertEquals(RuntimeState.READY, sm.state)
        sm.onSuspend()
        sm.onResume()
        assertEquals(RuntimeState.READY, sm.state)
    }

    // --- Snapshot conditional optionality (v2 spec contract) ---

    @Test
    fun `snapshot always contains required keys engine mode state generation`() {
        val sm = RuntimeStateMachine()
        val snap = sm.snapshot(engine = "unity", mode = "unity")
        assertEquals("unity", snap["engine"])
        assertEquals("unity", snap["mode"])
        assertEquals("unavailable", snap["state"])
        assertEquals(0, snap["generation"])
    }

    @Test
    fun `busy snapshot includes activeSessionId and omits error`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("session-7")
        val snap = sm.snapshot(engine = "unity", mode = "unity")
        assertTrue("busy snapshot must contain activeSessionId", snap.containsKey("activeSessionId"))
        assertEquals("session-7", snap["activeSessionId"])
        assertTrue("busy snapshot must omit error", !snap.containsKey("error"))
    }

    @Test
    fun `failed snapshot includes error and omits activeSessionId`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onFailure(GameCoreError(code = "x", message = "y"))
        val snap = sm.snapshot(engine = "unity", mode = "unity")
        assertTrue("failed snapshot must contain error", snap.containsKey("error"))
        @Suppress("UNCHECKED_CAST")
        val error = snap["error"] as Map<String, Any?>
        assertEquals("x", error["code"])
        assertEquals("y", error["message"])
        assertTrue(
            "failed snapshot must omit activeSessionId",
            !snap.containsKey("activeSessionId"),
        )
    }

    @Test
    fun `ready snapshot omits both activeSessionId and error`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        val snap = sm.snapshot(engine = "unity", mode = "unity")
        assertTrue("ready snapshot must omit activeSessionId", !snap.containsKey("activeSessionId"))
        assertTrue("ready snapshot must omit error", !snap.containsKey("error"))
    }

    @Test
    fun `engine recovery from failed increments generation`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        assertEquals(1, sm.generation)
        sm.onFailure(GameCoreError(code = "x", message = "y"))
        sm.onEngineReady()
        assertEquals(RuntimeState.READY, sm.state)
        assertEquals(
            "generation must increment on recovery from failed",
            2,
            sm.generation,
        )
        assertNull("lastError cleared on recovery", sm.lastError)
    }

    // --- Happy-path state cycle (drives evidence file) ---

    @Test
    fun `happy path cycle starting ready busy ready clears activeSessionId`() {
        val sm = RuntimeStateMachine()
        // STARTING -> READY -> BUSY -> READY, with activeSessionId lifecycle
        // tracked correctly. This is the cycle the evidence log captures.
        sm.onRequestStart()
        assertEquals(RuntimeState.STARTING, sm.state)

        sm.onEngineReady()
        assertEquals(RuntimeState.READY, sm.state)
        assertNull(sm.activeSessionId)

        sm.onSessionStarted("session-happy")
        assertEquals(RuntimeState.BUSY, sm.state)
        assertEquals("session-happy", sm.activeSessionId)

        sm.onSessionEnded()
        assertEquals(RuntimeState.READY, sm.state)
        assertNull(
            "activeSessionId cleared on BUSY exit",
            sm.activeSessionId,
        )
    }

    // --- onEngineReady hardening (no-op outside starting/failed) ---

    @Test
    fun `onEngineReady is no-op when already READY and preserves generation`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        assertEquals(RuntimeState.READY, sm.state)
        val generationBefore = sm.generation

        sm.onEngineReady()

        assertEquals(RuntimeState.READY, sm.state)
        assertEquals(generationBefore, sm.generation)
    }

    @Test
    fun `onEngineReady during BUSY preserves the active session`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("S1")
        val generationBefore = sm.generation

        sm.onEngineReady()

        assertEquals(RuntimeState.BUSY, sm.state)
        assertEquals("S1", sm.activeSessionId)
        assertEquals(generationBefore, sm.generation)
    }

    @Test
    fun `onEngineReady during SUSPENDED preserves suspended state`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSuspend()
        val generationBefore = sm.generation

        sm.onEngineReady()

        assertEquals(RuntimeState.SUSPENDED, sm.state)
        assertEquals(generationBefore, sm.generation)
    }

    // --- Session end while suspended (cancel/result during backgrounding) ---

    @Test
    fun `onSessionEnded while SUSPENDED clears session and resumes to READY`() {
        val sm = RuntimeStateMachine()
        sm.onRequestStart()
        sm.onEngineReady()
        sm.onSessionStarted("S1")
        sm.onSuspend()
        assertEquals(RuntimeState.SUSPENDED, sm.state)
        assertEquals("S1", sm.activeSessionId)

        // session.cancel / session.result arrives while suspended.
        sm.onSessionEnded()

        assertEquals(RuntimeState.SUSPENDED, sm.state)
        assertNull("activeSessionId cleared even while suspended", sm.activeSessionId)

        // Resume must land in READY (not stale BUSY) with no session id.
        sm.onResume()
        assertEquals(RuntimeState.READY, sm.state)
        assertNull(sm.activeSessionId)
    }
}
