import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../modal/modal.component';
import { IconComponent } from '../icon/icon.component';
import { TicketBlind } from '../../../core/models/ticket.model';

@Component({
  selector: 'sp-accept-modal',
  standalone: true,
  imports: [CommonModule, ModalComponent, IconComponent],
  template: `
    <sp-modal [open]="!!ticket" title="Confirm blind acceptance" (closed)="cancel.emit()">
      <ng-container *ngIf="ticket as t">
        <div class="am__intro">
          <sp-icon name="lock" [size]="16"></sp-icon>
          <p>
            You're about to commit to this ticket <strong>before seeing its title or
            description</strong>. This keeps assignment fair, no cherry-picking easy tickets.
          </p>
        </div>

        <p class="am__question">Accept this ticket and reveal its full content?</p>

        <div class="am__actions">
          <button class="sp-btn sp-btn--outline" (click)="cancel.emit()" [disabled]="loading">No, go back</button>
          <button class="sp-btn sp-btn--success" (click)="confirm.emit(t)" [disabled]="loading">
            {{ loading ? 'Accepting…' : 'Yes, accept' }}
          </button>
        </div>
      </ng-container>
    </sp-modal>
  `,
  styles: [
    `
      .am__intro {
        display: flex;
        gap: 10px;
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        padding: 12px 14px;
        color: var(--text-secondary);
        font-size: 13px;
        line-height: 1.5;
        margin-bottom: 18px;
      }
      .am__intro sp-icon {
        flex: none;
        margin-top: 2px;
        color: var(--alten-blue);
      }
      .am__question {
        font-size: 14.5px;
        font-weight: 600;
        text-align: center;
        margin: 18px 0;
      }
      .am__actions {
        display: flex;
        gap: 12px;
      }
      .am__actions .sp-btn {
        flex: 1;
      }
    `,
  ],
})
export class AcceptModalComponent {
  @Input() ticket: TicketBlind | null = null;
  @Input() loading = false;
  @Output() confirm = new EventEmitter<TicketBlind>();
  @Output() cancel = new EventEmitter<void>();
}
