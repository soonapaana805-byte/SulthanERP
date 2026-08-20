import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:sulthan_erp_captain/app.dart';
import 'package:sulthan_erp_captain/features/captain/order_page.dart';

void main() {
  testWidgets('shows the Captain login screen', (tester) async {
    await tester.pumpWidget(const CaptainApp());

    expect(find.text('SULTHAN ERP'), findsOneWidget);
    expect(find.text('Captain ordering'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Sign in'), findsOneWidget);
  });

  testWidgets('saves a soup serving style and kitchen note', (tester) async {
    ItemInstructionsResult? result;

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: FilledButton(
              onPressed: () async {
                result = await showDialog<ItemInstructionsResult>(
                  context: context,
                  builder: (_) => const ItemInstructionsDialog(
                    itemName: 'Chicken Soup',
                    isSoup: true,
                    initialServingStyle: 'Regular',
                    initialNotes: '',
                  ),
                );
              },
              child: const Text('Open instructions'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open instructions'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('1 by 2'));
    await tester.enterText(
      find.widgetWithText(TextField, 'Kitchen note (optional)'),
      'Less salt',
    );
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.byType(ItemInstructionsDialog), findsNothing);
    expect(result?.servingStyle, '1 by 2');
    expect(result?.notes, 'Less salt');
  });
}
