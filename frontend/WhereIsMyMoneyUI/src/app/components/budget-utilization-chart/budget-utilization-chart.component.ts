import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { Category } from '../../models/category/Category';
import { Transaction } from '../../models/transaction/Transaction';

@Component({
  selector: 'app-budget-utilization-chart',
  imports: [ChartModule, MessageModule, SkeletonModule],
  templateUrl: './budget-utilization-chart.component.html',
  styleUrl: './budget-utilization-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BudgetUtilizationChartComponent {
  readonly categories = input<Category[]>([]);
  readonly transactions = input<Transaction[]>([]);
  readonly loading = input(false);

  readonly utilization = computed(() =>
    this.categories().map((category) => ({
      name: category.name,
      allocated: category.budget,
      spent: this.transactions()
        .filter((transaction) => transaction.categoryIds.includes(category.id))
        .reduce((sum, transaction) => sum + Math.abs(Math.min(transaction.amount, 0)), 0),
    })),
  );

  readonly hasData = computed(() => this.utilization().length > 0);

  readonly chartData = computed(() => {
    const utilization = this.utilization();
    const style = getComputedStyle(document.documentElement);
    return {
      labels: utilization.map((item) => item.name),
      datasets: [
        {
          label: 'Allocated',
          data: utilization.map((item) => item.allocated),
          backgroundColor: style.getPropertyValue('--p-primary-color').trim(),
        },
        {
          label: 'Spent',
          data: utilization.map((item) => item.spent),
          backgroundColor: style.getPropertyValue('--p-orange-500').trim(),
        },
      ],
    };
  });

  readonly chartOptions = computed(() => {
    const style = getComputedStyle(document.documentElement);
    return {
      maintainAspectRatio: false,
      indexAxis: 'y',
      scales: {
        x: {
          ticks: {
            color: style.getPropertyValue('--p-text-color'),
          },
          grid: {
            color: style.getPropertyValue('--p-content-border-color'),
          },
        },
        y: {
          ticks: {
            color: style.getPropertyValue('--p-text-color'),
          },
          grid: {
            color: style.getPropertyValue('--p-content-border-color'),
          },
        },
      },
      plugins: {
        legend: {
          labels: {
            color: style.getPropertyValue('--p-text-color'),
          },
        },
      },
    };
  });
}
