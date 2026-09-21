import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AdminDashboardStats, ImpactStatApi } from '../models/statistics.model';
import { TicketService } from './ticket.service';
import { TicketStatus } from '../models/ticket.model';
import { ImpactStat } from '../models/landing.model';
import { IMPACT_STATS } from '../mocks/mock-landing';

const STATUS_ORDER: TicketStatus[] = ['New', 'Accepted', 'InProgress', 'Escalated', 'Resolved', 'Closed'];

const IMPACT_STAT_COLORS = [
  'var(--alten-blue)',
  'var(--ai-accent)',
  'var(--specialist-accent)',
  'var(--status-resolved)',
];

@Injectable({ providedIn: 'root' })
export class StatisticsService {
  constructor(private http: HttpClient, private ticketService: TicketService) {}

  getAdminDashboard(): Observable<AdminDashboardStats> {
    if (environment.useMockData) {
      return this.ticketService.tickets$.pipe(
        map((tickets) => {
          const total = tickets.length;
          const open = tickets.filter((t) => !['Resolved', 'Closed'].includes(t.status)).length;
          const statusBreakdown = STATUS_ORDER.map((status) => ({
            status,
            count: tickets.filter((t) => t.status === status).length,
          }));
          const domains = Array.from(new Set(tickets.map((t) => t.domain)));
          const domainBreakdown = domains.map((domain) => ({
            domain,
            count: tickets.filter((t) => t.domain === domain).length,
          }));
          return {
            totalTickets: total,
            totalTicketsDeltaPct: 8.3,
            openTickets: open,
            openTicketsDeltaPct: -5.1,
            avgResolutionHours: 2.4,
            avgResolutionDeltaPct: -31,
            refusalRatePct: 12.4,
            refusalRateDeltaPct: -2.1,
            aiAccuracyPct: 94.2,
            aiAccuracyDeltaPct: 1.8,
            statusBreakdown,
            domainBreakdown,
            resolutionTrend: [
              { label: 'Mon', hours: 4.8 },
              { label: 'Tue', hours: 4.4 },
              { label: 'Wed', hours: 3.9 },
              { label: 'Thu', hours: 3.4 },
              { label: 'Fri', hours: 3.0 },
              { label: 'Sat', hours: 2.6 },
              { label: 'Sun', hours: 2.4 },
            ],
          } satisfies AdminDashboardStats;
        }),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<AdminDashboardStats>(`${environment.apiBaseUrl}/statistics/admin-dashboard`);
  }

  getImpactStats(): Observable<ImpactStat[]> {
    if (environment.useMockData) {
      return of(IMPACT_STATS).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.get<ImpactStatApi[]>(`${environment.apiBaseUrl}/statistics/impact`).pipe(
      map((stats) =>
        stats.map((s, i) => ({
          ...s,
          color: IMPACT_STAT_COLORS[i % IMPACT_STAT_COLORS.length],
        }))
      )
    );
  }
}
