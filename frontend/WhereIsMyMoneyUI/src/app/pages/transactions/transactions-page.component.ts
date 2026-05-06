import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { TagModule } from 'primeng/tag';
import { CreateTransactionComponent } from '../../components/create-transaction-component/create-transaction-component';
import { PaginatedTableComponent } from '../../components/paginated-table/paginated-table.component';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { BudgetService } from '../../services/budget.service';
import { TransactionService } from '../../services/transaction.service';
import { PaginatorState } from 'primeng/paginator';
import { Transaction } from '../../models/transaction/Transaction';
import { PaginatedResponse } from '../../models/api/paginated-response.model';

@Component({
  selector: 'app-transactions-page',
  imports: [
    TagModule,
    PaginatedTableComponent,
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
  private lastBudgetId: number | null = null;
  private latestLoadRequestId = 0;

  readonly selectedBudget = computed(() => this.budgetService.selectedBudget());
  readonly isLoading = this.transactionService.isLoading;
  readonly rowsPerPageOptions = [10, 25, 50];

  readonly transactions = signal<PaginatedResponse<Transaction> | null>(null);
  first = 0;
  rows = 10;
  currentPage = 1;

  isCreateTransactionDialogVisible = false;

  constructor() {
    effect(() => {
      const budgetId = this.selectedBudget()?.id;

      if (budgetId == null) {
        this.lastBudgetId = null;
        this.first = 0;
        this.currentPage = 1;
        this.transactionService.clearTransactions();
        this.transactions.set(null);
        return;
      }

      if (this.lastBudgetId !== budgetId) {
        this.lastBudgetId = budgetId;
        this.first = 0;
        this.currentPage = 1;
      }

      void this.loadTransactions(budgetId);
    });
  }

  private async loadTransactions(budgetId: number): Promise<void> {
    const requestId = ++this.latestLoadRequestId;
    const response = await this.transactionService.getByBudgetId(
      budgetId,
      this.currentPage,
      this.rows,
    );

    if (requestId !== this.latestLoadRequestId) {
      return;
    }

    this.transactions.set(response);
  }

  addTransaction(): void {
    this.isCreateTransactionDialogVisible = true;
  }

  onTransactionCreated(): void {
    this.isCreateTransactionDialogVisible = false;

    const budgetId = this.selectedBudget()?.id;

    if (budgetId == null) {
      this.transactionService.clearTransactions();
      this.transactions.set(null);
      return;
    }

    void this.loadTransactions(budgetId);
  }

  onPageChange($event: PaginatorState): void {
    this.first = $event.first ?? 0;
    this.rows = $event.rows ?? this.rows;
    this.currentPage = Math.floor(this.first / this.rows) + 1;

    const budgetId = this.selectedBudget()?.id;

    if (budgetId == null) {
      this.transactionService.clearTransactions();
      this.transactions.set(null);
      return;
    }

    void this.loadTransactions(budgetId);
  }
}
