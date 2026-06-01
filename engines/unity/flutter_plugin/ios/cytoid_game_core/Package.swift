// swift-tools-version: 5.9

import Foundation
import PackageDescription

let packageRoot = URL(fileURLWithPath: Context.packageDirectory).resolvingSymlinksInPath()
let unityFrameworkPath = "Artifacts/UnityFramework.xcframework"
let buildEnvironment = ProcessInfo.processInfo.environment
let platformName = buildEnvironment["PLATFORM_NAME"] ?? ""
let sdkRoot = buildEnvironment["SDKROOT"] ?? ""
let isSimulatorBuild = platformName.contains("simulator") || sdkRoot.contains("iphonesimulator")
let hasUnityFramework = FileManager.default.fileExists(
    atPath: packageRoot.appendingPathComponent(unityFrameworkPath).path
) && !isSimulatorBuild

var dependencies: [Target.Dependency] = [
    .product(name: "FlutterFramework", package: "FlutterFramework")
]
var targets: [Target] = []
var swiftSettings: [SwiftSetting] = []

if hasUnityFramework {
    dependencies.append("UnityFramework")
    targets.append(.binaryTarget(name: "UnityFramework", path: unityFrameworkPath))
    swiftSettings.append(.define("CYTOID_UNITY_FRAMEWORK_AVAILABLE"))
}

targets.append(
    .target(
        name: "cytoid_game_core",
        dependencies: dependencies,
        swiftSettings: swiftSettings
    )
)

let package = Package(
    name: "cytoid_game_core",
    platforms: [
        .iOS("13.0")
    ],
    products: [
        .library(name: "cytoid-game-core", targets: ["cytoid_game_core"])
    ],
    dependencies: [
        .package(name: "FlutterFramework", path: "../FlutterFramework")
    ],
    targets: targets
)
