class AppStrings {
  const AppStrings._(this.code);

  final String code;

  static const en = AppStrings._('en');
  static const km = AppStrings._('km');

  static AppStrings of(String code) => code == 'km' ? km : en;

  bool get isKhmer => code == 'km';

  String get appName => _t('HR Employee', 'បុគ្គលិក HR');
  String get appTagline =>
      _t('Check in, request leave, and track your work day.', 'ចុះវត្តមាន ស្នើច្បាប់ឈប់សម្រាក និងតាមដានថ្ងៃធ្វើការ។');

  String get workEmail => _t('Work email', 'អ៊ីមែលការងារ');
  String get password => _t('Password', 'ពាក្យសម្ងាត់');
  String get signIn => _t('Sign in', 'ចូលប្រើ');
  String get signingIn => _t('Signing in...', 'កំពុងចូល...');
  String get advancedSettings => _t('Advanced settings', 'ការកំណត់កម្រិតខ្ពស់');
  String get hideAdvanced => _t('Hide advanced settings', 'លាក់ការកំណត់កម្រិតខ្ពស់');
  String get apiUrl => _t('API URL', 'URL API');
  String get language => _t('Language', 'ភាសា');
  String get english => _t('English', 'អង់គ្លេស');
  String get khmer => _t('Khmer', 'ខ្មែរ');

  String get home => _t('Home', 'ទំព័រដើម');
  String get attendance => _t('Attendance', 'វត្តមាន');
  String get leave => _t('Leave', 'ច្បាប់ឈប់សម្រាក');
  String get profile => _t('Profile', 'ប្រវត្តិរូប');
  String get dismiss => _t('Dismiss', 'បិទ');
  String get refresh => _t('Refresh', 'ផ្ទុកឡើងវិញ');

  String get homeSubtitle =>
      _t('Your personal workplace overview for today.', 'ទិដ្ឋភាពការងារផ្ទាល់ខ្លួនសម្រាប់ថ្ងៃនេះ។');
  String get attendanceSubtitle =>
      _t('Check in, check out, and review your attendance history.', 'ចុះឈ្មោះចូល ចេញ និងមើលប្រវត្តិវត្តមាន។');
  String get leaveSubtitle =>
      _t('Balances, requests, and approval status.', 'សមតុល្យ សំណើ និងស្ថានភាពអនុម័ត។');
  String get profileSubtitle =>
      _t('Your account details and sign-out.', 'ព័ត៌មានគណនី និងចាកចេញ។');

  String get checkIn => _t('Check in', 'ចុះឈ្មោះចូល');
  String get checkOut => _t('Check out', 'ចុះឈ្មោះចេញ');
  String get workMode => _t('Work mode', 'របៀបធ្វើការ');
  String get requestLeave => _t('Request leave', 'ស្នើច្បាប់ឈប់សម្រាក');
  String get myBalances => _t('My balances', 'សមតុល្យរបស់ខ្ញុំ');
  String get myRequests => _t('My requests', 'សំណើរបស់ខ្ញុំ');
  String get needsApproval => _t('Needs your approval', 'ត្រូវការអនុម័តរបស់អ្នក');
  String get noPendingLeave => _t('No pending team leave requests.', 'មិនមានសំណើរង់ចាំអនុម័ត។');
  String get noMyLeave => _t('You have not submitted any leave requests yet.', 'អ្នកមិនទាន់ដាក់ស្នើច្បាប់ឈប់សម្រាកទេ។');
  String get noBalances => _t('No leave balances available yet.', 'មិនទាន់មានសមតុល្យច្បាប់ឈប់សម្រាក។');
  String get approve => _t('Approve', 'អនុម័ត');
  String get reject => _t('Reject', 'បដិសេធ');
  String get leaveType => _t('Leave type', 'ប្រភេទច្បាប់');
  String get startDate => _t('Start date (YYYY-MM-DD)', 'ថ្ងៃចាប់ផ្តើម (YYYY-MM-DD)');
  String get endDate => _t('End date (YYYY-MM-DD)', 'ថ្ងៃបញ្ចប់ (YYYY-MM-DD)');
  String get reason => _t('Reason', 'មូលហេតុ');
  String get halfDay => _t('Half day', 'កន្លះថ្ងៃ');
  String get submitRequest => _t('Submit request', 'ដាក់ស្នើ');
  String get remaining => _t('remaining', 'នៅសល់');
  String get pending => _t('pending', 'កំពុងរង់ចាំ');
  String get days => _t('day(s)', 'ថ្ងៃ');

  String get account => _t('Account', 'គណនី');
  String get name => _t('Name', 'ឈ្មោះ');
  String get email => _t('Email', 'អ៊ីមែល');
  String get role => _t('Role', 'តួនាទី');
  String get employeeId => _t('Employee ID', 'អត្តលេខបុគ្គលិក');
  String get payslips => _t('Payslips', 'ប័ណ្ណប្រាក់ខែ');
  String get latestPaid => _t('Latest paid', 'បានបង់ថ្មីបំផុត');
  String get status => _t('Status', 'ស្ថានភាព');
  String get noPayslips =>
      _t('No payslips yet. Paid payroll will appear here.', 'មិនទាន់មានប័ណ្ណប្រាក់ខែ។ ប្រាក់ខែដែលបានបង់នឹងបង្ហាញនៅទីនេះ។');
  String get appSection => _t('App', 'កម្មវិធី');
  String get apiUrlHelp =>
      _t('Change only when connecting to another backend.', 'ផ្លាស់ប្តូរតែពេលភ្ជាប់ទៅម៉ាស៊ីនមេផ្សេង។');
  String get signOut => _t('Sign out', 'ចាកចេញ');
  String get myLeave => _t('My leave', 'ច្បាប់ឈប់សម្រាករបស់ខ្ញុំ');
  String get viewLeave => _t('View leave requests', 'មើលសំណើច្បាប់ឈប់សម្រាក');
  String get noPendingMine => _t('No pending leave requests.', 'មិនមានសំណើរង់ចាំ។');

  String get checkedIn => _t('Checked in successfully.', 'ចុះឈ្មោះចូលបានជោគជ័យ។');
  String get checkedOut => _t('Checked out successfully.', 'ចុះឈ្មោះចេញបានជោគជ័យ។');
  String get leaveSubmitted => _t('Leave request submitted.', 'បានដាក់ស្នើច្បាប់ឈប់សម្រាក។');
  String leaveDecided(String decision) => decision == 'approve'
      ? _t('Leave approved.', 'បានអនុម័តច្បាប់ឈប់សម្រាក។')
      : _t('Leave rejected.', 'បានបដិសេធច្បាប់ឈប់សម្រាក។');

  String get demoEmployee => _t('Employee', 'បុគ្គលិក');
  String get demoManager => _t('Manager', 'អ្នកគ្រប់គ្រង');
  String get demoAdmin => _t('HR Admin', 'អ្នកគ្រប់គ្រង HR');

  String _t(String english, String khmer) => isKhmer ? khmer : english;
}
