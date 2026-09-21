import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  MyTicketStats,
  PagedResult,
  TicketBlind,
  TicketFilters,
  TicketFull,
  TicketHistoryEntry,
  TicketStatusCounts,
} from '../models/ticket.model';
import { MOCK_TICKETS } from '../mocks/mock-tickets';
import { AuthService } from './auth.service';

function toBlind(t: TicketFull): TicketBlind {
  const { id, createdAt, priority, domain, status, slaMinutesRemaining, aiSuggestedRoute, sourceSystem } = t;
  return { id, createdAt, priority, domain, status, slaMinutesRemaining, aiSuggestedRoute, sourceSystem };
}

/** Omits undefined/empty filter values so HttpParams never serializes literal "undefined" strings into the query string. */
function toHttpParams(filters: TicketFilters): HttpParams {
  let params = new HttpParams();
  if (filters.status) params = params.set('status', filters.status);
  if (filters.priority) params = params.set('priority', filters.priority);
  if (filters.domain) params = params.set('domain', filters.domain);
  if (filters.search) params = params.set('search', filters.search);
  if (filters.page != null) params = params.set('page', filters.page);
  if (filters.pageSize != null) params = params.set('pageSize', filters.pageSize);
  return params;
}

function historyEntry(label: string, actor: string, icon: TicketHistoryEntry['icon'], isAiGenerated = false): TicketHistoryEntry {
  return { id: `h-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, label, actor, timestamp: new Date().toISOString(), isAiGenerated, icon };
}

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly ticketsSubject = new BehaviorSubject<TicketFull[]>(
    environment.useMockData ? structuredClone(MOCK_TICKETS) : []
  );
  readonly tickets$ = this.ticketsSubject.asObservable();

  constructor(private http: HttpClient, private auth: AuthService) {}

  /** Blind pool — never exposes title/description. scope='specialist' limits to escalated tickets. */
  getPool(scope: 'generalist' | 'specialist' = 'generalist'): Observable<TicketBlind[]> {
    if (environment.useMockData) {
      return this.tickets$.pipe(
        map((tickets) =>
          tickets
            .filter((t) =>
              scope === 'specialist'
                ? t.status === 'Escalated' && !t.assigneeId
                : t.status === 'New' && !t.assigneeId
            )
            .map(toBlind)
        ),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<TicketBlind[]>(`${environment.apiBaseUrl}/tickets/pool`, { params: { scope } });
  }

  /** Tickets already assigned to the current user — safe to surface as blind cards linking to full detail.
   *  includeAll=true also returns Resolved/Closed/Cancelled tickets (for the Ticket Management status filters). */
  getMyActiveTickets(includeAll = false): Observable<TicketBlind[]> {
    const userId = this.auth.currentUser()?.sub;
    if (environment.useMockData) {
      return this.tickets$.pipe(
        map((tickets) =>
          tickets
            .filter((t) => t.assigneeId === userId && (includeAll || !['Resolved', 'Closed'].includes(t.status)))
            .map(toBlind)
        ),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<TicketBlind[]>(`${environment.apiBaseUrl}/tickets/mine`, { params: { includeAll } });
  }

  /** Personal KPIs (tickets resolved today, average resolution time) for the dashboard cards. */
  getMyStats(): Observable<MyTicketStats> {
    const userId = this.auth.currentUser()?.sub;
    if (environment.useMockData) {
      return this.tickets$.pipe(
        map((tickets) => {
          const todayStr = new Date().toDateString();
          let resolvedToday = 0;
          const resolutionHours: number[] = [];
          for (const t of tickets) {
            if (t.assigneeId !== userId) continue;
            const resolvedEntry = [...t.history].reverse().find((h) => h.label === 'Marked as Resolved');
            if (!resolvedEntry) continue;
            const resolvedDate = new Date(resolvedEntry.timestamp);
            if (resolvedDate.toDateString() === todayStr) resolvedToday++;
            resolutionHours.push((resolvedDate.getTime() - new Date(t.createdAt).getTime()) / 3_600_000);
          }
          const avgResolutionHours = resolutionHours.length
            ? Math.round((resolutionHours.reduce((a, b) => a + b, 0) / resolutionHours.length) * 10) / 10
            : 0;
          return { resolvedToday, avgResolutionHours };
        }),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<MyTicketStats>(`${environment.apiBaseUrl}/tickets/my-stats`);
  }

  /** Full detail — only ever resolves for a ticket assigned to the current user (mirrors backend authorization). */
  getById(id: string): Observable<TicketFull> {
    if (environment.useMockData) {
      const userId = this.auth.currentUser()?.sub;
      const ticket = this.ticketsSubject.value.find((t) => t.id === id);
      if (!ticket) return throwError(() => new Error('Ticket not found.')).pipe(delay(environment.mockLatencyMs));
      const role = this.auth.currentUser()?.role;
      if (ticket.assigneeId !== userId && role !== 'Admin' && role !== 'TeamLead') {
        return throwError(() => new Error('Ticket not yet assigned to you.')).pipe(delay(environment.mockLatencyMs));
      }
      return of(structuredClone(ticket)).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.get<TicketFull>(`${environment.apiBaseUrl}/tickets/${id}`);
  }

  accept(id: string): Observable<TicketFull> {
    const user = this.auth.currentUser();
    if (environment.useMockData) {
      return this.mutate(id, (t) => {
        t.status = 'Accepted';
        t.assigneeId = user?.sub ?? null;
        t.assigneeName = user ? `${user.firstName} ${user.lastName}` : null;
        t.history.push(historyEntry('Accepted ticket from pool', t.assigneeName ?? 'Unknown', 'check'));
      });
    }
    return this.http.post<TicketFull>(`${environment.apiBaseUrl}/tickets/${id}/accept`, {});
  }

  decline(id: string): Observable<void> {
    if (environment.useMockData) {
      return this.mutate(id, (t) => {
        t.history.push(historyEntry('Declined from pool', this.auth.currentUser()?.firstName ?? 'Unknown', 'activity'));
      }).pipe(map(() => void 0));
    }
    return this.http.post<void>(`${environment.apiBaseUrl}/tickets/${id}/decline`, {});
  }

  escalate(id: string): Observable<TicketFull> {
    if (environment.useMockData) {
      return this.mutate(id, (t) => {
        t.status = 'Escalated';
        t.assigneeId = null;
        t.assigneeName = null;
        t.aiSuggestedRoute = 'Specialist';
        t.history.push(historyEntry('Escalated to Specialist', this.auth.currentUser()?.firstName ?? 'Unknown', 'escalate'));
      });
    }
    return this.http.post<TicketFull>(`${environment.apiBaseUrl}/tickets/${id}/escalate`, {});
  }

  resolve(id: string): Observable<TicketFull> {
    if (environment.useMockData) {
      return this.mutate(id, (t) => {
        t.status = 'Resolved';
        t.history.push(historyEntry('Marked as Resolved', this.auth.currentUser()?.firstName ?? 'Unknown', 'check'));
      });
    }
    return this.http.post<TicketFull>(`${environment.apiBaseUrl}/tickets/${id}/resolve`, {});
  }

  close(id: string): Observable<TicketFull> {
    if (environment.useMockData) {
      return this.mutate(id, (t) => {
        t.status = 'Closed';
        t.history.push(historyEntry('Ticket closed', this.auth.currentUser()?.firstName ?? 'Unknown', 'check'));
      });
    }
    return this.http.post<TicketFull>(`${environment.apiBaseUrl}/tickets/${id}/close`, {});
  }

  addComment(id: string, message: string): Observable<TicketFull> {
    if (environment.useMockData) {
      const user = this.auth.currentUser();
      return this.mutate(id, (t) => {
        t.comments.push({
          id: `c-${Date.now()}`,
          author: user ? `${user.firstName} ${user.lastName}` : 'Unknown',
          authorInitials: user?.initials ?? '??',
          message,
          timestamp: new Date().toISOString(),
        });
      });
    }
    return this.http.post<TicketFull>(`${environment.apiBaseUrl}/tickets/${id}/comments`, { message });
  }

  /** Status-bucket counts for the Ticket Management filter pills. mineOnly=false also includes the role's pool. */
  getStatusCounts(mineOnly: boolean): Observable<TicketStatusCounts> {
    const userId = this.auth.currentUser()?.sub;
    const role = this.auth.currentUser()?.role;
    if (environment.useMockData) {
      return this.tickets$.pipe(
        map((tickets) => {
          const mine = tickets.filter((t) => t.assigneeId === userId);
          const pool = mineOnly
            ? []
            : tickets.filter((t) => {
                if (t.assigneeId) return false;
                if (role === 'Specialist') return t.status === 'Escalated';
                if (role === 'Polyvalent') return t.status === 'New' || t.status === 'Escalated';
                return t.status === 'New';
              });
          const all = [...mine, ...pool];
          const count = (...statuses: string[]) => all.filter((t) => statuses.includes(t.status)).length;
          return {
            open: count('New', 'Accepted'),
            inProgress: count('InProgress'),
            forwarded: count('Escalated'),
            done: count('Resolved'),
            closed: count('Closed'),
            cancelled: count('Cancelled'),
          };
        }),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<TicketStatusCounts>(`${environment.apiBaseUrl}/tickets/status-counts`, { params: { mineOnly } });
  }

  /** Admin-facing list — full visibility, filters + pagination, backs "All Tickets" / Ticket Monitoring. */
  list(filters: TicketFilters): Observable<PagedResult<TicketFull>> {
    if (environment.useMockData) {
      return this.tickets$.pipe(
        map((tickets) => {
          let result = [...tickets];
          if (filters.status && filters.status !== 'All') result = result.filter((t) => t.status === filters.status);
          if (filters.priority && filters.priority !== 'All') result = result.filter((t) => t.priority === filters.priority);
          if (filters.domain && filters.domain !== 'All') result = result.filter((t) => t.domain === filters.domain);
          if (filters.search) {
            const q = filters.search.toLowerCase();
            result = result.filter(
              (t) => t.id.toLowerCase().includes(q) || t.title.toLowerCase().includes(q) || t.domain.toLowerCase().includes(q)
            );
          }
          result.sort((a, b) => (a.createdAt < b.createdAt ? 1 : -1));
          const page = filters.page ?? 1;
          const pageSize = filters.pageSize ?? 8;
          const start = (page - 1) * pageSize;
          return {
            items: result.slice(start, start + pageSize),
            total: result.length,
            page,
            pageSize,
          };
        }),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<PagedResult<TicketFull>>(`${environment.apiBaseUrl}/tickets`, { params: toHttpParams(filters) });
  }

  /** Read-only, blind (no title/description) paginated view of every ticket — backs the dashboard's "All Tickets" section. */
  getAllBlind(filters: TicketFilters): Observable<PagedResult<TicketBlind>> {
    if (environment.useMockData) {
      return this.tickets$.pipe(
        map((tickets) => {
          let result = tickets.map(toBlind);
          if (filters.status && filters.status !== 'All') result = result.filter((t) => t.status === filters.status);
          if (filters.priority && filters.priority !== 'All') result = result.filter((t) => t.priority === filters.priority);
          if (filters.domain && filters.domain !== 'All') result = result.filter((t) => t.domain === filters.domain);
          if (filters.search) {
            const q = filters.search.toLowerCase();
            result = result.filter(
              (t) =>
                t.id.toLowerCase().includes(q) ||
                t.domain.toLowerCase().includes(q) ||
                t.status.toLowerCase().includes(q) ||
                t.priority.toLowerCase().includes(q) ||
                t.aiSuggestedRoute.toLowerCase().includes(q)
            );
          }
          result.sort((a, b) => (a.createdAt < b.createdAt ? 1 : -1));
          const page = filters.page ?? 1;
          const pageSize = filters.pageSize ?? 8;
          const start = (page - 1) * pageSize;
          return {
            items: result.slice(start, start + pageSize),
            total: result.length,
            page,
            pageSize,
          };
        }),
        delay(environment.mockLatencyMs)
      );
    }
    return this.http.get<PagedResult<TicketBlind>>(`${environment.apiBaseUrl}/tickets/all`, { params: toHttpParams(filters) });
  }

  private mutate(id: string, fn: (t: TicketFull) => void): Observable<TicketFull> {
    const tickets = this.ticketsSubject.value;
    const idx = tickets.findIndex((t) => t.id === id);
    if (idx === -1) return throwError(() => new Error('Ticket not found.')).pipe(delay(environment.mockLatencyMs));
    const updated = structuredClone(tickets[idx]);
    fn(updated);
    const next = [...tickets];
    next[idx] = updated;
    this.ticketsSubject.next(next);
    return of(structuredClone(updated)).pipe(delay(environment.mockLatencyMs));
  }
}
