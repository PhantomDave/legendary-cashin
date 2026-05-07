import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { TransactionService } from '../../services/transaction.service';
import { BudgetService } from '../../services/budget.service';
import { CategoryService } from '../../services/category.service';
import { CreateScheduledTransactionComponent } from '../../components/create-scheduled-transaction-component/create-scheduled-transaction-component';
import { PaginatedTableComponent } from '../../components/paginated-table/paginated-table.component';
import { RecurringTransactions } from '../../models/transaction/ScheduledTransaction';
import { PaginatedResponse } from '../../models/api/paginated-response.model';
import { Category } from '../../models/category/Category';
import { ConfirmationService } from 'primeng/api';
import { ToastService } from '../../services/toast.service';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { RecurrenceFrequency } from '../../models/enum/recurrence-frequency';
import { DayOfWeek } from '../../models/enum/day-of-week';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-scheduled-page',
  imports: [
    SectionHeaderComponent,
    CreateScheduledTransactionComponent,
    PaginatedTableComponent,
    CurrencyPipe,
    DatePipe,
    ConfirmDialog,
    ButtonModule,
    TagModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './scheduled-page-component.html',
  styleUrl: './scheduled-page-component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduledPageComponent {
  readonly rowsPerPageOptions = [10, 25, 50];
  // Signals
  readonly categories = signal<Category[]>([]);
  readonly scheduledTransactions = signal<PaginatedResponse<RecurringTransactions> | null>(null);
  readonly editingSchedule = signal<RecurringTransactions | null>(null);
  readonly isCreateScheduledDialogVisible = signal<boolean>(false);
  // Pagination
  first = 0;
  rows = 10;
  currentPage = 1;
  // Enums for template
  readonly RecurrenceFrequency = RecurrenceFrequency;
  readonly DayOfWeek = DayOfWeek;
  private readonly transactionService = inject(TransactionService);
  readonly isLoading = this.transactionService.isLoading;
  private readonly budgetService = inject(BudgetService);
  readonly selectedBudget = computed(() => this.budgetService.selectedBudget());
  readonly budgets = this.budgetService.budgets;
  private readonly categoryService = inject(CategoryService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toast = inject(ToastService);
  private lastBudgetId: number | null = null;
  private latestLoadRequestId = 0;

  constructor() {
    void this.loadCategories();

    effect(() => {
      const budgetId = this.selectedBudget()?.id;

      if (budgetId == null) {
        this.lastBudgetId = null;
        this.first = 0;
        this.currentPage = 1;
        this.scheduledTransactions.set(null);
        this.editingSchedule.set(null);
        this.isCreateScheduledDialogVisible.set(false);
        return;
      }

      if (this.lastBudgetId !== budgetId) {
        this.lastBudgetId = budgetId;
        this.first = 0;
        this.currentPage = 1;
      }

      void this.loadScheduledTransactions(budgetId);
    });
  }

  openCreateModal(): void {
    this.editingSchedule.set(null);
    this.isCreateScheduledDialogVisible.set(true);
  }

  openEditModal(schedule: RecurringTransactions): void {
    this.editingSchedule.set(schedule);
    this.isCreateScheduledDialogVisible.set(true);
  }

  closeModal(): void {
    this.isCreateScheduledDialogVisible.set(false);
    this.editingSchedule.set(null);
  }

  async deleteScheduledTransaction(schedule: RecurringTransactions): Promise<void> {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the schedule for "${schedule.description}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        await this.transactionService.deleteScheduledTransaction(schedule.id);
        this.toast.success('Schedule deleted successfully');

        const budgetId = this.selectedBudget()?.id;
        if (budgetId != null) {
          void this.loadScheduledTransactions(budgetId);
        }
      },
    });
  }

  onScheduledTransactionCreated(): void {
    this.closeModal();
    const budgetId = this.selectedBudget()?.id;

    if (budgetId == null) {
      this.scheduledTransactions.set(null);
      return;
    }

    void this.loadScheduledTransactions(budgetId);
  }

  onScheduledTransactionUpdated(): void {
    this.closeModal();
    const budgetId = this.selectedBudget()?.id;

    if (budgetId == null) {
      this.scheduledTransactions.set(null);
      return;
    }

    void this.loadScheduledTransactions(budgetId);
  }

  onPageChange($event: { first?: number; rows?: number }): void {
    this.first = $event.first ?? 0;
    this.rows = $event.rows ?? this.rows;
    this.currentPage = Math.floor(this.first / this.rows) + 1;

    const budgetId = this.selectedBudget()?.id;

    if (budgetId == null) {
      this.scheduledTransactions.set(null);
      return;
    }

    void this.loadScheduledTransactions(budgetId);
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

  resolveFrequencyDisplay(frequency: RecurrenceFrequency, interval: number): string {
    const frequencyNames = {
      [RecurrenceFrequency.Daily]: 'Daily',
      [RecurrenceFrequency.Weekly]: 'Weekly',
      [RecurrenceFrequency.BiWeekly]: 'Bi-Weekly',
      [RecurrenceFrequency.Monthly]: 'Monthly',
      [RecurrenceFrequency.Quarterly]: 'Quarterly',
      [RecurrenceFrequency.Yearly]: 'Yearly',
    };

    const base = frequencyNames[frequency] ?? 'Unknown';

    if (interval === 1) {
      return base;
    }

    return `Every ${interval} ${base.toLowerCase()}s`;
  }

  resolveRecurrencePattern(schedule: RecurringTransactions): string {
    if (schedule.frequency === RecurrenceFrequency.Weekly && schedule.daysOfWeek?.length) {
      const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
      return schedule.daysOfWeek
        .sort()
        .map((day) => dayNames[day] ?? day)
        .join(', ');
    }

    if (schedule.frequency === RecurrenceFrequency.Monthly && schedule.dayOfMonth) {
      return `Day ${schedule.dayOfMonth}`;
    }

    return '-';
  }

  calculateRemainingDebt(schedule: RecurringTransactions): number | null {
    if (!schedule.endDate && !schedule.maxOccurrences) {
      return null;
    }

    let remainingOccurrences = 0;

    if (schedule.maxOccurrences) {
      remainingOccurrences = Math.max(0, schedule.maxOccurrences - schedule.generatedCount);
    } else if (schedule.endDate) {
      // Simple calculation: remaining days / frequency interval
      const now = new Date();
      const endDate = new Date(schedule.endDate);

      if (endDate <= now) {
        remainingOccurrences = 0;
      } else {
        // This is an approximation; actual calculation depends on the frequency
        const daysRemaining = Math.ceil(
          (endDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24),
        );
        // Simple approximation based on interval
        remainingOccurrences = Math.ceil(daysRemaining / (schedule.interval * 7)); // Assuming roughly 7 days per interval
      }
    }

    return remainingOccurrences > 0 ? schedule.amount * remainingOccurrences : 0;
  }

  private async loadCategories(): Promise<void> {
    const response = await this.categoryService.getCategories(1, 100);
    this.categories.set(response?.items ?? []);
  }

  private async loadScheduledTransactions(budgetId: number): Promise<void> {
    const requestId = ++this.latestLoadRequestId;
    const response = await this.transactionService.getScheduledTransactionsByBudgetId(
      budgetId,
      this.currentPage,
      this.rows,
    );

    if (requestId !== this.latestLoadRequestId) {
      return;
    }

    this.scheduledTransactions.set(response);
  }

  private resolveCategoryName(id: number): string {
    return this.categories().find((category) => category.id === id)?.name ?? `${id}`;
  }
}
