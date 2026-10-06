import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:paddywise_mobile/features/reporting_approval/screens/pending_reviews_screen.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

// Reused unchanged from Component 1's suite: http.runWithClient + MockClient.
import '../field_cultivation/fc_test_harness.dart';

/// A row exactly as GET /api/plans/pending returns it (PendingPlanSummaryDto).
Map<String, dynamic> _backendRow() => {
      'planId': 31,
      'cycleId': 12,
      'farmerName': 'Sunil Perera',
      'fieldName': 'Lower paddy',
      'divisionName': 'Polonnaruwa',
      'season': 'Maha',
      'year': 2026,
      'objective': 'Good yield.',
      'createdAt': '2026-10-01T05:00:00Z',
    };

/// A row in the shape the screen itself reads (id, division, variety…).
Map<String, dynamic> _screenShapedRow() => {
      'id': 31,
      'farmerName': 'Sunil Perera',
      'fieldName': 'Lower paddy',
      'division': 'Polonnaruwa',
      'variety': 'Bg 352',
    };

Future<List<RecordedCall>> _pump(WidgetTester tester, Responder respond, [Future<void> Function()? then]) {
  tester.view.physicalSize = const Size(1200, 3000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  return withFakeBackend(respond, () async {
    await tester.pumpWidget(MaterialApp(theme: AppTheme.theme, home: const Scaffold(body: PendingReviewsScreen())));
    await tester.pumpAndSettle();
    if (then != null) await then();
  });
}

Future<void> _openReview(WidgetTester tester) async {
  await tester.tap(find.text('Review & Decide').first);
  await tester.pumpAndSettle();
}

/// Component 4's officer review screen on mobile. Virul's suite has no test for it.
void main() {
  testWidgets('loads the queue from GET /plans/pending and shows the farmer and field', (tester) async {
    final calls = await _pump(tester, (_) => jsonResponse([_backendRow()]));

    expect(calls.single.method, 'GET');
    expect(calls.single.path, '/plans/pending');
    expect(find.text('Sunil Perera'), findsOneWidget);
    expect(find.text('Lower paddy'), findsOneWidget);
    expect(find.text('Bandara Wanninayake'), findsNothing); // no demo rows when the backend answers
  });

  testWidgets('"AI Draft Comment" GETs the draft for that plan and fills the comment box', (tester) async {
    final calls = await _pump(tester, (call) {
      if (call.path == '/plans/pending') return jsonResponse([_screenShapedRow()]);
      return jsonResponse({'draftComment': 'Move the top-dressing into the Tillering window.'});
    }, () async {
      await _openReview(tester);
      await tester.tap(find.text('AI Draft Comment'));
      await tester.pumpAndSettle();
    });

    expect(calls.last.path, '/reviews/plans/31/draft-revision-comment');
    expect(find.text('Move the top-dressing into the Tillering window.'), findsWidgets);
  });

  testWidgets('a backend row shows its plan number and division', (tester) async {
    await _pump(tester, (_) => jsonResponse([_backendRow()]));

    expect(find.text('Plan #31'), findsOneWidget);
    expect(find.text('Polonnaruwa'), findsOneWidget);
  },
      skip: true, // Known bug (finding 6): the screen reads plan['id'] / plan['division'] / plan['variety'],
      // but the backend sends planId / divisionName and no variety — it shows "Plan #null" and "Division".
  );

  testWidgets('a failed load shows an error, not invented farmers', (tester) async {
    await _pump(tester, (_) => jsonResponse({'message': 'Forbidden'}, 403));

    expect(find.text('Bandara Wanninayake'), findsNothing);
  },
      skip: true, // Known bug (finding 6): any failed or non-200 load shows hard-coded demo plans
      // ("Bandara Wanninayake", …) as if they were real — CLAUDE.md allows no offline fallback.
  );

  testWidgets('"Approve Plan" sends the decision to POST /plans/{id}/review', (tester) async {
    final calls = await _pump(tester, (call) {
      if (call.path == '/plans/pending') return jsonResponse([_screenShapedRow()]);
      return jsonResponse({'id': 31, 'status': 'Approved'});
    }, () async {
      await _openReview(tester);
      await tester.tap(find.text('Approve Plan'));
      await tester.pumpAndSettle();
    });

    expect(calls.where((c) => c.method == 'POST' && c.path == '/plans/31/review'), hasLength(1));
  },
      skip: true, // Known bug (finding 6): "Approve Plan" and "Request Revision" call no endpoint —
      // they close the sheet and show "Plan #31 successfully approved!" / "Revision request sent to farmer."
  );

  testWidgets('a failed AI draft shows an error, not an invented comment with a dosage', (tester) async {
    await _pump(tester, (call) {
      if (call.path == '/plans/pending') return jsonResponse([_screenShapedRow()]);
      return http.Response('', 500);
    }, () async {
      await _openReview(tester);
      await tester.tap(find.text('AI Draft Comment'));
      await tester.pumpAndSettle();
    });

    expect(find.textContaining('reduce initial basal fertilizer by 10%'), findsNothing);
  },
      skip: true, // Known bug (finding 6): when the draft endpoint fails, the screen fills the officer's
      // comment with a hard-coded "AI" comment that states a dosage ("by 10%").
  );
}
