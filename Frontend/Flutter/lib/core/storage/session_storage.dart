import 'dart:async';
import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../../features/auth/domain/entities/signed_in_user.dart';

class SessionStorage {
  static const _tokenKey = 'hr_token';
  static const _userKey = 'hr_user';

  String? token;
  SignedInUser? user;

  Future<void> load() async {
    final prefs = await SharedPreferences.getInstance();
    token = prefs.getString(_tokenKey);
    final raw = prefs.getString(_userKey);
    if (raw == null || raw.isEmpty) {
      user = null;
      return;
    }
    user = SignedInUser.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  void save(String nextToken, SignedInUser nextUser) {
    token = nextToken;
    user = nextUser;
    unawaited(_persist());
  }

  void clear() {
    token = null;
    user = null;
    unawaited(_clearPersist());
  }

  Future<void> _persist() async {
    final prefs = await SharedPreferences.getInstance();
    if (token != null && user != null) {
      await prefs.setString(_tokenKey, token!);
      await prefs.setString(_userKey, jsonEncode(user!.toJson()));
    }
  }

  Future<void> _clearPersist() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_userKey);
  }
}
