import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DashboardMetric } from '../../../models/dashboard-metric.model';
import { SectionHeaderComponent } from '../../section-header/section-header.component';
import { BudgetService } from '../../../services/budget.service';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-dashboard-page',
  imports: [CardModule, ButtonModule, TagModule, SectionHeaderComponent, CurrencyPipe],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent {
  private readonly budgetService = inject(BudgetService);
  readonly selectedBudget = computed(() => this.budgetService.selectedBudget());

  readonly metrics = computed<DashboardMetric[]>(() => [
    {
      label: 'Current Balance',
      value: this.selectedBudget()?.amount.toString() ?? '0',
      trend: '+5.7%',
      severity: 'success',
      isCurrency: true,
    },
  ]);
}
