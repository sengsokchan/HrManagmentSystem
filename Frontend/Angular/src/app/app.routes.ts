import { Routes } from '@angular/router';
import { authGuard, changePasswordGuard, guestGuard } from './Core/guards/auth.guard';
import { permissionGuard } from './Core/guards/permission.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./Auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./Auth/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent)
  },
  {
    path: 'change-password',
    canActivate: [changePasswordGuard],
    loadComponent: () =>
      import('./Auth/change-password/change-password.component').then((m) => m.ChangePasswordComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./Layouts/app-layout/app-layout.component').then((m) => m.AppLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'employees',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Employees/employees.component').then((m) => m.EmployeesComponent)
      },
      {
        path: 'attendance',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Attendance/attendance.component').then((m) => m.AttendanceComponent)
      },
      {
        path: 'leave',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Leave/leave.component').then((m) => m.LeaveComponent)
      },
      {
        path: 'payroll',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Payroll/payroll.component').then((m) => m.PayrollComponent)
      },
      {
        path: 'reports',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Reports/reports.component').then((m) => m.ReportsComponent)
      },
      {
        path: 'settings',
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./Features/Settings/settings.component').then((m) => m.SettingsComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
