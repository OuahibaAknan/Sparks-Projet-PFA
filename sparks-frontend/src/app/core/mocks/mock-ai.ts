import { AiInsightsStats, AiTicketMetadata } from '../models/ai.model';

export const MOCK_AI_METADATA: Record<string, AiTicketMetadata> = {
  'TKT-2840': {
    ticketId: 'TKT-2840',
    summary:
      'ECR-8821 approval completed but CAD publish workflow was not triggered, leaving CATIA V5 assembly referencing obsolete revision B. SAP BOM sync has propagated the incorrect revision downstream. Affects 3 vehicle programs.',
    classifiedDomain: 'BOM',
    classificationConfidencePct: 94,
    similarTickets: [
      { ticketId: 'TKT-2791', domain: 'BOM', similarityPct: 87 },
      { ticketId: 'TKT-2634', domain: 'ECR Workflow', similarityPct: 71 },
      { ticketId: 'TKT-2510', domain: 'CAD Data Sync', similarityPct: 63 },
    ],
    suggestedSolutions: [
      {
        id: 's1',
        rank: 1,
        text: 'Manually re-trigger CAD publish job from Teamcenter workflow admin panel. Confirmed resolves sync failure in 15 min.',
        sourceTicketId: 'TKT-2791',
        needsVerification: true,
        vote: null,
      },
      {
        id: 's2',
        rank: 2,
        text: 'Verify ECR publish rule conditions in Teamcenter admin; add missing CAD-BOM sync trigger to approval checklist.',
        sourceTicketId: 'TKT-2634',
        needsVerification: true,
        vote: null,
      },
    ],
    chat: [
      {
        id: 'm1',
        sender: 'ai',
        text: "Hello! I've analyzed TKT-2840. This appears to be a BOM structure mismatch caused by a recent ECR approval that wasn't fully synchronized across downstream assemblies. Would you like me to list similar resolved tickets?",
        timestamp: '2024-01-15T11:45:00',
      },
    ],
  },
};

export function buildFallbackAiMetadata(ticketId: string, domain: string): AiTicketMetadata {
  return {
    ticketId,
    summary: `SmartTicket AI has classified this ${domain} ticket and found related historical cases. Review the suggested solutions before applying any fix.`,
    classifiedDomain: domain,
    classificationConfidencePct: 82,
    similarTickets: [
      { ticketId: 'TKT-2634', domain, similarityPct: 68 },
      { ticketId: 'TKT-2510', domain, similarityPct: 55 },
    ],
    suggestedSolutions: [
      {
        id: 's1',
        rank: 1,
        text: `Check the ${domain} configuration against the last known-good state and re-sync affected records.`,
        sourceTicketId: 'TKT-2634',
        needsVerification: true,
        vote: null,
      },
    ],
    chat: [
      {
        id: 'm1',
        sender: 'ai',
        text: `Hello! I've analyzed ${ticketId}. Ask me for similar resolved tickets or a root-cause hypothesis any time.`,
        timestamp: new Date().toISOString(),
      },
    ],
  };
}

export const MOCK_AI_INSIGHTS: AiInsightsStats = {
  routingAccuracyPct: 94.2,
  routingAccuracyDeltaPct: 1.8,
  ticketsClassified: 1248,
  lowConfidenceCount: 4,
  estimatedModelCostUsd: 48.2,
  recurringIssueTrend: [
    { label: 'W44', value: 12 },
    { label: 'W45', value: 15 },
    { label: 'W46', value: 20 },
    { label: 'W47', value: 26 },
    { label: 'W48', value: 32 },
    { label: 'W49', value: 30 },
    { label: 'W50', value: 27 },
  ],
  routingAccuracyOverTime: [
    { label: 'W44', value: 89 },
    { label: 'W45', value: 90 },
    { label: 'W46', value: 91 },
    { label: 'W47', value: 91.5 },
    { label: 'W48', value: 93 },
    { label: 'W49', value: 93.6 },
    { label: 'W50', value: 94.2 },
  ],
  lowConfidenceQueue: [
    { ticketId: 'TKT-2841', currentDomain: 'CAD Data Sync', suggestedDomain: 'BOM', aiRoute: 'Specialist', confidencePct: 52 },
    { ticketId: 'TKT-2836', currentDomain: 'BOM', suggestedDomain: 'ECR Workflow', aiRoute: 'Generalist', confidencePct: 58 },
    { ticketId: 'TKT-2829', currentDomain: 'PLM-ERP Integration', suggestedDomain: 'Change Management', aiRoute: 'Specialist', confidencePct: 61 },
    { ticketId: 'TKT-2822', currentDomain: 'ECR Workflow', suggestedDomain: 'Document Control', aiRoute: 'Generalist', confidencePct: 63 },
  ],
  modelInfo: {
    version: 'SmartTicket-v2.3.1',
    lastUpdated: '2024-01-10',
    avgLatencyMs: 142,
    p95LatencyMs: 380,
    monthlyTokens: '2.4M tokens',
    monthlyCostUsd: 48.2,
  },
};
