import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'sp-cta',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent, ScrollRevealDirective],
  templateUrl: './cta.component.html',
  styleUrl: './cta.component.scss',
})
export class CtaComponent {}
