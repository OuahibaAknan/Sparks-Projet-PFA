import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../modal/modal.component';

@Component({
  selector: 'sp-confirm-modal',
  standalone: true,
  imports: [CommonModule, ModalComponent],
  template: `
    <sp-modal [open]="open" [title]="title" (closed)="cancelled.emit()">
      <p class="cm__message">{{ message }}</p>
      <div class="cm__actions">
        <button class="sp-btn sp-btn--outline" (click)="cancelled.emit()" [disabled]="loading">
          {{ cancelLabel }}
        </button>
        <button
          class="sp-btn"
          [class.sp-btn--danger]="danger"
          [class.sp-btn--primary]="!danger"
          (click)="confirmed.emit()"
          [disabled]="loading"
        >
          {{ loading ? loadingLabel : confirmLabel }}
        </button>
      </div>
    </sp-modal>
  `,
  styles: [
    `
      .cm__message {
        font-size: 14.5px;
        color: var(--text-secondary);
        line-height: 1.5;
        margin: 0 0 20px;
      }
      .cm__actions {
        display: flex;
        gap: 12px;
      }
      .cm__actions .sp-btn {
        flex: 1;
      }
    `,
  ],
})
export class ConfirmModalComponent {
  @Input() open = false;
  @Input() title = 'Are you sure?';
  @Input() message = '';
  @Input() confirmLabel = 'Confirm';
  @Input() loadingLabel = 'Working…';
  @Input() cancelLabel = 'Cancel';
  @Input() danger = false;
  @Input() loading = false;
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();
}
