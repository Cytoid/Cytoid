package org.cytoid.gamecore

import android.app.Activity
import org.json.JSONObject
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test

/**
 * Verifies [CytoidGameCoreBridge.synthesizeRuntimeFailure] for the two triggers
 * T4 owns (GENERATION_CHANGE, SURFACE_LOST). UNREACHABLE is exercised by T5/T6.
 *
 * Acceptance (from `.omo/plans/v2-host-impl.md` T4):
 *  1. GENERATION_CHANGE with active session → envelope emitted with
 *     `error.code = "runtime_recreated"`.
 *  2. GENERATION_CHANGE without active session → no envelope (idempotency).
 *  3. SURFACE_LOST with active session → envelope emitted with
 *     `error.code = "runtime_surface_lost"`.
 *  4. SURFACE_LOST without active session → no envelope.
 *
 * Idempotency is asserted as a post-condition: `activeSessionId` is null
 * immediately after synthesis, and a second call for the same session returns
 * null and emits nothing.
 *
 * The bridge is constructed via [CytoidGameCoreBridge.getOrCreate] in a pure
 * JVM (no Android Looper). The `emitOverride` seam captures synthesized
 * envelopes; without it, `emit` would touch `Looper.getMainLooper()` which
 * returns null in the stub jar and NPE inside `Handler`'s constructor.
 * The probe seam (`probeUnityAvailable`) is forced false so the mock path is
 * taken and no real Unity class load is attempted.
 */
class RuntimeFailureSynthesisTest {

    // Saved in @Before, restored in @After so the global probe mutation
    // does not leak into other test classes sharing the JVM.
    private var previousProbe: (() -> Boolean)? = null

    @Before
    fun resetBridgeInstance() {
        setCompanionInstance(null)
        previousProbe = probeUnityAvailable
        probeUnityAvailable = { false }
    }

    @After
    fun restoreBridgeInstance() {
        previousProbe?.let { probeUnityAvailable = it }
        previousProbe = null
        setCompanionInstance(null)
    }

    @Test
    fun `GENERATION_CHANGE with active session emits runtime_recreated envelope`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        // Drive state machine to READY, generation=1, then to BUSY(S1).
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        assertEquals("S1", bridge.runtimeState.activeSessionId)

        // Simulate the onUnityMessage(engine.ready) GENERATION_CHANGE path:
        // capture pre-call session, call onEngineReady (which here we drive
        // through the state machine directly to bump generation from FAILED),
        // then run the trigger check the bridge runs inline. Going through
        // onEngineReady from BUSY does NOT bump generation, so to reach the
        // generation>1 state we go FAILED → READY (the real recovery path).
        bridge.runtimeState.onFailure(
            GameCoreError(code = "prior_failure", message = "prior"),
        )
        assertNull(bridge.runtimeState.activeSessionId)
        // onFailure cleared the session — restore it to simulate the scenario
        // where the engine recreates WITHOUT the bridge observing the clear
        // (the GENERATION_CHANGE safety net's whole purpose).
        setBusinessActiveSessionForTest(bridge, "S1")
        val wasActiveSession = bridge.runtimeState.activeSessionId
        bridge.runtimeState.onEngineReady() // FAILED → READY, generation 1 → 2
        assertTrue("generation must be > 1 for GENERATION_CHANGE", bridge.runtimeState.generation > 1)

        if (wasActiveSession != null && bridge.runtimeState.generation > 1) {
            bridge.synthesizeRuntimeFailure(
                RuntimeFailureTrigger.GENERATION_CHANGE,
                wasActiveSession,
            )
        }

        // Envelope shape assertions.
        assertEquals("expected exactly one emitted envelope", 1, captured.size)
        val envelope = JSONObject(captured.first())
        assertEquals("S1", envelope.getString("id"))
        assertEquals("session.failed", envelope.getString("type"))
        assertEquals("cytoid.game-core.v2", envelope.getString("schema"))

