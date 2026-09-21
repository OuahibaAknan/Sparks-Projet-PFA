import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Fixed violet/sparkle styling — intentionally not themeable so AI-generated
 * content always reads as visually distinct from confirmed data, everywhere it appears.
 */
@Component({
  selector: 'sp-ai-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="sp-ai-badge" [class.sp-ai-badge--solid]="solid">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <path
          d="M12 2l1.9 5.8L20 9.5l-6.1 1.7L12 17l-1.9-5.8L4 9.5l6.1-1.7L12 2z"
        />
      </svg>
      {{ text }}
    </span>
  `,
  styles: [
    `
      .sp-ai-badge {
        display: inline-flex;
        align-items: center;
        gap: 5px;
        padding: 3px 10px;
        border-radius: 999px;
        font-size: 12.5px;
        font-weight: 600;
        white-space: nowrap;
        color: var(--ai-accent);
        background: var(--ai-accent-soft);
        border: 1px solid var(--ai-accent-border);
      }
      .sp-ai-badge--solid {
        color: #fff;
        background: var(--ai-accent);
        border-color: var(--ai-accent);
      }
    `,
  ],
})
export class AiBadgeComponent {
  @Input() text = 'AI-suggested';
  @Input() solid = false;
}
