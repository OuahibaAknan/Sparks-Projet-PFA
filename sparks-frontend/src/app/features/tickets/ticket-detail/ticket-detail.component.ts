import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { AiBadgeComponent } from '../../../shared/components/badge/ai-badge.component';
import { IconComponent, IconName } from '../../../shared/components/icon/icon.component';
import { AiPanelComponent } from './ai-panel/ai-panel.component';
import { TicketService } from '../../../core/services/ticket.service';
import { AuthService } from '../../../core/services/auth.service';
import { TicketFull, TicketHistoryEntry } from '../../../core/models/ticket.model';
import { priorityColor, statusColor, statusLabel } from '../../../shared/utils/badge-colors';
import { formatSla } from '../../../shared/utils/sla.util';

const HISTORY_ICON_MAP: Record<TicketHistoryEntry['icon'], IconName> = {
  plus: 'plus',
  sparkle: 'sparkles',
  check: 'check',
  activity: 'activity',
  escalate: 'arrow-up-right',
  lock: 'lock',
};

@Component({
  selector: 'sp-ticket-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BadgeComponent, AiBadgeComponent, IconComponent, AiPanelComponent],
  templateUrl: './ticket-detail.component.html',
  styleUrl: './ticket-detail.component.scss',
})
export class TicketDetailComponent implements OnInit {
  ticket = signal<TicketFull | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  commentDraft = '';
  actionLoading = signal(false);
  commentSubmitting = signal(false);

  statusColor = statusColor;
  priorityColor = priorityColor;
  statusLabel = statusLabel;
  slaLabel = formatSla;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private ticketService: TicketService,
    public auth: AuthService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.load(id);
  }

  private load(id: string): void {
    this.loading.set(true);
    this.ticketService.getById(id).subscribe({
      next: (t) => {
        this.ticket.set(t);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message ?? 'Unable to load this ticket.');
        this.loading.set(false);
      },
    });
  }

  historyIcon(icon: TicketHistoryEntry['icon']): IconName {
    return HISTORY_ICON_MAP[icon];
  }

  backRoute(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Specialist') return '/app/specialist';
    if (role === 'Admin') return '/app/admin';
    if (role === 'TeamLead') return '/app/team-lead';
    if (role === 'Polyvalent') return '/app/polyvalent';
    return '/app/generalist';
  }

  escalate(): void {
    const t = this.ticket();
    if (!t) return;
    this.actionLoading.set(true);
    this.ticketService.escalate(t.id).subscribe(() => {
      this.actionLoading.set(false);
      this.router.navigate([this.backRoute()]);
    });
  }

  resolve(): void {
    const t = this.ticket();
    if (!t) return;
    this.actionLoading.set(true);
    this.ticketService.resolve(t.id).subscribe((updated) => {
      this.ticket.set(updated);
      this.actionLoading.set(false);
    });
  }

  close(): void {
    const t = this.ticket();
    if (!t) return;
    this.actionLoading.set(true);
    this.ticketService.close(t.id).subscribe((updated) => {
      this.ticket.set(updated);
      this.actionLoading.set(false);
    });
  }

  addComment(): void {
    const t = this.ticket();
    const message = this.commentDraft.trim();
    if (!t || !message || this.commentSubmitting()) return;
    this.commentSubmitting.set(true);
    this.ticketService.addComment(t.id, message).subscribe({
      next: (updated) => {
        this.ticket.set(updated);
        this.commentDraft = '';
        this.commentSubmitting.set(false);
      },
      error: () => {
        this.commentSubmitting.set(false);
      },
    });
  }
}
