import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SectionHeaderComponent } from '../../section-header/section-header.component';
import { TransactionService } from '../../../services/transaction.service';
import { CreateTransactionComponent } from '../../create-transaction-component/create-transaction-component';

@Component({
  selector: 'app-transactions-page',
  imports: [TableModule, TagModule, SectionHeaderComponent, CreateTransactionComponent],
  templateUrl: './transactions-page.component.html',
  styleUrl: './transactions-page.component.scss',
})
export class TransactionsPageComponent implements OnInit {
  private readonly transactionService = inject(TransactionService);
  readonly transactions = this.transactionService.transactions;
  isCreateTransactionDialogVisible = false;

  async ngOnInit(): Promise<void> {
    await this.transactionService.getTransactions();
  }

  addTransaction(): void {
    console.log('Add Transaction button clicked');
    this.isCreateTransactionDialogVisible = true;
  }

  onTransactionCreated($event: Event) {
    console.log('Transaction created:', $event);
    this.isCreateTransactionDialogVisible = false;
    this.transactionService.getTransactions();
  }
}
