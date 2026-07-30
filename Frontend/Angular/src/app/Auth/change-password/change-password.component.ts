import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { I18nService } from '../../Core/i18n/i18n.service';
import { TranslatePipe } from '../../Core/i18n/translate.pipe';
import { HrStateService } from '../../Core/services/hr-state.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './change-password.component.html'
})
export class ChangePasswordComponent {
  readonly i18n = inject(I18nService);
  constructor(public readonly state: HrStateService) {}
}
