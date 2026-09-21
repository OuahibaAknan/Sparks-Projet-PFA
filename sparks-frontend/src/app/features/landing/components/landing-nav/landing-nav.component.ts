import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { LANDING_NAV_LINKS } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-landing-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent],
  templateUrl: './landing-nav.component.html',
  styleUrl: './landing-nav.component.scss',
})
export class LandingNavComponent {
  navLinks = LANDING_NAV_LINKS;
  menuOpen = signal(false);

  toggleMenu(): void {
    this.menuOpen.update((v) => !v);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
