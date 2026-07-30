import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ToastItem } from '../../../Core/models/hr.models';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-stack" aria-live="polite" aria-relevant="additions">
      @for (toast of toasts; track toast.id) {
        <div class="toast" [class]="'toast toast-' + toast.severity" role="status">
          <span>{{ toast.message }}</span>
          <button type="button" class="toast-dismiss" aria-label="Dismiss" (click)="dismiss.emit(toast.id)">
            X
          </button>
        </div>
      }
    </div>
  `
})
export class ToastHostComponent {
  @Input() toasts: ToastItem[] = [];
  @Output() dismiss = new EventEmitter<number>();
}
