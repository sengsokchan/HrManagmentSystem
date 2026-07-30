import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="status-badge" [attr.data-status]="normalized">{{ display }}</span>
  `,
  styles: [
    `
      .status-badge {
        display: inline-flex;
        align-items: center;
        padding: 0.15rem 0.55rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.01em;
        background: #eef2f6;
        color: #475467;
      }

      .status-badge[data-status='active'],
      .status-badge[data-status='present'],
      .status-badge[data-status='approved'],
      .status-badge[data-status='paid'] {
        background: #dcfce7;
        color: #166534;
      }

      .status-badge[data-status='pending'],
      .status-badge[data-status='managerapproved'],
      .status-badge[data-status='draft'],
      .status-badge[data-status='late'] {
        background: #fef3c7;
        color: #92400e;
      }

      .status-badge[data-status='inactive'],
      .status-badge[data-status='rejected'],
      .status-badge[data-status='absent'],
      .status-badge[data-status='resigned'] {
        background: #fee2e2;
        color: #991b1b;
      }
    `
  ]
})
export class StatusBadgeComponent {
  @Input({ required: true }) value: string | number | null | undefined = '';

  get display(): string {
    if (this.value == null || this.value === '') return '-';
    return String(this.value);
  }

  get normalized(): string {
    return this.display.replace(/\s+/g, '').toLowerCase();
  }
}
