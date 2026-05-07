import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { MonthlySummary } from '../../models/transaction/MonthlySummary';

@Component({
  selector: 'app-monthly-trend-chart',
  imports: [ChartModule, MessageModule, SkeletonModule],
  templateUrl: './monthly-trend-chart.component.html',
  styleUrl: './monthly-trend-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonthlyTrendChartComponent {
  readonly monthlySummary = input<MonthlySummary[]>([]);
  readonly loading = input(false);

  readonly hasData = computed(() => this.monthlySummary().length > 0);

  readonly chartData = computed(() => {
    const summary = this.monthlySummary();
    const style = getComputedStyle(document.documentElement);
    return {
      labels: summary.map((item) => formatMonth(item.year, item.month)),
      datasets: [
        {
          label: 'Income',
          data: summary.map((item) => item.income),
          borderColor: style.getPropertyValue('--p-green-500').trim(),
          backgroundColor: style.getPropertyValue('--p-green-500').trim(),
          fill: false,
          tension: 0.3,
        },
        {
          label: 'Expenses',
          data: summary.map((item) => item.expenses),
          borderColor: style.getPropertyValue('--p-orange-500').trim(),
          backgroundColor: style.getPropertyValue('--p-orange-500').trim(),
          fill: false,
          tension: 0.3,
        },
      ],
    };
  });

  readonly chartOptions = computed(() => {
    const style = getComputedStyle(document.documentElement);
    return {
      maintainAspectRatio: false,
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

function formatMonth(year: number, month: number): string {
  return new Date(year, month - 1, 1).toLocaleDateString(undefined, {
    month: 'short',
    year: '2-digit',
  });
}
