import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'sp-kpi-stat',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="sp-card kpi" [style.background]="tint">
      <div class="kpi__top">
        <span class="kpi__icon" *ngIf="icon" [style.color]="accent" [innerHTML]="icon"></span>
        <span class="kpi__delta" *ngIf="delta !== undefined" [class.kpi__delta--down]="delta < 0">
          {{ delta > 0 ? '+' : '' }}{{ delta }}%
        </span>
      </div>
      <div class="kpi__value" [style.color]="accent">{{ value }}</div>
      <div class="kpi__label">{{ label }}</div>
    </div>
  `,
  styles: [
    `
      .kpi {
        padding: 20px;
        display: flex;
        flex-direction: column;
        gap: 10px;
        min-width: 0;
      }
      .kpi__top {
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .kpi__icon {
        display: inline-flex;
      }
      .kpi__delta {
        font-size: 12.5px;
        font-weight: 700;
        color: var(--status-resolved);
      }
      .kpi__delta--down {
        color: var(--priority-critical);
      }
      .kpi__value {
        font-size: 28px;
        font-weight: 800;
        letter-spacing: -0.02em;
      }
      .kpi__label {
        font-size: 13.5px;
        color: var(--text-secondary);
        font-weight: 500;
      }
    `,
  ],
})
export class KpiStatComponent {
  @Input() label = '';
  @Input() value: string | number = '';
  @Input() delta?: number;
  @Input() accent = 'var(--text-primary)';
  @Input() tint = '#fff';
  @Input() icon?: string;
}
