import 'dart:convert';

import 'package:http/http.dart' as http;

import '../errors/api_exception.dart';

class ApiClient {
  ApiClient(String baseUrl) : _baseUrl = _clean(baseUrl);

  String _baseUrl;
  String? _token;

  void configure({required String baseUrl, String? token}) {
    _baseUrl = _clean(baseUrl);
    _token = token;
  }

  Future<Map<String, dynamic>> getMap(String path) async {
    final response = await _send(() => http.get(_uri(path), headers: _headers()));
    return Map<String, dynamic>.from(_decode(response) as Map);
  }

  Future<List<Map<String, dynamic>>> getList(String path) async {
    final response = await _send(() => http.get(_uri(path), headers: _headers()));
    final body = _decode(response);
    return _asItemList(body);
  }

  /// Supports both legacy arrays and paged API payloads: `{ items: [...] }`.
  List<Map<String, dynamic>> _asItemList(dynamic body) {
    final List<dynamic> items;
    if (body is List) {
      items = body;
    } else if (body is Map && body['items'] is List) {
      items = body['items'] as List<dynamic>;
    } else {
      throw ApiException('Unexpected list response from API.');
    }

    return items
        .map((item) => Map<String, dynamic>.from(item as Map))
        .toList();
  }

  Future<Map<String, dynamic>> postMap(
    String path,
    Map<String, dynamic> body, {
    bool skipAuth = false,
  }) async {
    final response = await _send(
      () => http.post(
        _uri(path),
        headers: _headers(skipAuth: skipAuth),
        body: jsonEncode(body),
      ),
    );
    return Map<String, dynamic>.from(_decode(response) as Map);
  }

  Future<Map<String, dynamic>> putMap(
    String path,
    Map<String, dynamic> body,
  ) async {
    final response = await _send(
      () => http.put(
        _uri(path),
        headers: _headers(),
        body: jsonEncode(body),
      ),
    );
    return Map<String, dynamic>.from(_decode(response) as Map);
  }

  Uri _uri(String path) => Uri.parse('$_baseUrl$path');

  Map<String, String> _headers({bool skipAuth = false}) {
    return {
      'Content-Type': 'application/json',
      if (!skipAuth && _token != null) 'Authorization': 'Bearer $_token',
    };
  }

  dynamic _decode(http.Response response) {
    final text = response.body.trim();
    final body = text.isEmpty ? null : jsonDecode(text);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      if (body is Map && body['message'] != null) {
        throw ApiException(body['message'].toString());
      }
      throw ApiException(
        '${response.statusCode} ${response.reasonPhrase ?? 'Request failed'}',
      );
    }
    return body;
  }

  Future<http.Response> _send(Future<http.Response> Function() request) async {
    try {
      return await request();
    } on http.ClientException catch (error) {
      throw ApiException(
        'Cannot connect to API. Start the backend on $_baseUrl, then try again. ${error.message}',
      );
    }
  }

  static String _clean(String value) => value.trim().replaceAll(RegExp(r'/$'), '');
}
