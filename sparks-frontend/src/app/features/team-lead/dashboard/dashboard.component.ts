import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { KpiStatComponent } from '../../../shared/components/kpi-stat/kpi-stat.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DonutChartComponent, DonutDatum } from '../../../shared/components/charts/donut-chart.component';
import { BarChartComponent } from '../../../shared/components/charts/bar-chart.component';
import { LineChartComponent } from '../../../shared/components/charts/line-chart.component';
import { StatisticsService } from '../../../core/services/statistics.service';
import { TicketService } from '../../../core/services/ticket.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminDashboardStats } from '../../../core/models/statistics.model';
import { priorityColor, statusColor, statusLabel } from '../../../shared/utils/badge-colors';
import { formatSla } from '../../../shared/utils/sla.util';
import { MyTicketStats, TicketBlind, TicketFull, TicketStatus } from '../../../core/models/ticket.model';
import { AllTicketsTableComponent } from '../../../shared/components/all-tickets-table/all-tickets-table.component';

@Component({
  selector: 'sp-team-lead-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    KpiStatComponent,
    IconComponent,
    BadgeComponent,
    DonutChartComponent,
    BarChartComponent,
    LineChartComponent,
    AllTicketsTableComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: '../../admin/dashboard/dashboard.component.scss',
})
export class TeamLeadDashboardComponent implements OnInit {
  stats = signal<AdminDashboardStats | null>(null);
  recentTickets = signal<TicketFull[]>([]);
  pool = signal<TicketBlind[]>([]);
  active = signal<TicketBlind[]>([]);
  roleStats = signal<MyTicketStats>({ resolvedToday: 0, avgResolutionHours: 0 });
  loading = signal(true);
  today = new Date();

  statusColor = statusColor;
  priorityColor = priorityColor;
  statusLabel = statusLabel;
  slaLabel = formatSla;

  constructor(private statisticsService: StatisticsService, private ticketService: TicketService, public auth: AuthService) {}

  ngOnInit(): void {
    this.refresh();
    this.ticketService.list({ page: 1, pageSize: 100 }).subscribe((res) => {
      this.pool.set(res.items.filter((ticket) => !ticket.assigneeName));
      this.recentTickets.set(res.items.slice(0, 5));
    });
    this.ticketService.getMyActiveTickets().subscribe((tickets) => this.active.set(tickets));
    this.ticketService.getMyStats().subscribe((stats) => this.roleStats.set(stats));
  }

  refresh(): void {
    this.loading.set(true);
    this.statisticsService.getAdminDashboard().subscribe((s) => {
      this.stats.set(s);
      this.loading.set(false);
    });
  }

  get firstName(): string {
    return this.auth.currentUser()?.firstName ?? '';
  }

  get statusDonutData(): DonutDatum[] {
    const s = this.stats();
    if (!s) return [];
    return s.statusBreakdown
      .filter((d) => d.count > 0)
      .map((d) => ({
        label: statusLabel(d.status as TicketStatus),
        value: d.count,
        color: statusColor(d.status as TicketStatus).fg,
      }));
  }

  get domainBarData() {
    const s = this.stats();
    if (!s) return [];
    return s.domainBreakdown.map((d) => ({ label: d.domain, value: d.count }));
  }

  get resolutionLineData() {
    const s = this.stats();
    if (!s) return [];
    return s.resolutionTrend.map((d) => ({ label: d.label, value: d.hours }));
  }
}
