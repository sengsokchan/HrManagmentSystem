import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [CommonModule],
  template: `
    <label class="form-field" [class.form-field-error]="!!error">
      @if (label) {
        <span>{{ label }}</span>
      }
      <ng-content></ng-content>
      @if (error) {
        <small class="field-error" role="alert">{{ error }}</small>
      }
    </label>
  `
})
export class FormFieldComponent {
  @Input() label = '';
  @Input() error = '';
}
