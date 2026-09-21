import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KpiStatComponent } from '../../../shared/components/kpi-stat/kpi-stat.component';
import { AllTicketsTableComponent } from '../../../shared/components/all-tickets-table/all-tickets-table.component';
import { TicketService } from '../../../core/services/ticket.service';
import { AuthService } from '../../../core/services/auth.service';
import { MyTicketStats, TicketBlind } from '../../../core/models/ticket.model';

@Component({
  selector: 'sp-polyvalent-dashboard',
  standalone: true,
  imports: [CommonModule, KpiStatComponent, AllTicketsTableComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: '../../generalist/dashboard/dashboard.component.scss',
})
export class PolyvalentDashboardComponent implements OnInit {
  generalistPool = signal<TicketBlind[]>([]);
  specialistPool = signal<TicketBlind[]>([]);
  active = signal<TicketBlind[]>([]);
  stats = signal<MyTicketStats>({ resolvedToday: 0, avgResolutionHours: 0 });

  constructor(private ticketService: TicketService, public auth: AuthService) {}

  ngOnInit(): void {
    this.ticketService.getPool('generalist').subscribe((tickets) => this.generalistPool.set(tickets));
    this.ticketService.getPool('specialist').subscribe((tickets) => this.specialistPool.set(tickets));
    this.ticketService.getMyActiveTickets().subscribe((tickets) => this.active.set(tickets));
    this.ticketService.getMyStats().subscribe((stats) => this.stats.set(stats));
  }

  get firstName(): string {
    return this.auth.currentUser()?.firstName ?? '';
  }

  get criticalCount(): number {
    return [...this.generalistPool(), ...this.specialistPool()].filter((t) => t.priority === 'Critical').length;
  }
}
