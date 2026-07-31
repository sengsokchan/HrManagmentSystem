import 'package:flutter/material.dart';

import '../providers/hr_controller.dart';

class DemoUserButtons extends StatelessWidget {
  const DemoUserButtons({super.key, required this.controller});

  final HrController controller;

  @override
  Widget build(BuildContext context) {
    final s = controller.s;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          s.isKhmer ? 'ចូលសាកល្បងរហ័ស' : 'Quick demo sign-in',
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 10),
        FilledButton.tonal(
          onPressed: () => controller.useDemo('admin@hr.local', 'Admin@123'),
          child: Text(s.demoAdmin),
        ),
        const SizedBox(height: 8),
        OutlinedButton(
          onPressed: () =>
              controller.useDemo('manager@hr.local', 'Manager@123'),
          child: Text(s.demoManager),
        ),
        const SizedBox(height: 8),
        OutlinedButton(
          onPressed: () =>
              controller.useDemo('employee@hr.local', 'Employee@123'),
          child: Text(s.demoEmployee),
        ),
      ],
    );
  }
}
