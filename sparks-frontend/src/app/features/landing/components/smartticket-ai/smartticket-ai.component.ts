import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { SMARTTICKET_CAPABILITIES } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-smartticket-ai',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent, ScrollRevealDirective],
  templateUrl: './smartticket-ai.component.html',
  styleUrl: './smartticket-ai.component.scss',
})
export class SmartticketAiComponent {
  capabilities = SMARTTICKET_CAPABILITIES;
}
