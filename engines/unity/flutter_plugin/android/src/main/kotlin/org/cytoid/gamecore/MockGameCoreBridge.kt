package org.cytoid.gamecore

import android.os.Handler
import android.os.Looper
import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.util.UUID

class MockGameCoreBridge(
    private val emit: (String) -> Unit,
    private val mainHandler: Handler = Handler(Looper.getMainLooper()),
) {
    private val runtimeState = RuntimeStateMachine()

    fun ensureRuntimeStarted() {
        runtimeState.onRequestStart()
        if (runtimeState.state != RuntimeState.STARTING && runtimeState.state != RuntimeState.READY) return
        if (runtimeState.state == RuntimeState.STARTING) mainHandler.postDelayed({ emitHostReady() }, HOST_READY_DELAY_MS)
    }

    fun showGameSurface() {
        if (runtimeState.state == RuntimeState.SUSPENDED) runtimeState.onResume()
        ensureRuntimeStarted()
    }

    fun hideGameSurface() = runtimeState.onSuspend()

    fun onOutboundMessage(jsonString: String) {
        try {
            val envelope = JSONObject(jsonString)
            when (envelope.getString("type")) {
                "session.start" -> handleSessionStart(envelope)
                "session.cancel" -> handleSessionCancel(envelope)
                "health.check" -> handleHealthCheck(envelope)
                "settings.apply" -> handleSettingsApply(envelope)
                else -> Log.w(TAG, "Unhandled outbound type: ${envelope.getString("type")}")
            }
        } catch (error: Exception) {
            Log.e(TAG, "Failed to parse outbound envelope", error)
        }
    }

    private fun handleSessionStart(envelope: JSONObject) {
        val sessionId = envelope.getString("id")
        val launch = envelope.optJSONObject("payload")
        val mode = launch?.optString("mode").orEmpty()
        val mods = launch?.optJSONArray("mods") ?: JSONArray()

        if (!isValidLaunch(launch, mode)) {
            val rejectedEnvelope = envelope(sessionId, "session.result", rejectedResult(sessionId, mode, mods)).toString()
            emit(rejectedEnvelope)
            return
        }

        val validLaunch = launch ?: return
        runtimeState.onSessionStarted(sessionId)
        emitEnvelope(envelope(sessionId, "session.started", sessionStarted(sessionId, mode)).toString())
        emitSampleLogs(sessionId)
        scheduleResult(sessionId, defaultResult(sessionId, mode, mods, validLaunch))
    }

    private fun isValidLaunch(launch: JSONObject?, mode: String): Boolean =
        launch != null && mode in SUPPORTED_MODES && launch.optJSONArray("mods") != null &&
            launch.optJSONObject("options") != null && (mode != "tier" || launch.optJSONObject("tier") != null)

    private fun sessionStarted(sessionId: String, mode: String): JSONObject =
        obj("sessionId" to sessionId, "mode" to mode, "generation" to runtimeState.generation)

    private fun scheduleResult(sessionId: String, payload: JSONObject) {
        mainHandler.postDelayed({
            if (runtimeState.activeSessionId != sessionId) return@postDelayed
            runtimeState.onSessionEnded()
            emitEnvelope(envelope(sessionId, "session.result", payload).toString())
        }, MOCK_GAME_RESULT_DELAY_MS)
    }

    private fun defaultResult(
        sessionId: String,
        mode: String,
        mods: JSONArray,
        launch: JSONObject,
    ): JSONObject {
        val payload = baseResult(sessionId, mode, mods).put("outcome", obj("kind" to defaultOutcome(mode)))
        when (mode) {
            "ranked", "practice" -> payload.put("level", levelResult(launch)).put("score", scoreResult())
            "tier" -> payload
                .put("level", levelResult(launch))
                .put("score", scoreResult())
                .put("tier", tierResult(launch.getJSONObject("tier")))
            "calibration", "globalCalibration" -> payload.put(
                "calibration",
                obj("baseNoteOffset" to 0.0, "levelNoteOffset" to 0.0),
            )
        }
        return payload
    }

    private fun defaultOutcome(mode: String): String = if (mode == "calibration" || mode == "globalCalibration") "calibration" else "completed"

    private fun baseResult(
        sessionId: String,
        mode: String,
        mods: JSONArray,
    ): JSONObject =
        obj(
            "sessionId" to sessionId,
            "mode" to mode.ifEmpty { "ranked" },
            "mods" to copyArray(mods),
            "flags" to obj("usedAutoMod" to containsAutoClassMod(mods)),
            // The smoke mock records no play events and emits no session.telemetry envelope.
            "telemetry" to obj("available" to false, "eventsRecorded" to 0, "bytes" to 0),
            "timestamp" to System.currentTimeMillis(),
        )

    private fun levelResult(launch: JSONObject): JSONObject {
        val level = launch.optJSONObject("level") ?: return obj("id" to "mock-level")
        val meta = level.optJSONObject("meta")
        val selected = level.optString("selectedDifficulty", "hard")
        val chart = selectedChart(meta, selected)
        return obj(
            "id" to (meta?.optString("id", "mock-level") ?: "mock-level"),
            "title" to (meta?.optString("title", "Mock Level") ?: "Mock Level"),
            "difficulty" to selected,
            "difficultyLevel" to (chart?.optInt("difficulty", 1) ?: 1),
        )
    }

    private fun selectedChart(meta: JSONObject?, selected: String): JSONObject? {
        val charts = meta?.optJSONArray("charts") ?: return null
        for (index in 0 until charts.length()) {
            val chart = charts.optJSONObject(index) ?: continue
            if (chart.optString("type") == selected) return chart
        }
        return null
    }

    private fun scoreResult(): JSONObject =
        obj(
            "score" to 950000,
            "accuracy" to 0.97,
            "maxCombo" to 50,
            "gradeCounts" to obj("perfect" to 900, "great" to 80, "good" to 15, "bad" to 3, "miss" to 2),
            "early" to 4,
            "late" to 6,
            "averageTimingError" to 0.0,
            "standardTimingError" to 12.5,
        )

    private fun tierResult(tier: JSONObject): JSONObject {
        val maxHealth = tier.optDouble("maxHealth", 1000.0)
        val health = tier.optDouble("health", tier.optDouble("initialHealth", maxHealth))
        val combo = tier.optInt("combo", tier.optInt("initialCombo", 0))
        return obj(
            "tierId" to tier.optString("tierId", "mock-tier"),
            "stageIndex" to tier.optInt("stageIndex", 0),
            "stageCount" to tier.optInt("stageCount", 1),
            "health" to (health * 0.85).coerceAtLeast(0.0),
            "maxHealth" to maxHealth,
            "combo" to combo + 50,
        )
    }

    private fun rejectedResult(
        sessionId: String,
        mode: String,
        mods: JSONArray,
    ): JSONObject =
        baseResult(sessionId, mode, mods)
            .put("outcome", obj("kind" to "rejected"))
            .put("error", obj("code" to "invalid_payload", "message" to "Mock session.start payload is structurally invalid"))

    private fun handleSessionCancel(envelope: JSONObject) {
        val sessionId = envelope.getString("id")
        val reason = envelope.optJSONObject("payload")?.optString("reason", "unknown") ?: "unknown"
        runtimeState.onSessionEnded()
        emitEnvelope(
            envelope(
                sessionId,
                "session.result",
                baseResult(sessionId, "ranked", JSONArray()).put("outcome", obj("kind" to "cancelled", "reason" to reason)),
            ).toString(),
        )
    }

    private fun handleHealthCheck(envelope: JSONObject) {
        val payload = obj("engine" to "mock", "generation" to runtimeState.generation, "state" to runtimeState.state.wireName)
        runtimeState.activeSessionId?.let { payload.put("activeSessionId", it) }
        emitEnvelope(envelope(envelope.getString("id"), "health.ok", payload).toString())
    }

    private fun handleSettingsApply(envelope: JSONObject) {
        val appliedFields = JSONArray()
        (envelope.optJSONObject("payload") ?: JSONObject()).keys().forEach { appliedFields.put(it) }
        emitEnvelope(
            envelope(
                envelope.getString("id"),
                "settings.applied",
                obj(
                    "applied" to true,
                    "appliedFields" to appliedFields,
                    "deferredFields" to JSONArray(),
                    "rejectedFields" to JSONArray(),
                    "errors" to JSONArray(),
                ),
            ).toString(),
        )
    }

    private fun emitSampleLogs(sessionId: String) {
        val samples = listOf(
            Triple("log", "Mock runtime accepted session", null),
            Triple("warning", "Mock Unity: storyboard texture cache miss", null),
            Triple("error", "Mock Unity: Unity artifact not mounted", "MockGameCoreBridge.kt:handleSessionStart"),
        )
        mainHandler.postDelayed({
            val logs = JSONArray()
            samples.forEach { (level, message, stackTrace) ->
                val entry = obj("level" to logLevel(level), "message" to message, "timestamp" to System.currentTimeMillis(), "sessionId" to sessionId)
                stackTrace?.let { entry.put("stackTrace", it) }
                logs.put(entry)
            }
            emitEnvelope(
                envelope(
                    UUID.randomUUID().toString(),
                    "logs.batch",
                    obj("reason" to "trigger", "triggerLevel" to "error", "timestamp" to System.currentTimeMillis(), "truncated" to false, "logs" to logs),
                ).toString(),
            )
        }, MOCK_LOG_DELAY_MS)
    }

    private fun logLevel(level: String): String =
        when (level) {
            "log" -> "debug"
            "warning" -> "warning"
            "error", "exception" -> "error"
            else -> level
        }

    private fun emitHostReady() {
        runtimeState.onEngineReady()
        emitEnvelope(
            envelope(
                UUID.randomUUID().toString(),
                "engine.ready",
                obj(
                    "engine" to "mock",
                    "engineVersion" to "cytoid_game_core",
                    "generation" to runtimeState.generation,
                    "display" to obj("targetFrameRate" to 60, "screenRefreshRate" to 60),
                ),
            ).toString(),
        )
    }

    private fun envelope(id: String, type: String, payload: JSONObject): JSONObject = obj("schema" to PROTOCOL_SCHEMA, "id" to id, "type" to type, "payload" to payload)

    private fun containsAutoClassMod(mods: JSONArray): Boolean {
        for (index in 0 until mods.length()) if (mods.optString(index) in AUTO_CLASS_MODS) return true
        return false
    }

    private fun copyArray(source: JSONArray): JSONArray {
        val copy = JSONArray()
        for (index in 0 until source.length()) copy.put(source.opt(index))
        return copy
    }

    private fun obj(vararg pairs: Pair<String, Any?>): JSONObject = JSONObject().apply { pairs.forEach { put(it.first, it.second) } }

    private fun emitEnvelope(json: String) = emit(json)

    companion object {
        private const val TAG = "MockGameCoreBridge"
        private const val PROTOCOL_SCHEMA = "cytoid.game-core.v2"
        private const val HOST_READY_DELAY_MS = 150L
        private const val MOCK_GAME_RESULT_DELAY_MS = 900L
        private const val MOCK_LOG_DELAY_MS = 600L
        private val SUPPORTED_MODES = setOf("ranked", "practice", "calibration", "globalCalibration", "tier")
        private val AUTO_CLASS_MODS = setOf("auto", "autoDrag", "autoHold", "autoFlick")
    }
}
