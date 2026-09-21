import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';

/**
 * Abstract, decorative preview of the SPARKS ecosystem for the hero section.
 * The full, explained breakdown lives in <sp-ecosystem-section> further down the page —
 * this component is intentionally lighter (no dense labels/grid) so the hero stays airy.
 */
@Component({
  selector: 'sp-hero-orbit',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './hero-orbit.component.html',
  styleUrl: './hero-orbit.component.scss',
})
export class HeroOrbitComponent {}
