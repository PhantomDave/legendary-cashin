import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CreateTransactionComponent } from '../../components/create-transaction-component/create-transaction-component';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { BudgetService } from '../../services/budget.service';
import { PatchTransactionRequest, TransactionService } from '../../services/transaction.service';
import { Transaction } from '../../models/transaction/Transaction';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { Inplace } from 'primeng/inplace';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category/Category';
import { ConfirmationService } from 'primeng/api';
import { ToastService } from '../../services/toast.service';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';

interface EditValues {
  date: Date;
  budgetId: number;
  description: string;
  amount: number;
  categoryIds: number[];
}

interface TransactionQueryFilters {
  date: string | null;
  budgetId: number | null;
  categoryId: number | null;
  uncategorized: boolean;
  description: string;
  amount: number | null;
  amountMatchMode: string | null;
}

@Component({
  selector: 'app-transactions-page',
  imports: [
    SectionHeaderComponent,
    CreateTransactionComponent,
    CurrencyPipe,
    DatePipe,
    Inplace,
    InputText,
    Button,
    InputNumberModule,
    FormsModule,
    DatePickerModule,
    SelectModule,
    MultiSelectModule,
    ConfirmDialog,
    TooltipModule,
    TableModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './transactions-page.component.html',
  styleUrl: './transactions-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsPageComponent {
  private readonly uncategorizedCategoryFilterValue = -1;
  private readonly transactionService = inject(TransactionService);
  private readonly budgetService = inject(BudgetService);
  private readonly categoryService = inject(CategoryService);
  private lastBudgetId: number | null = null;
  private latestLoadRequestId = 0;

  readonly selectedBudget = computed(() => this.budgetService.selectedBudget());
  readonly budgets = this.budgetService.budgets;
  readonly isLoading = this.transactionService.isLoading;
  readonly rowsPerPageOptions = [10, 25, 50];
  readonly categories = signal<Category[]>([]);
  readonly editingValues = signal<Record<number, EditValues>>({});
  readonly categoryOptions = computed(() =>
    this.categories().map((category) => ({ label: category.name, value: category.id })),
  );
  readonly categoryFilterOptions = computed(() => [
    { label: 'Uncategorized', value: this.uncategorizedCategoryFilterValue },
    ...this.categoryOptions(),
  ]);
  readonly amountMatchModes: Array<{ label: string; value: string }> = [
    { label: 'Equals', value: 'equals' },
    { label: 'Greater than', value: 'gt' },
    { label: 'Greater or equal', value: 'gte' },
    { label: 'Less than', value: 'lt' },
    { label: 'Less or equal', value: 'lte' },
  ];
  readonly activeFilters = signal<TransactionQueryFilters>({
    date: null,
    budgetId: null,
    categoryId: null,
    uncategorized: false,
    description: '',
    amount: null,
    amountMatchMode: null,
  });
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toast = inject(ToastService);
  readonly transactions = signal<PaginatedResponse<Transaction> | null>(null);
  first = 0;
  rows = 10;
  currentPage = 1;

  isCreateTransactionDialogVisible = false;

  constructor() {
    void this.loadCategories();

    effect(() => {
      const budgetId = this.selectedBudget()?.id;

      if (budgetId == null) {
        this.lastBudgetId = null;
        this.first = 0;
        this.currentPage = 1;
        this.activeFilters.set({
          date: null,
          budgetId: null,
          categoryId: null,
          uncategorized: false,
          description: '',
          amount: null,
          amountMatchMode: null,
        });
        this.transactionService.clearTransactions();
        this.transactions.set(null);
        return;
      }

      if (this.lastBudgetId !== budgetId) {
        this.lastBudgetId = budgetId;
        this.first = 0;
        this.currentPage = 1;
        this.activeFilters.set({
          date: null,
          budgetId: null,
          categoryId: null,
          uncategorized: false,
          description: '',
          amount: null,
          amountMatchMode: null,
        });
      }

      void this.loadTransactions(budgetId);
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const first = event.first ?? 0;
    const rows = event.rows ?? this.rows;
    this.first = first;
    this.rows = rows;
    this.currentPage = Math.floor(first / rows) + 1;
    const selectedCategory = this.readFilterNumber(event, 'categoryId');
    const uncategorized = selectedCategory === this.uncategorizedCategoryFilterValue;

    this.activeFilters.set({
      date: this.readFilterDate(event, 'date'),
      budgetId: this.readFilterNumber(event, 'budgetId'),
      categoryId: uncategorized ? null : selectedCategory,
      uncategorized,
      description: this.readFilterString(event, 'description'),
      amount: this.readFilterNumber(event, 'amount'),
      amountMatchMode: this.readFilterMatchMode(event, 'amount'),
    });

    const budgetId = this.selectedBudget()?.id;
    if (budgetId == null) {
      this.transactionService.clearTransactions();
      this.transactions.set(null);
      return;
    }

    void this.loadTransactions(budgetId);
  }

  getEditingValue(id: number): EditValues {
    return this.editingValues()[id]!;
  }

  private async loadTransactions(budgetId: number): Promise<void> {
    const requestId = ++this.latestLoadRequestId;
    const response = await this.transactionService.getByBudgetId(
      budgetId,
      this.currentPage,
      this.rows,
      this.activeFilters(),
    );

    if (requestId !== this.latestLoadRequestId) {
      return;
    }

    this.transactions.set(response);
  }

  addTransaction(): void {
    this.isCreateTransactionDialogVisible = true;
  }

  startEdit(item: Transaction): void {
    const parsedDate = new Date(item.date);
    this.editingValues.update((current) => ({
      ...current,
      [item.id]: {
        date: Number.isNaN(parsedDate.getTime()) ? new Date() : parsedDate,
        budgetId: item.budgetId,
        description: item.description,
        amount: item.amount,
        categoryIds: [...item.categoryIds],
      },
    }));
  }

  cancelEdit(id: number): void {
    this.editingValues.update((current) => {
      const { [id]: _, ...rest } = current;
      return rest;
    });
  }

  async saveEdit(item: Transaction, inplaceRef: Inplace): Promise<void> {
    const values = this.editingValues()[item.id];
    if (!values) {
      return;
    }

    const patch = this.buildPatch(item, values);
    if (Object.keys(patch).length === 0) {
      this.cancelEdit(item.id);
      inplaceRef.deactivate();
      return;
    }

    const updated = await this.transactionService.patchTransaction(item.id, patch);
    if (!updated) {
      return;
    }

    const selectedBudgetId = this.selectedBudget()?.id;
    if (selectedBudgetId != null && updated.budgetId !== selectedBudgetId) {
      await this.loadTransactions(selectedBudgetId);
    } else {
      this.transactions.update((current) => {
        if (!current) return current;
        return {
          ...current,
          items: current.items.map((transaction) =>
            transaction.id === item.id ? updated : transaction,
          ),
        };
      });
    }

    this.cancelEdit(item.id);
    inplaceRef.deactivate();
  }

  resolveBudgetName(budgetId: number): string {
    return this.budgets().find((budget) => budget.id === budgetId)?.name ?? `${budgetId}`;
  }

  resolveCategoryNames(categoryIds: number[]): string {
    if (categoryIds.length === 0) {
      return '-';
    }

    return categoryIds.map((id) => this.resolveCategoryName(id)).join(', ');
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

  private resolveCategoryName(id: number): string {
    return this.categories().find((category) => category.id === id)?.name ?? `${id}`;
  }

  private async loadCategories(): Promise<void> {
    const response = await this.categoryService.getCategories(1, 100);
    this.categories.set(response?.items ?? []);
  }

  private readFilterString(event: TableLazyLoadEvent, field: string): string {
    const metadata = this.getFilterMetadata(event, field);
    const value = metadata?.value;
    return typeof value === 'string' ? value.trim() : '';
  }

  private readFilterNumber(event: TableLazyLoadEvent, field: string): number | null {
    const metadata = this.getFilterMetadata(event, field);
    const value = metadata?.value;
    return typeof value === 'number' ? value : null;
  }

  private readFilterDate(event: TableLazyLoadEvent, field: string): string | null {
    const metadata = this.getFilterMetadata(event, field);
    const value = metadata?.value;
    if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
      return null;
    }

    return value.toISOString();
  }

  private readFilterMatchMode(event: TableLazyLoadEvent, field: string): string | null {
    const metadata = this.getFilterMetadata(event, field);
    return metadata?.matchMode ?? null;
  }

  private getFilterMetadata(
    event: TableLazyLoadEvent,
    field: string,
  ): { value?: unknown; matchMode?: string } | null {
    const filter = event.filters?.[field];
    if (!filter) {
      return null;
    }

    if (Array.isArray(filter)) {
      return filter[0] ?? null;
    }

    if ('constraints' in filter && Array.isArray(filter.constraints)) {
      return filter.constraints[0] ?? null;
    }

    return filter;
  }

  private buildPatch(item: Transaction, values: EditValues): PatchTransactionRequest {
    const patch: PatchTransactionRequest = {};

    const description = values.description.trim();
    if (description !== item.description) {
      patch.description = description;
    }

    if (values.amount !== item.amount) {
      patch.amount = values.amount;
    }

    const initialDate = new Date(item.date);
    if (
      !Number.isNaN(initialDate.getTime()) &&
      initialDate.toISOString() !== values.date.toISOString()
    ) {
      patch.date = values.date.toISOString();
    }

    if (values.budgetId !== item.budgetId) {
      patch.budgetId = values.budgetId;
    }

    if (!this.haveSameIds(values.categoryIds, item.categoryIds)) {
      patch.categoryIds = values.categoryIds;
    }

    return patch;
  }

  deleteTransaction(event: Event, transactionId: number): void {
    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: 'Do you want to delete this transaction?',
      header: 'Delete Transaction',
      icon: 'pi pi-info-circle',
      rejectLabel: 'Cancel',
      rejectButtonProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true,
      },
      acceptButtonProps: {
        label: 'Delete',
        severity: 'danger',
      },

      accept: () => {
        void this.transactionService.deleteTransaction(transactionId).then(() => {
          this.toast.success('Transaction deleted');
          const budgetId = this.selectedBudget()?.id;
          if (budgetId != null) {
            void this.loadTransactions(budgetId);
          }
        });
      },
    });
  }

  private haveSameIds(source: number[], target: number[]): boolean {
    if (source.length !== target.length) {
      return false;
    }

    const left = [...source].sort((a, b) => a - b);
    const right = [...target].sort((a, b) => a - b);

    return left.every((id, index) => id === right[index]);
  }
}
