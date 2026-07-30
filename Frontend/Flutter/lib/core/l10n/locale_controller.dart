import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app_strings.dart';

const _langKey = 'hr_lang';

class LocaleController extends ChangeNotifier {
  LocaleController();

  String _code = 'en';
  bool _ready = false;

  bool get ready => _ready;
  String get code => _code;
  AppStrings get strings => AppStrings.of(_code);
  bool get isKhmer => _code == 'km';

  Future<void> load() async {
    final prefs = await SharedPreferences.getInstance();
    _code = prefs.getString(_langKey) == 'km' ? 'km' : 'en';
    _ready = true;
    notifyListeners();
  }

  Future<void> setLang(String code) async {
    final next = code == 'km' ? 'km' : 'en';
    if (next == _code) return;
    _code = next;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_langKey, _code);
    notifyListeners();
  }

  Future<void> toggle() => setLang(isKhmer ? 'en' : 'km');
}
