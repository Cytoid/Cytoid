import 'dart:math' as math;

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import '../models/example_settings.dart';
import '../models/note_type_wire.dart';
import 'settings_widgets.dart';

/// Layout mirrors [SettingsFactory] in the legacy Cytoid client:
/// General → Gameplay → Visual → Advanced.
class SettingsScreen extends StatefulWidget {
  const SettingsScreen({
    super.key,
    required this.settings,
    required this.onChanged,
    required this.onStartGlobalCalibration,
  });

  final ExampleSettings settings;
  final ValueChanged<ExampleSettings> onChanged;
  final VoidCallback onStartGlobalCalibration;

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;

  ExampleSettings get settings => widget.settings;

  void _update(ExampleSettings value) => widget.onChanged(value);

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: 4, vsync: this);
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 24, 16, 0),
          child: Text(
            'Settings',
            style: Theme.of(context).textTheme.displaySmall,
          ),
        ),
        TabBar(
          controller: _tabs,
          isScrollable: true,
          tabAlignment: TabAlignment.start,
          tabs: const [
            Tab(text: 'General'),
            Tab(text: 'Gameplay'),
            Tab(text: 'Visual'),
            Tab(text: 'Advanced'),
          ],
        ),
        Expanded(
          child: TabBarView(
            controller: _tabs,
            children: [
              _GeneralTab(
                settings: settings,
                onChanged: _update,
                onStartGlobalCalibration: widget.onStartGlobalCalibration,
              ),
              _GameplayTab(settings: settings, onChanged: _update),
              _VisualTab(settings: settings, onChanged: _update),
              _AdvancedTab(settings: settings, onChanged: _update),
            ],
          ),
        ),
      ],
    );
  }
}

class _GeneralTab extends StatelessWidget {
  const _GeneralTab({
    required this.settings,
    required this.onChanged,
    required this.onStartGlobalCalibration,
  });

