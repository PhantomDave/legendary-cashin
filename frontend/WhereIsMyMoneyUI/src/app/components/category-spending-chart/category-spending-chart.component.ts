import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { Transaction } from '../../models/transaction/Transaction';
import { Category } from '../../models/category/Category';

@Component({
  selector: 'app-category-spending-chart',
  imports: [ChartModule, MessageModule, SkeletonModule],
  templateUrl: './category-spending-chart.component.html',
  styleUrl: './category-spending-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategorySpendingChartComponent {
  readonly transactions = input<Transaction[]>([]);
  readonly categories = input<Category[]>([]);
  readonly loading = input(false);

  private readonly chartPalette = computed(() => getChartPalette());
  readonly spendingByCategory = computed(() =>
    this.categories()
      .map((category) => ({
        category,
        spent: this.transactions()
          .filter((transaction) => transaction.categoryIds.includes(category.id))
          .reduce((sum, transaction) => sum + Math.abs(Math.min(transaction.amount, 0)), 0),
      }))
      .filter((item) => item.spent > 0),
  );
  readonly hasData = computed(() => this.spendingByCategory().length > 0);

  readonly chartData = computed(() => {
    const spending = this.spendingByCategory();
    const palette = this.chartPalette();

    return {
      labels: spending.map((item) => item.category.name),
      datasets: [
        {
          data: spending.map((item) => item.spent),
          backgroundColor: spending.map((_, index) => palette[index % palette.length]),
          borderColor: 'transparent',
        },
      ],
    };
  });

  readonly chartOptions = computed(() => {
    const style = getComputedStyle(document.documentElement);
    return {
      maintainAspectRatio: false,
      cutout: '60%',
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

function getChartPalette(): string[] {
  const style = getComputedStyle(document.documentElement);
  const palette = [
    style.getPropertyValue('--p-primary-color').trim(),
    style.getPropertyValue('--p-blue-500').trim(),
    style.getPropertyValue('--p-green-500').trim(),
    style.getPropertyValue('--p-orange-500').trim(),
    style.getPropertyValue('--p-pink-500').trim(),
    style.getPropertyValue('--p-purple-500').trim(),
  ].filter(Boolean);

  return palette.length > 0 ? palette : ['#3B82F6'];
}
