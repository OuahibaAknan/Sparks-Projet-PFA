import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TicketService } from '../../../core/services/ticket.service';
import { TicketBlind } from '../../../core/models/ticket.model';
import { BadgeComponent } from '../badge/badge.component';
import { priorityColor, roleColor, statusColor, statusLabel } from '../../utils/badge-colors';
import { formatSla } from '../../utils/sla.util';

const DEFAULT_PAGE_SIZE = 10;
const PAGE_SIZE_OPTIONS = [5, 10, 20, 50];

type SortKey = 'id' | 'priority' | 'domain' | 'aiSuggestedRoute' | 'status' | 'slaMinutesRemaining' | 'createdAt';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'sp-all-tickets-table',
  standalone: true,
  imports: [CommonModule, FormsModule, BadgeComponent],
  templateUrl: './all-tickets-table.component.html',
  styleUrl: './all-tickets-table.component.scss',
})
export class AllTicketsTableComponent implements OnInit {
  items = signal<TicketBlind[]>([]);
  total = signal(0);
  page = signal(1);
  loading = signal(true);
  pageSize = DEFAULT_PAGE_SIZE;
  pageSizeOptions = PAGE_SIZE_OPTIONS;
  search = '';
  sortKey: SortKey = 'createdAt';
  sortDirection: SortDirection = 'desc';

  priorityColor = priorityColor;
  roleColor = roleColor;
  statusColor = statusColor;
  statusLabel = statusLabel;
  slaLabel = formatSla;

  constructor(private ticketService: TicketService) {}

  ngOnInit(): void {
    this.load();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
      this.load();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages) {
      this.page.set(this.page() + 1);
      this.load();
    }
  }

  onPageSizeChange(value: string): void {
    const nextPageSize = Number(value);
    if (!Number.isFinite(nextPageSize) || nextPageSize <= 0) {
      return;
    }

    this.pageSize = nextPageSize;
    this.page.set(1);
    this.load();
  }

  onSearch(): void {
    this.page.set(1);
    this.load();
  }

  sortBy(column: SortKey): void {
    if (this.sortKey === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = column;
      this.sortDirection = 'asc';
    }

    this.items.set(this.sortTableRows([...this.items()]));
  }

  private sortTableRows(rows: TicketBlind[]): TicketBlind[] {
    const direction = this.sortDirection === 'asc' ? 1 : -1;

    return [...rows].sort((a, b) => {
      switch (this.sortKey) {
        case 'id':
          return a.id.localeCompare(b.id) * direction;
        case 'priority':
          return (this.priorityWeight(a.priority) - this.priorityWeight(b.priority)) * direction;
        case 'domain':
          return a.domain.localeCompare(b.domain) * direction;
        case 'aiSuggestedRoute':
          return a.aiSuggestedRoute.localeCompare(b.aiSuggestedRoute) * direction;
        case 'status':
          return a.status.localeCompare(b.status) * direction;
        case 'slaMinutesRemaining':
          return (a.slaMinutesRemaining - b.slaMinutesRemaining) * direction;
        case 'createdAt':
        default:
          return (new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()) * direction;
      }
    });
  }

  private priorityWeight(priority: TicketBlind['priority']): number {
    const order = { Low: 1, Medium: 2, High: 3, Critical: 4 };
    return order[priority] ?? 0;
  }

  private load(): void {
    this.loading.set(true);
    const keyword = this.search.trim();
    this.ticketService
      .getAllBlind({ page: this.page(), pageSize: this.pageSize, search: keyword || undefined })
      .subscribe((res) => {
        this.items.set(this.sortTableRows(res.items));
        this.total.set(res.total);
        this.loading.set(false);
      });
  }
}
