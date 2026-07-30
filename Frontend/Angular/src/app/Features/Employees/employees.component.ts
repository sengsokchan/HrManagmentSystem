import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '../../Core/i18n/translate.pipe';
import { HrStateService } from '../../Core/services/hr-state.service';
import { EmptyStateComponent } from '../../Shared/components/empty-state/empty-state.component';
import { FormFieldComponent } from '../../Shared/components/form-field/form-field.component';
import { ModalComponent } from '../../Shared/components/modal/modal.component';
import { StatusBadgeComponent } from '../../Shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    StatusBadgeComponent,
    EmptyStateComponent,
    ModalComponent,
    FormFieldComponent,
    TranslatePipe
  ],
  templateUrl: './employees.component.html'
})
export class EmployeesComponent {
  constructor(public readonly state: HrStateService) {}
}
