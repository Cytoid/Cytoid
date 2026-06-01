package org.cytoid.gamecore

import android.app.Activity
import android.app.Application
import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.View
import io.flutter.plugin.common.EventChannel
import io.flutter.plugin.common.MethodChannel
import org.json.JSONObject

class CytoidGameCoreBridge private constructor(
    private var activity: Activity,
) : EventChannel.StreamHandler {
    private val mainHandler = Handler(Looper.getMainLooper())
    private val mockBridge = MockGameCoreBridge(::emit)
    private var eventSink: EventChannel.EventSink? = null
    private var exclusiveUnityActivity: Activity? = null
    private var runtimeStarted = false
    private var engineReady = false
    private var surfaceVisible = false
    private var activePlayId: String? = null

    private val unityActivityLifecycleCallbacks =
        object : Application.ActivityLifecycleCallbacks {
            override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
                if (isUnityGameplayActivity(activity)) {
                    exclusiveUnityActivity = activity
                    surfaceVisible = true
                    runtimeStarted = true
                    Log.i(TAG, "Exclusive game core activity created: ${activity.javaClass.name}")
                }
            }

            override fun onActivityStarted(activity: Activity) = Unit

            override fun onActivityResumed(activity: Activity) {
                if (isUnityGameplayActivity(activity)) {
                    mainHandler.postDelayed({
                        applyExclusiveDisplayRefreshRate(activity)
                    }, REFRESH_RATE_APPLY_DELAY_MS)
                }
            }

            override fun onActivityPaused(activity: Activity) = Unit
            override fun onActivityStopped(activity: Activity) = Unit
            override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) = Unit

            override fun onActivityDestroyed(activity: Activity) {
                if (exclusiveUnityActivity === activity) {
                    exclusiveUnityActivity = null
                    surfaceVisible = false
                }
            }
        }

    val engineMode: String
        get() = if (useUnityRuntime) ENGINE_MODE_UNITY else ENGINE_MODE_MOCK

    private val useUnityRuntime: Boolean
        get() = BuildConfig.UNITY_ARTIFACT_AVAILABLE && unityPlayerClass != null

    init {
        instance = this
        activity.application.registerActivityLifecycleCallbacks(unityActivityLifecycleCallbacks)
        if (!useUnityRuntime) {
            Log.i(TAG, "Unity artifact missing, using mock game core")
        }
    }

    fun attachActivity(activity: Activity) {
        this.activity = activity
    }

    fun detachActivity() = Unit

    fun ensureRuntimeStarted() {
        runtimeStarted = true
        if (!useUnityRuntime) {
            mockBridge.ensureRuntimeStarted()
        }
    }

    fun showGameSurface(result: MethodChannel.Result) {
        runtimeStarted = true

        if (useUnityRuntime) {
            val intent =
                Intent()
                    .setClassName(activity.packageName, UNITY_GAMEPLAY_ACTIVITY)
                    .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
                    .addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP)
            runCatching {
                activity.startActivity(intent)
                surfaceVisible = true
                result.success(null)
            }.onFailure { error ->
                result.error(
                    "unity_launch_failed",
                    error.message ?: "Failed to launch exclusive Unity activity.",
                    null,
                )
            }
            return
        }

        surfaceVisible = true
        mockBridge.showGameSurface()
        result.success(null)
    }

    fun hideGameSurface() {
        surfaceVisible = false
        DisplayRefreshRateHelper.restoreDefaultRefreshRate(activity)

        if (exclusiveUnityActivity != null) {
            returnToFlutterActivity()
        }

        mockBridge.hideGameSurface()
    }

    fun applyExclusiveDisplayRefreshRate(gameplayActivity: Activity) {
        if (!useUnityRuntime) {
            return
        }

        val unityRootView = resolveUnityRootView(gameplayActivity) ?: return
        DisplayRefreshRateHelper.applyGameplayRefreshRate(gameplayActivity, unityRootView)
    }

    fun onOutboundMessage(jsonString: String) {
        if (isGameStartMessage(jsonString)) {
            activePlayId = JSONObject(jsonString).optString("id").takeIf { it.isNotEmpty() }
        }
        if (isSessionEndMessage(jsonString)) {
            activePlayId = null
        }

        if (useUnityRuntime) {
            sendToUnity(jsonString)
            return
        }
        mockBridge.onOutboundMessage(jsonString)
    }

    fun onUnityMessage(jsonString: String) {
        emit(jsonString)

        if (isHostReadyMessage(jsonString)) {
            engineReady = true
            runtimeStarted = true
        }
        if (isGameResultMessage(jsonString)) {
            activePlayId = null
        }
    }

    fun dispose() {
        activity.application.unregisterActivityLifecycleCallbacks(unityActivityLifecycleCallbacks)
        hideGameSurface()
        if (instance === this) {
            instance = null
        }
    }

    private fun returnToFlutterActivity() {
        val intent =
            Intent(activity, activity.javaClass)
                .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
                .addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP)

        runCatching {
            activity.startActivity(intent)
        }.onFailure { error ->
            Log.e(TAG, "Failed to return to Flutter activity", error)
        }
    }

    private fun isUnityGameplayActivity(activity: Activity): Boolean {
        return activity.javaClass.name == UNITY_GAMEPLAY_ACTIVITY
    }

    private fun resolveUnityRootView(gameplayActivity: Activity): View? {
        return runCatching {
            val connectionMethod =
                gameplayActivity.javaClass.methods.firstOrNull { method ->
                    method.name == "getUnityPlayerConnection" && method.parameterCount == 0
                } ?: return null

            val connection = connectionMethod.invoke(gameplayActivity) ?: return null
            val connectionClass = connection.javaClass

            runCatching {
                connectionClass.getMethod("getFrameLayout").invoke(connection) as? View
            }.getOrNull()
                ?: runCatching {
                    connectionClass.getMethod("getView").invoke(connection) as? View
                }.getOrNull()
        }.getOrNull()
    }

    private fun isGameResultMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "game.play.result"
        }.getOrDefault(false)
    }

    private fun isHostReadyMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "game.ready"
        }.getOrDefault(false)
    }

    private fun isGameStartMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "bridge.play.start"
        }.getOrDefault(false)
    }

    private fun isSessionEndMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "bridge.play.end"
        }.getOrDefault(false)
    }

    private fun isSessionEndedMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "game.play.ended"
        }.getOrDefault(false)
    }

    fun runtimeStatus(): Map<String, Any?> {
        val state =
            when {
                !runtimeStarted && !useUnityRuntime -> RUNTIME_UNAVAILABLE
                activePlayId != null -> RUNTIME_BUSY
                engineReady -> RUNTIME_READY
                runtimeStarted || surfaceVisible || useUnityRuntime -> RUNTIME_STARTING
                else -> RUNTIME_UNAVAILABLE
            }
        return mapOf(
            "state" to state,
            "engine" to engineMode,
            "activePlayId" to activePlayId,
        )
    }

    private fun sendToUnity(jsonString: String) {
        runCatching {
            Class.forName(UNITY_PLAYER_CLASS)
                .getMethod(
                    "UnitySendMessage",
                    String::class.java,
                    String::class.java,
                    String::class.java,
                )
                .invoke(null, UNITY_BRIDGE_OBJECT, UNITY_BRIDGE_METHOD, jsonString)
        }.onFailure { error ->
            Log.e(TAG, "UnitySendMessage failed", error)
        }
    }

    private fun emit(json: String) {
        if (isGameResultMessage(json) || isSessionEndedMessage(json)) {
            activePlayId = null
        }
        mainHandler.post {
            eventSink?.success(json)
        }
    }

    override fun onListen(arguments: Any?, events: EventChannel.EventSink?) {
        eventSink = events
    }

    override fun onCancel(arguments: Any?) {
        eventSink = null
    }

    companion object {
        private const val TAG = "CytoidGameCoreBridge"
        private const val ENGINE_MODE_UNITY = "unity"
        private const val ENGINE_MODE_MOCK = "mock"
        private const val RUNTIME_UNAVAILABLE = "unavailable"
        private const val RUNTIME_STARTING = "starting"
        private const val RUNTIME_READY = "ready"
        private const val RUNTIME_BUSY = "busy"
        private const val REFRESH_RATE_APPLY_DELAY_MS = 1500L
        private const val UNITY_BRIDGE_OBJECT = "GameBridge"
        private const val UNITY_BRIDGE_METHOD = "OnBridgeMessage"
        private const val UNITY_PLAYER_CLASS = "com.unity3d.player.UnityPlayer"
        private const val UNITY_PLAYER_FOR_ACTIVITY_CLASS =
            "com.unity3d.player.UnityPlayerForActivityOrService"
        private const val UNITY_GAMEPLAY_ACTIVITY = "me.tigerhix.cytoid.CytoidPluginActivity"

        @Volatile
        var instance: CytoidGameCoreBridge? = null
            private set

        fun getOrCreate(activity: Activity): CytoidGameCoreBridge {
            return instance ?: CytoidGameCoreBridge(activity)
        }

        private val unityPlayerClass: Class<*>? =
            runCatching {
                Class.forName(UNITY_PLAYER_FOR_ACTIVITY_CLASS)
            }.getOrElse {
                runCatching { Class.forName(UNITY_PLAYER_CLASS) }.getOrNull()
            }
    }
}
