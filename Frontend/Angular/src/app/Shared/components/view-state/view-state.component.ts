import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-view-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (loading) {
      <div class="view-loading" role="status" aria-live="polite">
        <div class="skeleton-grid">
          <div class="skeleton-block"></div>
          <div class="skeleton-block"></div>
          <div class="skeleton-block wide"></div>
        </div>
        <p>Loading…</p>
      </div>
    } @else if (error) {
      <div class="view-error" role="alert">
        <strong>Something went wrong</strong>
        <p>{{ error }}</p>
        <button type="button" class="primary-button" (click)="retry.emit()">Retry</button>
      </div>
    } @else {
      <ng-content></ng-content>
    }
  `
})
export class ViewStateComponent {
  @Input() loading = false;
  @Input() error = '';
  @Output() retry = new EventEmitter<void>();
}
