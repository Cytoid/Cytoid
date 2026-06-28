package org.cytoid.gamecore

import android.os.Handler
import io.mockk.every
import io.mockk.mockk
import org.json.JSONObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class MockGameCoreBridgeTest {

    @Test
    fun `session start emits started before result and every envelope carries schema`() {
        val captured = mutableListOf<String>()
        val mock = MockGameCoreBridge(captured::add, immediateHandler())

        mock.ensureRuntimeStarted()
        mock.onOutboundMessage(sessionStartEnvelope(sessionId = "S1", mods = emptyList()))

        val envelopes = captured.map(::JSONObject)
        assertTrue(envelopes.isNotEmpty())
        envelopes.forEach { envelope ->
            assertEquals("cytoid.game-core.v2", envelope.getString("schema"))
        }

        val startedIndex = envelopes.indexOfFirst { it.getString("type") == "session.started" }
        val resultIndex = envelopes.indexOfFirst { it.getString("type") == "session.result" }
        assertTrue("session.started must be emitted", startedIndex >= 0)
        assertTrue("session.result must be emitted", resultIndex >= 0)
        assertTrue("session.started must precede session.result", startedIndex < resultIndex)

        val startedPayload = envelopes[startedIndex].getJSONObject("payload")
        assertEquals("S1", startedPayload.getString("sessionId"))
        assertEquals("ranked", startedPayload.getString("mode"))

        val resultPayload = envelopes[resultIndex].getJSONObject("payload")
        assertEquals("S1", resultPayload.getString("sessionId"))
        assertEquals("completed", resultPayload.getJSONObject("outcome").getString("kind"))
        assertFalse(resultPayload.getJSONObject("telemetry").getBoolean("available"))
    }

    @Test
    fun `auto class mods suppress telemetry and no telemetry envelope is emitted`() {
        val captured = mutableListOf<String>()
        val mock = MockGameCoreBridge(captured::add, immediateHandler())

        mock.ensureRuntimeStarted()
        mock.onOutboundMessage(sessionStartEnvelope(sessionId = "S2", mods = listOf("autoDrag")))

        val envelopes = captured.map(::JSONObject)
        assertFalse(envelopes.any { it.getString("type") == "session.telemetry" })


        val resultPayload = envelopes.first { it.getString("type") == "session.result" }.getJSONObject("payload")
        assertTrue(resultPayload.getJSONObject("flags").getBoolean("usedAutoMod"))
        val telemetry = resultPayload.getJSONObject("telemetry")
        assertFalse(telemetry.getBoolean("available"))
        assertEquals(0, telemetry.getInt("eventsRecorded"))
        assertEquals(0, telemetry.getInt("bytes"))
    }

    private fun immediateHandler(): Handler {
        val handler = mockk<Handler>()
        every { handler.postDelayed(any(), any()) } answers {
            firstArg<Runnable>().run()
            true
        }
        return handler
    }

    private fun sessionStartEnvelope(
        sessionId: String,
        mods: List<String>,
    ): String =
        JSONObject()
            .put("schema", "cytoid.game-core.v2")
            .put("id", sessionId)
            .put("type", "session.start")
            .put(
                "payload",
                JSONObject()
                    .put("mode", "ranked")
                    .put("mods", mods)
                    .put("level", mockLevel())
                    .put("settings", JSONObject())
                    .put("options", JSONObject().put("recordPlayEvents", true)),
            )
            .toString()

    private fun mockLevel(): JSONObject =
        JSONObject()
            .put(
                "meta",
                JSONObject()
                    .put("id", "example.level")
                    .put("title", "Example Level")
                    .put(
                        "charts",
                        listOf(
                            JSONObject()
                                .put("type", "hard")
                                .put("difficulty", 14),
                        ),
                    ),
            )
            .put("selectedDifficulty", "hard")
}
