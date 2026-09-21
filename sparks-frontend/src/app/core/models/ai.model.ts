export interface SimilarTicket {
  ticketId: string;
  domain: string;
  similarityPct: number;
}

export interface SuggestedSolution {
  id: string;
  rank: number;
  text: string;
  sourceTicketId: string;
  needsVerification: boolean;
  vote: 'up' | 'down' | null;
}

export interface AiChatMessage {
  id: string;
  sender: 'ai' | 'user';
  text: string;
  timestamp: string;
}

export interface AiTicketMetadata {
  ticketId: string;
  summary: string;
  classifiedDomain: string;
  classificationConfidencePct: number;
  similarTickets: SimilarTicket[];
  suggestedSolutions: SuggestedSolution[];
  chat: AiChatMessage[];
}

export interface LowConfidenceItem {
  ticketId: string;
  currentDomain: string;
  suggestedDomain: string;
  aiRoute: 'Generalist' | 'Specialist';
  confidencePct: number;
}

export interface ModelMonitoringInfo {
  version: string;
  lastUpdated: string;
  avgLatencyMs: number;
  p95LatencyMs: number;
  monthlyTokens: string;
  monthlyCostUsd: number;
}

export interface AiInsightsStats {
  routingAccuracyPct: number;
  routingAccuracyDeltaPct: number;
  ticketsClassified: number;
  lowConfidenceCount: number;
  estimatedModelCostUsd: number;
  recurringIssueTrend: { label: string; value: number }[];
  routingAccuracyOverTime: { label: string; value: number }[];
  lowConfidenceQueue: LowConfidenceItem[];
  modelInfo: ModelMonitoringInfo;
}
