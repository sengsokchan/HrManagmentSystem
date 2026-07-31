import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';

class AppConfig {
  static const _fromDefine = String.fromEnvironment('API_BASE_URL', defaultValue: '');

  static String get defaultApiUrl {
    if (_fromDefine.trim().isNotEmpty) {
      return _fromDefine.trim();
    }

    if (kIsWeb) {
      return 'http://127.0.0.1:5088';
    }

    if (Platform.isAndroid) {
      // Android emulator localhost bridge to host machine.
      return 'http://10.0.2.2:5088';
    }

    return 'http://127.0.0.1:5088';
  }
}
