import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AttendanceView,
  EmployeeView,
  LeaveView,
  PagedResult,
  PayrollView
} from '../../Core/models/hr.models';
import { ApiService } from '../../Core/services/api.service';
import { HrStateService } from '../../Core/services/hr-state.service';
import { EmptyStateComponent } from '../../Shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../Shared/components/status-badge/status-badge.component';
import { ExcelColumn, ExcelExportService } from '../../Shared/services/excel-export.service';

export type ReportCategory = 'attendance' | 'leave' | 'payroll' | 'employees';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, EmptyStateComponent, StatusBadgeComponent],
  templateUrl: './reports.component.html'
})
export class ReportsComponent implements OnInit {
  categories: Array<{
    id: ReportCategory;
    label: string;
    hint: string;
    permission?: string;
  }> = [
    {
      id: 'attendance',
      label: 'Attendance',
      hint: 'Filter by work date range, status, and employee name.'
    },
    {
      id: 'leave',
      label: 'Leave',
      hint: 'Filter by leave date range, status, and leave type.'
    },
    {
      id: 'payroll',
      label: 'Payroll',
      hint: 'Filter by period dates, status, and employee name.',
      permission: 'payroll.read'
    },
    {
      id: 'employees',
      label: 'Employees',
      hint: 'Filter by employment status, department, and search.',
      permission: 'employees.read'
    }
  ];

  category: ReportCategory = 'attendance';
  from = '';
  to = '';
  status = '';
  search = '';
  leaveType = '';
  departmentId: number | null = null;

  loading = false;
  exporting = false;
  previewError = '';
  previewTotal = 0;

  attendanceRows: AttendanceView[] = [];
  leaveRows: LeaveView[] = [];
  payrollRows: PayrollView[] = [];
  employeeRows: EmployeeView[] = [];

  constructor(
    public readonly state: HrStateService,
    private readonly api: ApiService,
    private readonly excel: ExcelExportService
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    this.from = monthStart.toISOString().slice(0, 10);
    this.to = today.toISOString().slice(0, 10);
    const first = this.visibleCategories[0];
    if (first) this.category = first.id;
    void this.applyFilters();
  }

  get visibleCategories() {
    return this.categories.filter(
      (item) => !item.permission || this.state.can(item.permission)
    );
  }

  get activeCategory() {
    return (
      this.visibleCategories.find((item) => item.id === this.category) ??
      this.visibleCategories[0] ??
      this.categories[0]
    );
  }

  get hasRows(): boolean {
    if (this.category === 'attendance') return this.attendanceRows.length > 0;
    if (this.category === 'leave') return this.leaveRows.length > 0;
    if (this.category === 'payroll') return this.payrollRows.length > 0;
    return this.employeeRows.length > 0;
  }

  selectCategory(category: ReportCategory): void {
    if (!this.visibleCategories.some((item) => item.id === category)) return;
    this.category = category;
    this.status = '';
    this.search = '';
    this.leaveType = '';
    this.departmentId = null;
    void this.applyFilters();
  }

  async applyFilters(): Promise<void> {
    this.loading = true;
    this.previewError = '';
    this.state.renderVersion.update((value) => value + 1);

    try {
      if (this.category === 'attendance') {
        const items = await this.fetchAll<AttendanceView>('/api/attendance', {
          from: this.from,
          to: this.to,
          status: this.status,
          search: this.search
        });
        this.attendanceRows = items;
        this.previewTotal = items.length;
      } else if (this.category === 'leave') {
        const result = await this.fetchAll<LeaveView>('/api/leave-requests', {
          from: this.from,
          to: this.to,
          status: this.status,
          search: this.search
        });
        const items = this.leaveType
          ? result.filter((item) => item.leaveType === this.leaveType)
          : result;
        this.leaveRows = items;
        this.previewTotal = items.length;
      } else if (this.category === 'payroll') {
        if (!this.state.can('payroll.read')) {
          this.payrollRows = [];
          this.previewTotal = 0;
          this.previewError = 'You need payroll.read permission to view payroll reports.';
          return;
        }
        const result = await this.fetchAll<PayrollView>('/api/payroll', {
          status: this.status,
          search: this.search
        });
        const items = result.filter((item) => {
          const start = item.periodStart?.slice(0, 10) ?? '';
          const end = item.periodEnd?.slice(0, 10) ?? '';
          if (this.from && end < this.from) return false;
          if (this.to && start > this.to) return false;
          return true;
        });
        this.payrollRows = items;
        this.previewTotal = items.length;
      } else {
        const result = await this.fetchAll<EmployeeView>('/api/employees', {
          status: this.status,
          search: this.search
        });
        const items = this.departmentId
          ? result.filter((item) => item.departmentId === this.departmentId)
          : result;
        this.employeeRows = items;
        this.previewTotal = items.length;
      }
    } catch (error) {
      this.attendanceRows = [];
      this.leaveRows = [];
      this.payrollRows = [];
      this.employeeRows = [];
      this.previewTotal = 0;
      this.previewError = this.readError(error);
    } finally {
      this.loading = false;
      this.state.renderVersion.update((value) => value + 1);
    }
  }

