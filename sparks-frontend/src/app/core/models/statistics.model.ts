export interface KpiDelta {
  label: string;
  value: string;
  deltaPct: number;
  tone: 'positive' | 'negative';
}

export interface StatusBreakdownPoint {
  status: string;
  count: number;
}

export interface DomainBreakdownPoint {
  domain: string;
  count: number;
}

export interface ResolutionTrendPoint {
  label: string;
  hours: number;
}

export interface ImpactStatPoint {
  label: string;
  value: number;
}

export interface ImpactStatApi {
  label: string;
  deltaLabel: string;
  tone: 'positive' | 'negative';
  data: ImpactStatPoint[];
}

export interface AdminDashboardStats {
  totalTickets: number;
  totalTicketsDeltaPct: number;
  openTickets: number;
  openTicketsDeltaPct: number;
  avgResolutionHours: number;
  avgResolutionDeltaPct: number;
  refusalRatePct: number;
  refusalRateDeltaPct: number;
  aiAccuracyPct: number;
  aiAccuracyDeltaPct: number;
  statusBreakdown: StatusBreakdownPoint[];
  domainBreakdown: DomainBreakdownPoint[];
  resolutionTrend: ResolutionTrendPoint[];
}
