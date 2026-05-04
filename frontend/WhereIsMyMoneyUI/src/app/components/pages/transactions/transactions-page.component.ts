import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TransactionItem } from '../../../models/transaction-item.model';
import { SectionHeaderComponent } from '../../section-header/section-header.component';

@Component({
  selector: 'app-transactions-page',
  imports: [TableModule, TagModule, SectionHeaderComponent],
  templateUrl: './transactions-page.component.html',
  styleUrl: './transactions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsPageComponent {
  readonly transactions: TransactionItem[] = [
    {
      date: '2026-05-01',
      merchant: 'Cloud Hosting',
      amount: '$480.00',
      category: 'Infrastructure',
      status: 'Cleared',
    },
    {
      date: '2026-05-02',
      merchant: 'Payroll',
      amount: '$3,750.00',
      category: 'Operations',
      status: 'Pending',
    },
    {
      date: '2026-05-03',
      merchant: 'Client Refund',
      amount: '$120.00',
      category: 'Adjustments',
      status: 'Cleared',
    },
  ];
}
