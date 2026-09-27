import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/core/api/api_constants.dart';
import 'package:paddywise_mobile/services/auth_service.dart';

void main() {
  group('ApiConstants & BaseUrl Tests', () {
    test('ApiConstants.baseUrl returns configured or default emulator URL', () {
      if (const bool.hasEnvironment('API_BASE_URL')) {
        expect(
          ApiConstants.baseUrl,
          equals(const String.fromEnvironment('API_BASE_URL')),
        );
      } else {
        expect(ApiConstants.baseUrl, equals('http://10.0.2.2:5164/api'));
      }
    });

    test('AuthService.apiBaseUrl returns valid backend URL', () {
      expect(AuthService.apiBaseUrl, isNotEmpty);
      expect(AuthService.apiBaseUrl, startsWith('http://'));
      expect(AuthService.apiBaseUrl, endsWith('/api'));
    });

    test('AuthService.setApiBaseUrl overrides default and resetApiBaseUrl restores it', () async {
      final defaultUrl = AuthService.defaultApiBaseUrl;
      const customUrl = 'http://192.168.1.100:5164/api';

      await AuthService.setApiBaseUrl(customUrl);
      expect(AuthService.apiBaseUrl, equals(customUrl));

      await AuthService.resetApiBaseUrl();
      expect(AuthService.apiBaseUrl, equals(defaultUrl));
    });
  });
}
