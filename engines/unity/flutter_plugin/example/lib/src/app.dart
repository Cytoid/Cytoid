import 'dart:async';

import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'game_routes.dart';
import 'models/example_level.dart';
import 'models/example_mods.dart';
import 'models/example_settings.dart';
import 'screens/game_session_screen.dart';
import 'screens/level_select_screen.dart';
import 'screens/mods_selection_screen.dart';
import 'screens/result_screen.dart';
import 'screens/settings_screen.dart';
import 'screens/tier_stage_debug_screen.dart';
import 'screens/unity_logs_screen.dart';
import 'unity_log_store.dart';

class CytoidExampleApp extends StatefulWidget {
  const CytoidExampleApp({super.key});

  @override
  State<CytoidExampleApp> createState() => _CytoidExampleAppState();
}

class _CytoidExampleAppState extends State<CytoidExampleApp> {
  ExampleSettings _settings = ExampleSettings.initial();
  ExampleMods _mods = const ExampleMods();
  final CytoidGameCoreClient _client = CytoidGameCoreClient();

  @override
  void initState() {
    super.initState();
    unawaited(_client.ensureRuntimeStarted().catchError((_) {}));
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Cytoid',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xff00a8a8),
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: const Color(0xff101415),
        useMaterial3: true,
      ),
      onGenerateRoute: _onGenerateRoute,
      home: _ExampleHome(
        client: _client,
        settings: _settings,
        mods: _mods,
        onSettingsChanged: (settings) {
          setState(() {
            _settings = settings;
          });
          unawaited(
            _client
                .updateSettings(settings.toLaunchSettings())
                .catchError((_) {}),
          );
        },
        onModsChanged: (mods) {
          setState(() {
            _mods = mods;
          });
        },
      ),
    );
  }

  Route<void>? _onGenerateRoute(RouteSettings routeSettings) {
    switch (routeSettings.name) {
      case ExampleRoutes.game:
        final args = routeSettings.arguments as GameRouteArgs;
        return PageRouteBuilder<void>(
          settings: routeSettings,
          transitionDuration: Duration.zero,
          reverseTransitionDuration: Duration.zero,
          pageBuilder: (_, _, _) => GameSessionScreen(args: args),
        );
      case ExampleRoutes.result:
        final args = routeSettings.arguments as ResultRouteArgs;
        return PageRouteBuilder<void>(
          settings: routeSettings,
          transitionDuration: const Duration(milliseconds: 180),
          reverseTransitionDuration: const Duration(milliseconds: 120),
          pageBuilder: (_, _, _) => ResultScreen(args: args),
          transitionsBuilder: (_, animation, _, child) {
            return FadeTransition(opacity: animation, child: child);
          },
        );
    }
    return null;
  }
}

class _ExampleHome extends StatefulWidget {
  const _ExampleHome({
    required this.client,
    required this.settings,
    required this.mods,
    required this.onSettingsChanged,
    required this.onModsChanged,
  });

  final CytoidGameCoreClient client;
  final ExampleSettings settings;
  final ExampleMods mods;
  final ValueChanged<ExampleSettings> onSettingsChanged;
  final ValueChanged<ExampleMods> onModsChanged;

  @override
  State<_ExampleHome> createState() => _ExampleHomeState();
}

class _ExampleHomeState extends State<_ExampleHome> {
  static const _backNavigationChannel = MethodChannel(
    'cytoid/example_back_navigation',
  );
  static const _exitConfirmationWindow = Duration(seconds: 2);

  final ExampleLevelRepository _repository = const ExampleLevelRepository();
  late final Future<List<ExampleLevel>> _levelsFuture = _repository
      .loadLevels();
  late final UnityLogStore _logStore = UnityLogStore();

  int _selectedIndex = 0;
  DateTime? _lastExitBackPressedAt;

  @override
  void initState() {
    super.initState();
    _logStore.bind(widget.client);
  }

  @override
  void dispose() {
    _logStore.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final pages = [
      LevelSelectScreen(
        levelsFuture: _levelsFuture,
        settings: widget.settings,
        mods: widget.mods,
        client: widget.client,
        onCalibrationResult: _handleCalibrationResult,
      ),
      TierStageDebugScreen(
        levelsFuture: _levelsFuture,
        settings: widget.settings,
        client: widget.client,
      ),
      ModsSelectionScreen(mods: widget.mods, onChanged: widget.onModsChanged),
      SettingsScreen(
        settings: widget.settings,
        onChanged: widget.onSettingsChanged,
        onStartGlobalCalibration: _startGlobalCalibration,
      ),
      UnityLogsScreen(logStore: _logStore),
    ];

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (_, _) {
        unawaited(_handleRootBack());
      },
      child: Scaffold(
        body: SafeArea(
          child: Row(
            children: [
              NavigationRail(
                selectedIndex: _selectedIndex,
                onDestinationSelected: (index) {
                  setState(() {
                    _selectedIndex = index;
                  });
                },
                labelType: NavigationRailLabelType.all,
                destinations: const [
                  NavigationRailDestination(
                    icon: Icon(Icons.music_note_outlined),
                    selectedIcon: Icon(Icons.music_note),
                    label: Text('Play'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.military_tech_outlined),
                    selectedIcon: Icon(Icons.military_tech),
                    label: Text('Tier'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.extension_outlined),
                    selectedIcon: Icon(Icons.extension),
                    label: Text('Mods'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.tune_outlined),
                    selectedIcon: Icon(Icons.tune),
                    label: Text('Settings'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.receipt_long_outlined),
                    selectedIcon: Icon(Icons.receipt_long),
                    label: Text('Logs'),
                  ),
                ],
              ),
              const VerticalDivider(width: 1),
              Expanded(child: pages[_selectedIndex]),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _startGlobalCalibration() async {
    final level = await _repository.loadGlobalCalibrationGuide();
    if (!mounted) return;

    Navigator.of(context).pushNamed(
      ExampleRoutes.game,
      arguments: GameRouteArgs(
        client: widget.client,
        level: level,
        difficulty: level.defaultDifficulty,
        settings: widget.settings,
        mods: const ExampleMods(gameMode: GameMode.globalCalibration),
        onCalibrationResult: _handleCalibrationResult,
      ),
    );
  }

  void _handleCalibrationResult(GameResultPayload result) {
    final baseOffset = result.calibratedBaseNoteOffset;
    final levelOffset = result.calibratedLevelNoteOffset;
    String? message;

    if (baseOffset != null) {
      widget.onSettingsChanged(
        widget.settings.copyWith(baseNoteOffset: baseOffset),
      );
      message = 'Global calibration set to ${baseOffset.toStringAsFixed(3)} s';
    } else if (levelOffset != null) {
      widget.onSettingsChanged(
        widget.settings.copyWith(levelNoteOffset: levelOffset),
      );
      message = 'Level offset set to ${levelOffset.toStringAsFixed(2)} s';
    }

    if (message == null || !mounted) return;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(message!)));
    });
  }

  Future<void> _handleRootBack() async {
    final now = DateTime.now();
    final shouldExit =
        _lastExitBackPressedAt != null &&
        now.difference(_lastExitBackPressedAt!) < _exitConfirmationWindow;

    if (!shouldExit) {
      _lastExitBackPressedAt = now;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(
            content: Text('Press back again to exit'),
            duration: _exitConfirmationWindow,
          ),
        );
      return;
    }

    await _backNavigationChannel.invokeMethod<void>('moveTaskToBack');
  }
}
