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
    private val mockBridge: MockGameCoreBridge by lazy { MockGameCoreBridge(::onUnityMessage, mainHandler) }
    private var eventSink: EventChannel.EventSink? = null
    private var exclusiveUnityActivity: Activity? = null

    // v2 runtime state. Replaces the v1 ad-hoc boolean tracking (startup
    // requested, engine acknowledgement, surface shown) with a single source
    // of truth that also tracks generation, activeSessionId, and lastError.
    // The flag→state migration table from the plan is encoded by the initial
    // UNAVAILABLE state plus the @VisibleForTesting transition methods driven
    // by lifecycle events below.
    //
    // VisibleForTesting: the GENERATION_CHANGE trigger fires only when
    // generation > 1, a state unreachable through the public bridge API
    // without a prior onFailure (which T5/T6 wire). Tests drive the state
    // machine directly to set up that condition.
    @VisibleForTesting
    internal val runtimeState: RuntimeStateMachine = RuntimeStateMachine()

    // Testability seam for emit(): JVM unit tests cannot touch the Android
    // main Handler (Looper.getMainLooper() returns null in the stub jar),
    // so the default mainHandler.post path NPEs. When non-null, emit() calls
    // this override directly with the JSON string — letting tests capture
    // synthesized envelopes without a real EventSink. Production leaves this
    // null and uses the mainHandler path.
    @VisibleForTesting
    internal var emitOverride: ((String) -> Unit)? = null

    // Testability seam for [sendToUnity]: when non-null, replaces the reflective
    // `UnitySendMessage` invocation. JVM unit tests cannot load the real Unity
    // player class, so they swap this to drive the failure routing in a
    // controlled way (`{ throw RuntimeException(...) }`) or to simulate the
    // happy path (`{ /* no-op */ }`). Production leaves this null.
    @VisibleForTesting
    internal var invokeUnitySend: ((String) -> Unit)? = null

    // Testability seam for [returnToFlutterActivity]: when non-null, replaces
    // the `activity.startActivity(intent)` call. Same rationale as
    // [invokeUnitySend] — JVM tests cannot meaningfully drive the stubbed
    // android.jar `startActivity`, so they swap this to inject failures.
    @VisibleForTesting
    internal var invokeReturnToFlutter: (() -> Unit)? = null

    // Memory regression counter (v2 § Warm-resident). Tracks the number of
    // live Unity Activity instances observed via ActivityLifecycleCallbacks.
    // In the warm-resident policy the Activity is NOT finished on session end,
    // so this counter MUST stay at 0 or 1 across arbitrary session cycles.
    // A value > 1 indicates Activity accumulation (memory leak).
    //
    // @VisibleForTesting: production code does not read this; tests assert on
    // it to verify no accumulation across 10 sequential session cycles. It is
    // incremented in onActivityCreated and decremented in onActivityDestroyed
    // when the activity class matches CytoidNativeConfig.UNITY_GAMEPLAY_ACTIVITY.
    @VisibleForTesting
    internal var unityActivityInstanceCount: Int = 0
        private set

    // Tracks one-shot Application callback registration so attachActivity can
    // be called multiple times (config changes) without double-registering.
    private var lifecycleRegistered = false

    private val unityActivityLifecycleCallbacks =
        object : Application.ActivityLifecycleCallbacks {
            override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
                if (isUnityGameplayActivity(activity)) {
                    exclusiveUnityActivity = activity
                    unityActivityInstanceCount++
                    // Readiness is backed only by engine.ready / game.ready in
                    // onUnityMessage — NOT by Activity creation. Setting READY
                    // here lets waitForReady resolve before Unity's C# runtime
                    // has booted (P0 contract violation).
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
                    unityActivityInstanceCount--
                    // SURFACE_LOST trigger (v2 § Active-Session Runtime Failure):
                    // capture activeSessionId BEFORE any state cleanup so the
                    // primitive's idempotency gate sees the live value.
                    val activeSession = runtimeState.activeSessionId
                    if (activeSession != null) {
                        synthesizeRuntimeFailure(
                            RuntimeFailureTrigger.SURFACE_LOST,
                            activeSession,
                        )
                    } else {
                        // Surface destroyed without an active session: the
                        // runtime needs a full restart. Reset to UNAVAILABLE so
                        // the host knows to call startRuntime() again before
                        // launching a new session.
                        runtimeState.reset()
                    }
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
        // Missing Unity artifacts is NOT an error: the bridge falls back to
        // the mock runtime (docs/mock-engine.md, AGENTS.md). Fail-fast belongs
        // only on the real-Unity paths that actually need the artifacts.
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

        // GENERATION_CHANGE must run BEFORE the engine.ready envelope is
        // forwarded: if the engine recreated with an active session, the spec
        // requires the prior session's `runtime_recreated` session.result to
        // reach the host before the next engine.ready (v2 § Active-Session
        // Runtime Failure ordering).
        if (isEngineReadyMessage(jsonString) || isHostReadyMessage(jsonString)) {
            val wasActiveSession = runtimeState.activeSessionId
            runtimeState.onEngineReady()
            Log.i(TAG, "[CYTOID-DBG] <- Unity: ready received — state=${runtimeState.state}")
            if (wasActiveSession != null && runtimeState.generation > 1) {
                synthesizeRuntimeFailure(
                    RuntimeFailureTrigger.GENERATION_CHANGE,
                    wasActiveSession,
                )
            }
        }

        emit(jsonString)

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

    @VisibleForTesting
    internal fun returnToFlutterActivity() {
        try {
            val invoker = invokeReturnToFlutter
            if (invoker != null) {
                invoker()
            } else {
                val intent =
                    Intent(activity, activity.javaClass)
                        .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
                        .addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP)
                activity.startActivity(intent)
            }
        } catch (error: Throwable) {
            reportNativeSendFailure(error)
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

    /**
     * Synthesize a v2 `session.result` envelope with
     * `outcome.kind = "runtimeFailed"` for an active session killed by a
     * runtime-side event the engine itself cannot report (v2 § Active-Session
     * Runtime Failure).
     *
     * Contract:
     *  - Idempotent: gated on `activeSessionId == sessionId`. If the session
     *    already terminated (activeSessionId is null or a different id), this
     *    is a no-op and returns null. At most one synthesized result per session.
     *  - On success: transitions runtimeState to FAILED via onFailure (which
     *    clears activeSessionId), emits the envelope via [emit], returns the
     *    JSON string.
     *  - Active-session failures use `session.result`, NEVER `engine.error`.
     *
     * Returns the emitted JSON envelope string, or null if the gate suppressed
     * the synthesis (idempotency).
     */
    @VisibleForTesting
    internal fun synthesizeRuntimeFailure(
        trigger: RuntimeFailureTrigger,
        sessionId: String,
    ): String? {
        val currentSessionId = runtimeState.activeSessionId ?: return null
        if (currentSessionId != sessionId) return null

        val error = GameCoreError(
            code = trigger.errorCode,
            message = trigger.defaultMessage,
        )
        val envelope = JSONObject()
            .put("v", PROTOCOL_VERSION_V2)
            .put("id", sessionId)
            .put("type", "session.result")
            .put(
                "payload",
                JSONObject()
                    .put("sessionId", sessionId)
                    .put("outcome", JSONObject().put("kind", "runtimeFailed"))
                    .put("error", JSONObject(error.toMap())),
            )
            .toString()

        // onFailure clears activeSessionId AFTER transitioning to FAILED;
        // we already captured it above, so order is safe. This is the
        // idempotency seam: a second call sees activeSessionId == null.
        runtimeState.onFailure(error)
        emit(envelope)
        return envelope
    }

    @VisibleForTesting
    internal fun sendToUnity(jsonString: String) {
        try {
            val invoker = invokeUnitySend
            if (invoker != null) {
                invoker(jsonString)
            } else {
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
            }
        } catch (error: Throwable) {
            reportNativeSendFailure(error)
        }
    }

    /**
     * Route a native-side send failure (UnitySendMessage, startActivity back to
     * Flutter) to the v2 envelope the spec requires, based on whether a session
     * is currently active. The active-session routing rule is mandatory (v2 §
     * Active-Session Runtime Failure): `engine.error` is not used for active
     * sessions; the synthesized `session.result` carries the terminal outcome.
     *
     * Contract:
     *  - `activeSessionId != null` → ONLY `synthesizeRuntimeFailure(UNREACHABLE, …)`
     *    (T4 primitive emits `session.result`, NEVER `engine.error`).
     *  - `activeSessionId == null` → ONLY an `engine.error` envelope via [emit]
     *    with `error.code = "runtime_exception"` and a sanitized message.
     *  - Never both. Never rethrows — caller ([sendToUnity], [returnToFlutterActivity])
     *    keeps its original "returns normally" contract so `send()` on the Dart
     *    side resolves even when Unity-side dispatch throws.
     *  - Never leaks raw stack traces. The sanitized message is
     *    `"<ExceptionClassSimpleName>: <first line of message>"`.
     */
    private fun reportNativeSendFailure(error: Throwable) {
        val activeSessionId = runtimeState.activeSessionId
        if (activeSessionId != null) {
            synthesizeRuntimeFailure(
                RuntimeFailureTrigger.UNREACHABLE,
                activeSessionId,
            )
            return
        }
        val sanitized = sanitizeExceptionMessage(error)
        val envelope = JSONObject()
            .put("v", PROTOCOL_VERSION_V2)
            .put("id", NATIVE_BRIDGE_ERROR_ID)
            .put("type", "engine.error")
            .put(
                "payload",
                JSONObject().put(
                    "error",
                    JSONObject()
                        .put("code", "runtime_exception")
                        .put("message", sanitized),
                ),
            )
            .toString()
        emit(envelope)
    }

    private fun sanitizeExceptionMessage(error: Throwable): String {
        val simpleName = error.javaClass.simpleName.ifEmpty { "Throwable" }
        val firstLine = (error.message ?: "").substringBefore('\n').trim()
        return "$simpleName: $firstLine"
    }

    private fun emit(json: String) {
        if (isSessionResultMessage(jsonString = json) ||
            isGameResultMessage(json) ||
            isSessionEndedMessage(json)
        ) {
            runtimeState.onSessionEnded()
        }
        val override = emitOverride
        if (override != null) {
            override(json)
        } else {
            mainHandler.post {
                eventSink?.success(json)
            }
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
        private const val PROTOCOL_VERSION_V2 = 2
        private const val NATIVE_BRIDGE_ERROR_ID = "native-bridge"

        @Volatile
        var instance: CytoidGameCoreBridge? = null
            private set

        fun getOrCreate(activity: Activity): CytoidGameCoreBridge {
            return instance ?: CytoidGameCoreBridge(activity)
        }
    }
}

