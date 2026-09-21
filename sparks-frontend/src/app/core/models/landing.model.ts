import { IconName } from '../../shared/components/icon/icon.component';
import { LineDatum } from '../../shared/components/charts/line-chart.component';

export interface LandingNavLink {
  label: string;
  fragment: string;
}

export interface FlowSource {
  label: string;
  icon: IconName;
}

export interface FlowTarget {
  label: string;
  role: 'generalist' | 'specialist';
}

export interface EcosystemNode {
  icon: IconName;
  label: string;
  caption: string;
}

export interface SmartTicketCapability {
  icon: IconName;
  title: string;
  text: string;
}

export interface HowItWorksStep {
  step: string;
  icon: IconName;
  title: string;
  text: string;
}

export interface KeyFeature {
  icon: IconName;
  title: string;
  text: string;
  active?: boolean;
}

export interface SupportRole {
  role: 'generalist' | 'specialist';
  title: string;
  text: string;
  examples: string[];
}

export interface ProblemSolutionColumn {
  tone: 'before' | 'after';
  heading: string;
  caption: string;
  points: string[];
}

export interface FairAssignmentStep {
  step: string;
  icon: IconName;
  title: string;
  text: string;
}

export interface ImpactStat {
  label: string;
  deltaLabel: string;
  tone: 'positive' | 'negative';
  color: string;
  data: LineDatum[];
}

export interface FooterLinkGroup {
  title: string;
  links: { label: string; fragment?: string }[];
}