  exportExcel(): void {
    if (!this.hasRows || this.exporting) return;
    this.exporting = true;

    try {
      const stamp = new Date().toISOString().slice(0, 10);
      if (this.category === 'attendance') {
        this.excel.exportCsv(`attendance-report-${stamp}`, this.attendanceRows, this.attendanceColumns());
      } else if (this.category === 'leave') {
        this.excel.exportCsv(`leave-report-${stamp}`, this.leaveRows, this.leaveColumns());
      } else if (this.category === 'payroll') {
        this.excel.exportCsv(`payroll-report-${stamp}`, this.payrollRows, this.payrollColumns());
      } else {
        this.excel.exportCsv(`employees-report-${stamp}`, this.employeeRows, this.employeeColumns());
      }
    } finally {
      this.exporting = false;
    }
  }

  private attendanceColumns(): ExcelColumn<AttendanceView>[] {
    return [
      { header: 'Date', value: (row) => this.state.dateOnly(row.workDate) },
      { header: 'Employee', value: (row) => row.employeeName },
      { header: 'Check in', value: (row) => this.state.timeOnly(row.checkIn) },
      { header: 'Check out', value: (row) => this.state.timeOnly(row.checkOut) },
      { header: 'Status', value: (row) => row.status },
      { header: 'Late (min)', value: (row) => row.lateMinutes },
      { header: 'Overtime (min)', value: (row) => row.overtimeMinutes },
      { header: 'Work mode', value: (row) => row.workMode }
    ];
  }

  private leaveColumns(): ExcelColumn<LeaveView>[] {
    return [
      { header: 'Employee', value: (row) => row.employeeName },
      { header: 'Type', value: (row) => row.leaveType },
      { header: 'Start', value: (row) => this.state.dateOnly(row.startDate) },
      { header: 'End', value: (row) => this.state.dateOnly(row.endDate) },
      { header: 'Days', value: (row) => row.days },
      { header: 'Half day', value: (row) => row.isHalfDay },
      { header: 'Status', value: (row) => row.status },
      { header: 'Reason', value: (row) => row.reason },
      { header: 'Attachment', value: (row) => row.attachmentUrl }
    ];
  }

  private payrollColumns(): ExcelColumn<PayrollView>[] {
    return [
      { header: 'Employee', value: (row) => row.employeeName },
      { header: 'Period start', value: (row) => this.state.dateOnly(row.periodStart) },
      { header: 'Period end', value: (row) => this.state.dateOnly(row.periodEnd) },
      { header: 'Basic', value: (row) => row.basicSalary },
      { header: 'Allowance', value: (row) => row.allowance },
      { header: 'Bonus', value: (row) => row.bonus },
      { header: 'Tax', value: (row) => row.tax },
      { header: 'Deduction', value: (row) => row.deduction },
      { header: 'Overtime pay', value: (row) => row.overtimePay },
      { header: 'Net', value: (row) => row.netSalary },
      { header: 'Status', value: (row) => row.status }
    ];
  }

  private employeeColumns(): ExcelColumn<EmployeeView>[] {
    return [
      { header: 'Code', value: (row) => row.employeeCode },
      { header: 'Name', value: (row) => row.fullName },
      { header: 'Email', value: (row) => row.email },
      { header: 'Phone', value: (row) => row.phone },
      { header: 'Department', value: (row) => row.department },
      { header: 'Position', value: (row) => row.position },
      { header: 'Branch', value: (row) => row.branch },
      { header: 'Contract', value: (row) => row.contractType },
      { header: 'Join date', value: (row) => this.state.dateOnly(row.joinDate) },
      { header: 'Status', value: (row) => row.status }
    ];
  }

  private async fetchAll<T>(
    path: string,
    query: Record<string, string | number | boolean | null | undefined>
  ): Promise<T[]> {
    const pageSize = 200;
    let page = 1;
    let total = Number.POSITIVE_INFINITY;
    const items: T[] = [];

    while (items.length < total) {
      const result = await this.fetchPaged<T>(path, { ...query, page, pageSize });
      items.push(...result.items);
      total = result.total;
      if (!result.items.length || result.items.length < pageSize) break;
      page += 1;
      if (page > 100) break;
    }

    return items;
  }

  private async fetchPaged<T>(
    path: string,
    query: Record<string, string | number | boolean | null | undefined>
  ): Promise<PagedResult<T>> {
    const result = await this.api.request<PagedResult<T> | T[]>(path, 'GET', undefined, query);
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

  private readError(error: unknown): string {
    if (typeof error === 'object' && error !== null) {
      const candidate = error as { error?: { message?: string } | string; message?: string };
      if (typeof candidate.error === 'object' && candidate.error?.message) return candidate.error.message;
      if (typeof candidate.error === 'string' && candidate.error) return candidate.error;
      if (candidate.message) return candidate.message;
    }
    if (error instanceof Error) return error.message;
    return 'Failed to load report data.';
  }
}
