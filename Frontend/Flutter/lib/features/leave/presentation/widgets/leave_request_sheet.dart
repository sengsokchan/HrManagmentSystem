import 'package:flutter/material.dart';

import '../../../auth/presentation/providers/hr_controller.dart';

const _leaveTypes = [
  'Annual leave',
  'Sick leave',
  'Maternity leave',
  'Emergency leave',
  'Unpaid leave',
];

Future<void> showLeaveRequestSheet(
  BuildContext context,
  HrController controller,
) {
  final startDate = TextEditingController(
    text: DateTime.now().toIso8601String().substring(0, 10),
  );
  final endDate = TextEditingController(
    text: DateTime.now().toIso8601String().substring(0, 10),
  );
  final reason = TextEditingController();
  var leaveType = 'Annual leave';
  var isHalfDay = false;

  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          final s = controller.s;
          Map<String, dynamic>? balance;
          for (final item in controller.leaveBalances) {
            if (item['leaveType']?.toString() == leaveType) {
              balance = item;
              break;
            }
          }

          return Padding(
            padding: EdgeInsets.only(
              left: 20,
              right: 20,
              top: 20,
              bottom: MediaQuery.of(context).viewInsets.bottom + 20,
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  s.requestLeave,
                  style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 16),
                DropdownButtonFormField<String>(
                  // ignore: deprecated_member_use
                  value: leaveType,
                  decoration: InputDecoration(labelText: s.leaveType),
                  items: _leaveTypes
                      .map(
                        (type) => DropdownMenuItem(
                          value: type,
                          child: Text(type),
                        ),
                      )
                      .toList(),
                  onChanged: (value) {
                    if (value == null) return;
                    setState(() => leaveType = value);
                  },
                ),
                if (balance != null) ...[
                  const SizedBox(height: 8),
                  Text(
                    '${balance['remainingDays']} ${s.days} ${s.remaining} · ${balance['pendingDays']} ${s.pending}',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
                const SizedBox(height: 12),
                TextField(
                  controller: startDate,
                  decoration: InputDecoration(labelText: s.startDate),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: endDate,
                  decoration: InputDecoration(labelText: s.endDate),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: reason,
                  maxLines: 3,
                  decoration: InputDecoration(labelText: s.reason),
                ),
                const SizedBox(height: 8),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(s.halfDay),
                  value: isHalfDay,
                  onChanged: (value) => setState(() => isHalfDay = value),
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: controller.actionInProgress
                      ? null
                      : () async {
                          await controller.submitLeaveRequest(
                            leaveType: leaveType,
                            startDate: startDate.text.trim(),
                            endDate: endDate.text.trim(),
                            isHalfDay: isHalfDay,
                            reason: reason.text.trim(),
                          );
                          if (context.mounted) Navigator.of(context).pop();
                        },
                  child: Text(s.submitRequest),
                ),
              ],
            ),
          );
        },
      );
    },
  );
}
