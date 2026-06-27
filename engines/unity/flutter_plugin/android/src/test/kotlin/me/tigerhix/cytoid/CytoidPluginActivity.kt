package me.tigerhix.cytoid

import android.app.Activity

/**
 * Test-only stub whose fully qualified class name matches
 * [org.cytoid.gamecore.CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY].
 *
 * The real CytoidPluginActivity is compiled into the Unity AAR (not editable
 * from this repo). This stub lets JVM unit tests exercise the bridge's
 * `isUnityGameplayActivity(activity)` class-name check without loading the AAR,
 * by passing instances to the ActivityLifecycleCallbacks via reflection.
 */
class CytoidPluginActivity : Activity()
