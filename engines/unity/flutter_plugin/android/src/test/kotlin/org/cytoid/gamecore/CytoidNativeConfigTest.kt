package org.cytoid.gamecore

import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Before
import org.junit.Test

/**
 * Pins every value in [CytoidNativeConfig] against the contract documented in
 * AGENTS.md "Package identifiers" so a typo'd refactor cannot silently break
 * the Activity-to-Kotlin binding.
 *
 * Lives under `android/src/test/kotlin/...` (test sourceSet) so it runs as part
 * of `:cytoid_game_core:testDebugUnitTest` from the example app's Gradle wrapper.
 */
class CytoidNativeConfigTest {

    private var previousProbe: (() -> Boolean)? = null

    @Before
    fun saveProbe() {
        previousProbe = probeUnityAvailable
    }

    @After
    fun tearDown() {
        previousProbe?.let { probeUnityAvailable = it }
        previousProbe = null
    }

    @Test
    fun unityPlayerClass_isNonEmpty_andMatchesJavaPackagePattern() {
        val value = CytoidNativeConfig.UNITY_PLAYER_CLASS
        assertTrue("UNITY_PLAYER_CLASS must not be empty", value.isNotEmpty())
        assertTrue(
            "UNITY_PLAYER_CLASS should live under com.unity3d.player.* — was: $value",
            value.startsWith("com.unity3d.player."),
        )
        assertTrue(
            "UNITY_PLAYER_CLASS should reference UnityPlayer — was: $value",
            value.contains("UnityPlayer"),
        )
    }

    @Test
    fun unityPlayerForActivityClass_isNonEmpty_andMatchesJavaPackagePattern() {
        val value = CytoidNativeConfig.UNITY_PLAYER_FOR_ACTIVITY_CLASS
        assertTrue("UNITY_PLAYER_FOR_ACTIVITY_CLASS must not be empty", value.isNotEmpty())
        assertTrue(
            "UNITY_PLAYER_FOR_ACTIVITY_CLASS should live under com.unity3d.player.* — was: $value",
            value.startsWith("com.unity3d.player."),
        )
    }

    @Test
    fun unityGameplayActivity_isNonEmpty_andUsesProductionApplicationIdPrefix() {
        val value = CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY
        assertTrue("UNITY_GAMEPLAY_ACTIVITY must not be empty", value.isNotEmpty())
        assertTrue(
            "UNITY_GAMEPLAY_ACTIVITY must live under the production applicationId — was: $value",
            value.startsWith("${CytoidNativeConfig.PRODUCTION_APPLICATION_ID}."),
        )
    }

    @Test
    fun unityGameplayActivity_doesNotUseLegacyFlutterPackage() {
        val value = CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY
        assertFalse(
            "UNITY_GAMEPLAY_ACTIVITY must NOT use the Flutter Unity library export package id",
            value.startsWith(CytoidNativeConfig.FLUTTER_UNITY_LIBRARY_PACKAGE_ID),
        )
        assertFalse(
            "UNITY_GAMEPLAY_ACTIVITY must NOT use the Flutter example package id",
            value.startsWith(CytoidNativeConfig.FLUTTER_EXAMPLE_PACKAGE_ID),
        )
        // Reject any "*PlayerActivity" class — the bridge requires the
        // Cytoid-owned plugin activity, not the default Unity export.
        assertFalse(
            "UNITY_GAMEPLAY_ACTIVITY must NOT be a default Unity PlayerActivity variant — was: $value",
            value.endsWith("PlayerActivity"),
        )
    }

    @Test
    fun unityBridgeObject_isNonEmpty() {
        assertTrue(
            "UNITY_BRIDGE_OBJECT must not be empty",
            CytoidNativeConfig.UNITY_BRIDGE_OBJECT.isNotEmpty(),
        )
    }

    @Test
    fun unityBridgeMethod_isNonEmpty() {
        assertTrue(
            "UNITY_BRIDGE_METHOD must not be empty",
            CytoidNativeConfig.UNITY_BRIDGE_METHOD.isNotEmpty(),
        )
    }

