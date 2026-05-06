import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SectionHeaderComponent } from '../../section-header/section-header.component';
import { TransactionService } from '../../../services/transaction.service';
import { CreateTransactionComponent } from '../../create-transaction-component/create-transaction-component';
import { BudgetService } from '../../../services/budget.service';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-transactions-page',
  imports: [
    TableModule,
    TagModule,
    SectionHeaderComponent,
    CreateTransactionComponent,
    CurrencyPipe,
    DatePipe,
  ],
  templateUrl: './transactions-page.component.html',
  styleUrl: './transactions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsPageComponent {
  private readonly transactionService = inject(TransactionService);
  private readonly budgetService = inject(BudgetService);
  readonly selectedBudget = computed(() => this.budgetService.selectedBudget());
  readonly transactions = this.transactionService.transactions;

  isCreateTransactionDialogVisible = false;

  constructor() {
    effect(() => {
      const budgetId = this.selectedBudget()?.id;

      if (budgetId == null) {
        this.transactionService.clearTransactions();
        return;
      }

      void this.transactionService.getByBudgetId(budgetId);
    });
  }

  addTransaction(): void {
    this.isCreateTransactionDialogVisible = true;
  }

  onTransactionCreated(): void {
    this.isCreateTransactionDialogVisible = false;
    const budgetId = this.selectedBudget()?.id;

    if (budgetId == null) {
      this.transactionService.clearTransactions();
      return;
    }

    void this.transactionService.getByBudgetId(budgetId);
  }
}
