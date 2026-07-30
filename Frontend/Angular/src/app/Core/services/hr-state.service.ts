import { Injectable, isDevMode, signal } from '@angular/core';
import { Router } from '@angular/router';
import { I18nService } from '../i18n/i18n.service';
import {
  AttendanceView,
  Branch,
  ConfirmRequest,
  DashboardView,
  Department,
  EmployeeCreateResult,
  EmployeeForm,
  EmployeeFormErrors,
  EmployeeView,
  IssuedCredentials,
  LeaveForm,
  LeaveFormErrors,
  LeaveBalanceView,
  LeaveView,
  ListQuery,
  LoginResponse,
  PagedResult,
  PasswordResetResult,
  PayrollView,
  Position,
  RoleView,
  SignedInUser,
  ToastItem,
  ToastSeverity,
  ViewName
} from '../models/hr.models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class HrStateService {
  renderVersion = signal(0);

  token = localStorage.getItem('hr_token');
  user: SignedInUser | null = JSON.parse(localStorage.getItem('hr_user') || 'null') as SignedInUser | null;

  loginEmail = '';
  loginPassword = '';
  loginError = '';
  loginInProgress = false;

  forgotEmail = '';
  forgotEmployeeCode = '';
  forgotError = '';
  forgotInProgress = false;
  forgotResult: PasswordResetResult | null = null;
  forgotCopied = false;

  changeCurrentPassword = '';
  changeNewPassword = '';
  changeConfirmPassword = '';
  changePasswordError = '';
  changePasswordInProgress = false;
  issuedCredentials: IssuedCredentials | null = null;
  credentialsCopied = false;

  toasts: ToastItem[] = [];
  private toastSeq = 0;
  private toastTimers = new Map<number, number>();

  confirmDialog: ConfirmRequest | null = null;

  currentView: ViewName = 'dashboard';
  viewLoading = false;
  viewError = '';
  refreshing = false;
  saveInProgress = false;
  mobileNavOpen = false;

  viewMeta: Record<ViewName, [string, string]> = {
    dashboard: ['Dashboard', 'Operational overview for today.'],
    employees: ['Employees', 'Profiles, departments, branches, contracts, and status.'],
    attendance: ['Attendance', 'Daily check-in, check-out, late, overtime, and work mode.'],
    leave: ['Leave', 'Requests, balances, and approval workflow.'],
    payroll: ['Payroll', 'Generate runs, approve, and mark paid.'],
    reports: ['Reports', 'Filter by category and export attendance, leave, payroll, or employees to Excel.'],
    settings: ['Settings', 'Roles, permissions, and access profiles.']
  };

  departments: Department[] = [];
  positions: Position[] = [];
  branches: Branch[] = [];
  employees: EmployeeView[] = [];
  employeeTotal = 0;
  attendance: AttendanceView[] = [];
  attendanceTotal = 0;
  leaveRequests: LeaveView[] = [];
  leaveTotal = 0;
  leaveBalances: LeaveBalanceView[] = [];
  leaveBalanceEmployeeId: number | null = null;
  leaveBalanceYear = new Date().getFullYear();
  leaveBalanceEditType = 'Annual leave';
  leaveBalanceEditDays: number | null = 18;
  leaveBalanceSaving = false;
  roles: RoleView[] = [];
  payroll: PayrollView[] = [];
  payrollTotal = 0;
  payrollGenerating = false;
  payrollPeriodStart = '';
  payrollPeriodEnd = '';
  payrollActionId: number | null = null;
  dashboard: DashboardView | null = null;
  allEmployeesForSelect: EmployeeView[] = [];

  employeeQuery: ListQuery = { page: 1, pageSize: 10, search: '', status: '' };
  attendanceQuery: ListQuery = { page: 1, pageSize: 10, search: '', status: '', from: '', to: '' };
  leaveQuery: ListQuery = { page: 1, pageSize: 10, search: '', status: '', from: '', to: '' };
  payrollQuery: ListQuery = { page: 1, pageSize: 10, search: '', status: '' };

  workMode = 'Office';
  showEmployeeDialog = false;
  showLeaveDialog = false;
  employeeFormErrors: EmployeeFormErrors = {};
  leaveFormErrors: LeaveFormErrors = {};

  employeeForm: EmployeeForm = this.emptyEmployeeForm();
  leaveForm: LeaveForm = this.emptyLeaveForm();

  readonly isDev = isDevMode();

  constructor(
    private readonly api: ApiService,
    private readonly router: Router,
    private readonly i18n: I18nService
  ) {}

  get isAuthenticated(): boolean {
    return Boolean(this.token && this.user);
  }

  get mustChangePassword(): boolean {
    return Boolean(this.user?.mustChangePassword);
  }

  get title(): string {
    return this.i18n.t(`view.${this.currentView}.title`);
  }

  get subtitle(): string {
    return this.i18n.t(`view.${this.currentView}.sub`);
  }

  get pendingLeave(): LeaveView[] {
    return this.leaveRequests
      .filter((item) => item.status === 'Pending' || item.status === 'ManagerApproved')
      .slice(0, 5);
  }

  get leaveEmployees(): EmployeeView[] {
    if (this.can('leave.write') || this.can('employees.read')) {
      return this.allEmployeesForSelect;
    }

    return this.allEmployeesForSelect.filter((employee) => employee.id === this.user?.employeeId);
  }

  get employeePageCount(): number {
    return Math.max(1, Math.ceil(this.employeeTotal / this.employeeQuery.pageSize));
  }

  get attendancePageCount(): number {
    return Math.max(1, Math.ceil(this.attendanceTotal / this.attendanceQuery.pageSize));
  }

  get leavePageCount(): number {
    return Math.max(1, Math.ceil(this.leaveTotal / this.leaveQuery.pageSize));
  }

  get payrollPageCount(): number {
    return Math.max(1, Math.ceil(this.payrollTotal / this.payrollQuery.pageSize));
  }

  get todayAttendanceRecord(): AttendanceView | undefined {
    const today = this.today();
    return this.attendance.find(
      (item) => item.employeeId === this.user?.employeeId && item.workDate?.slice(0, 10) === today
    );
  }

  get alreadyCheckedIn(): boolean {
    return Boolean(this.todayAttendanceRecord?.checkIn);
  }

  get alreadyCheckedOut(): boolean {
    return Boolean(this.todayAttendanceRecord?.checkOut);
  }

  async init(): Promise<void> {
    if (!this.isAuthenticated) return;

    if (this.mustChangePassword) {
      await this.router.navigateByUrl('/change-password');
      return;
    }

    try {
      await this.loadReferenceData();
    } catch {
      // Session may be stale; interceptor will handle 401.
    }
  }

  async login(event?: Event): Promise<void> {
    event?.preventDefault();
    if (this.loginInProgress) return;

    this.loginInProgress = true;
    this.loginError = '';
    this.render();

    try {
      const [response] = await Promise.all([
        this.api.request<LoginResponse>('/api/auth/login', 'POST', {
          email: this.loginEmail.trim(),
          password: this.loginPassword
        }),
        this.wait(500)
      ]);

      this.token = response.token;
      this.user = {
        ...response.user,
        mustChangePassword: response.mustChangePassword ?? response.user.mustChangePassword ?? false
      };
      localStorage.setItem('hr_token', response.token);
      localStorage.setItem('hr_user', JSON.stringify(this.user));
      this.loginPassword = '';
      this.loginInProgress = false;
      this.render();

      if (this.mustChangePassword) {
        await this.router.navigateByUrl('/change-password');
        return;
      }

      this.currentView = 'dashboard';
      await this.router.navigateByUrl('/dashboard');
      void this.bootstrapAfterLogin();
    } catch (error) {
      this.loginError = this.errorMessage(error);
      this.loginInProgress = false;
      this.render();
    }
  }

  async submitForgotPassword(event?: Event): Promise<void> {
    event?.preventDefault();
    if (this.forgotInProgress) return;

    this.forgotError = '';
    this.forgotResult = null;
    this.forgotCopied = false;
    this.forgotInProgress = true;
    this.render();

    try {
      this.forgotResult = await this.api.request<PasswordResetResult>('/api/auth/forgot-password', 'POST', {
        email: this.forgotEmail.trim(),
        employeeCode: this.forgotEmployeeCode.trim()
      });
      this.notify('Temporary password created. Sign in and set a new passphrase.', 'success');
    } catch (error) {
      this.forgotError = this.errorMessage(error);
    } finally {
      this.forgotInProgress = false;
      this.render();
    }
  }

  async copyForgotPassword(): Promise<void> {
    if (!this.forgotResult) return;
    try {
      await navigator.clipboard.writeText(this.forgotResult.temporaryPassword);
      this.forgotCopied = true;
      this.notify('Temporary password copied.', 'info');
    } catch {
      this.notify('Could not copy automatically. Select and copy the password manually.', 'warning');
    }
    this.render();
  }

  clearForgotPasswordForm(): void {
    this.forgotError = '';
    this.forgotResult = null;
    this.forgotCopied = false;
    this.forgotEmployeeCode = '';
    this.render();
  }

  async submitChangePassword(event?: Event): Promise<void> {
    event?.preventDefault();
    if (this.changePasswordInProgress) return;

    this.changePasswordError = '';
    const current = this.changeCurrentPassword;
    const next = this.changeNewPassword.trim();
    const confirm = this.changeConfirmPassword.trim();

    if (next.length < 14) {
      this.changePasswordError =
        'Use a passphrase of at least 14 characters (for example River-Coffee-Moon-Train-84).';
      this.render();
      return;
    }
    if (next !== confirm) {
      this.changePasswordError = 'New passphrase and confirmation do not match.';
      this.render();
      return;
    }
    if (next === current.trim()) {
      this.changePasswordError = 'New passphrase must be different from the temporary password.';
      this.render();
      return;
    }

    this.changePasswordInProgress = true;
    this.render();

    try {
      await this.request<{ message: string }>('/api/auth/change-password', 'POST', {
        currentPassword: current,
        newPassword: next
      });

      if (this.user) {
        this.user = { ...this.user, mustChangePassword: false };
        localStorage.setItem('hr_user', JSON.stringify(this.user));
      }

      this.changeCurrentPassword = '';
      this.changeNewPassword = '';
      this.changeConfirmPassword = '';
      this.notify('Password updated. Welcome!', 'success');
      await this.router.navigateByUrl('/dashboard');
      void this.bootstrapAfterLogin();
    } catch (error) {
      this.changePasswordError = this.errorMessage(error);
    } finally {
      this.changePasswordInProgress = false;
      this.render();
    }
  }

  private async bootstrapAfterLogin(): Promise<void> {
    try {
      await this.loadReferenceData();
      await this.loadView('dashboard');
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    } finally {
      this.loginInProgress = false;
      this.render();
    }
  }

  logout(): void {
    this.clearSession();
    this.loginPassword = '';
    this.loginEmail = '';
    void this.router.navigateByUrl('/login');
  }

  handleUnauthorized(): void {
    this.clearSession();
    this.loginError = 'Your session expired. Please sign in again.';
    this.notify('Your session expired. Please sign in again.', 'warning');
  }

  private clearSession(): void {
    localStorage.removeItem('hr_token');
    localStorage.removeItem('hr_user');
    this.token = null;
    this.user = null;
    this.currentView = 'dashboard';
    this.showEmployeeDialog = false;
    this.showLeaveDialog = false;
    this.confirmDialog = null;
    this.viewError = '';
    this.mobileNavOpen = false;
    this.loadedViews.clear();
    this.render();
  }

  setDemoUser(email: string, password: string): void {
    if (!this.isDev) return;
    this.loginEmail = email;
    this.loginPassword = password;
    this.render();
  }

  private loadedViews = new Set<ViewName>();

  async onNavigate(view: ViewName): Promise<void> {
    const shouldLoad = this.currentView !== view || this.viewError || !this.loadedViews.has(view);
    this.currentView = view;
    this.mobileNavOpen = false;
    this.render();
    if (shouldLoad) {
      await this.loadView(view);
    }
  }

  async navigate(view: ViewName): Promise<void> {
    await this.router.navigateByUrl(`/${view}`);
  }

  toggleMobileNav(): void {
    this.mobileNavOpen = !this.mobileNavOpen;
    this.render();
  }

  closeMobileNav(): void {
    this.mobileNavOpen = false;
    this.render();
  }

  async refresh(): Promise<void> {
    this.refreshing = true;
    this.render();
    try {
      await this.loadView(this.currentView);
    } finally {
      this.refreshing = false;
      this.render();
    }
  }

  async retryView(): Promise<void> {
    await this.refresh();
  }

  async searchEmployees(): Promise<void> {
    this.employeeQuery.page = 1;
    await this.loadEmployees();
  }

  async changeEmployeePage(page: number): Promise<void> {
    this.employeeQuery.page = Math.min(Math.max(1, page), this.employeePageCount);
    await this.loadEmployees();
  }

  async changeAttendancePage(page: number): Promise<void> {
    this.attendanceQuery.page = Math.min(Math.max(1, page), this.attendancePageCount);
    await this.loadAttendance();
  }

  async changeLeavePage(page: number): Promise<void> {
    this.leaveQuery.page = Math.min(Math.max(1, page), this.leavePageCount);
    await this.loadLeave();
  }

  async changePayrollPage(page: number): Promise<void> {
    this.payrollQuery.page = Math.min(Math.max(1, page), this.payrollPageCount);
    await this.loadPayroll();
  }

  canManagePayroll(): boolean {
    return this.can('payroll.write') || this.can('employees.write') || this.user?.role === 'HR Admin';
  }

  initPayrollPeriodDefaults(): void {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), 1);
    const end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    this.payrollPeriodStart = start.toISOString().slice(0, 10);
    this.payrollPeriodEnd = end.toISOString().slice(0, 10);
  }

  async generatePayroll(): Promise<void> {
    if (!this.canManagePayroll() || this.payrollGenerating) return;
    if (!this.payrollPeriodStart || !this.payrollPeriodEnd) {
      this.notify('Choose a payroll period.', 'warning');
      return;
    }
    if (this.payrollPeriodEnd < this.payrollPeriodStart) {
      this.notify('Period end must be on or after start.', 'warning');
      return;
    }

    this.payrollGenerating = true;
    this.render();
    try {
      const result = await this.request<{ created: number; skipped: number }>(
        '/api/payroll/generate',
        'POST',
        {
          periodStart: this.payrollPeriodStart,
          periodEnd: this.payrollPeriodEnd
        }
      );
      this.notify(
        `Payroll generated: ${result.created} created, ${result.skipped} skipped.`,
        'success'
      );
      await this.loadPayroll();
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    } finally {
      this.payrollGenerating = false;
      this.render();
    }
  }

  async setPayrollStatus(id: number, status: 'Approved' | 'Paid'): Promise<void> {
    if (!this.canManagePayroll() || this.payrollActionId != null) return;

    const confirmed = await this.askConfirm({
      title: status === 'Paid' ? 'Mark payroll as paid' : 'Approve payroll',
      message:
        status === 'Paid'
          ? 'Mark this payroll run as Paid? Employees will see Paid status in the app.'
          : 'Approve this draft payroll run?',
      confirmLabel: status === 'Paid' ? 'Mark paid' : 'Approve'
    });
    if (!confirmed) return;

    this.payrollActionId = id;
    this.render();
    try {
      await this.request<PayrollView>(`/api/payroll/${id}/status`, 'PUT', { status });
      this.notify(`Payroll marked ${status}.`, 'success');
      await this.loadPayroll();
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    } finally {
      this.payrollActionId = null;
      this.render();
    }
  }

  async applyEmployeeFilters(): Promise<void> {
    this.employeeQuery.page = 1;
    await this.loadEmployees();
  }

  async openEmployeeDialog(employee?: EmployeeView): Promise<void> {
    if (!this.can('employees.write')) return;

    this.employeeFormErrors = {};
    this.employeeForm = employee
      ? {
          id: employee.id,
          fullName: employee.fullName,
          gender: employee.gender,
          dateOfBirth: employee.dateOfBirth?.slice(0, 10) ?? '',
          email: employee.email,
          phone: employee.phone,
          departmentId: employee.departmentId,
          positionId: employee.positionId,
          branchId: employee.branchId,
          managerId: employee.managerId,
          contractType: employee.contractType,
          joinDate: employee.joinDate?.slice(0, 10) ?? '',
          resignDate: employee.resignDate,
          status: employee.status,
          emergencyContact: employee.emergencyContact,
          educationHistory: employee.educationHistory,
          workExperience: employee.workExperience,
          basicSalary: employee.basicSalary ?? 500
        }
      : this.emptyEmployeeForm();

    this.showEmployeeDialog = true;
    this.render();
  }

  closeEmployeeDialog(): void {
    if (this.saveInProgress) return;
    this.showEmployeeDialog = false;
    this.employeeFormErrors = {};
    this.render();
  }

  async saveEmployee(): Promise<void> {
    if (this.saveInProgress) return;
    if (!this.validateEmployeeForm()) {
      this.render();
      return;
    }

    const id = this.employeeForm.id;
    const body = {
      fullName: this.employeeForm.fullName.trim(),
      gender: this.employeeForm.gender,
      dateOfBirth: this.employeeForm.dateOfBirth,
      email: this.employeeForm.email.trim(),
      phone: this.employeeForm.phone.trim(),
      departmentId: this.employeeForm.departmentId,
      positionId: this.employeeForm.positionId,
      branchId: this.employeeForm.branchId,
      managerId: this.employeeForm.managerId,
      contractType: this.employeeForm.contractType,
      joinDate: this.employeeForm.joinDate,
      resignDate: this.employeeForm.resignDate,
      status: this.employeeForm.status,
      emergencyContact: this.employeeForm.emergencyContact,
      educationHistory: this.employeeForm.educationHistory,
      workExperience: this.employeeForm.workExperience,
      basicSalary: this.employeeForm.basicSalary
    };

    this.saveInProgress = true;
    this.render();

    try {
      if (id) {
        await this.request<EmployeeView>(`/api/employees/${id}`, 'PUT', body);
        this.showEmployeeDialog = false;
        this.notify('Employee saved.', 'success');
      } else {
        const created = await this.request<EmployeeCreateResult | EmployeeView>('/api/employees', 'POST', body);
        this.showEmployeeDialog = false;

        const employee =
          created && typeof created === 'object' && 'employee' in created && created.employee
            ? created.employee
            : (created as EmployeeView);
        const loginEmail =
          created && typeof created === 'object' && 'loginEmail' in created && created.loginEmail
            ? created.loginEmail
            : employee?.email;
        const temporaryPassword =
          created && typeof created === 'object' && 'temporaryPassword' in created
            ? created.temporaryPassword
            : undefined;

        if (!employee?.fullName || !loginEmail || !temporaryPassword) {
          this.notify(
            'Employee may have been saved, but login credentials were not returned. Restart the API with the latest build, then create again.',
            'warning'
          );
        } else {
          this.issuedCredentials = {
            fullName: employee.fullName,
            loginEmail,
            temporaryPassword,
            reason: 'created'
          };
          this.credentialsCopied = false;
          this.notify('Employee created. Send the temporary password to the user.', 'success');
        }
      }
      await Promise.all([this.loadEmployees(), this.loadEmployeeSelectList()]);
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    } finally {
      this.saveInProgress = false;
      this.render();
    }
  }

  closeCredentialsDialog(): void {
    this.issuedCredentials = null;
    this.credentialsCopied = false;
    this.render();
  }

  async copyIssuedPassword(): Promise<void> {
    if (!this.issuedCredentials) return;
    try {
      await navigator.clipboard.writeText(this.issuedCredentials.temporaryPassword);
      this.credentialsCopied = true;
      this.notify('Temporary password copied.', 'info');
    } catch {
      this.notify('Could not copy automatically. Select and copy the password manually.', 'warning');
    }
    this.render();
  }

  async deactivateEmployee(id: number, name: string): Promise<void> {
    const confirmed = await this.askConfirm({
      title: 'Deactivate employee',
      message: `Deactivate ${name}? They will no longer appear as active.`,
      confirmLabel: 'Deactivate',
      danger: true
    });
    if (!confirmed) return;

    try {
      await this.request<void>(`/api/employees/${id}`, 'DELETE');
      this.notify('Employee deactivated.', 'success');
      await Promise.all([this.loadEmployees(), this.loadEmployeeSelectList()]);
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    }
  }

  async resetEmployeePassword(id: number, name: string): Promise<void> {
    const confirmed = await this.askConfirm({
      title: 'Reset password',
      message: `Reset the login password for ${name}? A new temporary password will be issued and they must change it on next sign-in.`,
      confirmLabel: 'Reset password',
      danger: true
    });
    if (!confirmed) return;

    try {
      const result = await this.request<PasswordResetResult>(`/api/employees/${id}/reset-password`, 'POST');
      this.issuedCredentials = {
        fullName: result.fullName,
        loginEmail: result.loginEmail,
        temporaryPassword: result.temporaryPassword,
        reason: 'reset'
      };
      this.credentialsCopied = false;
      this.notify('Password reset. Share the temporary password securely.', 'success');
      this.render();
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    }
  }

  async checkIn(): Promise<void> {
    if (this.alreadyCheckedIn) {
      this.notify('You are already checked in for today.', 'warning');
      return;
    }

    try {
      await this.postAttendance('/api/attendance/check-in');
      this.notify('Checked in.', 'success');
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    }
  }

  async checkOut(): Promise<void> {
    if (!this.alreadyCheckedIn) {
      this.notify('Check in before checking out.', 'warning');
      return;
    }
    if (this.alreadyCheckedOut) {
      this.notify('You are already checked out for today.', 'warning');
      return;
    }

    try {
      await this.postAttendance('/api/attendance/check-out');
      this.notify('Checked out.', 'success');
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    }
  }

  openLeaveDialog(): void {
    this.leaveFormErrors = {};
    this.leaveForm = this.emptyLeaveForm();
    this.showLeaveDialog = true;
    void this.loadLeaveBalances(this.leaveForm.employeeId);
    this.render();
  }

  closeLeaveDialog(): void {
    if (this.saveInProgress) return;
    this.showLeaveDialog = false;
    this.leaveFormErrors = {};
    this.render();
  }

  async onLeaveFormEmployeeChange(): Promise<void> {
    await this.loadLeaveBalances(this.leaveForm.employeeId);
  }

  estimatedLeaveDays(): number {
    const start = this.leaveForm.startDate;
    const end = this.leaveForm.endDate;
    if (!start || !end || end < start) return 0;
    if (this.leaveForm.isHalfDay) return start === end ? 0.5 : 0;
    const startMs = Date.parse(start);
    const endMs = Date.parse(end);
    if (Number.isNaN(startMs) || Number.isNaN(endMs)) return 0;
    return Math.floor((endMs - startMs) / 86400000) + 1;
  }

  selectedLeaveBalance(): LeaveBalanceView | null {
    const type = this.leaveForm.leaveType;
    if (type === 'Unpaid leave') return null;
    return this.leaveBalances.find((b) => b.leaveType === type) ?? null;
  }

  async loadLeaveBalances(employeeId?: number | null): Promise<void> {
    const id = employeeId ?? this.leaveBalanceEmployeeId ?? this.user?.employeeId ?? null;
    if (!id) {
      this.leaveBalances = [];
      this.render();
      return;
    }

    this.leaveBalanceEmployeeId = id;
    try {
      this.leaveBalances = await this.request<LeaveBalanceView[]>(
        '/api/leave-balances',
        'GET',
        undefined,
        { employeeId: id, year: this.leaveBalanceYear }
      );
    } catch (error) {
      this.leaveBalances = [];
      this.notify(this.errorMessage(error), 'error');
    }
    this.render();
  }

  async changeLeaveBalanceEmployee(employeeId: number): Promise<void> {
    this.leaveBalanceEmployeeId = employeeId;
    await this.loadLeaveBalances(employeeId);
  }

  async saveLeaveBalanceEntitlement(): Promise<void> {
    if (!this.can('leave.write') && !this.can('employees.write')) return;
    if (!this.leaveBalanceEmployeeId || this.leaveBalanceEditDays == null) return;
    if (this.leaveBalanceSaving) return;

    this.leaveBalanceSaving = true;
    this.render();
    try {
      await this.request<LeaveBalanceView>('/api/leave-balances', 'PUT', {
        employeeId: this.leaveBalanceEmployeeId,
        leaveType: this.leaveBalanceEditType,
        year: this.leaveBalanceYear,
        entitledDays: this.leaveBalanceEditDays
      });
      this.notify('Leave entitlement updated.', 'success');
      await this.loadLeaveBalances(this.leaveBalanceEmployeeId);
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    } finally {
      this.leaveBalanceSaving = false;
      this.render();
    }
  }

  async saveLeaveRequest(): Promise<void> {
    if (this.saveInProgress) return;
    if (!this.validateLeaveForm()) {
      this.render();
      return;
    }

    this.saveInProgress = true;
    this.render();

    try {
      await this.request<LeaveView>('/api/leave-requests', 'POST', {
        employeeId: this.leaveForm.employeeId,
        leaveType: this.leaveForm.leaveType,
        startDate: this.leaveForm.startDate,
        endDate: this.leaveForm.endDate,
        isHalfDay: this.leaveForm.isHalfDay,
        reason: this.leaveForm.reason.trim(),
        attachmentUrl: this.leaveForm.attachmentUrl.trim() || null
      });

      this.showLeaveDialog = false;
      this.notify('Leave request submitted.', 'success');
      await Promise.all([
        this.loadLeave(),
        this.loadDashboard(),
        this.loadLeaveBalances(this.leaveForm.employeeId)
      ]);
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    } finally {
      this.saveInProgress = false;
      this.render();
    }
  }

  canDecideLeave(leave: LeaveView): boolean {
    const managerCanAct = this.user?.role === 'Manager' && leave.status === 'Pending';
    const hrCanAct = this.can('leave.approve.hr') && (leave.status === 'Pending' || leave.status === 'ManagerApproved');
    return managerCanAct || hrCanAct;
  }

  async decideLeave(leave: LeaveView, decision: 'approve' | 'reject'): Promise<void> {
    if (decision === 'reject') {
      const confirmed = await this.askConfirm({
        title: 'Reject leave request',
        message: `Reject leave for ${leave.employeeName}?`,
        confirmLabel: 'Reject',
        danger: true
      });
      if (!confirmed) return;
    }

    try {
      await this.request<LeaveView>(`/api/leave-requests/${leave.id}/decision`, 'PUT', {
        decision,
        comment: decision === 'approve' ? 'Approved from admin panel' : 'Rejected from admin panel'
      });

      this.notify(`Leave ${decision}d.`, 'success');
      await Promise.all([
        this.loadLeave(),
        this.loadDashboard(),
        this.loadLeaveBalances(leave.employeeId)
      ]);
    } catch (error) {
      this.notify(this.errorMessage(error), 'error');
    }
  }

  can(permission: string): boolean {
    return this.user?.role === 'HR Admin' || Boolean(this.user?.permissions?.includes(permission));
  }

  /** Whether the signed-in user may open this app view (nav + deep links). */
  canAccessView(view: ViewName): boolean {
    switch (view) {
      case 'employees':
        return this.can('employees.read');
      case 'payroll':
        return this.can('payroll.read');
      case 'settings':
        return this.can('roles.read');
      case 'dashboard':
      case 'attendance':
      case 'leave':
      case 'reports':
        return true;
      default:
        return false;
    }
  }

  money(value: number | null | undefined): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value || 0);
  }

  dateOnly(value: string | null | undefined): string {
    return value ? value.slice(0, 10) : '-';
  }

  dateTime(value: string | null | undefined): string {
    return value ? new Date(value).toLocaleString() : '-';
  }

  timeOnly(value: string | null | undefined): string {
    return value ? new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '-';
  }

  dismissToast(id: number): void {
    const timer = this.toastTimers.get(id);
    if (timer) window.clearTimeout(timer);
    this.toastTimers.delete(id);
    this.toasts = this.toasts.filter((toast) => toast.id !== id);
    this.render();
  }

  resolveConfirm(accepted: boolean): void {
    const dialog = this.confirmDialog;
    this.confirmDialog = null;
    this.render();
    dialog?.resolve(accepted);
  }

  private askConfirm(options: {
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
    danger?: boolean;
  }): Promise<boolean> {
    return new Promise((resolve) => {
      this.confirmDialog = {
        title: options.title,
        message: options.message,
        confirmLabel: options.confirmLabel ?? 'Confirm',
        cancelLabel: options.cancelLabel ?? 'Cancel',
        danger: options.danger ?? false,
        resolve
      };
      this.render();
    });
  }

  private async loadView(view: ViewName): Promise<void> {
    this.viewLoading = true;
    this.viewError = '';
    this.render();

    try {
      if (view === 'dashboard') await this.loadDashboard();
      if (view === 'employees') await this.loadEmployees();
      if (view === 'attendance') await this.loadAttendance();
      if (view === 'leave') {
        await Promise.all([
          this.loadLeave(),
          this.loadLeaveBalances(this.user?.employeeId ?? this.allEmployeesForSelect[0]?.id ?? null)
        ]);
      }
      if (view === 'payroll') {
        if (!this.payrollPeriodStart) this.initPayrollPeriodDefaults();
        await this.loadPayroll();
      }
      if (view === 'reports') {
        await Promise.all([
          this.loadDashboard(),
          this.loadAttendance(true),
          this.loadLeave(true),
          this.loadPayroll(true)
        ]);
      }
      if (view === 'settings') await this.loadRoles();
      this.viewError = '';
      this.loadedViews.add(view);
    } catch (error) {
      this.viewError = this.errorMessage(error);
      this.loadedViews.delete(view);
    } finally {
      this.viewLoading = false;
      this.render();
    }
  }

  private async loadReferenceData(): Promise<void> {
    const [departments, positions, branches] = await Promise.all([
      this.request<Department[]>('/api/departments'),
      this.request<Position[]>('/api/positions'),
      this.request<Branch[]>('/api/branches')
    ]);

    this.departments = departments;
    this.positions = positions;
    this.branches = branches;
    await this.loadEmployeeSelectList();
  }

  private async loadEmployeeSelectList(): Promise<void> {
    try {
      const result = await this.request<PagedResult<EmployeeView> | EmployeeView[]>(
        '/api/employees',
        'GET',
        undefined,
        { page: 1, pageSize: 500 }
      );
      this.allEmployeesForSelect = this.unwrapPaged(result).items;
    } catch {
      this.allEmployeesForSelect = [];
    }
  }

  private async loadDashboard(): Promise<void> {
    const [dashboard, leave] = await Promise.all([
      this.request<DashboardView>('/api/dashboard'),
      this.request<PagedResult<LeaveView> | LeaveView[]>('/api/leave-requests', 'GET', undefined, {
        page: 1,
        pageSize: 50,
        status: 'Pending'
      })
    ]);
    this.dashboard = dashboard;
    const paged = this.unwrapPaged(leave);
    this.leaveRequests = paged.items;
    this.leaveTotal = paged.total;
  }

  private async loadEmployees(): Promise<void> {
    const result = await this.request<PagedResult<EmployeeView> | EmployeeView[]>(
      '/api/employees',
      'GET',
      undefined,
      {
        page: this.employeeQuery.page,
        pageSize: this.employeeQuery.pageSize,
        search: this.employeeQuery.search,
        status: this.employeeQuery.status
      }
    );
    const paged = this.unwrapPaged(result);
    this.employees = paged.items;
    this.employeeTotal = paged.total;
  }

  private async loadAttendance(summary = false): Promise<void> {
    const result = await this.request<PagedResult<AttendanceView> | AttendanceView[]>(
      '/api/attendance',
      'GET',
      undefined,
      {
        page: summary ? 1 : this.attendanceQuery.page,
        pageSize: summary ? 100 : this.attendanceQuery.pageSize,
        search: summary ? '' : this.attendanceQuery.search,
        status: summary ? '' : this.attendanceQuery.status,
        from: summary ? undefined : this.attendanceQuery.from,
        to: summary ? undefined : this.attendanceQuery.to
      }
    );
    const paged = this.unwrapPaged(result);
    this.attendance = paged.items;
    this.attendanceTotal = paged.total;
  }

  private async loadLeave(summary = false): Promise<void> {
    const result = await this.request<PagedResult<LeaveView> | LeaveView[]>(
      '/api/leave-requests',
      'GET',
      undefined,
      {
        page: summary ? 1 : this.leaveQuery.page,
        pageSize: summary ? 100 : this.leaveQuery.pageSize,
        search: summary ? '' : this.leaveQuery.search,
        status: summary ? '' : this.leaveQuery.status,
        from: summary ? undefined : this.leaveQuery.from,
        to: summary ? undefined : this.leaveQuery.to
      }
    );
    const paged = this.unwrapPaged(result);
    this.leaveRequests = paged.items;
    this.leaveTotal = paged.total;
  }

  private async loadRoles(): Promise<void> {
    if (!this.can('roles.read')) {
      this.roles = [];
      return;
    }
    this.roles = await this.request<RoleView[]>('/api/roles');
  }

  private async loadPayroll(summary = false): Promise<void> {
    if (!this.can('payroll.read')) {
      this.payroll = [];
      this.payrollTotal = 0;
      return;
    }

    const result = await this.request<PagedResult<PayrollView> | PayrollView[]>(
      '/api/payroll',
      'GET',
      undefined,
      {
        page: summary ? 1 : this.payrollQuery.page,
        pageSize: summary ? 100 : this.payrollQuery.pageSize,
        search: summary ? '' : this.payrollQuery.search,
        status: summary ? '' : this.payrollQuery.status
      }
    );
    const paged = this.unwrapPaged(result);
    this.payroll = paged.items;
    this.payrollTotal = paged.total;
  }

  private unwrapPaged<T>(result: PagedResult<T> | T[]): PagedResult<T> {
    if (Array.isArray(result)) {
      return { items: result, total: result.length, page: 1, pageSize: result.length || 10 };
    }

    return {
      items: result?.items ?? [],
      total: result?.total ?? 0,
      page: result?.page ?? 1,
      pageSize: result?.pageSize ?? 10
    };
  }

  private async postAttendance(endpoint: string): Promise<void> {
    await this.request<AttendanceView>(endpoint, 'POST', {
      employeeId: this.user?.employeeId,
      latitude: 11.5564,
      longitude: 104.9282,
      workMode: this.workMode
    });

    await Promise.all([this.loadAttendance(), this.loadDashboard()]);
  }

  private request<T>(
    path: string,
    method = 'GET',
    body?: unknown,
    query?: Record<string, string | number | boolean | null | undefined>
  ): Promise<T> {
    return this.api.request<T>(path, method, body, query);
  }

  private validateEmployeeForm(): boolean {
    const errors: EmployeeFormErrors = {};
    if (!this.employeeForm.fullName.trim()) errors.fullName = 'Full name is required.';
    if (!this.employeeForm.email.trim()) errors.email = 'Email is required.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.employeeForm.email.trim())) {
      errors.email = 'Enter a valid email address.';
    }
    if (!this.employeeForm.phone.trim()) errors.phone = 'Phone is required.';
    if (!this.employeeForm.departmentId) errors.departmentId = 'Department is required.';
    if (!this.employeeForm.positionId) errors.positionId = 'Position is required.';
    if (!this.employeeForm.branchId) errors.branchId = 'Branch is required.';
    if (!this.employeeForm.joinDate) errors.joinDate = 'Join date is required.';
    if (!this.employeeForm.dateOfBirth) errors.dateOfBirth = 'Date of birth is required.';
    if (this.employeeForm.basicSalary == null || Number(this.employeeForm.basicSalary) < 0) {
      errors.basicSalary = 'Basic salary must be 0 or more.';
    }
    this.employeeFormErrors = errors;
    return Object.keys(errors).length === 0;
  }

  private validateLeaveForm(): boolean {
    const errors: LeaveFormErrors = {};
    if (!this.leaveForm.employeeId) errors.employeeId = 'Employee is required.';
    if (!this.leaveForm.startDate) errors.startDate = 'Start date is required.';
    if (!this.leaveForm.endDate) errors.endDate = 'End date is required.';
    if (
      this.leaveForm.startDate &&
      this.leaveForm.endDate &&
      this.leaveForm.endDate < this.leaveForm.startDate
    ) {
      errors.endDate = 'End date must be on or after start date.';
    }
    if (this.leaveForm.isHalfDay && this.leaveForm.startDate !== this.leaveForm.endDate) {
      errors.endDate = 'Half-day leave must be a single day.';
    }
    if (!this.leaveForm.reason.trim()) errors.reason = 'Reason is required.';
    this.leaveFormErrors = errors;
    return Object.keys(errors).length === 0;
  }

  private emptyEmployeeForm(): EmployeeForm {
    return {
      id: null,
      fullName: '',
      gender: 'Male',
      dateOfBirth: '2000-01-01',
      email: '',
      phone: '',
      departmentId: this.departments[0]?.id ?? null,
      positionId: this.positions[0]?.id ?? null,
      branchId: this.branches[0]?.id ?? null,
      managerId: null,
      contractType: 'Full Time',
      joinDate: this.today(),
      resignDate: null,
      status: 'Active',
      emergencyContact: '',
      educationHistory: '',
      workExperience: '',
      basicSalary: 500
    };
  }

  private emptyLeaveForm(): LeaveForm {
    return {
      employeeId: this.user?.employeeId ?? this.allEmployeesForSelect[0]?.id ?? null,
      leaveType: 'Annual leave',
      startDate: this.today(),
      endDate: this.today(),
      isHalfDay: false,
      reason: '',
      attachmentUrl: ''
    };
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private notify(message: string, severity: ToastSeverity = 'info'): void {
    const id = ++this.toastSeq;
    this.toasts = [...this.toasts, { id, message, severity }];
    const duration = severity === 'error' ? 4500 : 2800;
    const timer = window.setTimeout(() => this.dismissToast(id), duration);
    this.toastTimers.set(id, timer);
    this.render();
  }

  private render(): void {
    this.renderVersion.update((value) => value + 1);
  }

  private wait(milliseconds: number): Promise<void> {
    return new Promise((resolve) => window.setTimeout(resolve, milliseconds));
  }

  private errorMessage(error: unknown): string {
    if (error instanceof Error) return error.message;

    if (typeof error === 'object' && error !== null) {
      const candidate = error as {
        error?: { message?: string; retryAt?: string } | string;
        message?: string;
        status?: number;
        statusText?: string;
      };

      if (typeof candidate.error === 'object' && candidate.error) {
        if (candidate.error.message) {
          if (candidate.error.retryAt) {
            return `${candidate.error.message} Try again after ${new Date(candidate.error.retryAt).toLocaleTimeString()}.`;
          }
          return candidate.error.message;
        }
      }
      if (typeof candidate.error === 'string' && candidate.error.trim().startsWith('<')) {
        return `${candidate.status || 'Request'} ${candidate.statusText || 'failed'} - API route is not reachable from the web app.`;
      }
      if (typeof candidate.error === 'string' && candidate.error) return candidate.error;
      if (candidate.message) return candidate.message;
      if (candidate.status === 401) return 'Authentication required.';
      if (candidate.status === 403) return 'You do not have permission to perform this action.';
      if (candidate.status === 404) return 'Resource not found.';
      if (candidate.status) return `${candidate.status} ${candidate.statusText || 'Request failed'}`;
    }

    return 'Request failed.';
  }
}
