package org.cytoid.gamecore

import android.app.Activity
import android.app.Application
import android.os.Bundle
import me.tigerhix.cytoid.CytoidPluginActivity
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
 * Verifies the Android Unity Activity lifecycle integration (v2 § Warm-resident,
 * T3 state machine, T4 SURFACE_LOST primitive) via the bridge's
 * ActivityLifecycleCallbacks.
 *
 * Acceptance (from `.omo/plans/v2-host-impl.md` T9):
 *  1. pause → resume cycle preserves prior state (single-slot memory from T3).
 *  2. destroyed without active session → state = UNAVAILABLE (caller must
 *     startRuntime() again).
 *  3. destroyed with active session → SURFACE_LOST synthesized via the T4
 *     primitive (idempotency).
 *  4. Memory regression: 10 sequential session cycles via low-level
 *     primitives (showGameSurface / hideGameSurface / onOutboundMessage),
 *     [CytoidGameCoreBridge.unityActivityInstanceCount] stays ≤ 1.
 *  5. Back-stack / warm-resident: hideGameSurface does NOT destroy the
 *     Unity Activity (counter unchanged, returnToFlutterActivity invoked).
 *
 * Low-level primitives are used directly (NOT PlaySession) to avoid a T7
 * dependency, per `.omo/notepads/v2-host-impl/decisions.md`.
 *
 * The bridge is constructed via [CytoidGameCoreBridge.getOrCreate] in a pure
 * JVM (no Android Looper). The `emitOverride` seam (T4) captures synthesized
 * envelopes; `invokeReturnToFlutter` (T5) captures back-stack navigation
 * without starting a real Activity. The lifecycle callbacks are driven via
 * reflection on the private `unityActivityLifecycleCallbacks` field, using
 * the [me.tigerhix.cytoid.CytoidPluginActivity] test stub whose class name
 * matches CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY so the bridge's
 * `isUnityGameplayActivity` check returns true.
 */
class ActivityLifecycleSynthesisTest {

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
    fun `pause then resume preserves READY state via single-slot memory`() {
        val bridge = newBridge()
        val activity = CytoidPluginActivity()
        driveToReady(bridge, activity)

        assertEquals(RuntimeState.READY, bridge.runtimeState.state)

        // When: Unity Activity paused (app backgrounded during READY).
        fireOnActivityPaused(activity)

        assertEquals(RuntimeState.SUSPENDED, bridge.runtimeState.state)

        // Then: resume restores READY (single-slot memory from T3).
        fireOnActivityResumed(activity)

        assertEquals(RuntimeState.READY, bridge.runtimeState.state)
    }

    @Test
    fun `pause then resume preserves BUSY state via single-slot memory`() {
        val bridge = newBridge()
        val activity = CytoidPluginActivity()
        driveToReady(bridge, activity)
        bridge.runtimeState.onSessionStarted("S1")

        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)
        assertEquals("S1", bridge.runtimeState.activeSessionId)

        // When: paused during BUSY, the session id is preserved.
        fireOnActivityPaused(activity)

        assertEquals(RuntimeState.SUSPENDED, bridge.runtimeState.state)
        assertEquals("S1", bridge.runtimeState.activeSessionId)

        // Then: resume restores BUSY and the active session id.
        fireOnActivityResumed(activity)

