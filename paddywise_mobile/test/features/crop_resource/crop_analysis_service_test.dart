import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:paddywise_mobile/features/crop-resource/services/crop_analysis_service.dart';

// Reused unchanged from Component 1's suite: http.runWithClient + MockClient.
import '../field_cultivation/fc_test_harness.dart';

Map<String, dynamic> _analysisJson() => {
      'fieldOverview': {'cycleId': 12, 'currentStage': 'Tillering', 'daysAfterSowing': 20},
      'diagnostics': {},
      'recommendations': [
        {'id': 'a1', 'action': 'Irrigate.', 'status': 'PENDING_OFFICER_REVIEW', 'requiresOfficerReview': true}
      ],
      'warnings': [],
      'requiresOfficerReview': false,
      'executiveSummary': 'Cycle is at Tillering stage.',
    };

/// CropAnalysisService against a fake backend, demo fallback OFF. Component 2's agent calls
/// are single requests with a 12-second timeout; there is no polling to test.
void main() {
  setUp(() => CropAnalysisService.useDemoFallback = false);

  test('runAnalysis POSTs the cycle, question and focus area, and parses the analysis', () async {
    final calls = await withFakeBackend((_) => jsonResponse(_analysisJson()), () async {
      final output = await CropAnalysisService.runAnalysis(12, question: 'Water?');
      expect(output.recommendations.single.status, 'PENDING_OFFICER_REVIEW');
    });

    expect(calls.single.path, '/cycles/12/analysis');
    expect(calls.single.body, {'cultivationCycleId': 12, 'farmerQuestion': 'Water?', 'focusArea': 'All'});
  });

  test('getLatestAnalysis GETs /analysis/latest', () async {
    final calls = await withFakeBackend((_) => jsonResponse(_analysisJson()), () async {
      expect((await CropAnalysisService.getLatestAnalysis(12)).executiveSummary, 'Cycle is at Tillering stage.');
    });

    expect(calls.single.method, 'GET');
    expect(calls.single.path, '/cycles/12/analysis/latest');
  });

  test('chatWithAi POSTs the question and parses the answer', () async {
    final calls = await withFakeBackend(
        (_) => jsonResponse({'answer': 'Keep 2 to 4 cm.', 'suggestedFollowUps': [], 'requiresOfficerReview': false}),
        () async {
      expect((await CropAnalysisService.chatWithAi(12, 'Water?')).answer, 'Keep 2 to 4 cm.');
    });

    expect(calls.single.path, '/cycles/12/ai-chat');
    expect(calls.single.body!['question'], 'Water?');
  });

  test('executeRecommendation sends decision "execute" and reports success or failure', () async {
    final calls = await withFakeBackend(
        (call) => call.body!['recommendationId'] == 'ok' ? jsonResponse({'success': true}) : jsonResponse({}, 400),
        () async {
      expect(await CropAnalysisService.executeRecommendation(12, 'ok', payloadJson: '{"activityType":"Irrigation"}'), isTrue);
      expect(await CropAnalysisService.executeRecommendation(12, 'refused'), isFalse);
    });

    expect(calls.first.path, '/cycles/12/recommendations/review');
    expect(calls.first.body, {
      'recommendationId': 'ok',
      'decision': 'execute',
      'recommendationJson': '{"activityType":"Irrigation"}',
    });
  });

  test('a dropped connection gives the connection message', () async {
    Object? error;
    await http.runWithClient(() async {
      try {
        await CropAnalysisService.runAnalysis(12);
      } catch (e) {
        error = e;
      }
    }, () => _ThrowingClient());

    expect(error.toString(), contains('Failed to connect to AI Analysis service.'));
  });

  test('a backend { message } (e.g. a 404 for an unknown cycle) reaches the farmer unchanged', () async {
    Object? error;
    await withFakeBackend((_) => jsonResponse({'message': 'Cultivation cycle 99 not found.'}, 404), () async {
      try {
        await CropAnalysisService.runAnalysis(99);
      } catch (e) {
        error = e;
      }
    });

    expect(error.toString(), contains('Cultivation cycle 99 not found.'));
  },
      skip: 'Known bug (finding 11): CropAnalysisService throws "Failed to connect to AI Analysis service…" '
          'for every non-200 response, hiding the backend\'s { message }.');
}

class _ThrowingClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) =>
      Future.error(http.ClientException('Connection reset by peer'));
}
