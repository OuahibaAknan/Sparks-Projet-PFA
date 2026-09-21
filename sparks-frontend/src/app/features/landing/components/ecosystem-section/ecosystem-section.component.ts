import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { ECOSYSTEM_HUBS, ECOSYSTEM_ROLES, ECOSYSTEM_SOURCES } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-ecosystem-section',
  standalone: true,
  imports: [CommonModule, IconComponent, ScrollRevealDirective],
  templateUrl: './ecosystem-section.component.html',
  styleUrl: './ecosystem-section.component.scss',
})
export class EcosystemSectionComponent {
  sources = ECOSYSTEM_SOURCES;
  hubs = ECOSYSTEM_HUBS;
  roles = ECOSYSTEM_ROLES;
}
