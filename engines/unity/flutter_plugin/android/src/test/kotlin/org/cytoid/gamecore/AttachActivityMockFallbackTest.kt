package org.cytoid.gamecore

import android.app.Activity
import android.app.Application
import io.mockk.every
import io.mockk.mockk
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Before
import org.junit.Test

/**
 * Verifies [CytoidGameCoreBridge.attachActivity] falls back to the mock
 * runtime (instead of throwing) when Unity artifacts are not loadable.
 *
 * Mock mode is a supported runtime per `docs/mock-engine.md` and AGENTS.md
 * ("Without artifacts, the plugin uses a mock engine"). Fail-fast belongs
 * only on the real-Unity paths that actually require the artifacts; the
 * absence of artifacts is the documented mock entry condition.
 *
 * The probe seam (`probeUnityAvailable`) is forced false to mimic the
 * no-artifact condition without a real Unity class path. A MockK Activity is
 * used because the JVM stub android.jar returns null from `getApplication()`.
 */
class AttachActivityMockFallbackTest {

    private var previousProbe: (() -> Boolean)? = null

    @Before
    fun resetInstanceCompanion() {
        setCompanionInstance(null)
        previousProbe = probeUnityAvailable
    }

    @After
    fun restoreProbeAndClearInstance() {
        previousProbe?.let { probeUnityAvailable = it }
        previousProbe = null
        setCompanionInstance(null)
    }

    @Test
    fun attachActivity_doesNotThrow_whenUnityArtifactsMissing() {
        probeUnityAvailable = { false }

        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        bridge.attachActivity(mockActivityWithApplication())
    }

    @Test
    fun attachActivity_doesNotThrow_whenProbeMimicsClassForNameFailure() {
        probeUnityAvailable = DEFAULT_PROBE

        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        bridge.attachActivity(mockActivityWithApplication())
    }

    @Test
    fun engineMode_reportsMock_whenUnityArtifactsMissing() {
        probeUnityAvailable = { false }

        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        bridge.attachActivity(mockActivityWithApplication())

        assertEquals("mock", bridge.engineMode)
        assertEquals("mock", bridge.mode)
    }

    private fun mockActivityWithApplication(): Activity {
        val activity = mockk<Activity>()
        every { activity.application } returns mockk(relaxed = true)
        return activity
    }

    private fun setCompanionInstance(value: CytoidGameCoreBridge?) {
        val field = CytoidGameCoreBridge::class.java.getDeclaredField("instance")
        field.isAccessible = true
        field.set(null, value)
    }

    private companion object {
        val DEFAULT_PROBE: () -> Boolean = {
            try {
                Class.forName(CytoidNativeConfig.UNITY_PLAYER_CLASS)
                true
            } catch (_: ClassNotFoundException) {
                false
            }
        }
    }
}