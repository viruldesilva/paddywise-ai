import 'dart:async';

import '../models/plan_models.dart';
import 'field_cultivation_service.dart';

/// Polls `GET /plans/{id}` while a plan is still being generated (status
/// Draft). The backend generates plans in the background, one at a time, so a
/// request returns a Draft straight away and the result arrives later.
///
/// Call [cancel] from the owning widget's `dispose`; no callback fires after it.
class PlanPoller {
  /// Consecutive failed polls tolerated before [onError] is called — one
  /// dropped connection on a farm should not end the wait.
  static const int maxConsecutiveErrors = 3;

  final int planId;

  /// Every poll result, including the last one (no longer Draft).
  final void Function(CultivationPlan plan) onUpdate;

  /// Polling stopped without a finished plan: repeated errors, or
  /// [FieldCultivationService.planPollLimit] passed (`timedOut` is true).
  final void Function(String message, {required bool timedOut}) onError;

  final Duration interval;
  final Duration limit;

  Timer? _timer;
  bool _inFlight = false;
  bool _cancelled = false;
  int _errors = 0;
  late DateTime _deadline;

  PlanPoller({
    required this.planId,
    required this.onUpdate,
    required this.onError,
    this.interval = FieldCultivationService.planPollInterval,
    this.limit = FieldCultivationService.planPollLimit,
  });

  bool get isActive => _timer != null;

  void start() {
    if (_cancelled || _timer != null) return;
    _deadline = DateTime.now().add(limit);
    _timer = Timer.periodic(interval, (_) => _poll());
  }

  void cancel() {
    _cancelled = true;
    _timer?.cancel();
    _timer = null;
  }

  Future<void> _poll() async {
    if (_inFlight || _cancelled) return;

    if (DateTime.now().isAfter(_deadline)) {
      cancel();
      onError(
        'Your plan is taking longer than usual. It is still being prepared — '
        'check your season again later.',
        timedOut: true,
      );
      return;
    }

    _inFlight = true;
    try {
      final plan = await FieldCultivationService.getPlan(planId);
      if (_cancelled) return;
      _errors = 0;
      if (plan.status != PlanStatus.draft) cancel();
      onUpdate(plan);
    } catch (e) {
      if (_cancelled) return;
      if (++_errors >= maxConsecutiveErrors) {
        cancel();
        onError(e.toString(), timedOut: false);
      }
    } finally {
      _inFlight = false;
    }
  }
}