        assertEquals(RuntimeState.BUSY, bridge.runtimeState.state)
        assertEquals("S1", bridge.runtimeState.activeSessionId)
    }

    @Test
    fun `destroyed without active session resets state to UNAVAILABLE`() {
        val bridge = newBridge()
        val activity = CytoidPluginActivity()
        driveToReady(bridge, activity)
        assertNull(bridge.runtimeState.activeSessionId)
        assertEquals(1, bridge.unityActivityInstanceCount)

        // When: Activity destroyed with no active session.
        fireOnActivityDestroyed(activity)

        // Then: state resets to UNAVAILABLE (caller must startRuntime() again).
        assertEquals(RuntimeState.UNAVAILABLE, bridge.runtimeState.state)
        assertEquals(0, bridge.unityActivityInstanceCount)
        assertNull("exclusiveUnityActivity cleared", bridge.exclusiveUnityActivityRef())
    }

    @Test
    fun `destroyed with active session synthesizes SURFACE_LOST via T4 primitive`() {
        val bridge = newBridge()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }
        val activity = CytoidPluginActivity()
        driveToReady(bridge, activity)
        bridge.runtimeState.onSessionStarted("S1")
        assertEquals("S1", bridge.runtimeState.activeSessionId)

        // When: Activity destroyed during active session.
        fireOnActivityDestroyed(activity)

        // Then: exactly one SURFACE_LOST envelope emitted.
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
        assertEquals(
            "runtime_surface_lost",
            payload.getJSONObject("error").getString("code"),
        )

        // Post-condition: state = FAILED, activeSessionId cleared (idempotency).
        assertEquals(RuntimeState.FAILED, bridge.runtimeState.state)
        assertNull(bridge.runtimeState.activeSessionId)
        assertEquals(0, bridge.unityActivityInstanceCount)
    }

    @Test
    fun `destroyed with active session is idempotent at callback level`() {
        val bridge = newBridge()
        val captured = mutableListOf<String>()
        bridge.emitOverride = { captured.add(it) }
        val activity = CytoidPluginActivity()
        driveToReady(bridge, activity)
        bridge.runtimeState.onSessionStarted("S1")

        // First destroy fires SURFACE_LOST.
        fireOnActivityDestroyed(activity)
        val emittedAfterFirst = captured.size

        // Second destroy is a no-op: exclusiveUnityActivity is already null,
        // so the `=== activity` check fails and nothing happens.
        fireOnActivityDestroyed(activity)

        assertEquals(
            "second destroy must not emit anything (callback-level idempotency)",
            emittedAfterFirst,
            captured.size,
        )
    }

    @Test
    fun `memory regression - 10 sequential session cycles keep instanceCount at 1`() {
        val bridge = newBridge()
        bridge.emitOverride = { /* swallow mock bridge emissions */ }
        bridge.invokeReturnToFlutter = { /* no-op: don't start real Activity */ }
        val activity = CytoidPluginActivity()

        // Start runtime → STARTING.
        bridge.runtimeState.onRequestStart()
        assertEquals(RuntimeState.STARTING, bridge.runtimeState.state)

        // Create Activity (counter=1, state still STARTING) → engine.ready ack → READY.
        fireOnActivityCreated(activity)
        bridge.runtimeState.onEngineReady()
        assertEquals(RuntimeState.READY, bridge.runtimeState.state)
        assertEquals(1, bridge.unityActivityInstanceCount)

        // 10 sequential session cycles via low-level primitives.
        // Warm-resident: hideGameSurface does NOT destroy the Activity.
        for (cycle in 1..10) {
            bridge.onOutboundMessage(
                """{"schema":"cytoid.game-core.v2","id":"S$cycle","type":"session.start","payload":{}}""",
            )
            assertEquals(
                "cycle $cycle: state must be BUSY after session.start",
                RuntimeState.BUSY,
                bridge.runtimeState.state,
            )

            bridge.onOutboundMessage(
                """{"schema":"cytoid.game-core.v2","id":"S$cycle","type":"session.cancel","payload":{"sessionId":"S$cycle"}}""",
            )
            bridge.onUnityMessage(
                """{"schema":"cytoid.game-core.v2","id":"S$cycle","type":"session.result","payload":{"sessionId":"S$cycle","outcome":{"kind":"cancelled","reason":"userBack"}}}""",
            )
            assertEquals(
                "cycle $cycle: state must be READY after terminal session.result",
                RuntimeState.READY,
                bridge.runtimeState.state,
            )

            // Hide surface (warm-resident: Activity stays alive, Flutter to front).
            bridge.hideGameSurface()

            // Counter MUST NOT accumulate — Activity is warm-resident.
            assertEquals(
                "cycle $cycle: instanceCount must stay at 1 (warm-resident)",
                1,
                bridge.unityActivityInstanceCount,
            )
        }

        // Final assertion: exactly one Activity instance throughout 10 cycles.
        assertEquals(
            "after 10 cycles, instanceCount must be exactly 1",
            1,
            bridge.unityActivityInstanceCount,
        )
    }

    @Test
    fun `back-stack - hideGameSurface keeps Activity warm and returns to Flutter`() {
        val bridge = newBridge()
        bridge.emitOverride = { /* swallow mock bridge emissions */ }
        var returnToFlutterCalled = false
        bridge.invokeReturnToFlutter = { returnToFlutterCalled = true }
        val activity = CytoidPluginActivity()

        driveToReady(bridge, activity)
        bridge.runtimeState.onSessionStarted("S1")
        bridge.runtimeState.onSessionEnded()

        assertEquals(RuntimeState.READY, bridge.runtimeState.state)
        assertEquals(1, bridge.unityActivityInstanceCount)

        // When: hideGameSurface is called (session ended, return to Flutter).
        bridge.hideGameSurface()

        // Then: returnToFlutterActivity was invoked (Flutter brought to front).
        assertTrue(
            "returnToFlutterActivity must be called by hideGameSurface",
            returnToFlutterCalled,
        )

        // Warm-resident: Activity is NOT destroyed, counter unchanged.
        assertEquals(
            "warm-resident: Activity must stay alive after hideGameSurface",
            1,
            bridge.unityActivityInstanceCount,
        )
        assertEquals(
            "exclusiveUnityActivity must still be tracked",
            activity,
            bridge.exclusiveUnityActivityRef(),
        )
    }

    @Test
    fun `Activity recreation - destroy then recreate keeps counter bounded`() {
        val bridge = newBridge()
        bridge.emitOverride = { /* swallow */ }

        // First Activity lifecycle.
        val activity1 = CytoidPluginActivity()
        bridge.runtimeState.onRequestStart()
        fireOnActivityCreated(activity1)
        assertEquals(1, bridge.unityActivityInstanceCount)

        // OS destroys Activity (e.g. memory pressure).
        fireOnActivityDestroyed(activity1)
        assertEquals(0, bridge.unityActivityInstanceCount)
        assertEquals(RuntimeState.UNAVAILABLE, bridge.runtimeState.state)

        // Host restarts runtime, new Activity created.
        bridge.runtimeState.onRequestStart()
        val activity2 = CytoidPluginActivity()
        fireOnActivityCreated(activity2)
        assertEquals(
            "after recreation, counter must be exactly 1 (not accumulating)",
            1,
            bridge.unityActivityInstanceCount,
        )
    }

    // --- Helpers ---

    private fun newBridge(): CytoidGameCoreBridge =
        CytoidGameCoreBridge.getOrCreate(Activity())

    private fun driveToReady(bridge: CytoidGameCoreBridge, activity: Activity) {
        bridge.runtimeState.onRequestStart()
        fireOnActivityCreated(activity)
        // Simulate the engine.ready ack; Activity creation no longer sets READY.
        bridge.runtimeState.onEngineReady()
    }

    private fun getLifecycleCallbacks(): Application.ActivityLifecycleCallbacks {
        val field = CytoidGameCoreBridge::class.java
            .getDeclaredField("unityActivityLifecycleCallbacks")
        field.isAccessible = true
        return field.get(bridgeRef()) as Application.ActivityLifecycleCallbacks
    }

    private fun fireOnActivityCreated(activity: Activity) {
        getLifecycleCallbacks().onActivityCreated(activity, null)
    }

    private fun fireOnActivityPaused(activity: Activity) {
        getLifecycleCallbacks().onActivityPaused(activity)
    }

    private fun fireOnActivityResumed(activity: Activity) {
        getLifecycleCallbacks().onActivityResumed(activity)
    }

    private fun fireOnActivityDestroyed(activity: Activity) {
        getLifecycleCallbacks().onActivityDestroyed(activity)
    }

    // Reads the private `exclusiveUnityActivity` field for assertions.
    private fun CytoidGameCoreBridge.exclusiveUnityActivityRef(): Activity? {
        val field = CytoidGameCoreBridge::class.java
            .getDeclaredField("exclusiveUnityActivity")
        field.isAccessible = true
        return field.get(this) as Activity?
    }

    // The lifecycle callbacks field holds a reference to the outer bridge
    // instance via the anonymous object's closure. We read the bridge via
    // the companion `instance` field (set by the constructor).
    private fun bridgeRef(): CytoidGameCoreBridge {
        val field = CytoidGameCoreBridge::class.java.getDeclaredField("instance")
        field.isAccessible = true
        return field.get(null) as CytoidGameCoreBridge
    }

    private fun setCompanionInstance(value: CytoidGameCoreBridge?) {
        val field = CytoidGameCoreBridge::class.java.getDeclaredField("instance")
        field.isAccessible = true
        field.set(null, value)
    }
}