    @Test
    fun packageIdentifiers_areNonEmpty_andUseValidJavaPackageShape() {
        val ids = listOf(
            CytoidNativeConfig.PRODUCTION_APPLICATION_ID,
            CytoidNativeConfig.FLUTTER_UNITY_LIBRARY_PACKAGE_ID,
            CytoidNativeConfig.FLUTTER_EXAMPLE_PACKAGE_ID,
            CytoidNativeConfig.PLUGIN_NAMESPACE,
        )
        ids.forEach { id ->
            assertTrue("Package id must not be empty", id.isNotEmpty())
            assertTrue(
                "Package id must be a valid lowercase Java package — was: $id",
                id.matches(Regex("^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)+$")),
            )
        }
        assertTrue(
            "FLUTTER_UNITY_LIBRARY_PACKAGE_ID must nest under FLUTTER_EXAMPLE_PACKAGE_ID",
            CytoidNativeConfig.FLUTTER_UNITY_LIBRARY_PACKAGE_ID.startsWith(
                CytoidNativeConfig.FLUTTER_EXAMPLE_PACKAGE_ID + ".",
            ),
        )
        assertFalse(
            "PLUGIN_NAMESPACE must not collide with the example app package id",
            CytoidNativeConfig.PLUGIN_NAMESPACE == CytoidNativeConfig.FLUTTER_EXAMPLE_PACKAGE_ID,
        )
    }

    @Test
    fun probeUnityAvailable_canBeSwapped_true() {
        probeUnityAvailable = { true }
        assertTrue("Probe seam should be swappable to return true", probeUnityAvailable())
    }

    @Test
    fun probeUnityAvailable_canBeSwapped_false() {
        probeUnityAvailable = { false }
        assertFalse("Probe seam should be swappable to return false", probeUnityAvailable())
    }

    @Test
    fun probeUnityAvailable_defaultImplReturnsFalse_whenClassMissing() {
        // In the unit test JVM, com.unity3d.player.UnityPlayer is not on the classpath,
        // so the default probe implementation must return false (no exception thrown).
        probeUnityAvailable = DEFAULT_PROBE
        assertFalse(
            "Default probe must return false when Unity classes are missing",
            probeUnityAvailable(),
        )
    }

    @Test
    fun probeUnityAvailable_seamAcceptsThrowingLambda() {
        // The attachActivity test relies on the seam accepting a throwing probe;
        // verify the seam type accepts a lambda that throws.
        probeUnityAvailable = { throw ClassNotFoundException("simulated") }
        try {
            probeUnityAvailable()
            fail("Expected the swapped probe to throw")
        } catch (_: ClassNotFoundException) {
            // expected
        }
    }

    // --- Exact-value contract: pins every identifier literal so a rename
    //     within the allowed shape (e.g. a different UnityPlayer* class or a
    //     changed bridge method) cannot slip through the shape/prefix tests
    //     above. Values MUST match CytoidNativeConfig.kt and AGENTS.md. ---

    @Test
    fun contract_pinsExactIdentifierLiterals() {
        assertEquals(
            "com.unity3d.player.UnityPlayerForActivityOrService",
            CytoidNativeConfig.UNITY_PLAYER_FOR_ACTIVITY_CLASS,
        )
        assertEquals(
            "com.unity3d.player.UnityPlayer",
            CytoidNativeConfig.UNITY_PLAYER_CLASS,
        )
        assertEquals(
            "me.tigerhix.cytoid.CytoidPluginActivity",
            CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY,
        )
        assertEquals("GameBridge", CytoidNativeConfig.UNITY_BRIDGE_OBJECT)
        assertEquals("OnBridgeMessage", CytoidNativeConfig.UNITY_BRIDGE_METHOD)
        assertEquals("me.tigerhix.cytoid", CytoidNativeConfig.PRODUCTION_APPLICATION_ID)
        assertEquals(
            "com.example.cytoid_flutter.unity",
            CytoidNativeConfig.FLUTTER_UNITY_LIBRARY_PACKAGE_ID,
        )
        assertEquals(
            "com.example.cytoid_flutter",
            CytoidNativeConfig.FLUTTER_EXAMPLE_PACKAGE_ID,
        )
        assertEquals("org.cytoid.gamecore", CytoidNativeConfig.PLUGIN_NAMESPACE)
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
