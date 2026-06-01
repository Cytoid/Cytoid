package org.cytoid.gamecore

import android.os.Handler
import android.os.Looper
import android.util.Log
import org.json.JSONObject
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.TimeZone
import java.util.UUID

class MockGameCoreBridge(
    private val emit: (String) -> Unit,
    private val mainHandler: Handler = Handler(Looper.getMainLooper()),
) {
    private var runtimeStarted = false
    private var surfaceVisible = false
    private var activePlayId: String? = null

    fun ensureRuntimeStarted() {
        if (runtimeStarted) return
        runtimeStarted = true
        mainHandler.postDelayed({ emitHostReady() }, HOST_READY_DELAY_MS)
    }

    fun showGameSurface() {
        ensureRuntimeStarted()
        surfaceVisible = true
    }

    fun hideGameSurface() {
        surfaceVisible = false
    }

    fun onOutboundMessage(jsonString: String) {
        try {
            val envelope = JSONObject(jsonString)
            when (envelope.getString("type")) {
                "bridge.status" -> handleStatus(envelope)
                "bridge.ping" -> handlePing(envelope)
                "bridge.play.start" -> handleGameStart(envelope)
                "bridge.settings.update" -> handleSettingsUpdate(envelope)
                "bridge.play.end" -> handleSessionEnd(envelope)
                else -> Log.w(TAG, "Unhandled outbound type: ${envelope.getString("type")}")
            }
        } catch (error: Exception) {
            Log.e(TAG, "Failed to parse outbound envelope", error)
        }
    }

    private fun handlePing(envelope: JSONObject) {
        val payload = envelope.optJSONObject("payload") ?: JSONObject()
        val pong =
            JSONObject()
                .put("v", PROTOCOL_VERSION)
                .put("id", envelope.getString("id"))
                .put("type", "game.pong")
                .put("payload", payload)
        emitEnvelope(pong.toString())
    }

    private fun handleGameStart(envelope: JSONObject) {
        val playId = envelope.getString("id")
        activePlayId = playId
        val launchPayload = envelope.optJSONObject("payload") ?: JSONObject()
        val gameMode = launchPayload.optString("gameMode", "")
        val tierPlay = launchPayload.optJSONObject("tierPlay")

        emitSampleGameLogs(playId)

        val resultPayload =
            if (gameMode.equals("Tier", ignoreCase = true) && tierPlay != null) {
                buildMockTierResult(launchPayload, tierPlay)
            } else {
                JSONObject()
                    .put("completed", false)
                    .put("failed", true)
                    .put("usedAutoMod", false)
                    .put("error", "Unity artifact not mounted")
                    .put("timestamp", utcTimestamp())
            }

        val result =
            JSONObject()
                .put("v", PROTOCOL_VERSION)
                .put("id", envelope.getString("id"))
                .put("type", "game.play.result")
                .put("payload", resultPayload)

        mainHandler.postDelayed({
            activePlayId = null
            emitEnvelope(result.toString())
        }, MOCK_GAME_RESULT_DELAY_MS)
    }

    private fun buildMockTierResult(
        launchPayload: JSONObject,
        tierPlay: JSONObject,
    ): JSONObject {
        val maxHealth = tierPlay.optDouble("maxHealth", 1000.0)
        val initialHealth = tierPlay.optDouble("initialHealth", maxHealth)
        val initialCombo = tierPlay.optInt("initialCombo", 0)
        val finalHealth = (initialHealth * 0.85).coerceAtLeast(0.0)
        val endingCombo = initialCombo + 50

        val tierResult =
            JSONObject()
                .put("tierId", tierPlay.opt("tierId") ?: JSONObject.NULL)
                .put("stageIndex", tierPlay.optInt("stageIndex", 0))
                .put("finalHealth", finalHealth)
                .put("maxHealth", maxHealth)
                .put("endingCombo", endingCombo)

        return JSONObject()
            .put("completed", true)
            .put("failed", false)
            .put("usedAutoMod", false)
            .put("gameMode", "Tier")
            .put("timestamp", utcTimestamp())
            .put("levelId", "mock-level")
            .put("score", 950000)
            .put("accuracy", 0.97)
            .put("maxCombo", endingCombo)
            .put("tierPlay", tierResult)
    }

    private fun handleSessionEnd(envelope: JSONObject) {
        Log.i(TAG, "bridge.play.end received")
        activePlayId = null
        val ended =
            JSONObject()
                .put("v", PROTOCOL_VERSION)
                .put("id", envelope.getString("id"))
                .put("type", "game.play.ended")
                .put("payload", JSONObject().put("ended", true))
        emitEnvelope(ended.toString())
    }

    private fun handleSettingsUpdate(envelope: JSONObject) {
        val updated =
            JSONObject()
                .put("v", PROTOCOL_VERSION)
                .put("id", envelope.getString("id"))
                .put("type", "game.settings.updated")
                .put("payload", JSONObject().put("applied", true))
        emitEnvelope(updated.toString())
    }

    private fun handleStatus(envelope: JSONObject) {
        val state =
            when {
                activePlayId != null -> "busy"
                runtimeStarted || surfaceVisible -> "ready"
                else -> "unavailable"
            }
        val payload =
            JSONObject()
                .put("state", state)
                .put("engine", "mock")
        activePlayId?.let { payload.put("activePlayId", it) }
        val status =
            JSONObject()
                .put("v", PROTOCOL_VERSION)
                .put("id", envelope.getString("id"))
                .put("type", "game.status")
                .put("payload", payload)
        emitEnvelope(status.toString())
    }

    private fun emitSampleGameLogs(playId: String) {
        val samples =
            listOf(
                Triple("log", "Mock game runtime started play", null),
                Triple("warning", "Mock Unity: storyboard texture cache miss", null),
                Triple("error", "Mock Unity: Unity artifact not mounted", "MockGameCoreBridge.kt:handleGameStart"),
            )

        mainHandler.postDelayed({
            val logs = org.json.JSONArray()
            samples.forEach { sample ->
                val payload =
                    JSONObject()
                        .put("level", sample.first)
                        .put("message", sample.second)
                        .put("timestamp", utcTimestamp())
                        .put("playId", playId)
                sample.third?.let { payload.put("stackTrace", it) }
                logs.put(payload)
            }

            val batchPayload =
                JSONObject()
                    .put("reason", "trigger")
                    .put("triggerLevel", "error")
                    .put("timestamp", utcTimestamp())
                    .put("truncated", false)
                    .put("logs", logs)
            val batchEnvelope =
                JSONObject()
                    .put("v", PROTOCOL_VERSION)
                    .put("id", UUID.randomUUID().toString())
                    .put("type", "game.logs.batch")
                    .put("payload", batchPayload)
            emitEnvelope(batchEnvelope.toString())
        }, 600L)
    }

    private fun emitHostReady() {
        val ready =
            JSONObject()
                .put("v", PROTOCOL_VERSION)
                .put("id", UUID.randomUUID().toString())
                .put("type", "game.ready")
                .put(
                    "payload",
                    JSONObject()
                        .put("initialized", true)
                        .put("engine", "mock")
                        .put("engineVersion", "mock"),
                )
        emitEnvelope(ready.toString())
    }

    private fun emitEnvelope(json: String) {
        emit(json)
    }

    private fun utcTimestamp(): String {
        val formatter = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US)
        formatter.timeZone = TimeZone.getTimeZone("UTC")
        return formatter.format(Date())
    }

    companion object {
        private const val TAG = "MockGameCoreBridge"
        private const val PROTOCOL_VERSION = 1
        private const val HOST_READY_DELAY_MS = 150L
        private const val MOCK_GAME_RESULT_DELAY_MS = 900L
    }
}
