import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('smoke: material app builds', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(home: Scaffold(body: Text('Cytoid example'))),
    );
    expect(find.text('Cytoid example'), findsOneWidget);
  });
}
