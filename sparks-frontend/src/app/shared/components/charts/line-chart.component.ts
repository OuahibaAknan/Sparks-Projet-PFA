import { AfterViewInit, Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface LineDatum {
  label: string;
  value: number;
}

@Component({
  selector: 'sp-line-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="line-chart" [class.line-chart--compact]="compact">
      <div class="line-chart__canvas">
        <svg [attr.viewBox]="'0 0 ' + width + ' ' + height" preserveAspectRatio="none" class="line-chart__svg">
          <defs>
            <linearGradient [attr.id]="gradientId" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" [attr.stop-color]="color" stop-opacity="0.28"></stop>
              <stop offset="100%" [attr.stop-color]="color" stop-opacity="0"></stop>
            </linearGradient>
          </defs>
          <polygon class="line-chart__area" [class.line-chart__area--in]="drawn()" [attr.points]="areaPoints" [attr.fill]="'url(#' + gradientId + ')'"></polygon>
          <polyline
            [attr.points]="linePoints"
            fill="none"
            [attr.stroke]="color"
            stroke-width="2.5"
            stroke-linejoin="round"
            stroke-linecap="round"
            [attr.stroke-dasharray]="pathLength"
            [attr.stroke-dashoffset]="drawn() ? 0 : pathLength"
            class="line-chart__line"
          ></polyline>
          @for (p of points; track $index; let i = $index) {
            <circle
              [attr.cx]="p.x"
              [attr.cy]="p.y"
              [attr.r]="hovered() === i ? 5.5 : 3"
              [attr.fill]="color"
              class="line-chart__dot"
              [class.line-chart__dot--in]="drawn()"
              [style.transitionDelay.ms]="drawn() ? i * 40 : 0"
            ></circle>
            <circle
              [attr.cx]="p.x"
              [attr.cy]="p.y"
              r="10"
              fill="transparent"
              class="line-chart__hit"
              (mouseenter)="hovered.set(i)"
              (mouseleave)="hovered.set(null)"
            ></circle>
          }
        </svg>

        <div
          class="line-chart__tooltip"
          *ngIf="hovered() !== null"
          [style.left.%]="(points[hovered()!].x / width) * 100"
          [style.top.%]="(points[hovered()!].y / height) * 100"
        >
          <strong>{{ data[hovered()!].label }}</strong>
          <span>value : <b [style.color]="color">{{ data[hovered()!].value }}</b></span>
        </div>
      </div>
      @if (!compact) {
        <div class="line-chart__labels">
          @for (d of data; track d.label) {
            <span>{{ d.label }}</span>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      .line-chart {
        width: 100%;
      }
      .line-chart__canvas {
        position: relative;
      }
      .line-chart__svg {
        width: 100%;
        height: 160px;
        display: block;
        overflow: visible;
      }
      .line-chart--compact .line-chart__svg {
        height: 56px;
      }
      .line-chart__line {
        transition: stroke-dashoffset 0.9s cubic-bezier(0.22, 1, 0.36, 1);
      }
      .line-chart__area {
        opacity: 0;
        transition: opacity 0.6s ease 0.3s;
      }
      .line-chart__area--in {
        opacity: 1;
      }
      .line-chart__dot {
        opacity: 0;
        transition: opacity 0.25s ease, r 0.15s ease;
      }
      .line-chart__dot--in {
        opacity: 1;
      }
      .line-chart__hit {
        cursor: pointer;
      }
      .line-chart__tooltip {
        position: absolute;
        transform: translate(-50%, -130%);
        background: #fff;
        border-radius: var(--radius);
        box-shadow: var(--shadow-lg);
        padding: 9px 13px;
        white-space: nowrap;
        z-index: 5;
        display: flex;
        flex-direction: column;
        gap: 2px;
        pointer-events: none;
        animation: line-tooltip-in 0.15s ease both;
      }
      .line-chart__tooltip strong {
        font-size: 12.5px;
        font-weight: 700;
        color: var(--text-primary);
      }
      .line-chart__tooltip span {
        font-size: 12px;
        color: var(--text-secondary);
      }
      .line-chart__tooltip b {
        font-weight: 700;
      }
      @keyframes line-tooltip-in {
        from {
          opacity: 0;
          transform: translate(-50%, -110%);
        }
        to {
          opacity: 1;
          transform: translate(-50%, -130%);
        }
      }
      .line-chart__labels {
        display: flex;
        justify-content: space-between;
        font-size: 10.5px;
        color: var(--text-muted);
        margin-top: 6px;
        padding: 0 2px;
      }
    `,
  ],
})
export class LineChartComponent implements AfterViewInit {
  @Input() data: LineDatum[] = [];
  @Input() color = 'var(--ai-accent)';
  @Input() compact = false;
  width = 300;
  height = 120;
  gradientId = `lc-${Math.random().toString(36).slice(2, 9)}`;

  hovered = signal<number | null>(null);
  drawn = signal(false);

  get points(): { x: number; y: number }[] {
    if (this.data.length === 0) return [];
    const values = this.data.map((d) => d.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min || 1;
    const step = this.width / Math.max(1, this.data.length - 1);
    return this.data.map((d, i) => ({
      x: i * step,
      y: this.height - ((d.value - min) / range) * (this.height - 16) - 8,
    }));
  }

  get linePoints(): string {
    return this.points.map((p) => `${p.x},${p.y}`).join(' ');
  }

  get areaPoints(): string {
    if (this.points.length === 0) return '';
    const first = this.points[0];
    const last = this.points[this.points.length - 1];
    return `${first.x},${this.height} ${this.linePoints} ${last.x},${this.height}`;
  }

  get pathLength(): number {
    const pts = this.points;
    let length = 0;
    for (let i = 1; i < pts.length; i++) {
      length += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    }
    return length || 1;
  }

  ngAfterViewInit(): void {
    requestAnimationFrame(() => setTimeout(() => this.drawn.set(true), 30));
  }
}
