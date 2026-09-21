export type PlmDomain =
  | 'BOM'
  | 'ECR Workflow'
  | 'CAD Data Sync'
  | 'PLM-ERP Integration'
  | 'Change Management'
  | 'Document Control';

export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export type TicketStatus =
  | 'New'
  | 'Accepted'
  | 'InProgress'
  | 'Escalated'
  | 'Resolved'
  | 'Closed'
  | 'Cancelled';

export type SuggestedRoute = 'Generalist' | 'Specialist';

export type SourceSystem = 'AIDE' | 'TOOLSUP';

/**
 * Metadata-only shape exposed by /api/tickets/pool.
 * Never carries title/description/attachments — enforces the blind-acceptance rule
 * client-side even though this is mocked, to mirror the real API contract.
 */
export interface TicketBlind {
  id: string;
  createdAt: string;
  priority: TicketPriority;
  domain: PlmDomain;
  status: TicketStatus;
  slaMinutesRemaining: number;
  aiSuggestedRoute: SuggestedRoute;
  sourceSystem: SourceSystem;
}

export interface TicketAttachment {
  id: string;
  name: string;
  url: string;
  sizeKb: number;
}

export interface TicketHistoryEntry {
  id: string;
  label: string;
  detail?: string;
  actor: string;
  timestamp: string;
  isAiGenerated: boolean;
  icon: 'plus' | 'sparkle' | 'check' | 'activity' | 'escalate' | 'lock';
}

export interface TicketComment {
  id: string;
  author: string;
  authorInitials: string;
  message: string;
  timestamp: string;
}

/** Full detail shape, only ever returned by /api/tickets/{id} after acceptance. */
export interface TicketFull extends TicketBlind {
  title: string;
  description: string;
  requesterName: string;
  site: string;
  assigneeId: string | null;
  assigneeName: string | null;
  partNumber?: string;
  ecrNumber?: string;
  attachments: TicketAttachment[];
  history: TicketHistoryEntry[];
  comments: TicketComment[];
  applicantId?: string | null;
  applicantFirstName?: string | null;
  applicantLastName?: string | null;
  referenceSTLA?: string | null;
  application?: string;
  module?: string;
  subModule?: string | null;
  closingDate?: string | null;
  reactivityHours?: number | null;
  level?: SuggestedRoute;
  summary?: string | null;
  jiraLink?: string | null;
  englishAccepted?: boolean;
}

export interface TicketFilters {
  status?: TicketStatus | 'All';
  priority?: TicketPriority | 'All';
  domain?: PlmDomain | 'All';
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface MyTicketStats {
  resolvedToday: number;
  avgResolutionHours: number;
}

export interface TicketStatusCounts {
  open: number;
  inProgress: number;
  forwarded: number;
  done: number;
  closed: number;
  cancelled: number;
}
