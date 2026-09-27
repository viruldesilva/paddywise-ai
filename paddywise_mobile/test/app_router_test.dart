import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/core/router/app_router.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('appRouter has /activities/new and /cycles/:id/activities/new configured', () {
    final shellRoute = appRouter.configuration.routes
        .whereType<dynamic>()
        .firstWhere((r) => r.routes.isNotEmpty);

    final paths = shellRoute.routes.map((r) => r.path).toList();
    expect(paths, contains('/activities/new'));
    expect(paths, contains('/cycles/:id/activities/new'));
    expect(paths, contains('/activities/:id/edit'));
    expect(paths, contains('/activities/advisor'));
  });
}
