import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'sp-modal',
  standalone: true,
  imports: [CommonModule, IconComponent],
  template: `
    <div class="sp-modal-backdrop" *ngIf="open" (click)="onBackdropClick()">
      <div class="sp-modal" [style.maxWidth]="maxWidth" (click)="$event.stopPropagation()">
        <div class="sp-modal__header" *ngIf="title">
          <h3>{{ title }}</h3>
          <button class="sp-modal__close" (click)="closed.emit()"><sp-icon name="x" [size]="18"></sp-icon></button>
        </div>
        <div class="sp-modal__body">
          <ng-content></ng-content>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .sp-modal-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(16, 24, 40, 0.55);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 100;
        padding: 20px;
      }
      .sp-modal {
        background: #fff;
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow-lg);
        width: 100%;
        max-width: 480px;
        max-height: 90vh;
        overflow-y: auto;
      }
      .sp-modal__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 20px 24px 0;
      }
      .sp-modal__header h3 {
        font-size: 17px;
        font-weight: 700;
      }
      .sp-modal__close {
        border: none;
        background: transparent;
        color: var(--text-muted);
        cursor: pointer;
        padding: 4px;
      }
      .sp-modal__body {
        padding: 20px 24px 24px;
      }
    `,
  ],
})
export class ModalComponent {
  @Input() open = false;
  @Input() title = '';
  @Input() maxWidth = '480px';
  @Input() closeOnBackdrop = true;
  @Output() closed = new EventEmitter<void>();

  onBackdropClick(): void {
    if (this.closeOnBackdrop) this.closed.emit();
  }
}
