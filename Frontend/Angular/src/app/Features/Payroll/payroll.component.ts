import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '../../Core/i18n/translate.pipe';
import { HrStateService } from '../../Core/services/hr-state.service';
import { EmptyStateComponent } from '../../Shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../Shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-payroll',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent, EmptyStateComponent, TranslatePipe],
  templateUrl: './payroll.component.html'
})
export class PayrollComponent {
  constructor(public readonly state: HrStateService) {}
}
