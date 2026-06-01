import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../game_routes.dart';
import '../models/example_level.dart';
import '../models/example_mods.dart';
import '../models/example_settings.dart';

class TierStageDebugScreen extends StatefulWidget {
  const TierStageDebugScreen({
    super.key,
    required this.levelsFuture,
    required this.settings,
    required this.client,
  });

  final Future<List<ExampleLevel>> levelsFuture;
  final ExampleSettings settings;
  final CytoidGameCoreClient client;

  @override
  State<TierStageDebugScreen> createState() => _TierStageDebugScreenState();
}

class _TierStageDebugScreenState extends State<TierStageDebugScreen> {
  final _tierIdController = TextEditingController(text: 'example-tier');
  final _stageIndexController = TextEditingController(text: '0');
  final _stageCountController = TextEditingController(text: '3');
  final _maxHealthController = TextEditingController(text: '1000');
  final _initialHealthController = TextEditingController(text: '700');
  final _initialComboController = TextEditingController(text: '0');
  final _introLabelController = TextEditingController();

  ExampleLevel? _selectedLevel;
  ExampleDifficulty? _selectedDifficulty;
  final Set<GameMod> _mods = {};

  @override
  void dispose() {
    _tierIdController.dispose();
    _stageIndexController.dispose();
    _stageCountController.dispose();
    _maxHealthController.dispose();
    _initialHealthController.dispose();
    _initialComboController.dispose();
    _introLabelController.dispose();
    super.dispose();
  }

  Future<void> _startTierSession() async {
    final level = _selectedLevel;
    final difficulty = _selectedDifficulty;
    if (level == null || difficulty == null) {
      _showMessage('Select a level and difficulty first.');
      return;
    }

    final maxHealth = double.tryParse(_maxHealthController.text.trim());
    if (maxHealth == null || maxHealth <= 0) {
      _showMessage('maxHealth must be a positive number.');
      return;
    }

    final stageIndex = int.tryParse(_stageIndexController.text.trim());
    if (stageIndex == null || stageIndex < 0) {
      _showMessage('stageIndex must be a non-negative integer.');
      return;
    }

    final tierPlay = TierPlayLaunch(
      tierId: _tierIdController.text.trim().isEmpty
          ? null
          : _tierIdController.text.trim(),
      stageIndex: stageIndex,
      stageCount: int.tryParse(_stageCountController.text.trim()),
      maxHealth: maxHealth,
      initialHealth: double.tryParse(_initialHealthController.text.trim()),
      initialCombo: int.tryParse(_initialComboController.text.trim()),
      introLabel: _introLabelController.text.trim().isEmpty
          ? null
          : _introLabelController.text.trim(),
    );

    final mods = ExampleMods(
      gameMode: GameMode.tier,
      enabledMods: _mods,
    );

    if (!mounted) return;
    final returned = await Navigator.of(context).pushNamed(
      ExampleRoutes.game,
      arguments: GameRouteArgs(
        client: widget.client,
        level: level,
        difficulty: difficulty,
        settings: widget.settings,
        mods: mods,
        tierPlay: tierPlay,
      ),
    );
    if (!mounted) return;
    if (returned is GameResultPayload && returned.tierRetry != null) {
      _showMessage('User want to retry the tier.');
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<ExampleLevel>>(
      future: widget.levelsFuture,
      builder: (context, snapshot) {
        final levels = snapshot.data;
        if (_selectedLevel == null && levels != null && levels.isNotEmpty) {
          _selectedLevel = levels.first;
        }
        _selectedDifficulty ??= _selectedLevel?.defaultDifficulty;

        return ListView(
          padding: const EdgeInsets.fromLTRB(18, 24, 18, 24),
          children: [
            Text('Tier stage debug', style: Theme.of(context).textTheme.displaySmall),
            const SizedBox(height: 8),
            Text(
              'Launch one Tier session with manual cross-stage inputs (HP, combo). '
              'The core does not advance stages automatically.',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 20),
            if (snapshot.hasError)
              Text('Failed to load levels: ${snapshot.error}')
            else if (levels == null)
              const Center(child: Text('Loading levels...'))
            else ...[
              DropdownMenu<ExampleLevel>(
                label: const Text('Level'),
                initialSelection: _selectedLevel,
                dropdownMenuEntries: [
                  for (final level in levels)
                    DropdownMenuEntry(value: level, label: level.title),
                ],
                onSelected: (level) {
                  setState(() {
                    _selectedLevel = level;
                    _selectedDifficulty = level?.defaultDifficulty;
                  });
                },
              ),
              const SizedBox(height: 12),
              DropdownMenu<ExampleDifficulty>(
                label: const Text('Difficulty'),
                initialSelection: _selectedDifficulty,
                dropdownMenuEntries: [
                  for (final difficulty in _selectedLevel?.difficulties ?? const [])
                    DropdownMenuEntry(
                      value: difficulty,
                      label: difficulty.label,
                    ),
                ],
                onSelected: (difficulty) {
                  setState(() => _selectedDifficulty = difficulty);
                },
              ),
              const SizedBox(height: 20),
              _numberField('tierId (echo)', _tierIdController, isNumeric: false),
              _numberField('stageIndex', _stageIndexController),
              _numberField('stageCount (optional)', _stageCountController),
              _numberField('maxHealth', _maxHealthController),
              _numberField('initialHealth', _initialHealthController),
              _numberField('initialCombo', _initialComboController),
              _numberField('introLabel (optional)', _introLabelController, isNumeric: false),
              const SizedBox(height: 12),
              Text('Tier mods', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                children: [
                  for (final mod in const [
                    GameMod.fast,
                    GameMod.slow,
                    GameMod.hideScanline,
                    GameMod.hideNotes,
                  ])
                    FilterChip(
                      label: Text(mod.wireName),
                      selected: _mods.contains(mod),
                      onSelected: (selected) {
                        setState(() {
                          if (selected) {
                            _mods.add(mod);
                          } else {
                            _mods.remove(mod);
                          }
                        });
                      },
                    ),
                ],
              ),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: _startTierSession,
                icon: const Icon(Icons.play_arrow),
                label: const Text('Start Tier session'),
              ),
            ],
          ],
        );
      },
    );
  }

  Widget _numberField(String label, TextEditingController controller, {bool isNumeric = true}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextField(
        controller: controller,
        decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()),
        keyboardType: isNumeric ? TextInputType.number : TextInputType.text,
        inputFormatters: isNumeric ? [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))] : null,
      ),
    );
  }
}
