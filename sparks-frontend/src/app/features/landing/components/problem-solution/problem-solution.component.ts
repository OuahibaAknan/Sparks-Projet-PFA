import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PROBLEM_SOLUTION } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-problem-solution',
  standalone: true,
  imports: [CommonModule, IconComponent, ScrollRevealDirective],
  templateUrl: './problem-solution.component.html',
  styleUrl: './problem-solution.component.scss',
})
export class ProblemSolutionComponent {
  columns = PROBLEM_SOLUTION;
}
