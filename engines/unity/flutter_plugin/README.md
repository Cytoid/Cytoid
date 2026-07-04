# cytoid_game_core

Flutter plugin for Cytoid's engine-agnostic game core host protocol.

The public Dart API intentionally avoids Unity-specific concepts. Flutter code talks to
`CytoidGameCoreClient`, sends JSON envelopes, and shows a fullscreen game surface from a
long-lived runtime.
The native implementation currently hosts Unity and can later be swapped for Godot
without changing Flutter UI code.

## API

```dart
import 'package:cytoid_game_core/cytoid_game_core.dart';

final client = CytoidGameCoreClient();
final session = PlaySession(client);
final result = await session.run(
  launch: launchPayload, // SessionLaunchPayload
);
```

`PlaySession.run` owns the full v2 lifecycle: it ensures the runtime is
started, shows the game surface, waits for `engine.ready`, sends the typed
`session.start` envelope, awaits the terminal `session.result` /
`session.failed`, and ALWAYS hides the surface (in a `finally` block, even
when an earlier step throws). Lower-level primitives
(`ensureRuntimeStarted`, `showGameSurface`, `hideGameSurface`, `send`,
`waitForReady`) remain available on `CytoidGameCoreClient` for callers that
need to deviate from that sequence.

Channels used by the plugin:

- `MethodChannel('cytoid/game_core')`
- `EventChannel('cytoid/game_core/events')`

## Unity artifacts

The plugin can compile without Unity artifacts and will use the mock engine. Install
runtime artifacts when testing the real Unity core:

```sh
cd engines/unity/flutter_plugin
export CYTOID_GAME_CORE_ARTIFACT_BASE_URL=https://example.com/cytoid-game-core
export CYTOID_GAME_CORE_ARTIFACT_VERSION=0.0.1
./tool/setup_unity_artifacts.sh
```

Expected artifact layout after setup:

```text
.cytoid_game_core/artifacts/unity/android/cytoid-unity-core.aar
.cytoid_game_core/artifacts/unity/android/*.aar
.cytoid_game_core/artifacts/unity/ios/UnityFramework.xcframework
```

Local artifacts are produced by Unity **Build Android/iOS Plugin Artifacts**
(menu or `CytoidCoreBuild.Export*LibraryForFlutter`), which runs `./tool/build_unity_aar.sh`
or `./tool/build_unity_ios_framework.sh` after export. You can also run those scripts
standalone to re-package an existing export. iOS packaging requires macOS + Xcode.
The example iOS app is SPM-only and does not use CocoaPods.

The Unity iOS export currently produces a device-only framework. With the
Unity artifact mounted, build and run the example on an iOS device; simulator
builds require a simulator slice or no Unity artifact so the mock runtime is used.

### Running the Swift test suite locally

The iOS plugin's pure-logic Swift tests (`RuntimeStateTests`,
`RuntimeFailureSynthesisTests`, `EnsureRuntimeStartedContractTests`,
`MessageQueueTimeoutTests`, `RejectedSessionResultTransitionTests`,
`BridgeSchemaValidationTests`, `WaitForReadyTests`) run without the
`FlutterFramework` SPM package or the `UnityFramework` binary target via an
isolated SwiftPM sandbox:

```sh
cd engines/unity/flutter_plugin
./tool/run_swift_tests_sandboxed.sh           # run the suite
./tool/run_swift_tests_sandboxed.sh --clean   # wipe sandbox first
./tool/run_swift_tests_sandboxed.sh --list-tests
```

The harness symlinks production sources into a sandbox package, generates a
minimal stub `Flutter` module, and writes a stripped `Package.swift`. Tests
that assert on real Flutter semantics (`ShowGameSurfaceAtomicityTests`) and
sources that use UIKit (`CytoidGameCorePlugin.swift`) are excluded — these
require an Xcode build with the full Flutter + UnityFramework dependencies.

## Example

```sh
cd engines/unity/flutter_plugin/example
flutter pub get
flutter run
```

Without artifacts, the example uses the mock runtime. With artifacts, the same
`/game` route shows the Unity gameplay surface.
