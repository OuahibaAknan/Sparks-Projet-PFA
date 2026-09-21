import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { HeroOrbitComponent } from './hero-orbit/hero-orbit.component';

@Component({
  selector: 'sp-hero',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent, ScrollRevealDirective, HeroOrbitComponent],
  templateUrl: './hero.component.html',
  styleUrl: './hero.component.scss',
})
export class HeroComponent {}
