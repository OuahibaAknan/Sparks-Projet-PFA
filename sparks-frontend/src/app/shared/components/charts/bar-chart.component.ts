import { AfterViewInit, Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface BarDatum {
  label: string;
  value: number;
}

@Component({
  selector: 'sp-bar-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bars">
      @for (d of data; track d.label; let i = $index) {
        <div
          class="bars__col"
          (mouseenter)="hovered.set(i)"
          (mouseleave)="hovered.set(null)"
        >
          <div class="bars__highlight" [class.bars__highlight--active]="hovered() === i"></div>

          <div class="bars__tooltip" *ngIf="hovered() === i">
            <strong>{{ d.label }}</strong>
            <span>tickets : <b [style.color]="color">{{ d.value }}</b></span>
          </div>

          <div class="bars__track">
            <div
              class="bars__fill"
              [class.bars__fill--hovered]="hovered() === i"
              [style.height.%]="grown() && max ? (d.value / max) * 100 : 0"
              [style.background]="color"
              [style.transitionDelay.ms]="i * 45"
            ></div>
          </div>
          <span class="bars__value">{{ d.value }}</span>
          <span class="bars__label">{{ d.label }}</span>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .bars {
        display: flex;
        align-items: flex-end;
        gap: 14px;
        height: 180px;
        padding-top: 8px;
        position: relative;
      }
      .bars__col {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        height: 100%;
        min-width: 0;
        position: relative;
        cursor: pointer;
      }
      .bars__highlight {
        position: absolute;
        inset: -8px 0 24px 0;
        border-radius: 8px;
        background: var(--surface);
        opacity: 0;
        transform: scaleY(0.9);
        transform-origin: bottom;
        transition: opacity 0.18s ease, transform 0.18s ease;
        pointer-events: none;
      }
      .bars__highlight--active {
        opacity: 1;
        transform: scaleY(1);
      }
      .bars__tooltip {
        position: absolute;
        left: 50%;
        top: -6px;
        transform: translate(-50%, -100%);
        background: #fff;
        border-radius: var(--radius);
        box-shadow: var(--shadow-lg);
        padding: 10px 14px;
        white-space: nowrap;
        z-index: 5;
        display: flex;
        flex-direction: column;
        gap: 2px;
        animation: bars-tooltip-in 0.15s ease both;
        pointer-events: none;
      }
      .bars__tooltip strong {
        font-size: 13px;
        font-weight: 700;
        color: var(--text-primary);
      }
      .bars__tooltip span {
        font-size: 12.5px;
        color: var(--text-secondary);
      }
      .bars__tooltip b {
        font-weight: 700;
      }
      @keyframes bars-tooltip-in {
        from {
          opacity: 0;
          transform: translate(-50%, -92%);
        }
        to {
          opacity: 1;
          transform: translate(-50%, -100%);
        }
      }
      .bars__track {
        flex: 1;
        width: 100%;
        display: flex;
        align-items: flex-end;
        position: relative;
        z-index: 1;
      }
      .bars__fill {
        width: 100%;
        border-radius: 6px 6px 0 0;
        min-height: 3px;
        transition: height 0.55s cubic-bezier(0.22, 1, 0.36, 1), filter 0.15s ease, transform 0.15s ease;
        transform-origin: bottom;
      }
      .bars__fill--hovered {
        filter: brightness(1.15);
        transform: scaleX(1.06);
      }
      .bars__value {
        font-size: 11.5px;
        font-weight: 700;
        color: var(--text-primary);
        margin-top: 6px;
        position: relative;
        z-index: 1;
      }
      .bars__label {
        font-size: 10.5px;
        color: var(--text-muted);
        margin-top: 2px;
        text-align: center;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        max-width: 100%;
        position: relative;
        z-index: 1;
      }
    `,
  ],
})
export class BarChartComponent implements AfterViewInit {
  @Input() data: BarDatum[] = [];
  @Input() color = 'var(--alten-navy)';

  hovered = signal<number | null>(null);
  grown = signal(false);

  get max(): number {
    return Math.max(...this.data.map((d) => d.value), 1);
  }

  ngAfterViewInit(): void {
    requestAnimationFrame(() => setTimeout(() => this.grown.set(true), 30));
  }
}
