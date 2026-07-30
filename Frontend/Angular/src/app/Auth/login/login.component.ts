import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { I18nService } from '../../Core/i18n/i18n.service';
import { TranslatePipe } from '../../Core/i18n/translate.pipe';
import { HrStateService } from '../../Core/services/hr-state.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  readonly i18n = inject(I18nService);
  constructor(public readonly state: HrStateService) {}
}
