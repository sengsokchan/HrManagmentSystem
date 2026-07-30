import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  Output,
  ViewChild
} from '@angular/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-backdrop" (click)="onBackdrop()">
      <section
        #panel
        class="modal-panel"
        [class.leave-modal]="wide"
        role="dialog"
        aria-modal="true"
        [attr.aria-labelledby]="labelledBy"
        (click)="$event.stopPropagation()"
        tabindex="-1">
        <ng-content></ng-content>
      </section>
    </div>
  `
})
export class ModalComponent implements AfterViewInit, OnDestroy {
  @Input() labelledBy = '';
  @Input() wide = false;
  @Input() closeOnBackdrop = true;
  @Output() closed = new EventEmitter<void>();

  @ViewChild('panel') panel?: ElementRef<HTMLElement>;

  private previouslyFocused: HTMLElement | null = null;

  ngAfterViewInit(): void {
    this.previouslyFocused = document.activeElement as HTMLElement | null;
    queueMicrotask(() => {
      const root = this.panel?.nativeElement;
      if (!root) return;
      const focusable = this.focusableElements(root);
      (focusable[0] ?? root).focus();
    });
  }

  ngOnDestroy(): void {
    this.previouslyFocused?.focus?.();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.closed.emit();
      return;
    }

    if (event.key !== 'Tab' || !this.panel) return;

    const focusable = this.focusableElements(this.panel.nativeElement);
    if (!focusable.length) {
      event.preventDefault();
      this.panel.nativeElement.focus();
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const active = document.activeElement as HTMLElement | null;

    if (event.shiftKey && active === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  onBackdrop(): void {
    if (this.closeOnBackdrop) {
      this.closed.emit();
    }
  }

  private focusableElements(root: HTMLElement): HTMLElement[] {
    return Array.from(
      root.querySelectorAll<HTMLElement>(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
      )
    ).filter((el) => !el.hasAttribute('disabled') && el.offsetParent !== null);
  }
}
