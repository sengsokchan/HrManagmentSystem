import { CommonModule } from '@angular/common';
import { Component, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../Core/i18n/i18n.service';
import { TranslatePipe } from '../../Core/i18n/translate.pipe';
import { HrStateService } from '../../Core/services/hr-state.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent implements OnDestroy {
  readonly i18n = inject(I18nService);
  constructor(public readonly state: HrStateService) {}

  ngOnDestroy(): void {
    this.state.clearForgotPasswordForm();
  }
}
