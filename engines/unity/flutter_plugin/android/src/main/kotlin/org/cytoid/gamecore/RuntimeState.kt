package org.cytoid.gamecore

/**
 * Native runtime lifecycle state per v2 host protocol § "Native Runtime Contract".
 *
 * Order matters: it is the order tests assert against and the order the iOS
 * Swift enum mirrors byte-for-byte. Do not re-order without updating
 * `RuntimeState.swift`, `RuntimeStateTest.kt`, and `RuntimeStateTests.swift`.
 *
 * The wire form is the lower-case enum name (e.g. `READY → "ready"`).
 */
enum class RuntimeState {
    UNAVAILABLE,
    STARTING,
    READY,
    BUSY,
    SUSPENDED,
    FAILED;

    /** v2 wire form, e.g. `READY → "ready"`. */
    val wireName: String get() = name.lowercase()

    companion object {
        /** Parse the wire form back to the enum, or null if unknown. */
        fun fromWireName(value: String?): RuntimeState? {
            if (value == null) return null
            return entries.firstOrNull { it.wireName == value }
        }
    }
}
