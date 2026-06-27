package org.cytoid.gamecore

import androidx.annotation.VisibleForTesting

/**
 * Single source of truth for native Unity class names, package ids, and
 * legacy references used by the Android plugin.
 *
 * Rationale: scattered string literals made it easy to drift between the
 * Kotlin bridge, AndroidManifest, and AAR-side Activity implementations.
 * Centralizing them here lets a unit test pin every value against the
 * contract documented in AGENTS.md "Package identifiers".
 *
 * The values in this file MUST match:
 *  - `engines/unity/flutter_plugin/android/src/main/AndroidManifest.xml`
 *    (`android:name` on the `<activity>` element)
 *  - the Unity export's `CytoidPluginActivity` (compiled into the AAR)
 *  - the C# `GameBridge` MonoBehaviour name and `OnBridgeMessage` method
 */
object CytoidNativeConfig {

    // --- Unity player classes (loaded from the AAR at runtime) ---

    /** Fullscreen-capable Unity player used by exportable activities. */
    const val UNITY_PLAYER_FOR_ACTIVITY_CLASS =
        "com.unity3d.player.UnityPlayerForActivityOrService"

    /** Fallback Unity player class for general-purpose send-message use. */
    const val UNITY_PLAYER_CLASS = "com.unity3d.player.UnityPlayer"

    // --- Exclusive Unity Activity hosted by the AAR ---

    /**
     * The Cytoid-owned Unity Activity launched in exclusive mode.
     * MUST match the `<activity android:name="...">` in `AndroidManifest.xml`.
     *
     * Legacy alternatives that MUST NOT be used (documented for grep visibility):
     *  - `com.unity3d.player.UnityPlayerActivity` (default Unity export)
     *  - `com.example.cytoid_flutter.unity.UnityPlayerActivity`
     *    (Flutter Unity library export package id)
     */
    const val UNITY_GAMEPLAY_ACTIVITY = "me.tigerhix.cytoid.CytoidPluginActivity"

    // --- UnitySendMessage reflection targets (defined on the C# side) ---

    /** Name of the C# MonoBehaviour that receives host→engine messages. */
    const val UNITY_BRIDGE_OBJECT = "GameBridge"

    /** Method on [UNITY_BRIDGE_OBJECT] invoked with one JSON envelope string. */
    const val UNITY_BRIDGE_METHOD = "OnBridgeMessage"

    // --- Package identifiers (mirrors AGENTS.md "Package identifiers") ---

    /** Production Android applicationId (set in Unity ProjectSettings). */
    const val PRODUCTION_APPLICATION_ID = "me.tigerhix.cytoid"

    /** Flutter Unity library export package id (legacy, do NOT use at runtime). */
    const val FLUTTER_UNITY_LIBRARY_PACKAGE_ID = "com.example.cytoid_flutter.unity"

    /** Flutter example app package id (legacy, do NOT use at runtime). */
    const val FLUTTER_EXAMPLE_PACKAGE_ID = "com.example.cytoid_flutter"

    /** This plugin's Android namespace (matches `build.gradle.kts` namespace). */
    const val PLUGIN_NAMESPACE = "org.cytoid.gamecore"
}

/**
 * Runtime probe used by [CytoidGameCoreBridge.attachActivity] and
 * [CytoidGameCoreBridge.showGameSurface] to fail fast when Unity artifacts
 * are missing from the build.
 *
 * Why an injectable `var` (not an inline `Class.forName`): tests need to flip
 * the probe to `false` (or to throw `ClassNotFoundException`) without the real
 * Unity classes on the classpath. The bridge calls this top-level function;
 * tests reassign it from within the same Kotlin module.
 *
 * Default implementation reflects on [CytoidNativeConfig.UNITY_PLAYER_CLASS]
 * because that class is present iff the AAR shipped `libunity.so`/`libil2cpp.so`
 * for the current ABI. A file-existence check would NOT be sufficient — the AAR
 * could be present without native libs matching the device ABI.
 */
@VisibleForTesting
internal var probeUnityAvailable: () -> Boolean = {
    try {
        Class.forName(CytoidNativeConfig.UNITY_PLAYER_CLASS)
        true
    } catch (_: ClassNotFoundException) {
        false
    } catch (_: LinkageError) {
        // Class present but unloadable (UnsatisfiedLinkError /
        // ExceptionInInitializerError / ABI mismatch): route to mock rather
        // than letting the linkage failure escape and crash the caller.
        false
    }
}
