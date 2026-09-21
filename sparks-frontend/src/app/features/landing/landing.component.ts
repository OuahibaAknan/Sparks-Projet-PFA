import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LandingNavComponent } from './components/landing-nav/landing-nav.component';
import { HeroComponent } from './components/hero/hero.component';
import { EcosystemSectionComponent } from './components/ecosystem-section/ecosystem-section.component';
import { ProblemSolutionComponent } from './components/problem-solution/problem-solution.component';
import { HowItWorksComponent } from './components/how-it-works/how-it-works.component';
import { SmartticketAiComponent } from './components/smartticket-ai/smartticket-ai.component';
import { SupportRolesComponent } from './components/support-roles/support-roles.component';
import { FairTicketAssignmentComponent } from './components/fair-ticket-assignment/fair-ticket-assignment.component';
import { FeaturesComponent } from './components/features/features.component';
import { ImpactComponent } from './components/impact/impact.component';
import { CtaComponent } from './components/cta/cta.component';
import { FooterComponent } from './components/footer/footer.component';

@Component({
  selector: 'sp-landing',
  standalone: true,
  imports: [
    CommonModule,
    LandingNavComponent,
    HeroComponent,
    EcosystemSectionComponent,
    ProblemSolutionComponent,
    HowItWorksComponent,
    SmartticketAiComponent,
    SupportRolesComponent,
    FairTicketAssignmentComponent,
    FeaturesComponent,
    ImpactComponent,
    CtaComponent,
    FooterComponent,
  ],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
})
export class LandingComponent {}
