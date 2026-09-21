import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { HOW_IT_WORKS_STEPS } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-how-it-works',
  standalone: true,
  imports: [CommonModule, IconComponent, ScrollRevealDirective],
  templateUrl: './how-it-works.component.html',
  styleUrl: './how-it-works.component.scss',
})
export class HowItWorksComponent {
  steps = HOW_IT_WORKS_STEPS;
}
