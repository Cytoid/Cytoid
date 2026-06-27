package org.cytoid.gamecore

import android.app.Activity
import android.app.Application
import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.View
import androidx.annotation.VisibleForTesting
import io.flutter.plugin.common.EventChannel
import io.flutter.plugin.common.MethodChannel
import org.json.JSONObject

class CytoidGameCoreBridge private constructor(
    private var activity: Activity,
) : EventChannel.StreamHandler {
    // Lazy so the constructor does not touch the Android Looper before
    // attachActivity has confirmed Unity artifacts are available. This also
    // makes the bridge cheap to construct in JVM unit tests that exercise
    // fail-fast before any Handler use.
    private val mainHandler: Handler by lazy { Handler(Looper.getMainLooper()) }
    private val mockBridge: MockGameCoreBridge by lazy { MockGameCoreBridge(::emit, mainHandler) }
    private var eventSink: EventChannel.EventSink? = null
    private var exclusiveUnityActivity: Activity? = null

    // v2 runtime state. Replaces the v1 ad-hoc boolean tracking (startup
    // requested, engine acknowledgement, surface shown) with a single source
    // of truth that also tracks generation, activeSessionId, and lastError.
    // The flag→state migration table from the plan is encoded by the initial
    // UNAVAILABLE state plus the @VisibleForTesting transition methods driven
    // by lifecycle events below.
    private val runtimeState: RuntimeStateMachine = RuntimeStateMachine()

    // Tracks one-shot Application callback registration so attachActivity can
    // be called multiple times (config changes) without double-registering.
    private var lifecycleRegistered = false

    private val unityActivityLifecycleCallbacks =
        object : Application.ActivityLifecycleCallbacks {
            override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
                if (isUnityGameplayActivity(activity)) {
                    exclusiveUnityActivity = activity
                    runtimeState.onEngineReady()
                    Log.i(TAG, "Exclusive game core activity created: ${activity.javaClass.name}")
                }
            }

            override fun onActivityStarted(activity: Activity) = Unit

            override fun onActivityResumed(activity: Activity) {
                if (isUnityGameplayActivity(activity)) {
                    Log.i(TAG, "[CYTOID-DBG] Unity activity RESUMED (lifecycle)")
                    runtimeState.onResume()
                    mainHandler.postDelayed({
                        applyExclusiveDisplayRefreshRate(activity)
                    }, REFRESH_RATE_APPLY_DELAY_MS)
                }
            }

            override fun onActivityPaused(activity: Activity) {
                if (isUnityGameplayActivity(activity)) {
                    Log.i(TAG, "[CYTOID-DBG] Unity activity PAUSED (lifecycle)")
                    runtimeState.onSuspend()
                }
            }

            override fun onActivityStopped(activity: Activity) = Unit
            override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) = Unit

            override fun onActivityDestroyed(activity: Activity) {
                if (exclusiveUnityActivity === activity) {
                    Log.i(TAG, "[CYTOID-DBG] Unity activity DESTROYED")
                    exclusiveUnityActivity = null
                }
            }
        }

    val engineMode: String
        get() = if (useUnityRuntime) ENGINE_MODE_UNITY else ENGINE_MODE_MOCK

    val mode: String
        get() = if (useUnityRuntime) ENGINE_MODE_UNITY else ENGINE_MODE_MOCK

    private val useUnityRuntime: Boolean
        get() = probeUnityAvailable()

    init {
        instance = this
        if (!useUnityRuntime) {
            Log.i(TAG, "Unity artifact missing, using mock game core")
        }
    }

    fun attachActivity(activity: Activity) {
        if (!probeUnityAvailable()) {
            throw IllegalStateException(
                "Unity artifacts not loaded. Run setup_unity_artifacts.sh then flutter clean.",
            )
        }
        this.activity = activity
        if (!lifecycleRegistered) {
            activity.application.registerActivityLifecycleCallbacks(unityActivityLifecycleCallbacks)
            lifecycleRegistered = true
        }
    }

    fun detachActivity() = Unit

    fun ensureRuntimeStarted() {
        runtimeState.onRequestStart()
        if (!useUnityRuntime) {
            mockBridge.ensureRuntimeStarted()
        }
    }

    fun showGameSurface(result: MethodChannel.Result) {
        if (!probeUnityAvailable()) {
            throw IllegalStateException(
                "Unity artifacts not loaded. Run setup_unity_artifacts.sh then flutter clean.",
            )
        }
        runtimeState.onRequestStart()
        Log.i(TAG, "[CYTOID-DBG] showGameSurface called: useUnityRuntime=$useUnityRuntime exclusiveUnityActivity=$exclusiveUnityActivity")

        if (useUnityRuntime) {
            val intent =
                Intent()
                    .setClassName(activity.packageName, CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY)
                    .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
                    .addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP)
            runCatching {
                activity.startActivity(intent)
                Log.i(TAG, "[CYTOID-DBG] showGameSurface: startActivity OK (REORDER_TO_FRONT|SINGLE_TOP)")
                result.success(null)
            }.onFailure { error ->
                Log.e(TAG, "[CYTOID-DBG] showGameSurface: startActivity FAILED", error)
                result.error(
                    "unity_launch_failed",
                    error.message ?: "Failed to launch exclusive Unity activity.",
                    null,
                )
            }
            return
        }

        mockBridge.showGameSurface()
        result.success(null)
    }

    fun hideGameSurface() {
        Log.i(TAG, "[CYTOID-DBG] hideGameSurface called: state=${runtimeState.state} exclusiveUnityActivity=$exclusiveUnityActivity")
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
        val type = runCatching { JSONObject(jsonString).optString("type") }.getOrDefault("")
        Log.i(TAG, "[CYTOID-DBG] -> Unity: type=$type state=${runtimeState.state} activeSessionId=${runtimeState.activeSessionId}")

        when {
            isSessionStartMessage(jsonString) -> {
                runtimeState.onSessionStarted(JSONObject(jsonString).optString("id"))
            }
            isSessionEndMessage(jsonString) -> {
                runtimeState.onSessionEnded()
            }
            isGameStartMessage(jsonString) -> {
                // v1 fallback: bridge.play.start arrives without v2 session.started,
                // so treat it as READY→BUSY using the envelope id.
                runtimeState.onSessionStarted(JSONObject(jsonString).optString("id"))
            }
            isSessionEndMessageV1(jsonString) -> {
                runtimeState.onSessionEnded()
            }
        }

        if (useUnityRuntime) {
            sendToUnity(jsonString)
            return
        }
        mockBridge.onOutboundMessage(jsonString)
    }

    fun onUnityMessage(jsonString: String) {
        val type = runCatching { JSONObject(jsonString).optString("type") }.getOrDefault("")
        Log.i(TAG, "[CYTOID-DBG] <- Unity: type=$type state=${runtimeState.state} activeSessionId=${runtimeState.activeSessionId}")

        emit(jsonString)

        // v2 engine.ready or v1 fallback game.ready both complete the
        // STARTING→READY transition (single-slot resume memory preserved).
        if (isEngineReadyMessage(jsonString) || isHostReadyMessage(jsonString)) {
            runtimeState.onEngineReady()
            Log.i(TAG, "[CYTOID-DBG] <- Unity: ready received — state=${runtimeState.state}")
        }
        // v2 session.started: explicit READY→BUSY signal carries the sessionId.
        if (isSessionStartedMessage(jsonString)) {
            val sessionId = JSONObject(jsonString).optString("id")
            if (sessionId.isNotEmpty()) {
                runtimeState.onSessionStarted(sessionId)
            }
        }
        if (isSessionResultMessage(jsonString) || isGameResultMessage(jsonString)) {
            runtimeState.onSessionEnded()
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
        return activity.javaClass.name == CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY
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

    private fun isSessionResultMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "session.result"
        }.getOrDefault(false)
    }

    private fun isHostReadyMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "game.ready"
        }.getOrDefault(false)
    }

    private fun isEngineReadyMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "engine.ready"
        }.getOrDefault(false)
    }

    private fun isGameStartMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "bridge.play.start"
        }.getOrDefault(false)
    }

    private fun isSessionStartMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "session.start"
        }.getOrDefault(false)
    }

    private fun isSessionStartedMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "session.started"
        }.getOrDefault(false)
    }

    private fun isSessionEndMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "session.cancel"
        }.getOrDefault(false)
    }

    private fun isSessionEndMessageV1(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "bridge.play.end"
        }.getOrDefault(false)
    }

    private fun isSessionEndedMessage(jsonString: String): Boolean {
        return runCatching {
            JSONObject(jsonString).getString("type") == "game.play.ended"
        }.getOrDefault(false)
    }

    /**
     * v2 runtime snapshot. Conditional optionality per spec:
     * required keys `engine`, `mode`, `state`, `generation` always present;
     * `activeSessionId` only when `state = busy`; `error` only when
     * `state = failed`.
     */
    fun runtimeStatus(): Map<String, Any?> {
        val snapshot = runtimeState.snapshot(engine = engineMode, mode = mode)
        Log.i(
            TAG,
            "[CYTOID-DBG] runtimeStatus(): $snapshot",
        )
        return snapshot
    }

    private fun sendToUnity(jsonString: String) {
        runCatching {
            Class.forName(CytoidNativeConfig.UNITY_PLAYER_CLASS)
                .getMethod(
                    "UnitySendMessage",
                    String::class.java,
                    String::class.java,
                    String::class.java,
                )
                .invoke(
                    null,
                    CytoidNativeConfig.UNITY_BRIDGE_OBJECT,
                    CytoidNativeConfig.UNITY_BRIDGE_METHOD,
                    jsonString,
                )
        }.onFailure { error ->
            Log.e(TAG, "[CYTOID-DBG] UnitySendMessage FAILED (Unity not loaded yet?): ${error.javaClass.simpleName}: ${error.message}")
        }.onSuccess {
            Log.i(TAG, "[CYTOID-DBG] UnitySendMessage OK")
        }
    }

    private fun emit(json: String) {
        if (isSessionResultMessage(jsonString = json) ||
            isGameResultMessage(json) ||
            isSessionEndedMessage(json)
        ) {
            runtimeState.onSessionEnded()
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
        private const val REFRESH_RATE_APPLY_DELAY_MS = 1500L

        @Volatile
        var instance: CytoidGameCoreBridge? = null
            private set

        fun getOrCreate(activity: Activity): CytoidGameCoreBridge {
            return instance ?: CytoidGameCoreBridge(activity)
        }
    }
}