  final ExampleSettings settings;
  final ValueChanged<ExampleSettings> onChanged;
  final VoidCallback onStartGlobalCalibration;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        _SliderTile(
          icon: Icons.music_note,
          title: 'Music volume',
          value: settings.musicVolume,
          min: 0,
          max: 1,
          divisions: 20,
          display: '${(settings.musicVolume * 100).round()}%',
          onChanged: (value) =>
              onChanged(settings.copyWith(musicVolume: value)),
        ),
        _SliderTile(
          icon: Icons.graphic_eq,
          title: 'SFX volume',
          value: settings.soundEffectsVolume,
          min: 0,
          max: 1,
          divisions: 20,
          display: '${(settings.soundEffectsVolume * 100).round()}%',
          onChanged: (value) =>
              onChanged(settings.copyWith(soundEffectsVolume: value)),
        ),
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: InputDecorator(
            decoration: const InputDecoration(
              labelText: 'Hit sound',
              border: OutlineInputBorder(),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                isExpanded: true,
                value: settings.hitSound,
                items: [
                  for (final sound in ExampleSettings.hitSoundOptions)
                    DropdownMenuItem(value: sound, child: Text(sound)),
                ],
                onChanged: (value) {
                  if (value != null) {
                    onChanged(settings.copyWith(hitSound: value));
                  }
                },
              ),
            ),
          ),
        ),
        EnumDropdownTile<HoldHitSoundTiming>(
          label: 'Hold hit sound timing',
          value: settings.holdHitSoundTiming,
          items: HoldHitSoundTiming.values,
          labelBuilder: holdHitSoundTimingLabel,
          onChanged: (value) =>
              onChanged(settings.copyWith(holdHitSoundTiming: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Hit haptic feedback'),
          subtitle: const Text('Short vibration when a note is cleared'),
          value: settings.hitTapticFeedback,
          onChanged: (value) =>
              onChanged(settings.copyWith(hitTapticFeedback: value)),
        ),
        EnumDropdownTile<GraphicsQuality>(
          label: 'Graphics quality',
          value: settings.graphicsQuality,
          items: GraphicsQuality.values,
          labelBuilder: graphicsQualityLabel,
          onChanged: (value) =>
              onChanged(settings.copyWith(graphicsQuality: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Storyboard effects'),
          value: settings.displayStoryboardEffects,
          onChanged: (value) =>
              onChanged(settings.copyWith(displayStoryboardEffects: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Skip music on completion'),
          value: settings.skipMusicOnCompletion,
          onChanged: (value) =>
              onChanged(settings.copyWith(skipMusicOnCompletion: value)),
        ),
        _SliderTile(
          icon: Icons.timer,
          title: 'Base note offset',
          subtitle: 'Seconds',
          value: settings.baseNoteOffset,
          min: _offsetMin(settings.baseNoteOffset),
          max: _offsetMax(settings.baseNoteOffset),
          divisions: _offsetDivisions(settings.baseNoteOffset),
          display: '${settings.baseNoteOffset.toStringAsFixed(2)} s',
          onChanged: (value) =>
              onChanged(settings.copyWith(baseNoteOffset: value)),
        ),
        _SliderTile(
          icon: Icons.headphones,
          title: 'Headset note offset',
          subtitle: 'Seconds',
          value: settings.headsetNoteOffset,
          min: _offsetMin(settings.headsetNoteOffset),
          max: _offsetMax(settings.headsetNoteOffset),
          divisions: _offsetDivisions(settings.headsetNoteOffset),
          display: '${settings.headsetNoteOffset.toStringAsFixed(2)} s',
          onChanged: (value) =>
              onChanged(settings.copyWith(headsetNoteOffset: value)),
        ),
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: FilledButton.icon(
            onPressed: onStartGlobalCalibration,
            icon: const Icon(Icons.ads_click),
            label: const Text('Global calibration'),
          ),
        ),
        _BridgeOnlyHeader(title: 'Bridge host'),
        _SliderTile(
          icon: Icons.tune,
          title: 'Level note offset',
          subtitle: 'Per-chart offset (seconds)',
          value: settings.levelNoteOffset,
          min: _offsetMin(settings.levelNoteOffset),
          max: _offsetMax(settings.levelNoteOffset),
          divisions: _offsetDivisions(settings.levelNoteOffset),
          display: '${settings.levelNoteOffset.toStringAsFixed(2)} s',
          onChanged: (value) =>
              onChanged(settings.copyWith(levelNoteOffset: value)),
        ),
      ],
    );
  }
}

class _GameplayTab extends StatelessWidget {
  const _GameplayTab({required this.settings, required this.onChanged});

  final ExampleSettings settings;
  final ValueChanged<ExampleSettings> onChanged;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Early / late indicators'),
          value: settings.displayEarlyLateIndicators,
          onChanged: (value) =>
              onChanged(settings.copyWith(displayEarlyLateIndicators: value)),
        ),
        HitboxSizeTile(
          title: 'Hitbox size (tap)',
          value: settings.hitboxClick,
          onChanged: (value) =>
              onChanged(settings.copyWith(hitboxClick: value)),
        ),
        HitboxSizeTile(
          title: 'Hitbox size (drag)',
          value: settings.hitboxDrag,
          onChanged: (value) => onChanged(settings.copyWith(hitboxDrag: value)),
        ),
        HitboxSizeTile(
          title: 'Hitbox size (hold)',
          value: settings.hitboxHold,
          onChanged: (value) => onChanged(settings.copyWith(hitboxHold: value)),
        ),
        HitboxSizeTile(
          title: 'Hitbox size (flick)',
          value: settings.hitboxFlick,
          onChanged: (value) =>
              onChanged(settings.copyWith(hitboxFlick: value)),
        ),
        _SliderTile(
          icon: Icons.swap_horiz,
          title: 'Horizontal margin',
          value: settings.horizontalMargin.toDouble(),
          min: 1,
          max: 5,
          divisions: 4,
          display: '${settings.horizontalMargin}',
          onChanged: (value) =>
              onChanged(settings.copyWith(horizontalMargin: value.round())),
        ),
        _SliderTile(
          icon: Icons.swap_vert,
          title: 'Vertical margin',
          value: settings.verticalMargin.toDouble(),
          min: 1,
          max: 5,
          divisions: 4,
          display: '${settings.verticalMargin}',
          onChanged: (value) =>
              onChanged(settings.copyWith(verticalMargin: value.round())),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Restrict play area aspect ratio'),
          value: settings.restrictPlayAreaAspectRatio,
          onChanged: (value) =>
              onChanged(settings.copyWith(restrictPlayAreaAspectRatio: value)),
        ),
      ],
    );
  }
}

