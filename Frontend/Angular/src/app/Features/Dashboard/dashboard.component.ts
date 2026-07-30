import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { HrStateService } from '../../Core/services/hr-state.service';
import { StatusBadgeComponent } from '../../Shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, StatusBadgeComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  constructor(public readonly state: HrStateService) {}
}
