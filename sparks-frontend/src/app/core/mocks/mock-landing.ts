import {
  EcosystemNode,
  FairAssignmentStep,
  FlowSource,
  FlowTarget,
  FooterLinkGroup,
  HowItWorksStep,
  ImpactStat,
  KeyFeature,
  LandingNavLink,
  ProblemSolutionColumn,
  SmartTicketCapability,
  SupportRole,
} from '../models/landing.model';
import { TicketBlind } from '../models/ticket.model';

export const LANDING_NAV_LINKS: LandingNavLink[] = [
  { label: 'Home', fragment: 'top' },
  { label: 'Ecosystem', fragment: 'ecosystem' },
  { label: 'How It Works', fragment: 'how-it-works' },
  { label: 'SmartTicket AI', fragment: 'smartticket-ai' },
  { label: 'Features', fragment: 'features' },
  { label: 'About', fragment: 'about' },
];

/** Compact hero visual — abstract preview only, the full breakdown lives in the Ecosystem section. */
export const FLOW_SOURCES: FlowSource[] = [
  { label: 'AIDE', icon: 'layers' },
  { label: 'TOOLSUP', icon: 'monitor' },
];

export const FLOW_TARGETS: FlowTarget[] = [
  { label: 'Generalists', role: 'generalist' },
  { label: 'Specialists', role: 'specialist' },
];

/** Full ecosystem pipeline stages for the dedicated Ecosystem section. */
export const ECOSYSTEM_SOURCES: EcosystemNode[] = [
  { icon: 'layers', label: 'AIDE', caption: 'Engineering support requests' },
  { icon: 'monitor', label: 'TOOLSUP', caption: 'Toolchain incident reports' },
];

export const ECOSYSTEM_HUBS: EcosystemNode[] = [
  { icon: 'cpu', label: 'SPARKS', caption: 'Centralizes and qualifies every ticket' },
  { icon: 'sparkles', label: 'SmartTicket AI', caption: 'Analyzes, classifies and routes automatically' },
];

export const ECOSYSTEM_ROLES: EcosystemNode[] = [
  { icon: 'users', label: 'Generalists', caption: 'Standard and common PLM incidents' },
  { icon: 'user', label: 'Specialists', caption: 'Complex and advanced PLM issues' },
];

export const SMARTTICKET_CAPABILITIES: SmartTicketCapability[] = [
  { icon: 'search', title: 'Ticket Analysis', text: 'Reads every incoming ticket in real time as it lands in the pool.' },
  { icon: 'tag', title: 'Automatic Qualification', text: 'Qualifies incidents by PLM domain the moment they arrive.' },
  { icon: 'layers', title: 'Classification', text: 'Classifies tickets against historical patterns and known issue types.' },
  { icon: 'alert-triangle', title: 'Priority & Severity', text: 'Scores urgency and business impact to set the right SLA.' },
  { icon: 'shuffle', title: 'Intelligent Routing', text: 'Sends every ticket to the right Generalist or Specialist queue.' },
  { icon: 'layers', title: 'Duplicate Detection', text: 'Flags tickets that match already-open or resolved incidents.' },
  { icon: 'lightbulb', title: 'Solution Recommendations', text: 'Suggests fixes drawn from a growing base of resolved cases.' },
  { icon: 'edit', title: 'Ticket Summarization', text: 'Condenses long threads into a concise, actionable summary.' },
];

export const HOW_IT_WORKS_STEPS: HowItWorksStep[] = [
  {
    step: '01',
    icon: 'mail',
    title: 'Ticket Received',
    text: 'Support requests raised on AIDE or TOOLSUP land automatically in the SPARKS ticket pool.',
  },
  {
    step: '02',
    icon: 'cpu',
    title: 'AI Qualification',
    text: 'SmartTicket AI classifies the PLM domain, severity and priority before any engineer sees it.',
  },
  {
    step: '03',
    icon: 'shuffle',
    title: 'Intelligent Routing',
    text: 'The ticket is routed blind to a Generalist or escalated to the right Specialist queue.',
  },
  {
    step: '04',
    icon: 'clock',
    title: 'Faster Resolution',
    text: 'Elegant, guided resolution (with AI-suggested fixes) cuts time to close significantly.',
  },
];

export const KEY_FEATURES: KeyFeature[] = [
  {
    icon: 'tag',
    title: 'Intelligent Ticket Qualification',
    text: 'AI automatically classifies incoming tickets by PLM domain, priority and severity.',
  },
  {
    icon: 'shuffle',
    title: 'Smart Routing',
    text: 'Routing engine sends every ticket to the right Generalist or Specialist queue.',
  },
  {
    icon: 'sparkles',
    title: 'AI-Powered Assistance',
    text: 'SmartTicket AI suggests summaries, similar cases and resolutions as you work.',
  },
  {
    icon: 'shield',
    title: 'Fair Ticket Assignment',
    text: 'Blind acceptance prevents cherry-picking and promotes balanced ticket distribution.',
  },
  {
    icon: 'lightbulb',
    title: 'Knowledge-Based Recommendations',
    text: 'Recommendations are drawn from a growing base of resolved PLM incidents.',
  },
  {
    icon: 'monitor',
    title: 'Real-Time Monitoring',
    text: 'Live SLA countdowns and dashboards keep PLM support management informed.',
  },
];

