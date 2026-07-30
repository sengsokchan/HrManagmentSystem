import 'package:flutter/material.dart';

import '../../../../core/constants/app_colors.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../shared/widgets/section_card.dart';
import '../../../auth/presentation/providers/hr_controller.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key, required this.controller});

  final HrController controller;

  @override
  Widget build(BuildContext context) {
    final user = controller.user;
    final s = controller.s;
    final today = controller.todayAttendance;
    final pendingMine = controller.myLeaveRequests
        .where((item) {
          final status = item['status']?.toString() ?? '';
          return status == 'Pending' || status == 'ManagerApproved';
        })
        .length;

    return ListView(
      children: [
        _GreetingCard(
          name: user?.displayName ?? s.demoEmployee,
          role: user?.role ?? '',
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: s.isKhmer ? 'ថ្ងៃនេះ' : 'Today',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                today == null
                    ? (s.isKhmer ? 'អ្នកមិនទាន់ចុះឈ្មោះចូលទេ។' : 'You have not checked in yet.')
                    : (s.isKhmer
                        ? 'ចុះឈ្មោះចូលនៅ ${Formatters.time(today['checkIn'])} (${today['status']})។'
                        : 'Checked in at ${Formatters.time(today['checkIn'])} (${today['status']}).'),
                style: const TextStyle(fontSize: 16),
              ),
              if (today?['checkOut'] != null) ...[
                const SizedBox(height: 6),
                Text(
                  s.isKhmer
                      ? 'ចុះឈ្មោះចេញនៅ ${Formatters.time(today!['checkOut'])}។'
                      : 'Checked out at ${Formatters.time(today!['checkOut'])}.',
                ),
              ],
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: controller.actionInProgress || !controller.canCheckIn
                          ? null
                          : controller.checkIn,
                      icon: const Icon(Icons.login),
                      label: Text(s.checkIn),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: controller.actionInProgress || !controller.canCheckOut
                          ? null
                          : controller.checkOut,
                      icon: const Icon(Icons.logout),
                      label: Text(s.checkOut),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: s.myLeave,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                pendingMine == 0
                    ? s.noPendingMine
                    : (s.isKhmer
                        ? '$pendingMine សំណើកំពុងរង់ចាំអនុម័ត។'
                        : '$pendingMine request(s) waiting for approval.'),
              ),
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: () => controller.selectView('Leave'),
                child: Text(s.viewLeave),
              ),
            ],
          ),
        ),
        if (controller.canApproveLeave) ...[
          const SizedBox(height: 16),
          SectionCard(
            title: s.isKhmer ? 'អនុម័តក្រុម' : 'Team approvals',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  controller.pendingApprovals.isEmpty
                      ? s.noPendingLeave
                      : (s.isKhmer
                          ? '${controller.pendingApprovals.length} សំណើត្រូវពិនិត្យ។'
                          : '${controller.pendingApprovals.length} request(s) need your review.'),
                ),
                const SizedBox(height: 12),
                OutlinedButton(
                  onPressed: () => controller.selectView('Leave'),
                  child: Text(s.isKhmer ? 'ពិនិត្យអនុម័ត' : 'Review approvals'),
                ),
              ],
            ),
          ),
        ],
        if (user?.isAdmin == true) ...[
          const SizedBox(height: 16),
          SectionCard(
            title: s.isKhmer ? 'សម្គាល់អ្នកគ្រប់គ្រង' : 'Admin note',
            child: Text(
              s.isKhmer
                  ? 'ការគ្រប់គ្រង HR ពេញលេញមាននៅលើគេហទំព័រ។ កម្មវិធីទូរស័ព្ទនេះផ្តោតលើការងារប្រចាំថ្ងៃរបស់បុគ្គលិក។'
                  : 'Full HR administration is available in the web portal. This mobile app focuses on everyday employee tasks.',
              style: const TextStyle(color: AppColors.muted),
            ),
          ),
        ],
      ],
    );
  }
}

class _GreetingCard extends StatelessWidget {
  const _GreetingCard({required this.name, required this.role});

  final String name;
  final String role;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: AppColors.teal,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            name,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 22,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            role,
            style: const TextStyle(color: Colors.white70, fontSize: 14),
          ),
        ],
      ),
    );
  }
}
