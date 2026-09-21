import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { AiBadgeComponent } from '../../../../shared/components/badge/ai-badge.component';
import { BlindTicketCardComponent } from '../../../../shared/components/blind-ticket-card/blind-ticket-card.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { FAIR_ASSIGNMENT_DEMO_TICKET, FAIR_ASSIGNMENT_STEPS } from '../../../../core/mocks/mock-landing';
import { TicketBlind } from '../../../../core/models/ticket.model';

@Component({
  selector: 'sp-fair-ticket-assignment',
  standalone: true,
  imports: [CommonModule, IconComponent, AiBadgeComponent, BlindTicketCardComponent, ScrollRevealDirective],
  templateUrl: './fair-ticket-assignment.component.html',
  styleUrl: './fair-ticket-assignment.component.scss',
})
export class FairTicketAssignmentComponent {
  steps = FAIR_ASSIGNMENT_STEPS;
  demoTicket = FAIR_ASSIGNMENT_DEMO_TICKET;
  revealed = signal(false);
  declined = signal(false);

  reveal(_ticket: TicketBlind): void {
    this.declined.set(false);
    this.revealed.set(true);
  }

  decline(_ticket: TicketBlind): void {
    this.declined.set(true);
  }

  resetDemo(): void {
    this.revealed.set(false);
    this.declined.set(false);
  }
}
