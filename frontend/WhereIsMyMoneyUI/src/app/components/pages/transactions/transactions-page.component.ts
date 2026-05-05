import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SectionHeaderComponent } from '../../section-header/section-header.component';
import { TransactionService } from '../../../services/transaction.service';

@Component({
  selector: 'app-transactions-page',
  imports: [TableModule, TagModule, SectionHeaderComponent],
  templateUrl: './transactions-page.component.html',
  styleUrl: './transactions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsPageComponent implements OnInit {
  private readonly transactionService = inject(TransactionService);
  readonly transactions = this.transactionService.transactions;
  async ngOnInit(): Promise<void> {
    await this.transactionService.getTransactions();
  }

  addTransaction(): () => void {
    throw new Error('Method not implemented.');
  }
}
