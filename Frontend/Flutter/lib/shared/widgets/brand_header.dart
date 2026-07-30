import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';
import '../../core/l10n/app_strings.dart';

class BrandHeader extends StatelessWidget {
  const BrandHeader({super.key, this.strings});

  final AppStrings? strings;

  @override
  Widget build(BuildContext context) {
    final s = strings ?? AppStrings.en;
    return Row(
      children: [
        Container(
          width: 56,
          height: 56,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: AppColors.teal,
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Text(
            'HR',
            style: TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w800,
              fontSize: 22,
            ),
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              FittedBox(
                fit: BoxFit.scaleDown,
                alignment: Alignment.centerLeft,
                child: Text(
                  s.appName,
                  maxLines: 1,
                  style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
                ),
              ),
              Text(
                s.appTagline,
                style: const TextStyle(color: AppColors.muted, fontSize: 15),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
