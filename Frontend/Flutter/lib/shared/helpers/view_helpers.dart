import 'package:flutter/material.dart';

class ViewHelpers {
  static IconData iconFor(String view) => switch (view) {
        'Attendance' => Icons.schedule_outlined,
        'Leave' => Icons.beach_access_outlined,
        'Profile' => Icons.person_outline,
        _ => Icons.home_outlined,
      };
}
