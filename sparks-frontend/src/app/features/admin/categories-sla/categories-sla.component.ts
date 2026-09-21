import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { PlmDomain, TicketPriority } from '../../../core/models/ticket.model';

interface SlaRule {
  domain: PlmDomain;
  low: number;
  medium: number;
  high: number;
  critical: number;
}

const DEFAULT_RULES: SlaRule[] = [
  { domain: 'BOM', low: 48, medium: 24, high: 8, critical: 2 },
  { domain: 'ECR Workflow', low: 48, medium: 24, high: 6, critical: 2 },
  { domain: 'CAD Data Sync', low: 36, medium: 18, high: 8, critical: 3 },
  { domain: 'PLM-ERP Integration', low: 48, medium: 24, high: 8, critical: 4 },
  { domain: 'Change Management', low: 48, medium: 20, high: 8, critical: 3 },
  { domain: 'Document Control', low: 24, medium: 12, high: 4, critical: 1 },
];

@Component({
  selector: 'sp-categories-sla',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, ModalComponent],
  templateUrl: './categories-sla.component.html',
  styleUrl: './categories-sla.component.scss',
})
export class CategoriesSlaComponent {
  rules = signal<SlaRule[]>(structuredClone(DEFAULT_RULES));
  editing = signal<SlaRule | null>(null);

  edit(rule: SlaRule): void {
    this.editing.set(structuredClone(rule));
  }

  save(): void {
    const updated = this.editing();
    if (!updated) return;
    this.rules.set(this.rules().map((r) => (r.domain === updated.domain ? updated : r)));
    this.editing.set(null);
  }
}
