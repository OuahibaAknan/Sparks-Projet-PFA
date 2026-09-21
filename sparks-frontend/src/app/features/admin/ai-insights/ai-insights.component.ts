import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { LineChartComponent } from '../../../shared/components/charts/line-chart.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { AiService } from '../../../core/services/ai.service';
import { AiInsightsStats, LowConfidenceItem } from '../../../core/models/ai.model';
import { PLM_DOMAINS } from '../../../shared/constants/plm-domains';

@Component({
  selector: 'sp-ai-insights',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, LineChartComponent, ModalComponent],
  templateUrl: './ai-insights.component.html',
  styleUrl: './ai-insights.component.scss',
})
export class AiInsightsComponent implements OnInit {
  stats = signal<AiInsightsStats | null>(null);
  correcting = signal<LowConfidenceItem | null>(null);
  correctionDomain = '';
  correctionRoute: 'Generalist' | 'Specialist' = 'Generalist';
  domains = PLM_DOMAINS;
  reviewingTicketId = signal<string | null>(null);
  reviewError = signal<string | null>(null);

  constructor(private aiService: AiService) {}

  ngOnInit(): void {
    this.aiService.getInsights().subscribe((s) => this.stats.set(s));
  }

  /** Approves the AI's own suggestion as-is — still an explicit admin action, never automatic. */
  approve(item: LowConfidenceItem): void {
    this.submitReview(item.ticketId, item.suggestedDomain, item.aiRoute);
  }

  openCorrect(item: LowConfidenceItem): void {
    this.correcting.set(item);
    this.correctionDomain = item.suggestedDomain;
    this.correctionRoute = item.aiRoute;
  }

  saveCorrection(): void {
    const item = this.correcting();
    if (!item) return;
    this.submitReview(item.ticketId, this.correctionDomain, this.correctionRoute, () => this.correcting.set(null));
  }

  private submitReview(ticketId: string, domain: string, route: 'Generalist' | 'Specialist', onSuccess?: () => void): void {
    this.reviewingTicketId.set(ticketId);
    this.reviewError.set(null);
    this.aiService.reviewDomainSuggestion(ticketId, domain, route).subscribe({
      next: () => {
        this.reviewingTicketId.set(null);
        this.removeFromQueue(ticketId);
        onSuccess?.();
      },
      error: () => {
        this.reviewingTicketId.set(null);
        this.reviewError.set(`Failed to apply the domain review for ${ticketId}.`);
      },
    });
  }

  private removeFromQueue(ticketId: string): void {
    const s = this.stats();
    if (!s) return;
    this.stats.set({
      ...s,
      lowConfidenceQueue: s.lowConfidenceQueue.filter((i) => i.ticketId !== ticketId),
      lowConfidenceCount: Math.max(0, s.lowConfidenceCount - 1),
    });
  }
}
