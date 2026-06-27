import Foundation

/// Native runtime lifecycle state per v2 host protocol § "Native Runtime Contract".
///
/// Order MUST match the Kotlin enum byte-for-byte — the cross-platform test
/// matrix (RuntimeStateTests.swift / RuntimeStateTest.kt) asserts the same
/// 6 states in the same order. Do not re-order without updating both files.
public enum RuntimeState: String, CaseIterable {
    case unavailable
    case starting
    case ready
    case busy
    case suspended
    case failed

    /// v2 wire form (already lowercased by the raw value).
    public var wireName: String { rawValue }

    /// Parse the wire form back to the enum, or nil if unknown.
    public static func from(wireName: String?) -> RuntimeState? {
        guard let wireName else { return nil }
        return RuntimeState(rawValue: wireName)
    }
}
