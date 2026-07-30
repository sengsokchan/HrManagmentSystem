class Formatters {
  static String today() {
    final now = DateTime.now();
    return _ymd(now);
  }

  static String date(dynamic value) {
    final parsed = _parse(value);
    if (parsed == null) return '-';
    return _ymd(parsed);
  }

  static String time(dynamic value) {
    final parsed = _parse(value);
    if (parsed == null) return '-';
    final hour = parsed.hour.toString().padLeft(2, '0');
    final minute = parsed.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }

  static String money(dynamic value) {
    final number = value is num
        ? value.toDouble()
        : double.tryParse(value?.toString() ?? '');
    if (number == null) return '-';
    return '\$${number.toStringAsFixed(2)}';
  }

  static DateTime? _parse(dynamic value) {
    if (value == null) return null;
    if (value is DateTime) return value;
    return DateTime.tryParse(value.toString());
  }

  static String _ymd(DateTime value) {
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '${value.year}-$month-$day';
  }
}
