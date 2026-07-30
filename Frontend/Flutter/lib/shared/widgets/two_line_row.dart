import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';

class TwoLineRow extends StatelessWidget {
  const TwoLineRow({
    super.key,
    required this.title,
    required this.subtitle,
    this.trailing,
    this.onTap,
  });

  final String title;
  final String subtitle;
  final Object? trailing;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final trailingWidget = _buildTrailing();
    final content = Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 2),
                Text(
                  subtitle,
                  style: const TextStyle(color: AppColors.muted, fontSize: 13),
                ),
              ],
            ),
          ),
          if (trailingWidget != null) ...[
            const SizedBox(width: 8),
            trailingWidget,
          ],
        ],
      ),
    );

    if (onTap == null) return content;
    return InkWell(onTap: onTap, child: content);
  }

  Widget? _buildTrailing() {
    if (trailing == null) return null;
    if (trailing is Widget) return trailing as Widget;
    return Text(
      trailing.toString(),
      style: const TextStyle(
        color: AppColors.muted,
        fontWeight: FontWeight.w600,
        fontSize: 12,
      ),
    );
  }
}
