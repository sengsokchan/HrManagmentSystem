export type ViewName = 'dashboard' | 'employees' | 'attendance' | 'leave' | 'payroll' | 'reports' | 'settings';

export type ToastSeverity = 'success' | 'error' | 'warning' | 'info';

export interface ToastItem {
  id: number;
  message: string;
  severity: ToastSeverity;
}

export interface ConfirmRequest {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  danger: boolean;
  resolve: (value: boolean) => void;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ListQuery {
  page: number;
  pageSize: number;
  search: string;
  status: string;
  from?: string;
  to?: string;
}

export interface SignedInUser {
  id: number;
  email: string;
  role: string;
  employeeId: number | null;
  employeeName: string | null;
  permissions: string[];
  mustChangePassword?: boolean;
}

export interface LoginResponse {
  token: string;
  user: SignedInUser;
  mustChangePassword?: boolean;
}

export interface EmployeeCreateResult {
  employee: EmployeeView;
  loginEmail: string;
  temporaryPassword: string;
}

export interface PasswordResetResult {
  fullName: string;
  loginEmail: string;
  temporaryPassword: string;
}

export interface IssuedCredentials {
  fullName: string;
  loginEmail: string;
  temporaryPassword: string;
  reason: 'created' | 'reset';
}

export interface Department {
  id: number;
  name: string;
}

export interface Position {
  id: number;
  departmentId: number;
  title: string;
}

export interface Branch {
  id: number;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
}

export interface EmployeeView {
  id: number;
  employeeCode: string;
  fullName: string;
  gender: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  departmentId: number;
  department: string;
  positionId: number;
  position: string;
  branchId: number;
  branch: string;
  managerId: number | null;
  manager: string | null;
  contractType: string;
  joinDate: string;
  resignDate: string | null;
  status: string;
  emergencyContact: string;
  educationHistory: string;
  workExperience: string;
  basicSalary: number;
}

export interface AttendanceView {
  id: number;
  employeeId: number;
  employeeName: string;
  workDate: string;
  checkIn: string | null;
  checkOut: string | null;
  status: string;
  latitude: number | null;
  longitude: number | null;
  workMode: string;
  lateMinutes: number;
  overtimeMinutes: number;
}

export interface LeaveView {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  isHalfDay: boolean;
  days: number;
  reason: string;
  attachmentUrl: string | null;
  status: string;
  managerComment: string | null;
  hrComment: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface LeaveBalanceView {
  employeeId: number;
  employeeName: string;
  leaveType: string;
  year: number;
  entitledDays: number;
  usedDays: number;
  pendingDays: number;
  remainingDays: number;
}

export interface RoleView {
  id: number;
  name: string;
  permissions: string[];
}

export interface PayrollView {
  id: number;
  employeeId: number;
  employeeName: string;
  periodStart: string;
  periodEnd: string;
  basicSalary: number;
  allowance: number;
  bonus: number;
  tax: number;
  deduction: number;
  overtimePay: number;
  netSalary: number;
  status: string;
}

export interface AuditLog {
  id: number;
  userId: number;
  action: string;
  entityName: string;
  entityId: string;
  details: string;
  createdAt: string;
}

export interface DashboardView {
  totalEmployees: number;
  todayAttendance: number;
  lateEmployees: number;
  employeesOnLeave: number;
  pendingLeave: number;
  payrollSummary: number;
  recentActivity: AuditLog[];
}

export interface EmployeeForm {
  id: number | null;
  fullName: string;
  gender: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  departmentId: number | null;
  positionId: number | null;
  branchId: number | null;
  managerId: number | null;
  contractType: string;
  joinDate: string;
  resignDate: string | null;
  status: string;
  emergencyContact: string;
  educationHistory: string;
  workExperience: string;
  basicSalary: number;
}

export interface EmployeeFormErrors {
  fullName?: string;
  email?: string;
  phone?: string;
  departmentId?: string;
  positionId?: string;
  branchId?: string;
  joinDate?: string;
  dateOfBirth?: string;
  basicSalary?: string;
}

export interface LeaveForm {
  employeeId: number | null;
  leaveType: string;
  startDate: string;
  endDate: string;
  isHalfDay: boolean;
  reason: string;
  attachmentUrl: string;
}

export interface LeaveFormErrors {
  employeeId?: string;
  startDate?: string;
  endDate?: string;
  reason?: string;
}