class _VisualTab extends StatelessWidget {
  const _VisualTab({required this.settings, required this.onChanged});

  final ExampleSettings settings;
  final ValueChanged<ExampleSettings> onChanged;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        _SliderTile(
          icon: Icons.radio_button_checked,
          title: 'Note size',
          subtitle: 'Relative offset (-0.5 to 0.5)',
          value: settings.noteSize,
          min: -0.5,
          max: 0.5,
          divisions: 20,
          display: settings.noteSize.toStringAsFixed(2),
          onChanged: (value) => onChanged(settings.copyWith(noteSize: value)),
        ),
        _SliderTile(
          icon: Icons.auto_awesome,
          title: 'Clear effects size',
          subtitle: 'Relative offset (-0.5 to 0.5)',
          value: settings.clearEffectsSize,
          min: -0.5,
          max: 0.5,
          divisions: 20,
          display: settings.clearEffectsSize.toStringAsFixed(2),
          onChanged: (value) =>
              onChanged(settings.copyWith(clearEffectsSize: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Display boundaries'),
          value: settings.displayBoundaries,
          onChanged: (value) =>
              onChanged(settings.copyWith(displayBoundaries: value)),
        ),
        _SliderTile(
          icon: Icons.wallpaper,
          title: 'Background opacity',
          value: settings.coverOpacity,
          min: 0,
          max: 1,
          divisions: 20,
          display: '${(settings.coverOpacity * 100).round()}%',
          onChanged: (value) =>
              onChanged(settings.copyWith(coverOpacity: value)),
        ),
        ColorHexTile(
          title: 'Ring color',
          hex: settings.ringColor,
          onChanged: (value) => onChanged(settings.copyWith(ringColor: value)),
        ),
        ColorHexTile(
          title: 'Tap fill (up)',
          hex: settings.clickFill,
          onChanged: (value) => onChanged(settings.copyWith(clickFill: value)),
        ),
        ColorHexTile(
          title: 'Tap fill (down)',
          hex: settings.clickFillAlt,
          onChanged: (value) =>
              onChanged(settings.copyWith(clickFillAlt: value)),
        ),
        ColorHexTile(
          title: 'Drag fill (up)',
          hex: settings.dragFill,
          onChanged: (value) => onChanged(settings.copyWith(dragFill: value)),
        ),
        ColorHexTile(
          title: 'Drag fill (down)',
          hex: settings.dragFillAlt,
          onChanged: (value) =>
              onChanged(settings.copyWith(dragFillAlt: value)),
        ),
        ColorHexTile(
          title: 'C-drag fill (up)',
          hex: settings.cDragFill,
          onChanged: (value) => onChanged(settings.copyWith(cDragFill: value)),
        ),
        ColorHexTile(
          title: 'C-drag fill (down)',
          hex: settings.cDragFillAlt,
          onChanged: (value) =>
              onChanged(settings.copyWith(cDragFillAlt: value)),
        ),
        ColorHexTile(
          title: 'Hold fill (up)',
          hex: settings.holdFill,
          onChanged: (value) => onChanged(settings.copyWith(holdFill: value)),
        ),
        ColorHexTile(
          title: 'Hold fill (down)',
          hex: settings.holdFillAlt,
          onChanged: (value) =>
              onChanged(settings.copyWith(holdFillAlt: value)),
        ),
        ColorHexTile(
          title: 'Long hold fill (up)',
          hex: settings.longHoldFill,
          onChanged: (value) =>
              onChanged(settings.copyWith(longHoldFill: value)),
        ),
        ColorHexTile(
          title: 'Long hold fill (down)',
          hex: settings.longHoldFillAlt,
          onChanged: (value) =>
              onChanged(settings.copyWith(longHoldFillAlt: value)),
        ),
        ColorHexTile(
          title: 'Flick fill (up)',
          hex: settings.flickFill,
          onChanged: (value) => onChanged(settings.copyWith(flickFill: value)),
        ),
        ColorHexTile(
          title: 'Flick fill (down)',
          hex: settings.flickFillAlt,
          onChanged: (value) =>
              onChanged(settings.copyWith(flickFillAlt: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Use fill color for drag child nodes'),
          value: settings.useFillColorForDragChildNodes,
          onChanged: (value) => onChanged(
            settings.copyWith(useFillColorForDragChildNodes: value),
          ),
        ),
      ],
    );
  }
}

class _AdvancedTab extends StatelessWidget {
  const _AdvancedTab({required this.settings, required this.onChanged});

