import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class ApiClient {
  ApiClient({required String baseUrl, this.token, http.Client? client})
    : baseUrl = normalizeBaseUrl(baseUrl),
      _client = client ?? http.Client();

  final String baseUrl;
  final String? token;
  final http.Client _client;

  static String normalizeBaseUrl(String input) {
    var value = input.trim();
    if (!value.contains('://')) {
      value = 'http://$value';
    }

    final uri = Uri.tryParse(value);
    if (uri == null || uri.host.isEmpty) {
      throw const ApiException('Enter a valid API address.');
    }

    return Uri(
      scheme: uri.scheme,
      host: uri.host,
      port: uri.hasPort ? uri.port : null,
    ).toString().replaceAll(RegExp(r'/$'), '');
  }

  Future<dynamic> get(String path) => _send('GET', path);

  Future<dynamic> post(String path, [Object? body]) =>
      _send('POST', path, body: body);

  Future<dynamic> _send(String method, String path, {Object? body}) async {
    final uri = Uri.parse('$baseUrl/api/$path');
    final headers = <String, String>{
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      if (token != null && token!.isNotEmpty) 'Authorization': 'Bearer $token',
    };

    try {
      final response = method == 'GET'
          ? await _client
                .get(uri, headers: headers)
                .timeout(const Duration(seconds: 15))
          : await _client
                .post(
                  uri,
                  headers: headers,
                  body: body == null ? null : jsonEncode(body),
                )
                .timeout(const Duration(seconds: 20));

      dynamic decoded;
      if (response.body.isNotEmpty) {
        try {
          decoded = jsonDecode(response.body);
        } on FormatException {
          decoded = response.body;
        }
      }

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw ApiException(
          _errorMessage(decoded, response.statusCode),
          statusCode: response.statusCode,
        );
      }

      return decoded;
    } on TimeoutException {
      throw const ApiException(
        'Server response timed out. Check Wi-Fi and the API address.',
      );
    } on http.ClientException catch (error) {
      throw ApiException('Cannot reach the server: ${error.message}');
    }
  }

  static String _errorMessage(dynamic body, int statusCode) {
    if (body is Map<String, dynamic>) {
      final message = body['message'] ?? body['Message'];
      if (message is String && message.isNotEmpty) return message;
    }
    if (body is String && body.isNotEmpty) return body;
    return 'Request failed ($statusCode).';
  }

  void close() => _client.close();
}
