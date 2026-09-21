import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AiInsightsStats, AiTicketMetadata } from '../models/ai.model';
import { MOCK_AI_INSIGHTS, MOCK_AI_METADATA, buildFallbackAiMetadata } from '../mocks/mock-ai';

@Injectable({ providedIn: 'root' })
export class AiService {
  constructor(private http: HttpClient) {}

  getTicketMetadata(ticketId: string, domain: string): Observable<AiTicketMetadata> {
    if (environment.useMockData) {
      const metadata = MOCK_AI_METADATA[ticketId] ?? buildFallbackAiMetadata(ticketId, domain);
      return of(metadata).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.get<AiTicketMetadata>(`${environment.apiBaseUrl}/tickets/${ticketId}/ai-metadata`);
  }

  getInsights(): Observable<AiInsightsStats> {
    if (environment.useMockData) {
      return of(MOCK_AI_INSIGHTS).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.get<AiInsightsStats>(`${environment.apiBaseUrl}/statistics/ai-insights`);
  }

  /** Always hits the real backend (Gemini-powered) — not mocked, even when the rest of the app runs on mock data. */
  askAssistant(ticketId: string, question: string): Observable<string> {
    return this.http
      .post<{ reply: string }>(`${environment.apiBaseUrl}/tickets/${ticketId}/ai-chat`, { question })
      .pipe(map((res) => res.reply));
  }

  /** Explicit admin approval/correction of an AI domain+route suggestion — the only path that ever
   * writes the AI's suggestion to the ticket; it is never applied automatically. */
  reviewDomainSuggestion(ticketId: string, domain: string, route: 'Generalist' | 'Specialist'): Observable<void> {
    if (environment.useMockData) {
      return of(undefined).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.post<void>(`${environment.apiBaseUrl}/tickets/${ticketId}/ai-domain-review`, { domain, route });
  }
}
