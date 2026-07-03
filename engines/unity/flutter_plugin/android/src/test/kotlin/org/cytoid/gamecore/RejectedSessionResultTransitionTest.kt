package org.cytoid.gamecore

import android.app.Activity
import org.json.JSONObject
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Before
import org.junit.Test

/**
 * Regression coverage for the rejected-`session.result` state-machine bug.
 *
 * Before the fix, `CytoidGameCoreBridge.emit` skipped
 * `runtimeState.onSessionEnded()` whenever `outcome.kind == "rejected"`,
 * leaving the runtime stuck in BUSY with a stale `activeSessionId`. The
 * v2 § session.result contract treats `session.result` as terminal for ALL
 * outcome kinds, including `rejected` (a rejected `session.start` is still
 * a terminal `session.result` for the active session id).
 *
 * The host enters BUSY optimistically at `session.start` time, so a rejected
 * result MUST transition BUSY → READY so the next session can launch —
 * otherwise the runtime is unusable until process death.
 */
class RejectedSessionResultTransitionTest {

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
    fun `rejected session_result for active session transitions BUSY to READY`() {
        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        bridge.emitOverride = { /* forward-only */ }

        // Drive to BUSY(S1) — the optimistic state set at session.start.
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)
        assertEquals("S1", bridge.runtimeState.activeSessionId)

        // Engine rejects the start with a terminal session.result envelope,
        // delivered through the real inbound path (onUnityMessage → emit).
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.result",
             "payload":{"sessionId":"S1","outcome":{"kind":"rejected"},
               "error":{"code":"level_not_found","message":"missing"}}}
            """.trimIndent(),
        )

        // The bug: state stayed BUSY and activeSessionId stayed "S1".
        // The fix: state returns to READY and activeSessionId is cleared.
        assertEquals(
            "rejected session.result must transition BUSY → READY",
            RuntimeState.READY,
            bridge.runtimeState.state,
        )
        assertNull(
            "rejected session.result must clear activeSessionId",
            bridge.runtimeState.activeSessionId,
        )
    }

    @Test
    fun `runtime is reusable after a rejected session_result`() {
        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        bridge.emitOverride = { /* forward-only */ }

        // First session: rejected.
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.result",
             "payload":{"sessionId":"S1","outcome":{"kind":"rejected"},
               "error":{"code":"level_not_found","message":"missing"}}}
            """.trimIndent(),
        )
        assertEquals(RuntimeState.READY, bridge.runtimeState.state)

        // Second session.start must succeed — this is the user-visible
        // symptom the fix addresses (back-to-back gameplay after a rejection).
        bridge.runtimeState.onSessionStarted("S2")
        assertEquals(
            "second session must be launchable after a rejected first",
            RuntimeState.BUSY,
            bridge.runtimeState.state,
        )
        assertEquals("S2", bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `rejected session_result for a different session id does not disturb active session`() {
        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        bridge.emitOverride = { /* forward-only */ }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S-active")
        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)

        // A late/unsolicited rejected result for a DIFFERENT id must not
        // tear down the currently-active session. This is the protection
        // the original `resultId == activeSessionId` branch was meant to
        // provide — the fix preserves it.
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S-stale","type":"session.result",
             "payload":{"sessionId":"S-stale","outcome":{"kind":"rejected"},
               "error":{"code":"level_not_found","message":"missing"}}}
            """.trimIndent(),
        )

        assertEquals(
            "stale-id rejected result must not disturb the active session",
            RuntimeState.BUSY,
            bridge.runtimeState.state,
        )
        assertEquals(
            "S-active",
            bridge.runtimeState.activeSessionId,
        )
    }

    @Test
    fun `rejected session_result envelope is still forwarded to the event sink`() {
        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }

        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted("S1")
        bridge.onUnityMessage(
            """
            {"schema":"cytoid.game-core.v2","id":"S1","type":"session.result",
             "payload":{"sessionId":"S1","outcome":{"kind":"rejected"},
               "error":{"code":"level_not_found","message":"missing"}}}
            """.trimIndent(),
        )

        assertEquals(
            "rejected session.result must still be forwarded to the host",
            1,
            captured.size,
        )
        val envelope = JSONObject(captured.first())
        assertEquals("session.result", envelope.getString("type"))
        assertEquals("rejected", envelope.getJSONObject("payload").getJSONObject("outcome").getString("kind"))
    }

    private fun setCompanionInstance(value: CytoidGameCoreBridge?) {
        val field = CytoidGameCoreBridge::class.java.getDeclaredField("instance")
        field.isAccessible = true
        field.set(null, value)
    }
}
