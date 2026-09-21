import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TicketBlind } from '../../../core/models/ticket.model';
import { BadgeComponent } from '../badge/badge.component';
import { AiBadgeComponent } from '../badge/ai-badge.component';
import { IconComponent } from '../icon/icon.component';
import { priorityColor } from '../../utils/badge-colors';
import { formatSla } from '../../utils/sla.util';

@Component({
  selector: 'sp-blind-ticket-card',
  standalone: true,
  imports: [CommonModule, RouterLink, BadgeComponent, AiBadgeComponent, IconComponent],
  template: `
    <div class="btc sp-card">
      <div class="btc__top">
        <span class="btc__id">{{ ticket.id }}</span>
        <sp-badge [text]="ticket.priority" [fg]="priorityFg" [bg]="priorityBg" [dot]="true"></sp-badge>
        <span class="btc__sla" [class.btc__sla--urgent]="ticket.slaMinutesRemaining < 180">
          <sp-icon name="clock" [size]="13"></sp-icon>
          {{ slaLabel }}
        </span>
      </div>

      <div class="btc__locked">
        <sp-icon name="lock" [size]="14"></sp-icon>
        <div class="btc__lines">
          <span class="btc__line btc__line--w1"></span>
          <span class="btc__line btc__line--w2"></span>
        </div>
      </div>
      <p class="btc__hint">Full details revealed upon acceptance</p>

      <div class="btc__tags">
        <sp-badge [text]="ticket.domain" fg="var(--alten-navy)" bg="#eaf3fa"></sp-badge>
        <sp-ai-badge [text]="'AI suggests: ' + ticket.aiSuggestedRoute"></sp-ai-badge>
      </div>

      <div class="btc__footer">
        <span class="btc__date">{{ ticket.createdAt }}</span>
        <div class="btc__actions" *ngIf="mode === 'pool'">
          <button class="sp-btn sp-btn--outline sp-btn--sm" (click)="decline.emit(ticket)">Decline</button>
          <button class="sp-btn sp-btn--success sp-btn--sm" (click)="accept.emit(ticket)">Accept</button>
        </div>
        <a
          *ngIf="mode === 'active'"
          class="sp-btn sp-btn--outline sp-btn--sm"
          [routerLink]="['/app/tickets', ticket.id]"
        >
          Open ticket
          <sp-icon name="arrow-right" [size]="14"></sp-icon>
        </a>
      </div>
    </div>
  `,
  styles: [
    `
      .btc {
        padding: 18px;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }
      .btc__top {
        display: flex;
        align-items: center;
        gap: 10px;
        flex-wrap: wrap;
      }
      .btc__id {
        font-weight: 700;
        font-size: 14.5px;
        color: var(--text-primary);
        margin-right: auto;
      }
      .btc__sla {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 12px;
        font-weight: 600;
        color: var(--text-secondary);
      }
      .btc__sla--urgent {
        color: var(--priority-critical);
      }
      .btc__locked {
        display: flex;
        align-items: center;
        gap: 10px;
        background: var(--surface);
        border: 1px dashed var(--border);
        border-radius: var(--radius);
        padding: 12px 14px;
        color: var(--text-muted);
      }
      .btc__lines {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 6px;
      }
      .btc__line {
        height: 8px;
        border-radius: 4px;
        background: #dde3ea;
      }
      .btc__line--w1 {
        width: 85%;
      }
      .btc__line--w2 {
        width: 55%;
      }
      .btc__hint {
        font-size: 12px;
        color: var(--text-muted);
        margin-top: -6px;
      }
      .btc__tags {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
      }
      .btc__footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-top: 4px;
        padding-top: 12px;
        border-top: 1px solid var(--border);
      }
      .btc__date {
        font-size: 12.5px;
        color: var(--text-muted);
      }
      .btc__actions {
        display: flex;
        gap: 8px;
      }
    `,
  ],
})
export class BlindTicketCardComponent {
  @Input({ required: true }) ticket!: TicketBlind;
  @Input() mode: 'pool' | 'active' = 'pool';
  @Output() accept = new EventEmitter<TicketBlind>();
  @Output() decline = new EventEmitter<TicketBlind>();

  get priorityFg(): string {
    return priorityColor(this.ticket.priority).fg;
  }
  get priorityBg(): string {
    return priorityColor(this.ticket.priority).bg;
  }
  get slaLabel(): string {
    return formatSla(this.ticket.slaMinutesRemaining);
  }
}
