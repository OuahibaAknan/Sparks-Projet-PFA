import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'sp-badge',
  standalone: true,
  template: `
    <span class="sp-badge" [style.color]="fg" [style.background]="bg">
      <span *ngIf="dot" class="sp-badge__dot" [style.background]="fg"></span>
      {{ text }}
    </span>
  `,
  styles: [
    `
      .sp-badge {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 3px 10px;
        border-radius: 999px;
        font-size: 12.5px;
        font-weight: 600;
        line-height: 1.6;
        white-space: nowrap;
      }
      .sp-badge__dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        flex: none;
      }
    `,
  ],
  imports: [CommonModule],
})
export class BadgeComponent {
  @Input() text = '';
  @Input() fg = 'var(--text-secondary)';
  @Input() bg = 'var(--status-new-bg)';
  @Input() dot = false;
}
