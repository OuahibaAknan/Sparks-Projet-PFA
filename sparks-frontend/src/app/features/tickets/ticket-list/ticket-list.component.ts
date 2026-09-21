import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { TicketService } from '../../../core/services/ticket.service';
import { PagedResult, TicketFull, TicketPriority, TicketStatus } from '../../../core/models/ticket.model';
import { priorityColor, statusColor, statusLabel } from '../../../shared/utils/badge-colors';
import { formatSla } from '../../../shared/utils/sla.util';

const STATUSES: (TicketStatus | 'All')[] = ['All', 'New', 'Accepted', 'InProgress', 'Escalated', 'Resolved', 'Closed'];
const PRIORITIES: (TicketPriority | 'All')[] = ['All', 'Low', 'Medium', 'High', 'Critical'];

@Component({
  selector: 'sp-ticket-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BadgeComponent, IconComponent, PaginationComponent],
  templateUrl: './ticket-list.component.html',
  styleUrl: './ticket-list.component.scss',
})
export class TicketListComponent implements OnInit {
  result = signal<PagedResult<TicketFull> | null>(null);
  loading = signal(true);
  statusFilter: TicketStatus | 'All' = 'All';
  priorityFilter: TicketPriority | 'All' = 'All';
  search = '';
  page = 1;
  pageSize = 8;
  pageSizeOptions = [8, 10, 20, 50];

  statuses = STATUSES;
  priorities = PRIORITIES;

  statusColor = statusColor;
  priorityColor = priorityColor;
  statusLabel = statusLabel;
  slaLabel = formatSla;

  constructor(private ticketService: TicketService) {}

  ngOnInit(): void {
    this.refresh();
  }

  setStatus(status: TicketStatus | 'All'): void {
    this.statusFilter = status;
    this.page = 1;
    this.refresh();
  }

  setPriority(priority: TicketPriority | 'All'): void {
    this.priorityFilter = priority;
    this.page = 1;
    this.refresh();
  }

  onSearch(): void {
    this.page = 1;
    this.refresh();
  }

  onPageChange(page: number): void {
    this.page = page;
    this.refresh();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.refresh();
  }

  private refresh(): void {
    this.loading.set(true);
    this.ticketService
      .list({
        status: this.statusFilter,
        priority: this.priorityFilter,
        search: this.search,
        page: this.page,
        pageSize: this.pageSize,
      })
      .subscribe((res) => {
        this.result.set(res);
        this.loading.set(false);
      });
  }
}
