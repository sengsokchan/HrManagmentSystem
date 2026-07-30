import 'dart:async';

import 'package:flutter/material.dart';

import '../../../../core/config/app_config.dart';
import '../../../../core/l10n/app_strings.dart';
import '../../../../core/l10n/locale_controller.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/storage/session_storage.dart';
import '../../../../core/utils/formatters.dart';
import '../../domain/entities/signed_in_user.dart';
import '../../domain/usecases/sign_in_usecase.dart';

class HrController extends ChangeNotifier {
  HrController({
    required this.apiClient,
    required this.signInUseCase,
    required this.sessionStorage,
    required this.localeController,
  }) {
    localeController.addListener(notifyListeners);
  }

  final ApiClient apiClient;
  final SignInUseCase signInUseCase;
  final SessionStorage sessionStorage;
  final LocaleController localeController;

  final apiUrl = TextEditingController(text: AppConfig.defaultApiUrl);
  final email = TextEditingController(text: 'employee@hr.local');
  final password = TextEditingController(text: 'Employee@123');

  String activeView = 'Home';
  String message = '';
  bool signingIn = false;
  bool loading = false;
  bool actionInProgress = false;
  String workMode = 'Office';

  List<Map<String, dynamic>> attendance = [];
  List<Map<String, dynamic>> leaveRequests = [];
  List<Map<String, dynamic>> leaveBalances = [];
  List<Map<String, dynamic>> myPayroll = [];

  String? get token => sessionStorage.token;
  SignedInUser? get user => sessionStorage.user;
  bool get signedIn => token != null && user != null;
  AppStrings get s => localeController.strings;

  List<String> get views => const ['Home', 'Attendance', 'Leave', 'Profile'];

  String viewLabel(String view) => switch (view) {
        'Attendance' => s.attendance,
        'Leave' => s.leave,
        'Profile' => s.profile,
        _ => s.home,
      };

  String get activeViewLabel => viewLabel(activeView);

  String get subtitle {
    return switch (activeView) {
      'Attendance' => s.attendanceSubtitle,
      'Leave' => s.leaveSubtitle,
      'Profile' => s.profileSubtitle,
      _ => s.homeSubtitle,
    };
  }

  List<Map<String, dynamic>> get myAttendance {
    final employeeId = user?.employeeId;
    if (employeeId == null) return attendance;
    return attendance.where((item) => item['employeeId'] == employeeId).toList();
  }

  List<Map<String, dynamic>> get myLeaveRequests {
    final employeeId = user?.employeeId;
    if (employeeId == null) return leaveRequests;
    return leaveRequests.where((item) => item['employeeId'] == employeeId).toList();
  }

  List<Map<String, dynamic>> get pendingApprovals {
    return leaveRequests.where(canDecideLeave).toList();
  }

  Map<String, dynamic>? get todayAttendance {
    final today = Formatters.today();
    for (final record in myAttendance) {
      if (Formatters.date(record['workDate']) == today) {
        return record;
      }
    }
    return null;
  }

  bool get canCheckIn => todayAttendance == null;

  bool get canCheckOut {
    final record = todayAttendance;
    return record != null &&
        record['checkIn'] != null &&
        record['checkOut'] == null;
  }

  bool get canApproveLeave =>
      can('leave.approve.manager') || can('leave.approve.hr');

  bool can(String permission) => user?.can(permission) ?? false;

  bool canDecideLeave(Map<String, dynamic> leave) {
    final status = leave['status']?.toString() ?? '';
    if (user?.isManager == true && status == 'Pending') return true;
    if (can('leave.approve.hr') &&
        (status == 'Pending' || status == 'ManagerApproved')) {
      return true;
    }
    return false;
  }

  Future<void> signIn() async {
    if (signingIn) return;

    signingIn = true;
    message = '';
    _configureApi();
    notifyListeners();

    final startedAt = DateTime.now();
    try {
      final response = await signInUseCase(email.text.trim(), password.text);
      final elapsed = DateTime.now().difference(startedAt);
      if (elapsed < const Duration(milliseconds: 500)) {
        await Future<void>.delayed(const Duration(milliseconds: 500) - elapsed);
      }

      sessionStorage.save(response.token, response.user);
      activeView = 'Home';
      _configureApi();
      signingIn = false;
      notifyListeners();
      unawaited(loadWorkspace());
    } catch (error) {
      message = error.toString();
      signingIn = false;
      notifyListeners();
    }
  }

