import XCTest
@testable import cytoid_game_core

final class BridgeSchemaValidationTests: XCTestCase {

    func testOnUnityMessageIgnoresWrongSchemaEnvelope() {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }
        bridge.runtimeState.onRequestStart()

        bridge.onUnityMessage("{\"schema\":\"cytoid.game-core.v1\",\"id\":\"ready\",\"type\":\"engine.ready\",\"payload\":{}}")

        XCTAssertEqual(bridge.runtimeState.state, .starting)
        XCTAssertEqual(captured.values.count, 0)
    }

    func testOnOutboundMessageIgnoresWrongSchemaSessionStart() {
        let bridge = CytoidGameCoreBridge()
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()

        bridge.onOutboundMessage("{\"schema\":\"cytoid.game-core.v1\",\"id\":\"S1\",\"type\":\"session.start\",\"payload\":{}}")

        XCTAssertEqual(bridge.runtimeState.state, .ready)
        XCTAssertNil(bridge.runtimeState.activeSessionId)
    }

    func testSessionResultDrivesSingleBusyToReadyTransition() {
        let bridge = CytoidGameCoreBridge()
        let captured = CapturedEmits()
        bridge.emitOverride = { captured.append($0) }
        bridge.runtimeState.onRequestStart()
        bridge.runtimeState.onEngineReady()
        bridge.runtimeState.onSessionStarted(sessionId: "S1")

        bridge.onUnityMessage("{\"schema\":\"cytoid.game-core.v2\",\"id\":\"S1\",\"type\":\"session.result\",\"payload\":{}}")

        XCTAssertEqual(bridge.runtimeState.state, .ready)
        XCTAssertNil(bridge.runtimeState.activeSessionId)
        XCTAssertEqual(captured.values.count, 1)

        bridge.runtimeState.onSessionStarted(sessionId: "S2")
        bridge.onUnityMessage("{\"schema\":\"cytoid.game-core.v1\",\"id\":\"legacy\",\"type\":\"game.play.result\",\"payload\":{}}")

        XCTAssertEqual(bridge.runtimeState.state, .busy)
        XCTAssertEqual(bridge.runtimeState.activeSessionId, "S2")
        XCTAssertEqual(captured.values.count, 1)
    }

    private final class CapturedEmits {
        private(set) var values: [String] = []
        func append(_ value: String) { values.append(value) }
    }
}
