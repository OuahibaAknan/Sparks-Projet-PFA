import { TicketPriority, TicketStatus } from '../../core/models/ticket.model';
import { UserRole } from '../../core/models/user.model';

export interface BadgeColor {
  fg: string;
  bg: string;
}

const STATUS_COLORS: Record<TicketStatus, BadgeColor> = {
  New: { fg: 'var(--status-new)', bg: 'var(--status-new-bg)' },
  Accepted: { fg: 'var(--status-accepted)', bg: 'var(--status-accepted-bg)' },
  InProgress: { fg: 'var(--status-inprogress)', bg: 'var(--status-inprogress-bg)' },
  Escalated: { fg: 'var(--status-escalated)', bg: 'var(--status-escalated-bg)' },
  Resolved: { fg: 'var(--status-resolved)', bg: 'var(--status-resolved-bg)' },
  Closed: { fg: 'var(--status-closed)', bg: 'var(--status-closed-bg)' },
  Cancelled: { fg: 'var(--status-cancelled)', bg: 'var(--status-cancelled-bg)' },
};

const PRIORITY_COLORS: Record<TicketPriority, BadgeColor> = {
  Low: { fg: 'var(--priority-low)', bg: 'var(--priority-low-bg)' },
  Medium: { fg: 'var(--priority-medium)', bg: 'var(--priority-medium-bg)' },
  High: { fg: 'var(--priority-high)', bg: 'var(--priority-high-bg)' },
  Critical: { fg: 'var(--priority-critical)', bg: 'var(--priority-critical-bg)' },
};

const ROLE_COLORS: Record<UserRole, BadgeColor> = {
  Generalist: { fg: 'var(--text-secondary)', bg: 'var(--status-new-bg)' },
  Specialist: { fg: 'var(--alten-blue)', bg: 'var(--status-accepted-bg)' },
  Admin: { fg: 'var(--ai-accent)', bg: 'var(--ai-accent-soft)' },
  TeamLead: { fg: '#B45309', bg: '#FEF3C7' },
  Polyvalent: { fg: '#0F766E', bg: '#CCFBF1' },
};

export function statusColor(status: TicketStatus): BadgeColor {
  return STATUS_COLORS[status];
}

export function priorityColor(priority: TicketPriority): BadgeColor {
  return PRIORITY_COLORS[priority];
}

export function roleColor(role: UserRole): BadgeColor {
  return ROLE_COLORS[role];
}

export function statusLabel(status: TicketStatus): string {
  if (status === 'InProgress') return 'In Progress';
  return status;
}
