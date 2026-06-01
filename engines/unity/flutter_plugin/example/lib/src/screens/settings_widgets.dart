import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../models/example_settings.dart';
import '../models/note_type_wire.dart';

class HitboxSizeTile extends StatelessWidget {
  const HitboxSizeTile({
    super.key,
    required this.title,
    required this.value,
    required this.onChanged,
  });

  final String title;
  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title),
          const SizedBox(height: 6),
          SegmentedButton<int>(
            segments: [
              for (var i = 0; i < ExampleSettings.hitboxSizeLabels.length; i++)
                ButtonSegment(
                  value: i,
                  label: Text(ExampleSettings.hitboxSizeLabels[i]),
                ),
            ],
            selected: {value},
            onSelectionChanged: (selection) => onChanged(selection.first),
          ),
        ],
      ),
    );
  }
}

class ColorHexTile extends StatelessWidget {
  const ColorHexTile({
    super.key,
    required this.title,
    required this.hex,
    required this.onChanged,
  });

  final String title;
  final String hex;
  final ValueChanged<String> onChanged;

  Color? get _color {
    final normalized = hex.startsWith('#') ? hex.substring(1) : hex;
    if (normalized.length != 6) return null;
    final value = int.tryParse(normalized, radix: 16);
    if (value == null) return null;
    return Color(0xFF000000 | value);
  }

  Future<void> _openPicker(BuildContext context) async {
    final controller = TextEditingController(text: hex);
    final picked = await showDialog<String>(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text(title),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final preset in ExampleSettings.colorPresets)
                      _ColorSwatch(
                        color: _parseHex(preset),
                        selected: preset.toUpperCase() == hex.toUpperCase(),
                        onTap: () => Navigator.pop(context, preset),
                      ),
                  ],
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: controller,
                  decoration: const InputDecoration(
                    labelText: 'Hex (#RRGGBB)',
                    border: OutlineInputBorder(),
                  ),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[#0-9A-Fa-f]')),
                  ],
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () {
                var value = controller.text.trim();
                if (!value.startsWith('#')) value = '#$value';
                Navigator.pop(context, value.toUpperCase());
              },
              child: const Text('Apply'),
            ),
          ],
        );
      },
    );
    controller.dispose();
    if (picked != null) {
      onChanged(picked);
    }
  }

  static Color _parseHex(String value) {
    final normalized = value.startsWith('#') ? value.substring(1) : value;
    return Color(0xFF000000 | int.parse(normalized, radix: 16));
  }

  @override
  Widget build(BuildContext context) {
    final swatch = _color;
    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(title),
      subtitle: Text(hex),
      leading: CircleAvatar(
        backgroundColor: swatch ?? Colors.grey,
        radius: 16,
      ),
      trailing: const Icon(Icons.palette_outlined),
      onTap: () => _openPicker(context),
    );
  }
}

class _ColorSwatch extends StatelessWidget {
  const _ColorSwatch({
    required this.color,
    required this.selected,
    required this.onTap,
  });

  final Color color;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      customBorder: const CircleBorder(),
      child: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: color,
          border: Border.all(
            color: selected
                ? Theme.of(context).colorScheme.primary
                : Colors.white24,
            width: selected ? 3 : 1,
          ),
        ),
      ),
    );
  }
}

class EnumDropdownTile<T> extends StatelessWidget {
  const EnumDropdownTile({
    super.key,
    required this.label,
    required this.value,
    required this.items,
    required this.labelBuilder,
    required this.onChanged,
  });

  final String label;
  final T value;
  final List<T> items;
  final String Function(T item) labelBuilder;
  final ValueChanged<T> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          border: const OutlineInputBorder(),
        ),
        child: DropdownButtonHideUnderline(
          child: DropdownButton<T>(
            isExpanded: true,
            value: value,
            items: [
              for (final item in items)
                DropdownMenuItem(
                  value: item,
                  child: Text(labelBuilder(item)),
                ),
            ],
            onChanged: (item) {
              if (item != null) onChanged(item);
            },
          ),
        ),
      ),
    );
  }
}

String graphicsQualityLabel(GraphicsQuality quality) {
  return switch (quality) {
    GraphicsQuality.veryLow => 'Very low',
    GraphicsQuality.low => 'Low',
    GraphicsQuality.medium => 'Medium',
    GraphicsQuality.high => 'High',
    GraphicsQuality.ultra => 'Ultra',
  };
}

String holdHitSoundTimingLabel(HoldHitSoundTiming timing) {
  return switch (timing) {
    HoldHitSoundTiming.begin => 'Begin',
    HoldHitSoundTiming.end => 'End',
    HoldHitSoundTiming.both => 'Both',
  };
}

String androidDspBufferLabel(int size) {
  if (size < 0) return 'Default';
  return '$size';
}
