import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { CurrencyPipe } from '@angular/common';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { DashboardMetric } from '../../models/dashboard-metric.model';
import { BudgetService } from '../../services/budget.service';
import { TransactionService } from '../../services/transaction.service';

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
  private readonly transactionService = inject(TransactionService);
  private readonly transactionMetrics = computed(() => this.transactionService.metrics());
  readonly metrics = computed<DashboardMetric[]>(() => {
    const budget = this.selectedBudget();
    const tm = this.transactionMetrics();

    const balanceTrend =
      tm && tm.balance30DaysAgo !== 0
        ? (((budget?.amount ?? 0) - tm.balance30DaysAgo) / Math.abs(tm.balance30DaysAgo)) * 100
        : null;

    return [
      {
        label: 'Current Balance',
        value: budget?.amount.toString() ?? '0',
        trend: formatTrend(balanceTrend),
        severity: trendSeverity(balanceTrend),
        isCurrency: true,
      },
      {
        label: 'Year to Date',
        value: tm?.yearToDate.toString() ?? '0',
        trend: formatTrend(tm?.yearToDateTrend ?? null),
        severity: trendSeverity(tm?.yearToDateTrend ?? null),
        isCurrency: true,
      },
      {
        label: 'Month to Date',
        value: tm?.monthToDate.toString() ?? '0',
        trend: formatTrend(tm?.monthToDateTrend ?? null),
        severity: trendSeverity(tm?.monthToDateTrend ?? null),
        isCurrency: true,
      },
      {
        label: 'Predicted End of Month',
        value: tm?.predictedEndOfMonth.toString() ?? '0',
        trend: formatTrend(tm?.predictedEndOfMonthTrend ?? null),
        severity: trendSeverity(tm?.predictedEndOfMonthTrend ?? null),
        isCurrency: true,
      },
      {
        label: 'Balance 30 Days Ago',
        value: tm?.balance30DaysAgo.toString() ?? '0',
        trend: formatTrend(tm?.balance30DaysAgoTrend ?? null),
        severity: trendSeverity(tm?.balance30DaysAgoTrend ?? null),
        isCurrency: true,
      },
    ];
  });

  constructor() {
    effect(() => {
      const budget = this.selectedBudget();
      if (budget) {
        void this.transactionService.getMetrics(budget.id);
      }
    });
  }
}

function formatTrend(value: number | null): string {
  if (value === null) return 'N/A';
  const sign = value >= 0 ? '+' : '';
  return `${sign}${value.toFixed(1)}%`;
}

function trendSeverity(value: number | null): DashboardMetric['severity'] {
  if (value === null) return 'info';
  return value >= 0 ? 'success' : 'warn';
}