        val payload = envelope.getJSONObject("payload")
        assertEquals("S1", payload.getString("sessionId"))
        assertFalse("payload must not carry outcome", payload.has("outcome"))
        // timestamp present and parseable as Long.
        payload.getLong("timestamp")

        val error = payload.getJSONObject("error")
        assertEquals("runtime_recreated", error.getString("code"))
        assertNotNull(error.getString("message"))
        assertTrue(
            "error message must mention generation/recreation",
            error.getString("message").contains("recreated", ignoreCase = true),
        )

        // Post-condition: activeSessionId cleared (idempotency seam).
        assertNull(bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `GENERATION_CHANGE without active session emits nothing`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        // READY with no active session.
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        assertNull(bridge.runtimeState.activeSessionId)
        val generationBefore = bridge.runtimeState.generation

        // Call the primitive directly — gate should short-circuit.
        val result = bridge.synthesizeRuntimeFailure(
            RuntimeFailureTrigger.GENERATION_CHANGE,
            "S1",
        )

        assertNull("primitive must return null when no active session", result)
        assertEquals(
            "no envelope may be emitted when no active session",
            0,
            captured.size,
        )
        assertEquals(
            "generation must be unchanged (primitive short-circuited before onFailure)",
            generationBefore,
            bridge.runtimeState.generation,
        )
        assertNull(bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `SURFACE_LOST with active session emits runtime_surface_lost envelope`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S2")
        assertEquals("S2", bridge.runtimeState.activeSessionId)

        val result = bridge.synthesizeRuntimeFailure(
            RuntimeFailureTrigger.SURFACE_LOST,
            "S2",
        )

        assertNotNull(result)
        assertEquals(1, captured.size)

        val envelope = JSONObject(captured.first())
        assertEquals("S2", envelope.getString("id"))
        assertEquals("session.failed", envelope.getString("type"))
        assertEquals("cytoid.game-core.v2", envelope.getString("schema"))
        val payload = envelope.getJSONObject("payload")
        assertEquals("S2", payload.getString("sessionId"))
        assertFalse("payload must not carry outcome", payload.has("outcome"))
        // timestamp present and parseable as Long.
        payload.getLong("timestamp")
        assertEquals(
            "runtime_surface_lost",
            payload.getJSONObject("error").getString("code"),
        )

        assertNull(bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `SURFACE_LOST without active session emits nothing`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        assertNull(bridge.runtimeState.activeSessionId)

        val result = bridge.synthesizeRuntimeFailure(
            RuntimeFailureTrigger.SURFACE_LOST,
            "S2",
        )

        assertNull(result)
        assertEquals(0, captured.size)
        assertNull(bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `synthesizeRuntimeFailure is idempotent when called twice for same session`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S3")

        val first = bridge.synthesizeRuntimeFailure(
            RuntimeFailureTrigger.SURFACE_LOST,
            "S3",
        )
        val second = bridge.synthesizeRuntimeFailure(
            RuntimeFailureTrigger.SURFACE_LOST,
            "S3",
        )

        assertNotNull("first call must emit", first)
        assertNull("second call must be a no-op", second)
        assertEquals("exactly one envelope, not two", 1, captured.size)
    }

    @Test
    fun `onUnityMessage engine_ready emits runtime_recreated session result before engine_ready envelope`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        // Drive to READY gen=1, BUSY(S1), then FAILED (clears activeSessionId),
        // then restore S1 via reflection to simulate the safety-net case.
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        bridge.runtimeState.onFailure(
            GameCoreError(code = "prior_failure", message = "prior"),
        )
        setBusinessActiveSessionForTest(bridge, "S1")

        // engine.ready arrives through onUnityMessage: gen FAILED→READY (1→2),
        // GENERATION_CHANGE synth must fire, THEN the engine.ready envelope.
        bridge.onUnityMessage(
            """{"schema":"cytoid.game-core.v2","id":"r1","type":"engine.ready","payload":{}}""",
        )

        assertEquals("expected synth + engine.ready, in that order", 2, captured.size)

        val first = JSONObject(captured[0])
        assertEquals("first emitted must be the synthesized session.failed", "session.failed", first.getString("type"))
        assertEquals("S1", first.getString("id"))
        val firstPayload = first.getJSONObject("payload")
        assertEquals("S1", firstPayload.getString("sessionId"))
        assertFalse("payload must not carry outcome", firstPayload.has("outcome"))
        // timestamp present and parseable as Long.
        firstPayload.getLong("timestamp")
        assertEquals(
            "runtime_recreated",
            firstPayload.getJSONObject("error").getString("code"),
        )

        val second = JSONObject(captured[1])
        assertEquals("second emitted must be the engine.ready itself", "engine.ready", second.getString("type"))
    }

    @Test
    fun `inbound session failed via onUnityMessage transitions to FAILED and preserves error`() {
        val bridge = newBridgeWithCapture()
        bridge.emitOverride = { /* forward-only; ignore */ }

        // Drive to BUSY(S1) — an active session the engine will report dead.
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)

        // Engine delivers a real session.failed envelope (not native-synthesized).
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.failed",
             "payload":{"sessionId":"S1","timestamp":1782148800000,
               "error":{"code":"engine_crashed","message":"engine died"}}}
            """.trimIndent(),
        )

        // State must be FAILED (NOT downgraded to READY) and the error preserved.
        assertEquals(RuntimeState.FAILED, bridge.runtimeState.state)
        assertNull("activeSessionId cleared on failure", bridge.runtimeState.activeSessionId)
        val error = bridge.runtimeState.lastError
        assertNotNull("error captured from inbound session.failed", error)
        assertEquals("engine_crashed", error?.code)
    }

    @Test
    fun `emit session_result transitions active session to READY once`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)

        invokeEmit(
            bridge,
            """{"schema":"cytoid.game-core.v2","id":"S1","type":"session.result","payload":{"sessionId":"S1","outcome":{"kind":"completed"}}}""",
        )

        assertEquals(RuntimeState.READY, bridge.runtimeState.state)
        assertNull(bridge.runtimeState.activeSessionId)
        assertEquals("session.result is forwarded exactly once", 1, captured.size)
    }

    @Test
    fun `schema invalid envelopes are ignored by native transition entry points`() {
        val bridge = newBridgeWithCapture()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()

        bridge.onOutboundMessage("""{"id":"S1","type":"session.start","payload":{}}""")
        assertEquals(RuntimeState.READY, bridge.runtimeState.state)

        bridge.runtimeState.onSessionStarted("S1")
        bridge.onUnityMessage("""{"schema":"wrong","id":"S1","type":"session.result","payload":{}}""")

        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)
        assertEquals("schema-invalid inbound envelope must not be forwarded", 0, captured.size)
    }

    private fun newBridgeWithCapture(): CytoidGameCoreBridge =
        CytoidGameCoreBridge.getOrCreate(Activity())

    private fun invokeEmit(bridge: CytoidGameCoreBridge, jsonString: String) {
        val method = CytoidGameCoreBridge::class.java.getDeclaredMethod("emit", String::class.java)
        method.isAccessible = true
        method.invoke(bridge, jsonString)
    }

    private fun setCompanionInstance(value: CytoidGameCoreBridge?) {
        val field = CytoidGameCoreBridge::class.java.getDeclaredField("instance")
        field.isAccessible = true
        field.set(null, value)
    }

    // The state machine clears activeSessionId in onFailure; the
    // GENERATION_CHANGE safety net exists for the unreachable-by-construction
    // case where activeSessionId survives a failure. This helper restores
    // activeSessionId via the same reflection seam RuntimeStateTest uses to
    // exercise transitions, so we can simulate that scenario without going
    // through onSessionStarted (which is no-op outside READY).
    private fun setBusinessActiveSessionForTest(bridge: CytoidGameCoreBridge, sessionId: String) {
        val field = RuntimeStateMachine::class.java.getDeclaredField("activeSessionId")
        field.isAccessible = true
        field.set(bridge.runtimeState, sessionId)
    }
}
