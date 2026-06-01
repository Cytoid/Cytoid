import 'package:cytoid_game_core/cytoid_game_core.dart';
import 'package:flutter/material.dart';

import '../models/example_mods.dart';

class ModsSelectionScreen extends StatelessWidget {
  const ModsSelectionScreen({
    super.key,
    required this.mods,
    required this.onChanged,
  });

  final ExampleMods mods;
  final ValueChanged<ExampleMods> onChanged;

  void _handleModToggle(GameMod mod, bool selected) {
    final newMods = Set<GameMod>.from(mods.enabledMods);

    if (selected) {
      newMods.add(mod);
      switch (mod) {
        case GameMod.fast:
          newMods.remove(GameMod.slow);
        case GameMod.slow:
          newMods.remove(GameMod.fast);
        case GameMod.flipAll:
          newMods.remove(GameMod.flipX);
          newMods.remove(GameMod.flipY);
        case GameMod.flipX:
        case GameMod.flipY:
          newMods.remove(GameMod.flipAll);
        case GameMod.exHard:
          newMods.remove(GameMod.hard);
        case GameMod.auto:
        case GameMod.autoDrag:
        case GameMod.autoHold:
        case GameMod.autoFlick:
          newMods.remove(GameMod.ap);
          newMods.remove(GameMod.fc);
        case GameMod.ap:
        case GameMod.fc:
          newMods.remove(GameMod.auto);
          newMods.remove(GameMod.autoDrag);
          newMods.remove(GameMod.autoHold);
          newMods.remove(GameMod.autoFlick);
        case GameMod.hard:
          newMods.remove(GameMod.exHard);
        case GameMod.hideScanline:
        case GameMod.hideNotes:
          break;
      }
    } else {
      newMods.remove(mod);
    }

    onChanged(mods.copyWith(enabledMods: newMods));
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 24, 16, 24),
      children: [
        Text('Mods & Mode', style: Theme.of(context).textTheme.displaySmall),
        const SizedBox(height: 18),
        _SectionHeader(title: 'Game Mode'),
        Wrap(
          spacing: 8,
          children: GameMode.values
              .where(
                (mode) =>
                    mode != GameMode.globalCalibration &&
                    mode != GameMode.tier,
              )
              .map((mode) {
                return ChoiceChip(
                  label: Text(_modeLabel(mode)),
                  selected: mods.gameMode == mode,
                  onSelected: (selected) {
                    if (selected) {
                      onChanged(mods.copyWith(gameMode: mode));
                    }
                  },
                );
              })
              .toList(),
        ),
        const SizedBox(height: 12),
        _SectionHeader(title: 'Mods'),
        Wrap(
          spacing: 8,
          runSpacing: 4,
          children: GameMod.values.map((mod) {
            return FilterChip(
              label: Text(mod.wireName),
              selected: mods.enabledMods.contains(mod),
              onSelected: (selected) => _handleModToggle(mod, selected),
            );
          }).toList(),
        ),
      ],
    );
  }
}

String _modeLabel(GameMode mode) {
  return switch (mode) {
    GameMode.standard => 'Standard',
    GameMode.practice => 'Practice',
    GameMode.calibration => 'Calibration',
    GameMode.globalCalibration => 'Global calibration',
    GameMode.tier => 'Tier',
  };
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8, top: 4),
      child: Text(
        title,
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }
}
