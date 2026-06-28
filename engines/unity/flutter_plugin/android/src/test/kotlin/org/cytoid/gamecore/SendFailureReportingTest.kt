package org.cytoid.gamecore

import android.app.Activity
import org.json.JSONObject
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Before
import org.junit.Test

/**
 * Verifies [CytoidGameCoreBridge.sendToUnity] and
 * [CytoidGameCoreBridge.returnToFlutterActivity] route native-side send
 * failures per the v2 § Active-Session Runtime Failure contract:
 *
 *  - `activeSessionId == null` → ONLY `engine.error` with
 *    `error.code = "runtime_exception"`, sanitized message, NO stack trace.
 *  - `activeSessionId != null` → ONLY synthesized `session.failed` via the
 *    T4 primitive with `error.code = "runtime_unreachable"`.
 *
 * The active-session routing rule is the trickiest invariant in the v2 plan:
 * active-session failures use `session.failed` ONLY, never `engine.error`.
 * Pre-session failures use `engine.error` ONLY. These tests lock both halves.
 *
 * The bridge is constructed via [CytoidGameCoreBridge.getOrCreate] in a pure
 * JVM (no Android Looper). The `emitOverride` seam (T4) captures emitted
 * envelopes; `invokeUnitySend` / `invokeReturnToFlutter` (T5) inject the
 * native-send failures.
 */
class SendFailureReportingTest {

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
    fun `sendToUnity failure with no active session emits engine_error only`() {
        val bridge = newBridge()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }
        bridge.invokeUnitySend = { throw RuntimeException("Unity player gone") }

        // Pre-session state: no active session id, state == UNAVAILABLE.
        assertNull(bridge.runtimeState.activeSessionId)

        // When: sendToUnity throws.
        bridge.sendToUnity("{\"type\":\"test\"}")

        // Then: exactly one envelope emitted, of type engine.error.
        assertEquals("expected exactly one emitted envelope", 1, captured.size)

        val envelope = JSONObject(captured.first())
        assertEquals("cytoid.game-core.v2", envelope.getString("schema"))
        assertEquals("engine.error", envelope.getString("type"))

        val error = envelope.getJSONObject("payload").getJSONObject("error")
        assertEquals("runtime_exception", error.getString("code"))

        // Sanitized message format: "<SimpleClassName>: <first line>".
        assertEquals("RuntimeException: Unity player gone", error.getString("message"))

        // NO stack trace leak: error MUST NOT carry a `details` object.
        assertFalse(
            "engine.error must not leak stack trace via details",
            error.has("details"),
        )
        // The message itself must not contain a stack-trace signature.
        assertFalse(
            "message must not contain stack-trace signature",
            error.getString("message").contains("at org.", ignoreCase = true),
        )

        // Spec compliance: NO session.result OR session.failed emitted for pre-session failure.
        val sessionResults = captured.count {
            runCatching { JSONObject(it).getString("type") }.getOrNull() == "session.result"
        }
        assertEquals("pre-session failure must not emit session.result", 0, sessionResults)
        val sessionFailedCount = captured.count {
            runCatching { JSONObject(it).getString("type") }.getOrNull() == "session.failed"
        }
        assertEquals("pre-session failure must not emit session.failed", 0, sessionFailedCount)
    }

    @Test
    fun `sendToUnity failure with active session synthesizes session_result only`() {
        val bridge = newBridge()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }
        bridge.invokeUnitySend = { throw RuntimeException("Unity player gone") }

        // Drive the state machine READY -> BUSY("S1").
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        assertEquals("S1", bridge.runtimeState.activeSessionId)

        // When: sendToUnity throws during the active session.
        bridge.sendToUnity("{\"type\":\"test\"}")

        // Then: exactly one envelope, of type session.failed (NOT engine.error).
        assertEquals("expected exactly one emitted envelope", 1, captured.size)

        val envelope = JSONObject(captured.first())
        assertEquals("cytoid.game-core.v2", envelope.getString("schema"))
        assertEquals("session.failed", envelope.getString("type"))
        assertEquals("S1", envelope.getString("id"))

        val payload = envelope.getJSONObject("payload")
        assertEquals("S1", payload.getString("sessionId"))
        assertFalse("payload must not carry outcome", payload.has("outcome"))
        // timestamp present and parseable as Long.
        payload.getLong("timestamp")

        val error = payload.getJSONObject("error")
        assertEquals("runtime_unreachable", error.getString("code"))
        assertNotNull(error.getString("message"))

        // Spec compliance (the trickiest rule): NO engine.error for active-session failure.
        val engineErrors = captured.count {
            runCatching { JSONObject(it).getString("type") }.getOrNull() == "engine.error"
        }
        assertEquals("active-session failure must not emit engine.error", 0, engineErrors)

        // Idempotency seam from T4: activeSessionId cleared after synthesis.
        assertNull(bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `returnToFlutterActivity failure with no active session emits engine_error`() {
        val bridge = newBridge()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }
        bridge.invokeReturnToFlutter = { throw RuntimeException("Activity launch failed") }

        // Pre-session state.
        assertNull(bridge.runtimeState.activeSessionId)

        // When: returnToFlutterActivity throws.
        bridge.returnToFlutterActivity()

        // Then: exactly one envelope, of type engine.error.
        assertEquals("expected exactly one emitted envelope", 1, captured.size)

        val envelope = JSONObject(captured.first())
        assertEquals("engine.error", envelope.getString("type"))

        val error = envelope.getJSONObject("payload").getJSONObject("error")
        assertEquals("runtime_exception", error.getString("code"))
        assertEquals(
            "RuntimeException: Activity launch failed",
            error.getString("message"),
        )
        assertFalse("engine.error must not leak stack trace", error.has("details"))

        // Spec compliance: NO session.result OR session.failed for pre-session failure.
        val sessionResults = captured.count {
            runCatching { JSONObject(it).getString("type") }.getOrNull() == "session.result"
        }
        assertEquals("pre-session failure must not emit session.result", 0, sessionResults)
        val sessionFailedCount = captured.count {
            runCatching { JSONObject(it).getString("type") }.getOrNull() == "session.failed"
        }
        assertEquals("pre-session failure must not emit session.failed", 0, sessionFailedCount)
    }

    private fun newBridge(): CytoidGameCoreBridge =
        CytoidGameCoreBridge.getOrCreate(Activity())

    private fun setCompanionInstance(value: CytoidGameCoreBridge?) {
        val field = CytoidGameCoreBridge::class.java.getDeclaredField("instance")
        field.isAccessible = true
        field.set(null, value)
    }
}
