package org.cytoid.gamecore

import android.app.Activity
import org.junit.After
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Before
import org.junit.Test

/**
 * Verifies [CytoidGameCoreBridge.attachActivity] fails fast with an actionable
 * message when Unity artifacts are not loadable at runtime.
 *
 * Acceptance (from `.omo/plans/v2-host-impl.md` T1):
 *  - probeUnityAvailable swapped to mimic Class.forName throwing
 *    ClassNotFoundException internally (the production probe swallows that and
 *    returns false; the swap mirrors the same shape).
 *  - Calling attachActivity must throw IllegalStateException.
 *  - Message MUST contain both "setup_unity_artifacts.sh" and "flutter clean".
 *
 * The probe seam is the testability contract: never inline Class.forName in
 * attachActivity, otherwise this test cannot exercise the missing-artifact path
 * without a real Unity classpath.
 */
class AttachActivityFailFastTest {

    @Before
    fun resetInstanceCompanion() {
        setCompanionInstance(null)
    }

    @After
    fun restoreProbeAndClearInstance() {
        probeUnityAvailable = DEFAULT_PROBE
        setCompanionInstance(null)
    }

    @Test
    fun attachActivity_throwsIllegalStateException_whenProbeReturnsFalse() {
        probeUnityAvailable = { false }

        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        try {
            bridge.attachActivity(Activity())
            fail("Expected attachActivity to throw IllegalStateException when Unity is unavailable")
        } catch (expected: IllegalStateException) {
            assertRequiredRemediationMessage(expected)
        }
    }

    @Test
    fun attachActivity_throwsIllegalStateException_whenProbeMimicsClassForNameFailure() {
        // Same shape as the production default probe: try Class.forName, catch
        // ClassNotFoundException, return false. In the test JVM, Unity classes
        // are absent so the inner Class.forName really does throw.
        probeUnityAvailable = DEFAULT_PROBE

        val bridge = CytoidGameCoreBridge.getOrCreate(Activity())
        try {
            bridge.attachActivity(Activity())
            fail("Expected attachActivity to throw IllegalStateException when probe reports Unity missing")
        } catch (expected: IllegalStateException) {
            assertRequiredRemediationMessage(expected)
        }
    }

    private fun assertRequiredRemediationMessage(error: IllegalStateException) {
        val message = error.message ?: error("IllegalStateException must carry a message")
        assertTrue(
            "Message must name setup_unity_artifacts.sh — was: $message",
            message.contains("setup_unity_artifacts.sh"),
        )
        assertTrue(
            "Message must instruct user to run flutter clean — was: $message",
            message.contains("flutter clean"),
        )
    }

    private fun setCompanionInstance(value: CytoidGameCoreBridge?) {
        // Kotlin hoists `companion object`'s `var instance` to a private static
        // field on the OUTER class, so reflect there (not on Companion).
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

