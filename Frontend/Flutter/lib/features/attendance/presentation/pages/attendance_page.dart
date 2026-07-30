import 'package:flutter/material.dart';

import '../../../../core/utils/formatters.dart';
import '../../../../shared/widgets/section_card.dart';
import '../../../../shared/widgets/two_line_row.dart';
import '../../../auth/presentation/providers/hr_controller.dart';

class AttendancePage extends StatelessWidget {
  const AttendancePage({super.key, required this.controller});

  final HrController controller;

  @override
  Widget build(BuildContext context) {
    final today = controller.todayAttendance;
    final s = controller.s;

    return ListView(
      children: [
        SectionCard(
          title: s.workMode,
          child: SegmentedButton<String>(
            segments: [
              ButtonSegment(
                value: 'Office',
                label: Text(s.isKhmer ? 'ការិយាល័យ' : 'Office'),
              ),
              ButtonSegment(
                value: 'Remote',
                label: Text(s.isKhmer ? 'ពីចម្ងាយ' : 'Remote'),
              ),
              ButtonSegment(
                value: 'Field',
                label: Text(s.isKhmer ? 'ទីវាល' : 'Field'),
              ),
            ],
            selected: {controller.workMode},
            onSelectionChanged: (selection) {
              controller.setWorkMode(selection.first);
            },
          ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: s.isKhmer ? 'ថ្ងៃនេះ' : 'Today',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                today == null
                    ? (s.isKhmer
                        ? 'មិនទាន់មានវត្តមានសម្រាប់ថ្ងៃនេះ។'
                        : 'No attendance recorded for today.')
                    : (s.isKhmer
                        ? 'ចូល ${Formatters.time(today['checkIn'])} · ចេញ ${Formatters.time(today['checkOut'])} · ${today['status']}'
                        : 'In ${Formatters.time(today['checkIn'])} · Out ${Formatters.time(today['checkOut'])} · ${today['status']}'),
              ),
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                    child: FilledButton(
                      onPressed: controller.actionInProgress || !controller.canCheckIn
                          ? null
                          : controller.checkIn,
                      child: Text(s.checkIn),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: controller.actionInProgress || !controller.canCheckOut
                          ? null
                          : controller.checkOut,
                      child: Text(s.checkOut),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        SectionCard(
          title: s.isKhmer ? 'ប្រវត្តិរបស់ខ្ញុំ' : 'My history',
          child: controller.myAttendance.isEmpty
              ? Text(s.isKhmer ? 'រកមិនឃើញកំណត់ត្រាវត្តមាន។' : 'No attendance records found.')
              : Column(
                  children: controller.myAttendance
                      .map(
                        (item) => TwoLineRow(
                          title: Formatters.date(item['workDate']),
                          subtitle:
                              '${Formatters.time(item['checkIn'])} - ${Formatters.time(item['checkOut'])}',
                          trailing: item['status']?.toString(),
                        ),
                      )
                      .toList(),
                ),
        ),
      ],
    );
  }
}