  Future<void> loadWorkspace() async {
    if (!signedIn) return;

    loading = true;
    message = '';
    notifyListeners();

    try {
      final attendanceResult = await apiClient.getList('/api/attendance');
      final leaveResult = await apiClient.getList('/api/leave-requests');
      final balanceResult = user?.employeeId == null
          ? <Map<String, dynamic>>[]
          : await apiClient.getList(
              '/api/leave-balances?employeeId=${user!.employeeId}&year=${DateTime.now().year}',
            );
      final payrollResult = await apiClient.getList('/api/payroll');

      attendance = attendanceResult;
      leaveRequests = leaveResult;
      leaveBalances = balanceResult;
      myPayroll = _filterMyPayroll(payrollResult);
    } catch (error) {
      message = error.toString();
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> checkIn() async {
    await _runAttendanceAction(
      () => apiClient.postMap('/api/attendance/check-in', {
        'employeeId': user?.employeeId,
        'latitude': 11.5564,
        'longitude': 104.9282,
        'workMode': workMode,
      }),
      s.checkedIn,
    );
  }

  Future<void> checkOut() async {
    await _runAttendanceAction(
      () => apiClient.postMap('/api/attendance/check-out', {
        'employeeId': user?.employeeId,
        'latitude': 11.5564,
        'longitude': 104.9282,
        'workMode': workMode,
      }),
      s.checkedOut,
    );
  }

  Future<void> submitLeaveRequest({
    required String leaveType,
    required String startDate,
    required String endDate,
    required bool isHalfDay,
    required String reason,
  }) async {
    if (actionInProgress) return;

    actionInProgress = true;
    message = '';
    notifyListeners();

    try {
      await apiClient.postMap('/api/leave-requests', {
        'employeeId': user?.employeeId,
        'leaveType': leaveType,
        'startDate': startDate,
        'endDate': endDate,
        'isHalfDay': isHalfDay,
        'reason': reason,
        'attachmentUrl': null,
      });
      message = s.leaveSubmitted;
      await loadWorkspace();
    } catch (error) {
      message = error.toString();
    } finally {
      actionInProgress = false;
      notifyListeners();
    }
  }

  Future<void> decideLeave(
    Map<String, dynamic> leave,
    String decision,
  ) async {
    if (actionInProgress) return;

    final id = leave['id'];
    if (id == null) return;

    actionInProgress = true;
    message = '';
    notifyListeners();

    try {
      await apiClient.putMap(
        '/api/leave-requests/$id/decision',
        {
          'decision': decision,
          'comment': decision == 'approve'
              ? 'Approved from mobile app'
              : 'Rejected from mobile app',
        },
      );
      message = s.leaveDecided(decision);
      await loadWorkspace();
    } catch (error) {
      message = error.toString();
    } finally {
      actionInProgress = false;
      notifyListeners();
    }
  }

  Future<void> _runAttendanceAction(
    Future<Map<String, dynamic>> Function() request,
    String successMessage,
  ) async {
    if (actionInProgress) return;

    actionInProgress = true;
    message = '';
    notifyListeners();

    try {
      await request();
      message = successMessage;
      await loadWorkspace();
    } catch (error) {
      message = error.toString();
    } finally {
      actionInProgress = false;
      notifyListeners();
    }
  }

  void setWorkMode(String mode) {
    workMode = mode;
    notifyListeners();
  }

  void clearMessage() {
    message = '';
    notifyListeners();
  }

  void selectView(String view) {
    activeView = view;
    notifyListeners();
  }

  void signOut() {
    sessionStorage.clear();
    attendance = [];
    leaveRequests = [];
    leaveBalances = [];
    myPayroll = [];
    activeView = 'Home';
    message = '';
    notifyListeners();
  }

  void useDemo(String nextEmail, String nextPassword) {
    email.text = nextEmail;
    password.text = nextPassword;
    notifyListeners();
  }

  List<Map<String, dynamic>> get myPaidPayroll {
    return myPayroll
        .where((item) => (item['status']?.toString() ?? '') == 'Paid')
        .toList();
  }

  List<Map<String, dynamic>> _filterMyPayroll(
    List<Map<String, dynamic>> payroll,
  ) {
    final employeeId = user?.employeeId;
    if (employeeId == null) return const [];
    final mine = payroll.where((item) => item['employeeId'] == employeeId).toList();
    mine.sort((a, b) {
      final aEnd = a['periodEnd']?.toString() ?? '';
      final bEnd = b['periodEnd']?.toString() ?? '';
      return bEnd.compareTo(aEnd);
    });
    return mine;
  }

  @override
  void dispose() {
    localeController.removeListener(notifyListeners);
    apiUrl.dispose();
    email.dispose();
    password.dispose();
    super.dispose();
  }

  void _configureApi() {
    apiClient.configure(baseUrl: apiUrl.text, token: token);
  }
}
