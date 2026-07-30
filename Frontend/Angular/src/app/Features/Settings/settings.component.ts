import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { HrStateService } from '../../Core/services/hr-state.service';
import { EmptyStateComponent } from '../../Shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, EmptyStateComponent],
  templateUrl: './settings.component.html'
})
export class SettingsComponent {
  constructor(public readonly state: HrStateService) {}
}
