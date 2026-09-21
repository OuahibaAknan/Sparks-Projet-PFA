import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LineChartComponent } from '../../../../shared/components/charts/line-chart.component';
import { IconComponent } from '../../../../shared/components/icon/icon.component';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { StatisticsService } from '../../../../core/services/statistics.service';
import { ImpactStat } from '../../../../core/models/landing.model';

@Component({
  selector: 'sp-impact',
  standalone: true,
  imports: [CommonModule, LineChartComponent, IconComponent, ScrollRevealDirective],
  templateUrl: './impact.component.html',
  styleUrl: './impact.component.scss',
})
export class ImpactComponent implements OnInit {
  stats = signal<ImpactStat[]>([]);

  constructor(private statisticsService: StatisticsService) {}

  ngOnInit(): void {
    this.statisticsService.getImpactStats().subscribe((stats) => this.stats.set(stats));
  }
}
