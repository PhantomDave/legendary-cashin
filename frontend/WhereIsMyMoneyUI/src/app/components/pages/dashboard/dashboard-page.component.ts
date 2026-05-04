import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DashboardMetric } from '../../../models/dashboard-metric.model';
import { SectionHeaderComponent } from '../../section-header/section-header.component';

@Component({
  selector: 'app-dashboard-page',
  imports: [CardModule, ButtonModule, TagModule, SectionHeaderComponent],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent {
  readonly metrics: DashboardMetric[] = [
    { label: 'Cash On Hand', value: '$28,430', trend: '+5.7%', severity: 'success' },
    { label: 'Monthly Burn', value: '$6,900', trend: '-1.4%', severity: 'info' },
    { label: 'Upcoming Bills', value: '$2,320', trend: '4 due soon', severity: 'warn' },
  ];
}
