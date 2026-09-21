import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'sp-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  template: `
    <div class="sp-pagination" *ngIf="total > 0">
      <span class="sp-pagination__summary">
        Showing {{ startIndex }}–{{ endIndex }} of {{ total }}
      </span>
      <div class="sp-pagination__controls">
        <label class="sp-pagination__size" *ngIf="pageSizeOptions.length">
          Rows per page:
          <select [ngModel]="pageSize" (ngModelChange)="pageSizeChange.emit($event)">
            <option *ngFor="let size of pageSizeOptions" [ngValue]="size">{{ size }}</option>
          </select>
        </label>
        <ng-container *ngIf="totalPages > 1">
          <button class="sp-btn sp-btn--outline sp-btn--sm" [disabled]="page <= 1" (click)="pageChange.emit(page - 1)">
            <sp-icon name="chevron-left" [size]="16"></sp-icon>
          </button>
          <span class="sp-pagination__page">{{ page }} / {{ totalPages }}</span>
          <button class="sp-btn sp-btn--outline sp-btn--sm" [disabled]="page >= totalPages" (click)="pageChange.emit(page + 1)">
            <sp-icon name="chevron-right" [size]="16"></sp-icon>
          </button>
        </ng-container>
      </div>
    </div>
  `,
  styles: [
    `
      .sp-pagination {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 16px 4px 4px;
      }
      .sp-pagination__summary {
        font-size: 13.5px;
        color: var(--text-secondary);
      }
      .sp-pagination__controls {
        display: flex;
        align-items: center;
        gap: 10px;
      }
      .sp-pagination__page {
        font-size: 13.5px;
        font-weight: 600;
        color: var(--text-primary);
      }
      .sp-pagination__size {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        font-size: 13.5px;
        color: var(--text-secondary);
      }
      .sp-pagination__size select {
        border: 1px solid var(--border);
        border-radius: var(--radius);
        padding: 4px 8px;
        font-size: 13.5px;
        color: var(--text-primary);
        background: var(--surface);
      }
    `,
  ],
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 8;
  @Input() total = 0;
  @Input() pageSizeOptions: number[] = [];
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }
  get startIndex(): number {
    return this.total === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }
  get endIndex(): number {
    return Math.min(this.page * this.pageSize, this.total);
  }
}
