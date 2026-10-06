import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/crop-resource/models/crop_activity_models.dart';
import 'package:paddywise_mobile/features/crop-resource/models/crop_analysis_models.dart';

/// JSON in and out for Component 2's mobile models, checked against the keys the backend's
/// CropActivityService.ValidateActivityDetails requires.
void main() {
  group('ActivityType', () {
    test('parses labels case-insensitively and falls back to Fertilizer', () {
      expect(ActivityType.fromString('irrigation'), ActivityType.irrigation);
      expect(ActivityType.fromString(' PESTICIDE '), ActivityType.pesticide);
      expect(ActivityType.fromString('Other'), ActivityType.other);
      expect(ActivityType.fromString('Harvesting'), ActivityType.fertilizer);
    });
  });

  group('CropActivityDto', () {
    final json = {
      'id': 7,
      'cultivationCycleId': 12,
      'activityType': 'Fertilizer',
      'date': '2026-10-01T00:00:00',
      'detailsJson': '{"type":"Urea","quantity":50}',
      'loggedByUserId': 9,
      'loggedByUserName': 'Sunil',
      'createdAt': '2026-10-01T05:00:00Z',
      'fieldName': 'Lower paddy',
      'farmerName': 'Sunil',
      'farmerId': 9,
      'cycleName': 'Maha 2026',
    };

    test('fromJson keeps only the date part and parses the details', () {
      final dto = CropActivityDto.fromJson(json);
      expect(dto.date, '2026-10-01');
      expect(dto.parsedDetails, {'type': 'Urea', 'quantity': 50});
      expect(dto.cycleName, 'Maha 2026');
    });

    test('toJson round-trips and omits absent optional fields', () {
      final dto = CropActivityDto.fromJson(json);
      expect(CropActivityDto.fromJson(dto.toJson()).toJson(), dto.toJson());

      final bare = CropActivityDto.fromJson({'id': 1, 'detailsJson': 'not json'});
      expect(bare.toJson().containsKey('fieldName'), isFalse);
      expect(bare.parsedDetails, isEmpty); // malformed details never throw
    });

    test('copyWith changes only what it is given', () {
      final dto = CropActivityDto.fromJson(json).copyWith(date: '2026-10-02');
      expect(dto.date, '2026-10-02');
      expect(dto.id, 7);
    });
  });

  group('requests sent to the backend', () {
    test('Create and Update requests send the same three fields', () {
      const create = CreateCropActivityRequest(activityType: 'Irrigation', date: '2026-10-01', detailsJson: '{}');
      const update = UpdateCropActivityRequest(activityType: 'Irrigation', date: '2026-10-01', detailsJson: '{}');
      expect(create.toJson(), {'activityType': 'Irrigation', 'date': '2026-10-01', 'detailsJson': '{}'});
      expect(update.toJson(), create.toJson());
    });

    test('each form payload carries every key the backend requires for its type', () {
      final fertilizer = FertilizerFormData(
          date: '2026-10-01', type: 'Urea', quantity: 50, cropStage: 'Tillering', region: 'Dry', method: 'Broadcasting');
      final irrigation = IrrigationFormData(date: '2026-10-01', waterLevel: 0, duration: 2, source: 'Canal');
      final pesticide = PesticideFormData(
          date: '2026-10-01', product: 'Fipronil', targetPest: 'Stem borer', quantity: 1.5, method: 'Spraying');
      final other = OtherFormData(date: '2026-10-01', specificActivity: 'Weeding');

      expect(fertilizer.toJson().keys, containsAll(['type', 'quantity', 'cropStage', 'region', 'method']));
      expect(irrigation.toJson().keys, containsAll(['waterLevel', 'duration', 'source']));
      expect(pesticide.toJson().keys, containsAll(['product', 'targetPest', 'quantity', 'method']));
      expect(other.toJson().keys, contains('specificActivity'));
      expect(fertilizer.toJson()['activityType'], 'Fertilizer');
      expect(irrigation.toJson()['waterLevel'], 0); // a 0 cm level is valid on the backend
    });
  });

  group('CultivationCycleSummary', () {
    test('displayName and defaults', () {
      final cycle = CultivationCycleSummary.fromJson({
        'id': 12,
        'season': 'Maha',
        'year': 2026,
        'varietyName': 'Bg 352',
        'sowingDate': '2026-09-01',
      });
      expect(cycle.displayName, 'Maha 2026 · Bg 352');
      expect(cycle.fieldName, 'Field');
      expect(cycle.currentStage, 'Tillering');
      expect(cycle.actualHarvestDate, isNull);
    });
  });

  group('analysis models', () {
    Map<String, dynamic> recommendation({String status = 'PENDING_OFFICER_REVIEW'}) => {
          'id': 'a1b2',
          'dbId': 31,
          'category': 'Fertilizer',
          'priority': 'HIGH',
          'action': 'Apply 1st Top Dressing of Urea at 50 kg/ha.',
          'reason': 'Tillering.',
          'evidence': 'No Urea logged.',
          'confidenceScore': 0.9,
          'citations': [
            {'document': 'DOA Sri Lanka', 'section': 'Split application'}
          ],
          'requiresOfficerReview': true,
          'status': status,
          'reviewedAt': null,
        };

    test('ActivityRecommendation parses every lifecycle status', () {
      for (final status in ['PENDING_OFFICER_REVIEW', 'APPROVED', 'REJECTED', 'EXECUTED']) {
        expect(ActivityRecommendation.fromJson(recommendation(status: status)).status, status);
      }
      final rec = ActivityRecommendation.fromJson(recommendation());
      expect(rec.dbId, 31);
      expect(rec.citations.single.document, 'DOA Sri Lanka');
      expect(rec.requiresOfficerReview, isTrue);
    });

    test('an officer-queue row (int id, officerName/officerComment) maps onto the same model', () {
      final rec = ActivityRecommendation.fromJson({
        'id': 31,
        'recommendationUid': 'a1b2',
        'action': 'Irrigate.',
        'status': 'APPROVED',
        'officerName': 'Officer Silva',
        'officerComment': 'OK',
      });
      expect(rec.id, '31');
      expect(rec.dbId, 31);
      expect(rec.reviewedBy, 'Officer Silva');
      expect(rec.reviewNotes, 'OK');
    });

    test('a missing status defaults to pending officer review', () {
      final rec = ActivityRecommendation.fromJson({'id': 'x', 'action': 'Irrigate.'});
      expect(rec.status, 'PENDING_OFFICER_REVIEW');
      expect(rec.confidenceScore, 0.85);
    });

    test('CropActivityAnalysisOutput parses the whole analysis and tolerates gaps', () {
      final output = CropActivityAnalysisOutput.fromJson({
        'fieldOverview': {'cycleId': 12, 'currentStage': 'Tillering', 'daysAfterSowing': 20},
        'diagnostics': {
          'water': {'status': 'Low'},
          'fertilizer': {'status': 'DueSoon', 'totalUreaKgPerHa': 0},
        },
        'recommendations': [recommendation()],
        'warnings': ['NITROGEN EXCESS ALERT'],
        'requiresOfficerReview': true,
        'executiveSummary': 'Cycle is at Tillering stage.',
      });
      expect(output.fieldOverview.daysAfterSowing, 20);
      expect(output.diagnostics.water.status, 'Low');
      expect(output.recommendations, hasLength(1));
      expect(output.warnings.single, 'NITROGEN EXCESS ALERT');
      expect(CropActivityAnalysisOutput.fromJson({}).recommendations, isEmpty);
    });

    test('AiChatResponse and AiChatMessage', () {
      final reply = AiChatResponse.fromJson(
          {'answer': 'Keep 2 to 4 cm of water.', 'suggestedFollowUps': ['When to drain?'], 'requiresOfficerReview': false});
      expect(reply.answer, 'Keep 2 to 4 cm of water.');
      expect(reply.suggestedFollowUps, ['When to drain?']);
      const message = AiChatMessage(role: 'user', content: 'Water?');
      expect(AiChatMessage.fromJson(jsonDecode(jsonEncode(message.toJson())) as Map<String, dynamic>).content, 'Water?');
    });
  });
}
