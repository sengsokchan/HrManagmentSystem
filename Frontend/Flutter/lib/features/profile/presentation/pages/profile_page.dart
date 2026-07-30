import 'package:flutter/material.dart';

import '../../../../core/constants/app_colors.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../shared/widgets/section_card.dart';
import '../../../../shared/widgets/two_line_row.dart';
import '../../../auth/presentation/providers/hr_controller.dart';

class ProfilePage extends StatelessWidget {
  const ProfilePage({super.key, required this.controller});

  final HrController controller;

  @override
  Widget build(BuildContext context) {
    final user = controller.user;
    final s = controller.s;
    final latestPaid =
        controller.myPaidPayroll.isNotEmpty ? controller.myPaidPayroll.first : null;

    return ListView(
      children: [
        SectionCard(
          title: s.account,
          child: Column(
            children: [
              _InfoRow(label: s.name, value: user?.displayName ?? '-'),
              _InfoRow(label: s.email, value: user?.email ?? '-'),
              _InfoRow(label: s.role, value: user?.role ?? '-'),
              _InfoRow(
                label: s.employeeId,
                value: user?.employeeId?.toString() ?? '-',
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: s.payslips,
          child: latestPaid == null && controller.myPayroll.isEmpty
              ? Text(s.noPayslips)
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (latestPaid != null) ...[
                      Text(
                        s.latestPaid,
                        style: const TextStyle(
                          color: AppColors.muted,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        Formatters.money(latestPaid['netSalary']),
                        style: const TextStyle(
                          fontSize: 28,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        '${Formatters.date(latestPaid['periodStart'])} '
                        'to ${Formatters.date(latestPaid['periodEnd'])}',
                        style: const TextStyle(color: AppColors.muted),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${s.status}: ${latestPaid['status']}',
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          color: AppColors.teal,
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],
                    ...controller.myPayroll.take(5).map(
                          (item) => TwoLineRow(
                            title: Formatters.money(item['netSalary']),
                            subtitle:
                                '${Formatters.date(item['periodStart'])} to ${Formatters.date(item['periodEnd'])}',
                            trailing: item['status']?.toString(),
                          ),
                        ),
                  ],
                ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: s.appSection,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(s.appTagline),
              const SizedBox(height: 12),
              Text(s.language, style: const TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: [
                  ButtonSegment(value: 'en', label: Text(s.english)),
                  ButtonSegment(value: 'km', label: Text(s.khmer)),
                ],
                selected: {controller.localeController.code},
                onSelectionChanged: (values) {
                  controller.localeController.setLang(values.first);
                },
              ),
              const SizedBox(height: 12),
              TextField(
                controller: controller.apiUrl,
                decoration: InputDecoration(
                  labelText: s.apiUrl,
                  helperText: s.apiUrlHelp,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 20),
        FilledButton(
          onPressed: controller.signOut,
          style: FilledButton.styleFrom(
            backgroundColor: AppColors.danger,
            foregroundColor: Colors.white,
          ),
          child: Text(s.signOut),
        ),
      ],
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          SizedBox(
            width: 110,
            child: Text(label, style: const TextStyle(color: AppColors.muted)),
          ),
          Expanded(
            child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
          ),
        ],
      ),
    );
  }
}
