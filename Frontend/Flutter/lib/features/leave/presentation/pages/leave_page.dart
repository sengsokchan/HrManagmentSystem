import 'package:flutter/material.dart';

import '../../../../core/utils/formatters.dart';
import '../../../../shared/widgets/section_card.dart';
import '../../../../shared/widgets/two_line_row.dart';
import '../../../auth/presentation/providers/hr_controller.dart';
import '../widgets/leave_request_sheet.dart';

class LeavePage extends StatelessWidget {
  const LeavePage({super.key, required this.controller});

  final HrController controller;

  @override
  Widget build(BuildContext context) {
    final s = controller.s;
    return Stack(
      children: [
        ListView(
          children: [
            SectionCard(
              title: '${s.myBalances} · ${DateTime.now().year}',
              child: controller.leaveBalances.isEmpty
                  ? Text(s.noBalances)
                  : Column(
                      children: controller.leaveBalances
                          .map(
                            (balance) => Padding(
                              padding: const EdgeInsets.only(bottom: 10),
                              child: TwoLineRow(
                                title: balance['leaveType']?.toString() ?? '-',
                                subtitle:
                                    '${s.isKhmer ? 'បានប្រើ' : 'Used'} ${balance['usedDays']} · ${s.pending} ${balance['pendingDays']} · ${s.isKhmer ? 'មានសិទ្ធិ' : 'Entitled'} ${balance['entitledDays']}',
                                trailing: '${balance['remainingDays']} ${s.remaining}',
                              ),
                            ),
                          )
                          .toList(),
                    ),
            ),
            const SizedBox(height: 16),
            if (controller.canApproveLeave) ...[
              SectionCard(
                title: s.needsApproval,
                child: controller.pendingApprovals.isEmpty
                    ? Text(s.noPendingLeave)
                    : Column(
                        children: controller.pendingApprovals
                            .map((leave) => _ApprovalTile(
                                  leave: leave,
                                  controller: controller,
                                ))
                            .toList(),
                      ),
              ),
              const SizedBox(height: 16),
            ],
            SectionCard(
              title: s.myRequests,
              child: controller.myLeaveRequests.isEmpty
                  ? Text(s.noMyLeave)
                  : Column(
                      children: controller.myLeaveRequests
                          .map(
                            (leave) => TwoLineRow(
                              title: leave['leaveType']?.toString() ?? '-',
                              subtitle:
                                  '${Formatters.date(leave['startDate'])} to ${Formatters.date(leave['endDate'])} · ${leave['days'] ?? ''} ${s.days} · ${leave['reason'] ?? ''}',
                              trailing: leave['status']?.toString(),
                            ),
                          )
                          .toList(),
                    ),
            ),
            const SizedBox(height: 80),
          ],
        ),
        Positioned(
          right: 20,
          bottom: 20,
          child: FloatingActionButton.extended(
            onPressed: controller.actionInProgress
                ? null
                : () => showLeaveRequestSheet(context, controller),
            icon: const Icon(Icons.add),
            label: Text(s.requestLeave),
          ),
        ),
      ],
    );
  }
}

class _ApprovalTile extends StatelessWidget {
  const _ApprovalTile({
    required this.leave,
    required this.controller,
  });

  final Map<String, dynamic> leave;
  final HrController controller;

  @override
  Widget build(BuildContext context) {
    final s = controller.s;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TwoLineRow(
            title: '${leave['employeeName'] ?? '-'} · ${leave['leaveType'] ?? '-'}',
            subtitle:
                '${Formatters.date(leave['startDate'])} to ${Formatters.date(leave['endDate'])} · ${leave['days'] ?? ''} ${s.days}',
            trailing: leave['status']?.toString(),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: controller.actionInProgress
                      ? null
                      : () => controller.decideLeave(leave, 'reject'),
                  child: Text(s.reject),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: FilledButton(
                  onPressed: controller.actionInProgress
                      ? null
                      : () => controller.decideLeave(leave, 'approve'),
                  child: Text(s.approve),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
