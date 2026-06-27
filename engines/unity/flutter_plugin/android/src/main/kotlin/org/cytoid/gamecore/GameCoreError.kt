package org.cytoid.gamecore

/**
 * Structured error shape used by `engine.error` and the runtime snapshot's
 * conditional `error` field (v2 § ErrorPayload).
 *
 * Mirrors `ErrorPayload` in the wire spec; named `GameCoreError` on the native
 * side so the bridge surface reads as an error type rather than a payload.
 */
data class GameCoreError(
    val code: String,
    val message: String,
    val details: Map<String, Any?>? = null,
) {
    /** Wire form for the v2 envelope and runtime snapshot. */
    fun toMap(): Map<String, Any?> = buildMap {
        put("code", code)
        put("message", message)
        if (details != null) put("details", details)
    }

    companion object {
        /**
         * Parse from a v2 wire object, or null if the shape is invalid.
         *
         * `details`, when present, MUST be a Map. A present-but-non-Map
         * `details` is treated as malformed (returns null) rather than
         * silently dropped — the spec (`docs/host-protocol-v2.md:1336-1340`)
         * defines `details` as an object.
         */
        fun fromMap(value: Any?): GameCoreError? {
            if (value !is Map<*, *>) return null
            val code = value["code"] as? String ?: return null
            val message = value["message"] as? String ?: return null
            val detailsRaw = value["details"]
            if (detailsRaw != null && detailsRaw !is Map<*, *>) return null
            @Suppress("UNCHECKED_CAST")
            val details = detailsRaw as? Map<String, Any?>
            return GameCoreError(code = code, message = message, details = details)
        }
    }
}
