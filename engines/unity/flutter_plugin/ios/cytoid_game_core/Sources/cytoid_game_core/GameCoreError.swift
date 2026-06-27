import Foundation

/// Structured error shape used by `engine.error` and the runtime snapshot's
/// conditional `error` field (v2 § ErrorPayload).
///
/// Mirrors `ErrorPayload` in the wire spec; named `GameCoreError` on the native
/// side so the bridge surface reads as an error type rather than a payload.
public struct GameCoreError: Equatable {
    public let code: String
    public let message: String
    public let details: [String: Any]?

    public init(code: String, message: String, details: [String: Any]? = nil) {
        self.code = code
        self.message = message
        self.details = details
    }

    /// Wire form for the v2 envelope and runtime snapshot.
    public func toMap() -> [String: Any] {
        var map: [String: Any] = [
            "code": code,
            "message": message,
        ]
        if let details {
            map["details"] = details
        }
        return map
    }

    /// Parse from a v2 wire object, or nil if the shape is invalid.
    ///
    /// `details`, when present, MUST be a dictionary. A present-but-non-dict
    /// `details` is treated as malformed (returns nil) rather than silently
    /// dropped — the spec (`docs/host-protocol-v2.md:1336-1340`) defines
    /// `details` as an object.
    public static func from(map value: Any?) -> GameCoreError? {
        guard let dict = value as? [String: Any] else { return nil }
        guard let code = dict["code"] as? String else { return nil }
        guard let message = dict["message"] as? String else { return nil }
        if let detailsValue = dict["details"], !(detailsValue is [String: Any]) {
            return nil
        }
        let details = dict["details"] as? [String: Any]
        return GameCoreError(code: code, message: message, details: details)
    }

    public static func == (lhs: GameCoreError, rhs: GameCoreError) -> Bool {
        lhs.code == rhs.code && lhs.message == rhs.message
    }
}
