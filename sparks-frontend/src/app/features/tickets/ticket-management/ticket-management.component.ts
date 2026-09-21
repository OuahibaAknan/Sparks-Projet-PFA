import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BlindTicketCardComponent } from '../../../shared/components/blind-ticket-card/blind-ticket-card.component';
import { AcceptModalComponent } from '../../../shared/components/accept-modal/accept-modal.component';
import { TicketService } from '../../../core/services/ticket.service';
import { AuthService } from '../../../core/services/auth.service';
import { TicketBlind, TicketPriority, TicketStatus, TicketStatusCounts } from '../../../core/models/ticket.model';

type StatusKey = 'open' | 'inProgress' | 'forwarded' | 'done' | 'closed' | 'cancelled';
type PoolTabKey = 'generalist' | 'specialist' | 'active';

interface StatusPill {
  key: StatusKey;
  label: string;
  fg: string;
  bg: string;
  statuses: TicketStatus[];
}

interface PoolTab {
  key: PoolTabKey;
  label: string;
}

const PILLS: StatusPill[] = [
  { key: 'open', label: 'Open', fg: 'var(--status-resolved)', bg: 'var(--status-resolved-bg)', statuses: ['New', 'Accepted'] },
  { key: 'inProgress', label: 'In Progress', fg: 'var(--status-escalated)', bg: 'var(--status-escalated-bg)', statuses: ['InProgress'] },
  { key: 'forwarded', label: 'Forwarded', fg: 'var(--status-new)', bg: 'var(--status-new-bg)', statuses: ['Escalated'] },
  { key: 'done', label: 'Done', fg: 'var(--status-accepted)', bg: 'var(--status-accepted-bg)', statuses: ['Resolved'] },
  { key: 'closed', label: 'Closed', fg: 'var(--status-inprogress)', bg: 'var(--status-inprogress-bg)', statuses: ['Closed'] },
  { key: 'cancelled', label: 'Cancelled', fg: 'var(--status-cancelled)', bg: 'var(--status-cancelled-bg)', statuses: ['Cancelled'] },
];

const PRIORITIES: (TicketPriority | 'All')[] = ['All', 'Low', 'Medium', 'High', 'Critical'];

@Component({
  selector: 'sp-ticket-management',
  standalone: true,
  imports: [CommonModule, BlindTicketCardComponent, AcceptModalComponent],
  templateUrl: './ticket-management.component.html',
  styleUrl: './ticket-management.component.scss',
})
export class TicketManagementComponent implements OnInit {
  pills = PILLS;
  priorities = PRIORITIES;

  selectedPill = signal<StatusKey | null>(null);
  counts = signal<TicketStatusCounts | null>(null);
  countsLoading = signal(true);

  generalistPool = signal<TicketBlind[]>([]);
  specialistPool = signal<TicketBlind[]>([]);
  active = signal<TicketBlind[]>([]);
  loading = signal(true);

  tab = signal<PoolTabKey>('generalist');
  priorityFilter = signal<TicketPriority | 'All'>('All');
  ticketToConfirm = signal<TicketBlind | null>(null);
  acceptLoading = signal(false);

  constructor(private ticketService: TicketService, public auth: AuthService, private router: Router) {}

  get role(): string | undefined {
    return this.auth.currentUser()?.role;
  }

  tabs = computed<PoolTab[]>(() => {
    switch (this.role) {
      case 'Specialist':
        return [
          { key: 'specialist', label: 'In wait' },
          { key: 'active', label: 'My Tickets' },
        ];
      case 'Polyvalent':
        return [
          { key: 'generalist', label: 'Generalist Pool' },
          { key: 'specialist', label: 'Specialist Pool' },
          { key: 'active', label: 'My Tickets' },
        ];
      default:
        return [
          { key: 'generalist', label: 'In wait' },
          { key: 'active', label: 'My Tickets' },
        ];
    }
  });

  get pool(): TicketBlind[] {
    return this.tab() === 'specialist' ? this.specialistPool() : this.generalistPool();
  }

  filteredPool = computed(() => this.filterByPriority(this.pool));

  filteredActive = computed(() => {
    const byPriority = this.filterByPriority(this.active());
    const pill = this.pills.find((p) => p.key === this.selectedPill());
    return pill ? byPriority.filter((t) => pill.statuses.includes(t.status)) : byPriority;
  });

  ngOnInit(): void {
    this.tab.set(this.role === 'Specialist' ? 'specialist' : 'generalist');
    this.refreshCounts();
    this.refreshPools();
  }

  selectTab(key: PoolTabKey): void {
    this.tab.set(key);
  }

  selectPill(key: StatusKey): void {
    this.selectedPill.set(this.selectedPill() === key ? null : key);
  }

  countFor(key: StatusKey): number {
    return this.counts()?.[key] ?? 0;
  }

  setPriorityFilter(priority: TicketPriority | 'All'): void {
    this.priorityFilter.set(priority);
  }

  openAccept(ticket: TicketBlind): void {
    this.ticketToConfirm.set(ticket);
  }

  confirmAccept(ticket: TicketBlind): void {
    this.acceptLoading.set(true);
    this.ticketService.accept(ticket.id).subscribe({
      next: () => {
        this.acceptLoading.set(false);
        this.ticketToConfirm.set(null);
        this.router.navigate(['/app/tickets', ticket.id]);
      },
      error: () => {
        this.acceptLoading.set(false);
      },
    });
  }

  decline(ticket: TicketBlind): void {
    this.generalistPool.set(this.generalistPool().filter((t) => t.id !== ticket.id));
    this.specialistPool.set(this.specialistPool().filter((t) => t.id !== ticket.id));
    this.ticketService.decline(ticket.id).subscribe();
  }

  private refreshCounts(): void {
    this.countsLoading.set(true);
    this.ticketService.getStatusCounts(true).subscribe((counts) => {
      this.counts.set(counts);
      this.countsLoading.set(false);
    });
  }

  private refreshPools(): void {
    this.loading.set(true);
    const role = this.role;
    if (role !== 'Specialist') {
      this.ticketService.getPool('generalist').subscribe((tickets) => {
        this.generalistPool.set(tickets);
        this.loading.set(false);
      });
    }
    if (role === 'Specialist' || role === 'Polyvalent') {
      this.ticketService.getPool('specialist').subscribe((tickets) => {
        this.specialistPool.set(tickets);
        this.loading.set(false);
      });
    }
    this.ticketService.getMyActiveTickets(true).subscribe((tickets) => this.active.set(tickets));
  }

  private filterByPriority(tickets: TicketBlind[]): TicketBlind[] {
    const priority = this.priorityFilter();
    return priority === 'All' ? tickets : tickets.filter((t) => t.priority === priority);
  }
}
