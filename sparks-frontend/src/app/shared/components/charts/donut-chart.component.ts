import { AfterViewInit, Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface DonutDatum {
  label: string;
  value: number;
  color: string;
}

@Component({
  selector: 'sp-donut-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="donut">
      <svg viewBox="0 0 42 42" class="donut__svg" [class.donut__svg--in]="grown()">
        <circle cx="21" cy="21" r="15.9" fill="transparent" stroke="var(--surface)" stroke-width="6"></circle>
        @for (seg of segments; track seg.label; let i = $index) {
          <circle
            cx="21"
            cy="21"
            r="15.9"
            fill="transparent"
            [attr.stroke]="seg.color"
            [attr.stroke-width]="hovered() === i ? 8 : 6"
            [style.opacity]="hovered() === null || hovered() === i ? 1 : 0.45"
            [attr.stroke-dasharray]="(grown() ? seg.dash : 0) + ' ' + (100 - (grown() ? seg.dash : 0))"
            [attr.stroke-dashoffset]="seg.offset"
            stroke-linecap="round"
            class="donut__segment"
            [style.transitionDelay.ms]="i * 90"
            (mouseenter)="hovered.set(i)"
            (mouseleave)="hovered.set(null)"
          ></circle>
        }
      </svg>
      <div class="donut__center">
        @if (hovered() !== null) {
          <div class="donut__hover-value" [style.color]="segments[hovered()!].color">{{ segments[hovered()!].value }}</div>
          <div class="donut__hover-label">{{ segments[hovered()!].label }}</div>
        } @else {
          <div class="donut__total">{{ total }}</div>
          <div class="donut__total-label">Total</div>
        }
      </div>
    </div>
    <ul class="donut__legend">
      @for (d of data; track d.label; let i = $index) {
        <li
          [class.donut__legend-item--dim]="hovered() !== null && hovered() !== i"
          (mouseenter)="hovered.set(i)"
          (mouseleave)="hovered.set(null)"
        >
          <span class="donut__dot" [style.background]="d.color"></span>
          {{ d.label }} <strong>({{ d.value }})</strong>
        </li>
      }
    </ul>
  `,
  styles: [
    `
      .donut {
        position: relative;
        width: 160px;
        height: 160px;
        margin: 0 auto;
      }
      .donut__svg {
        width: 100%;
        height: 100%;
        transform: rotate(-90deg) scale(0.75);
        opacity: 0;
        transition: opacity 0.4s ease, transform 0.5s cubic-bezier(0.22, 1, 0.36, 1);
      }
      .donut__svg--in {
        opacity: 1;
        transform: rotate(-90deg) scale(1);
      }
      .donut__segment {
        cursor: pointer;
        transition: stroke-dasharray 0.7s cubic-bezier(0.22, 1, 0.36, 1), stroke-width 0.15s ease, opacity 0.15s ease;
      }
      .donut__center {
        position: absolute;
        inset: 0;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        text-align: center;
        pointer-events: none;
      }
      .donut__total {
        font-size: 22px;
        font-weight: 800;
      }
      .donut__total-label {
        font-size: 11.5px;
        color: var(--text-muted);
      }
      .donut__hover-value {
        font-size: 22px;
        font-weight: 800;
        animation: donut-pop-in 0.15s ease both;
      }
      .donut__hover-label {
        font-size: 11px;
        color: var(--text-secondary);
        font-weight: 600;
        max-width: 100px;
      }
      @keyframes donut-pop-in {
        from {
          opacity: 0;
          transform: scale(0.85);
        }
        to {
          opacity: 1;
          transform: scale(1);
        }
      }
      .donut__legend {
        list-style: none;
        margin: 18px 0 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        gap: 8px 16px;
        justify-content: center;
        font-size: 12.5px;
        color: var(--text-secondary);
      }
      .donut__legend li {
        display: flex;
        align-items: center;
        gap: 6px;
        cursor: pointer;
        border-radius: 6px;
        padding: 2px 4px;
        transition: opacity 0.15s ease, background 0.15s ease;
      }
      .donut__legend li:hover {
        background: var(--surface);
      }
      .donut__legend-item--dim {
        opacity: 0.4;
      }
      .donut__dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        flex: none;
      }
    `,
  ],
})
export class DonutChartComponent implements AfterViewInit {
  @Input() data: DonutDatum[] = [];

  hovered = signal<number | null>(null);
  grown = signal(false);

  get total(): number {
    return this.data.reduce((sum, d) => sum + d.value, 0);
  }

  get segments(): (DonutDatum & { dash: number; offset: number })[] {
    const total = this.total || 1;
    let cumulative = 0;
    return this.data.map((d) => {
      const dash = (d.value / total) * 100;
      const offset = -cumulative;
      cumulative += dash;
      return { ...d, dash, offset };
    });
  }

  ngAfterViewInit(): void {
    requestAnimationFrame(() => setTimeout(() => this.grown.set(true), 30));
  }
}
