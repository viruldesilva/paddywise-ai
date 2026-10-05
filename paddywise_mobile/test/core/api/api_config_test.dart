import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/core/api/api_config.dart';

void main() {
  group('ApiConfig Tests', () {
    test('productionBaseUrl matches Azure App Service host', () {
      expect(
        ApiConfig.productionBaseUrl,
        'https://paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net',
      );
    });

    group('normalizeBaseUrl', () {
      test('prepends https:// when scheme is missing', () {
        const raw =
            'paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net';
        final normalized = ApiConfig.normalizeBaseUrl(raw);
        expect(
          normalized,
          'https://paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net/api',
        );
      });

      test('preserves existing https:// and http:// schemes', () {
        expect(
          ApiConfig.normalizeBaseUrl('https://example.com/api'),
          'https://example.com/api',
        );
        expect(
          ApiConfig.normalizeBaseUrl('http://10.0.2.2:5164'),
          'http://10.0.2.2:5164/api',
        );
        expect(
          ApiConfig.normalizeBaseUrl('http://10.0.2.2:5164/api'),
          'http://10.0.2.2:5164/api',
        );
      });

      test('removes trailing slashes and trims whitespace', () {
        const raw = '   https://example.com/api///   ';
        expect(ApiConfig.normalizeBaseUrl(raw), 'https://example.com/api');
      });

      test('ensures /api suffix is present without duplication', () {
        expect(
          ApiConfig.normalizeBaseUrl('https://example.com'),
          'https://example.com/api',
        );
        expect(
          ApiConfig.normalizeBaseUrl('https://example.com/api'),
          'https://example.com/api',
        );
      });

      test('handles empty input gracefully', () {
        expect(ApiConfig.normalizeBaseUrl(''), '');
        expect(ApiConfig.normalizeBaseUrl('   '), '');
      });
    });

    group('buildUri', () {
      test('safely constructs Uri with no double slashes (leading slash in path)', () {
        final uri = ApiConfig.buildUri(
          'paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net',
          '/auth/login',
        );

        expect(uri.scheme, 'https');
        expect(
          uri.host,
          'paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net',
        );
        expect(uri.path, '/api/auth/login');
        expect(
          uri.toString(),
          'https://paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net/api/auth/login',
        );
      });

      test('safely constructs Uri when path has no leading slash', () {
        final uri = ApiConfig.buildUri(
          'http://10.0.2.2:5164',
          'auth/login',
        );

        expect(uri.scheme, 'http');
        expect(uri.host, '10.0.2.2');
        expect(uri.port, 5164);
        expect(uri.path, '/api/auth/login');
        expect(uri.toString(), 'http://10.0.2.2:5164/api/auth/login');
      });

      test('attaches query parameters safely', () {
        final uri = ApiConfig.buildUri(
          'https://example.com/api',
          '/cycles',
          {'season': 'Yala', 'year': 2026},
        );

        expect(uri.path, '/api/cycles');
        expect(uri.queryParameters, {'season': 'Yala', 'year': '2026'});
      });
    });

    group('isLocalUrl', () {
      test('identifies local development URLs', () {
        expect(ApiConfig.isLocalUrl('http://10.0.2.2:5164/api'), isTrue);
        expect(ApiConfig.isLocalUrl('http://localhost:5164'), isTrue);
        expect(ApiConfig.isLocalUrl('http://127.0.0.1:5164/api'), isTrue);
        expect(ApiConfig.isLocalUrl('http://192.168.1.100:5164/api'), isTrue);
      });

      test('identifies remote / production URLs', () {
        expect(
          ApiConfig.isLocalUrl(
            'https://paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net',
          ),
          isFalse,
        );
        expect(
          ApiConfig.isLocalUrl(
            'paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net',
          ),
          isFalse,
        );
      });
    });
  });
}
