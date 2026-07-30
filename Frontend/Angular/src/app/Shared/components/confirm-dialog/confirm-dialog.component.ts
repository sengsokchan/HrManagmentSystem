import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ModalComponent } from '../modal/modal.component';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, ModalComponent],
  template: `
    <app-modal labelledBy="confirmDialogTitle" [closeOnBackdrop]="false" (closed)="cancel.emit()">
      <div class="modal-form">
        <div class="modal-header">
          <h3 id="confirmDialogTitle">{{ title }}</h3>
          <button type="button" class="icon-button" aria-label="Close" (click)="cancel.emit()">X</button>
        </div>
        <p class="confirm-message">{{ message }}</p>
        <div class="modal-actions">
          <button type="button" class="ghost-button" (click)="cancel.emit()">{{ cancelLabel }}</button>
          <button type="button" class="primary-button" [class.danger-button]="danger" (click)="confirm.emit()">
            {{ confirmLabel }}
          </button>
        </div>
      </div>
    </app-modal>
  `
})
export class ConfirmDialogComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) message = '';
  @Input() confirmLabel = 'Confirm';
  @Input() cancelLabel = 'Cancel';
  @Input() danger = false;
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}
