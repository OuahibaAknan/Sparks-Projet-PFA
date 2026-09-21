import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { take } from 'rxjs/operators';
import { IconComponent, IconName } from '../../../shared/components/icon/icon.component';
import { TicketService } from '../../../core/services/ticket.service';
import { TicketFull } from '../../../core/models/ticket.model';

interface ReportDef {
  id: string;
  icon: IconName;
  title: string;
  description: string;
}

const REPORTS: ReportDef[] = [
  { id: 'all-tickets', icon: 'list', title: 'Full Ticket Export', description: 'All tickets with status, priority, domain, SLA and assignee.' },
  { id: 'resolved', icon: 'check-circle', title: 'Resolved & Closed Report', description: 'Tickets resolved or closed, for SLA compliance review.' },
  { id: 'escalations', icon: 'alert-triangle', title: 'Escalation Report', description: 'Tickets escalated from Generalist to Specialist pool.' },
];

@Component({
  selector: 'sp-reports',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
})
export class ReportsComponent {
  reports = REPORTS;
  generating = signal<string | null>(null);

  constructor(private ticketService: TicketService) {}

  generate(report: ReportDef): void {
    this.generating.set(report.id);
    this.ticketService
      .tickets$.pipe(take(1))
      .subscribe((tickets) => {
        let filtered = tickets;
        if (report.id === 'resolved') filtered = tickets.filter((t) => t.status === 'Resolved' || t.status === 'Closed');
        if (report.id === 'escalations') filtered = tickets.filter((t) => t.status === 'Escalated' || t.aiSuggestedRoute === 'Specialist');
        this.downloadCsv(report.id, filtered);
        this.generating.set(null);
      });
  }

  private downloadCsv(name: string, tickets: TicketFull[]): void {
    const header = ['ID', 'Title', 'Priority', 'Domain', 'Status', 'Assignee', 'Created', 'SLA Minutes Remaining'];
    const rows = tickets.map((t) => [
      t.id,
      `"${t.title.replace(/"/g, '""')}"`,
      t.priority,
      t.domain,
      t.status,
      t.assigneeName ?? 'Unassigned',
      t.createdAt,
      t.slaMinutesRemaining,
    ]);
    const csv = [header, ...rows].map((r) => r.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `sparks-${name}-${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }
}
