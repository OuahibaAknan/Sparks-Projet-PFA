import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { KEY_FEATURES } from '../../../../core/mocks/mock-landing';

@Component({
  selector: 'sp-features',
  standalone: true,
  imports: [CommonModule, IconComponent, ScrollRevealDirective],
  templateUrl: './features.component.html',
  styleUrl: './features.component.scss',
})
export class FeaturesComponent {
  features = KEY_FEATURES;
}
