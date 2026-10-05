import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/cycle_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/plan_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/widgets/status_badges.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

Future<(String, Color)> _render(WidgetTester tester, Widget badge) async {
  await tester.pumpWidget(MaterialApp(home: Scaffold(body: Center(child: badge))));
  final text = tester.widget<Text>(find.byType(Text));
  return (text.data!, text.style!.color!);
}

void main() {
  group('CycleStatusBadge', () {
    final expected = <CycleStatus, (String, Color)>{
      CycleStatus.planned: ('PLANNED', AppColors.badgeBuyerText),
      CycleStatus.active: ('ACTIVE', AppColors.successText),
      CycleStatus.harvested: ('HARVESTED', AppColors.badgeOfficerText),
      CycleStatus.abandoned: ('ABANDONED', AppColors.inkSoft),
    };

    for (final entry in expected.entries) {
      testWidgets('${entry.key.wire} shows its label in its colour', (tester) async {
        expect(await _render(tester, CycleStatusBadge(entry.key)), entry.value);
      });
    }
  });

  group('PlanStatusBadge', () {
    final expected = <PlanStatus, (String, Color)>{
      PlanStatus.draft: ('BEING PREPARED', AppColors.inkSoft),
      PlanStatus.validationFailed: ('NEEDS ANOTHER TRY', AppColors.errorText),
      PlanStatus.pendingOfficerApproval: ('WAITING FOR OFFICER', AppColors.badgeBuyerText),
      PlanStatus.approved: ('APPROVED', AppColors.successText),
      PlanStatus.rejected: ('REJECTED', AppColors.errorText),
      PlanStatus.revisionRequested: ('CHANGES REQUESTED', AppColors.badgeAdminText),
    };

    for (final entry in expected.entries) {
      testWidgets('${entry.key.wire} shows its label in its colour', (tester) async {
        expect(await _render(tester, PlanStatusBadge(entry.key)), entry.value);
      });
    }
  });
}
