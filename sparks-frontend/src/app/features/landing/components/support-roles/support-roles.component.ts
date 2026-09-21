import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { SUPPORT_ROLES } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-support-roles',
  standalone: true,
  imports: [CommonModule, IconComponent, ScrollRevealDirective],
  templateUrl: './support-roles.component.html',
  styleUrl: './support-roles.component.scss',
})
export class SupportRolesComponent {
  roles = SUPPORT_ROLES;
}
