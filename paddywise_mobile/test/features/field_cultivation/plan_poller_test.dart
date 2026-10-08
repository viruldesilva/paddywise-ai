import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/plan_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/services/plan_poller.dart';

import 'fc_test_harness.dart';

/// PlanPoller against a fake backend, with a short interval so it runs in real time.
void main() {
  const interval = Duration(milliseconds: 10);

  test('polls until the plan leaves Draft, then stops by itself', () async {
    var polls = 0;
    final updates = <PlanStatus>[];
    final calls = await withFakeBackend((_) {
      polls++;
      return jsonResponse(planJson(status: polls < 3 ? 'Draft' : 'ValidationFailed'));
    }, () async {
      final done = Completer<void>();
      final poller = PlanPoller(
        planId: 31,
        interval: interval,
        onUpdate: (plan) {
          updates.add(plan.status);
          if (plan.status != PlanStatus.draft) done.complete();
        },
        onError: (_, {required timedOut}) => fail('no error expected'),
      )..start();
      await done.future;
      expect(poller.isActive, isFalse);
      await Future<void>.delayed(interval * 5);
    });

    expect(updates, [PlanStatus.draft, PlanStatus.draft, PlanStatus.validationFailed]);
    expect(calls, hasLength(3));
    expect(calls.map((c) => c.path), everyElement('/plans/31'));
  });

  test('tolerates a dropped poll, and reports only after repeated failures', () async {
    var polls = 0;
    String? error;
    await withFakeBackend((_) {
      polls++;
      return polls == 1
          ? jsonResponse({'message': 'blip'}, 503)
          : polls == 2
              ? jsonResponse(planJson(status: 'Draft'))
              : jsonResponse({'message': 'Server is down.'}, 503);
    }, () async {
      final done = Completer<void>();
      PlanPoller(
        planId: 31,
        interval: interval,
        onUpdate: (_) {},
        onError: (message, {required timedOut}) {
          expect(timedOut, isFalse);
          error = message;
          done.complete();
        },
      ).start();
      await done.future;
    });

    expect(error, 'Server is down.');
    expect(polls, 2 + PlanPoller.maxConsecutiveErrors);
  });

  test('gives up after the limit without a finished plan', () async {
    bool? reportedTimeout;
    await withFakeBackend((_) => jsonResponse(planJson(status: 'Draft')), () async {
      final done = Completer<void>();
      PlanPoller(
        planId: 31,
        interval: interval,
        limit: interval * 3,
        onUpdate: (_) {},
        onError: (_, {required timedOut}) {
          reportedTimeout = timedOut;
          done.complete();
        },
      ).start();
      await done.future;
    });

    expect(reportedTimeout, isTrue);
  });

  test('no callback fires after cancel', () async {
    var updates = 0;
    final calls = await withFakeBackend((_) => jsonResponse(planJson(status: 'Draft')), () async {
      final poller = PlanPoller(
        planId: 31,
        interval: interval,
        onUpdate: (_) => updates++,
        onError: (_, {required timedOut}) => fail('no error expected'),
      )..start();
      await Future<void>.delayed(interval * 3);
      poller.cancel();
      final seen = updates;
      await Future<void>.delayed(interval * 5);
      expect(updates, seen);
    });

    expect(calls.length, lessThanOrEqualTo(updates + 1));
  });
}
