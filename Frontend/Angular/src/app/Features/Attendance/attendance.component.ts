import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HrStateService } from '../../Core/services/hr-state.service';
import { EmptyStateComponent } from '../../Shared/components/empty-state/empty-state.component';
import { StatusBadgeComponent } from '../../Shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent, EmptyStateComponent],
  templateUrl: './attendance.component.html'
})
export class AttendanceComponent {
  constructor(public readonly state: HrStateService) {}
}
