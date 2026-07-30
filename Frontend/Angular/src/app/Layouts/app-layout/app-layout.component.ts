import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { I18nService } from '../../Core/i18n/i18n.service';
import { TranslatePipe } from '../../Core/i18n/translate.pipe';
import { ViewName } from '../../Core/models/hr.models';
import { HrStateService } from '../../Core/services/hr-state.service';
import { ConfirmDialogComponent } from '../../Shared/components/confirm-dialog/confirm-dialog.component';
import { ToastHostComponent } from '../../Shared/components/toast-host/toast-host.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ToastHostComponent,
    ConfirmDialogComponent,
    TranslatePipe
  ],
  templateUrl: './app-layout.component.html'
})
export class AppLayoutComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  readonly i18n = inject(I18nService);

  private readonly allNavItems: Array<{ view: ViewName; labelKey: string }> = [
    { view: 'dashboard', labelKey: 'nav.dashboard' },
    { view: 'employees', labelKey: 'nav.employees' },
    { view: 'attendance', labelKey: 'nav.attendance' },
    { view: 'leave', labelKey: 'nav.leave' },
    { view: 'payroll', labelKey: 'nav.payroll' },
    { view: 'reports', labelKey: 'nav.reports' },
    { view: 'settings', labelKey: 'nav.settings' }
  ];

  constructor(
    public readonly state: HrStateService,
    private readonly router: Router
  ) {}

  get navItems(): Array<{ view: ViewName; labelKey: string }> {
    this.i18n.lang();
    return this.allNavItems.filter((item) => this.state.canAccessView(item.view));
  }

  ngOnInit(): void {
    void this.syncFromUrl(this.router.url);

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((event) => {
        void this.syncFromUrl(event.urlAfterRedirects);
      });
  }

  private async syncFromUrl(url: string): Promise<void> {
    const segment = url.split('?')[0].split('/').filter(Boolean)[0] as ViewName | undefined;
    const views: ViewName[] = [
      'dashboard',
      'employees',
      'attendance',
      'leave',
      'payroll',
      'reports',
      'settings'
    ];
    if (!segment || !views.includes(segment)) return;

    if (!this.state.canAccessView(segment)) {
      await this.router.navigateByUrl('/dashboard');
      return;
    }

    await this.state.onNavigate(segment);
  }
}