export const SUPPORT_ROLES: SupportRole[] = [
  {
    role: 'generalist',
    title: 'Generalists',
    text: 'Handle standard and common PLM incidents, the everyday volume of support requests across BOM, ECR and document workflows.',
    examples: ['BOM export errors', 'Document access requests', 'Standard ECR workflow issues', 'Routine PLM-ERP sync checks'],
  },
  {
    role: 'specialist',
    title: 'Specialists',
    text: 'Resolve complex and advanced PLM issues escalated from the generalist queue, deep technical incidents that need domain expertise.',
    examples: ['CAD data corruption', 'Complex change-management conflicts', 'Cross-system integration failures', 'Root-cause engineering analysis'],
  },
];

export const PROBLEM_SOLUTION: ProblemSolutionColumn[] = [
  {
    tone: 'before',
    heading: 'Before SPARKS',
    caption: 'Manual, uneven, and hard to track',
    points: [
      'Support employees manually choose which tickets to work on.',
      'Easy tickets get picked first, left to sit unresolved.',
      'Complex tickets can remain unresolved for longer periods.',
      'Ticket distribution across the team is not balanced.',
    ],
  },
  {
    tone: 'after',
    heading: 'With SPARKS',
    caption: 'Intelligent, fair, and measurable',
    points: [
      'Tickets are intelligently qualified the moment they arrive.',
      'Tickets are routed according to real expertise, not preference.',
      'Generalists handle standard incidents at volume.',
      'Specialists handle complex PLM incidents that need them.',
      'SmartTicket AI assists the support team at every step.',
    ],
  },
];

export const FAIR_ASSIGNMENT_STEPS: FairAssignmentStep[] = [
  { step: '01', icon: 'ticket', title: 'New Ticket', text: 'A ticket enters the SPARKS pool from AIDE or TOOLSUP.' },
  { step: '02', icon: 'lock', title: 'Limited Initial Information', text: 'Only ID, priority, domain and SLA are visible. No title, no description.' },
  { step: '03', icon: 'thumbs-up', title: 'Accept or Decline', text: 'The engineer commits blind, based on metadata alone.' },
  { step: '04', icon: 'eye', title: 'Ticket Details Revealed', text: 'Full context, history and AI insights unlock only after acceptance.' },
  { step: '05', icon: 'check-circle', title: 'Ticket Assigned', text: 'The ticket is now owned end-to-end by the accepting engineer.' },
];

export const FAIR_ASSIGNMENT_DEMO_TICKET: TicketBlind = {
  id: 'TKT-2851',
  createdAt: '2026-07-18',
  priority: 'High',
  domain: 'ECR Workflow',
  status: 'New',
  slaMinutesRemaining: 210,
  aiSuggestedRoute: 'Generalist',
  sourceSystem: 'AIDE',
};

export const IMPACT_STATS: ImpactStat[] = [
  {
    label: 'Faster Ticket Resolution',
    deltaLabel: '+38%',
    tone: 'positive',
    color: 'var(--alten-blue)',
    data: [
      { label: 'W1', value: 42 },
      { label: 'W2', value: 48 },
      { label: 'W3', value: 55 },
      { label: 'W4', value: 61 },
      { label: 'W5', value: 68 },
      { label: 'W6', value: 74 },
    ],
  },
  {
    label: 'Smarter Ticket Routing',
    deltaLabel: '+94%',
    tone: 'positive',
    color: 'var(--ai-accent)',
    data: [
      { label: 'W1', value: 70 },
      { label: 'W2', value: 76 },
      { label: 'W3', value: 81 },
      { label: 'W4', value: 85 },
      { label: 'W5', value: 90 },
      { label: 'W6', value: 94 },
    ],
  },
  {
    label: 'Reduced Manual Qualification',
    deltaLabel: '-52%',
    tone: 'positive',
    color: 'var(--specialist-accent)',
    data: [
      { label: 'W1', value: 95 },
      { label: 'W2', value: 84 },
      { label: 'W3', value: 74 },
      { label: 'W4', value: 63 },
      { label: 'W5', value: 55 },
      { label: 'W6', value: 46 },
    ],
  },
  {
    label: 'Improved Support Efficiency',
    deltaLabel: '+47%',
    tone: 'positive',
    color: 'var(--status-resolved)',
    data: [
      { label: 'W1', value: 50 },
      { label: 'W2', value: 58 },
      { label: 'W3', value: 63 },
      { label: 'W4', value: 70 },
      { label: 'W5', value: 78 },
      { label: 'W6', value: 84 },
    ],
  },
];

export const FOOTER_LINK_GROUPS: FooterLinkGroup[] = [
  {
    title: 'Quick Links',
    links: [
      { label: 'Ecosystem', fragment: 'ecosystem' },
      { label: 'How It Works', fragment: 'how-it-works' },
      { label: 'Features', fragment: 'features' },
      { label: 'Impact', fragment: 'impact' },
    ],
  },
  {
    title: 'SmartTicket AI',
    links: [
      { label: 'AIDE Integration' },
      { label: 'TOOLSUP Integration' },
      { label: 'AI Qualification' },
      { label: 'Fair Assignment', fragment: 'fair-assignment' },
    ],
  },
  {
    title: 'About',
    links: [{ label: 'About SPARKS' }, { label: 'ALTEN Maroc' }, { label: 'Careers' }, { label: 'Training Policy' }],
  },
  {
    title: 'Contact',
    links: [{ label: 'support@sparks.alten.ma' }, { label: '+212 5 22 00 00 00' }, { label: 'Casablanca, Morocco' }],
  },
];

export const INTEGRATIONS = ['Teamcenter', 'CATIA V5/V6', 'SAP ERP', 'Enovia', 'Windchill', 'AIDE Portal'];
