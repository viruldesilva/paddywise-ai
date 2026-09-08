import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/main.dart';

void main() {
  testWidgets('App renders Kumburu login screen by default',
      (WidgetTester tester) async {
    await tester.pumpWidget(const PaddyWiseApp());

    // Verify Kumburu brand is present
    expect(find.text('Kumburu'), findsOneWidget);
    expect(find.text('Sign in'), findsOneWidget);
    expect(find.text('Email Address'), findsOneWidget);
    expect(find.text('Password'), findsOneWidget);
  });
}
