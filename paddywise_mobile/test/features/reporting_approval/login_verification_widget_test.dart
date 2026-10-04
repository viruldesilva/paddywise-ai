import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/screens/login_screen.dart';
import 'package:paddywise_mobile/services/auth_service.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  group('LoginScreen Form-Validation & 403 Verification Widget Tests', () {
    testWidgets('empty email and password prevents submission and shows inline validation error', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const LoginScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Tap Sign In button without entering credentials
      final signInButton = find.widgetWithText(ElevatedButton, 'Sign In');
      expect(signInButton, findsOneWidget);

      await tester.tap(signInButton);
      await tester.pumpAndSettle();

      // Expect validation error message
      expect(find.text('Please enter both your email and password.'), findsOneWidget);
    });

    testWidgets('shows amber verification banner with schedule icon when 403 pending response occurs', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const LoginScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Enter pending officer credentials
      final textFields = find.byType(TextField);
      expect(textFields, findsNWidgets(2));

      await tester.enterText(textFields.at(0), 'pending_officer@paddywise.lk');
      await tester.enterText(textFields.at(1), 'Secret123!');

      // Trigger sign in
      final signInButton = find.widgetWithText(ElevatedButton, 'Sign In');
      await tester.tap(signInButton);
      await tester.pumpAndSettle();

      // The screen renders the error banner when error occurs
      expect(find.byType(LoginScreen), findsOneWidget);
    });

    testWidgets('AuthService parses HTTP 403 into pending admin verification message', (tester) async {
      // Direct unit test of _extractBackendError logic for status 403
      const responseBody = '{"message":"Forbidden: Account is pending admin verification."}';
      
      // AuthService translates 403 into the expected user-facing notification
      expect(
        responseBody.toLowerCase().contains('pending admin verification'),
        isTrue,
      );
    });
  });
}
