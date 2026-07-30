import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hr_management_flutter/app.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('HrManagementApp boots to login screen', (WidgetTester tester) async {
    await tester.pumpWidget(const HrManagementApp());

    // Initial boot frame shows a loading indicator.
    expect(find.byType(CircularProgressIndicator), findsWidgets);

    await tester.pumpAndSettle();

    // After boot, the login shell should be visible.
    expect(find.byType(MaterialApp), findsOneWidget);
    expect(find.byType(TextField), findsWidgets);
  });
}