  final ExampleSettings settings;
  final ValueChanged<ExampleSettings> onChanged;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        if (defaultTargetPlatform == TargetPlatform.android)
          EnumDropdownTile<int>(
            label: 'DSP buffer size',
            value: settings.androidDspBufferSize,
            items: ExampleSettings.androidDspBufferOptions,
            labelBuilder: androidDspBufferLabel,
            onChanged: (value) =>
                onChanged(settings.copyWith(androidDspBufferSize: value)),
          ),
        if (defaultTargetPlatform == TargetPlatform.android ||
            defaultTargetPlatform == TargetPlatform.iOS)
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Use native audio'),
            value: settings.useNativeAudio,
            onChanged: (value) =>
                onChanged(settings.copyWith(useNativeAudio: value)),
          ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Display profiler (Graphy)'),
          value: settings.displayProfiler,
          onChanged: (value) =>
              onChanged(settings.copyWith(displayProfiler: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Display note IDs'),
          value: settings.displayNoteIds,
          onChanged: (value) =>
              onChanged(settings.copyWith(displayNoteIds: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Experimental note AR'),
          value: settings.useExperimentalNoteAr,
          onChanged: (value) =>
              onChanged(settings.copyWith(useExperimentalNoteAr: value)),
        ),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Experimental note animations'),
          value: settings.useExperimentalNoteAnimations,
          onChanged: (value) => onChanged(
            settings.copyWith(useExperimentalNoteAnimations: value),
          ),
        ),
        _SliderTile(
          icon: Icons.sync,
          title: 'Judgment offset',
          subtitle: 'Seconds',
          value: settings.judgmentOffset,
          min: _offsetMin(settings.judgmentOffset),
          max: _offsetMax(settings.judgmentOffset),
          divisions: _offsetDivisions(settings.judgmentOffset),
          display: '${settings.judgmentOffset.toStringAsFixed(2)} s',
          onChanged: (value) =>
              onChanged(settings.copyWith(judgmentOffset: value)),
        ),
        const _BridgeOnlyHeader(title: 'Bridge host'),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: const Text('Adapt HUD to safe area'),
          subtitle: const Text(
            'Moves gameplay overlay controls away from notches and cutouts',
          ),
          value: settings.adaptOverlayToSafeArea,
          onChanged: (value) =>
              onChanged(settings.copyWith(adaptOverlayToSafeArea: value)),
        ),
      ],
    );
  }
}

class _BridgeOnlyHeader extends StatelessWidget {
  const _BridgeOnlyHeader({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 12, bottom: 8),
      child: Text(
        title,
        style: Theme.of(context).textTheme.labelLarge?.copyWith(
          color: Theme.of(context).colorScheme.outline,
        ),
      ),
    );
  }
}

double _offsetExtent(double value) {
  return math.max(0.5, (value.abs() * 10).ceil() / 10);
}

double _offsetMin(double value) => -_offsetExtent(value);

double _offsetMax(double value) => _offsetExtent(value);

int _offsetDivisions(double value) => (_offsetExtent(value) * 200).round();

class _SliderTile extends StatelessWidget {
  const _SliderTile({
    required this.icon,
    required this.title,
    required this.value,
    required this.min,
    required this.max,
    required this.divisions,
    required this.display,
    required this.onChanged,
    this.subtitle,
  });

  final IconData icon;
  final String title;
  final String? subtitle;
  final double value;
  final double min;
  final double max;
  final int divisions;
  final String display;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title),
                    if (subtitle != null)
                      Text(
                        subtitle!,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                  ],
                ),
              ),
              Text(display),
            ],
          ),
          Slider(
            value: value,
            min: min,
            max: max,
            divisions: divisions,
            onChanged: onChanged,
          ),
        ],
      ),
    );
  }
}
