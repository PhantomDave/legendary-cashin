import { Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputTextModule } from 'primeng/inputtext';
import {
  PatchScheduledTransactionRequest,
  TransactionService,
} from '../../services/transaction.service';
import { BudgetService } from '../../services/budget.service';
import { RecurringTransactions } from '../../models/transaction/ScheduledTransaction';
import { RecurrenceFrequency } from '../../models/enum/recurrence-frequency';
import { DayOfWeek } from '../../models/enum/day-of-week';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category/Category';
import { InputNumberModule } from 'primeng/inputnumber';

@Component({
  selector: 'app-create-scheduled-transaction-component',
  imports: [
    DialogModule,
    InputGroupModule,
    InputGroupAddon,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    DatePickerModule,
    SelectModule,
    MultiSelectModule,
    CheckboxModule,
    TooltipModule,
    InputNumberModule,
  ],
  templateUrl: './create-scheduled-transaction-component.html',
  styleUrl: './create-scheduled-transaction-component.scss',
})
export class CreateScheduledTransactionComponent {
  visible = model<boolean>(false);
  budgetId = input<number>();
  editingSchedule = input<RecurringTransactions | null>(null);

  readonly scheduledTransactionCreated = output<void>();
  readonly scheduledTransactionUpdated = output<void>();
  readonly categories = signal<Category[]>([]);
  readonly categoryOptions = computed(() =>
    this.categories().map((category) => ({ label: category.name, value: category.id })),
  );
  readonly RecurrenceFrequency = RecurrenceFrequency;
  readonly frequencyOptions = [
    { label: 'Daily', value: RecurrenceFrequency.Daily },
    { label: 'Weekly', value: RecurrenceFrequency.Weekly },
    { label: 'Bi-Weekly', value: RecurrenceFrequency.BiWeekly },
    { label: 'Monthly', value: RecurrenceFrequency.Monthly },
    { label: 'Quarterly', value: RecurrenceFrequency.Quarterly },
    { label: 'Yearly', value: RecurrenceFrequency.Yearly },
  ];
  readonly daysOfWeekOptions = [
    { label: 'Sunday', value: DayOfWeek.Sunday },
    { label: 'Monday', value: DayOfWeek.Monday },
    { label: 'Tuesday', value: DayOfWeek.Tuesday },
    { label: 'Wednesday', value: DayOfWeek.Wednesday },
    { label: 'Thursday', value: DayOfWeek.Thursday },
    { label: 'Friday', value: DayOfWeek.Friday },
    { label: 'Saturday', value: DayOfWeek.Saturday },
  ];
  readonly isEditMode = computed(() => !!this.editingSchedule());
  readonly dialogTitle = computed(() =>
    this.isEditMode() ? 'Edit Scheduled Transaction' : 'New Scheduled Transaction',
  );
  readonly buttonLabel = computed(() =>
    this.isEditMode() ? 'Update Schedule' : 'Create Schedule',
  );
  readonly showInterval = computed(() => this.frequency() !== RecurrenceFrequency.Daily);
  readonly intervalLabel = computed(() => {
    const freq = this.frequency();
    switch (freq) {
      case RecurrenceFrequency.Weekly:
        return 'Every (weeks)';
      case RecurrenceFrequency.BiWeekly:
        return 'Every (2-week cycles)';
      case RecurrenceFrequency.Monthly:
        return 'Every (months)';
      case RecurrenceFrequency.Quarterly:
        return 'Every (quarters)';
      case RecurrenceFrequency.Yearly:
        return 'Every (years)';
      default:
        return 'Every (interval)';
    }
  });
  readonly intervalTooltip = computed(() => {
    const freq = this.frequency();
    switch (freq) {
      case RecurrenceFrequency.Daily:
        return 'Runs every day';
      case RecurrenceFrequency.Weekly:
        return 'Interval = 1 means every week, 2 means every other week, etc.';
      case RecurrenceFrequency.BiWeekly:
        return 'Interval = 1 means every 2 weeks, 2 means every 4 weeks, etc.';
      case RecurrenceFrequency.Monthly:
        return 'Interval = 1 means every month, 2 means every other month, etc.';
      case RecurrenceFrequency.Quarterly:
        return 'Interval = 1 means every quarter (3 months), 2 means every 6 months, etc.';
      case RecurrenceFrequency.Yearly:
        return 'Interval = 1 means every year, 2 means every other year, etc.';
      default:
        return 'Set the recurrence interval';
    }
  });
  private readonly formBuilder = inject(FormBuilder);
  readonly scheduledTransactionForm = this.formBuilder.group({
    description: ['', [Validators.required, Validators.minLength(3)]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    categoryIds: [[] as number[]],
    frequency: [RecurrenceFrequency.Monthly, [Validators.required]],
    interval: [1, [Validators.required, Validators.min(1)]],
    startDate: [null as Date | null, [Validators.required]],
    endDate: [null as Date | null],
    maxOccurrences: [null as number | null],
    dayOfMonth: [null as number | null],
    daysOfWeek: [[] as DayOfWeek[]],
    isActive: [true],
  });
  readonly showDaysOfWeek = computed(
    () => this.scheduledTransactionForm.get('frequency')?.value === RecurrenceFrequency.Weekly,
  );
  readonly showDayOfMonth = computed(
    () => this.scheduledTransactionForm.get('frequency')?.value === RecurrenceFrequency.Monthly,
  );
  readonly frequency = computed(() => this.scheduledTransactionForm.get('frequency')?.value);
  private readonly transactionService = inject(TransactionService);
  private readonly budgetService = inject(BudgetService);
  readonly selectedBudget = this.budgetService.selectedBudget;
  private readonly categoryService = inject(CategoryService);
  private readonly _endDate = signal<Date | null>(null);
  readonly showMaxOccurrences = computed(() => !this._endDate());

  constructor() {
    void this.loadCategories();

    this.scheduledTransactionForm.get('endDate')?.valueChanges.subscribe((v) => {
      this._endDate.set(v ?? null);
    });

    this.scheduledTransactionForm.get('frequency')?.valueChanges.subscribe((frequency) => {
      if (frequency !== RecurrenceFrequency.Weekly) {
        this.scheduledTransactionForm.get('daysOfWeek')?.reset([]);
      }
      if (frequency !== RecurrenceFrequency.Monthly) {
        this.scheduledTransactionForm.get('dayOfMonth')?.reset(null);
      }
    });

    effect(() => {
      const schedule = this.editingSchedule();
      if (schedule) {
        this.populateFormWithSchedule(schedule);
      } else {
        this.scheduledTransactionForm.reset({
          description: '',
          amount: 0,
          categoryIds: [],
          frequency: RecurrenceFrequency.Monthly,
          interval: 1,
          startDate: null,
          endDate: null,
          maxOccurrences: null,
          dayOfMonth: null,
          daysOfWeek: [],
          isActive: true,
        });
      }
    });
  }

  isInvalid(controlName: string): boolean {
    const control = this.scheduledTransactionForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  async onSubmit(): Promise<void> {
    if (!this.scheduledTransactionForm.valid || !this.selectedBudget() || !this.budgetId()) {
      return;
    }

    const {
      description,
      amount,
      categoryIds,
      frequency,
      interval,
      startDate,
      endDate,
      maxOccurrences,
      dayOfMonth,
      daysOfWeek,
      isActive,
    } = this.scheduledTransactionForm.getRawValue();

    if (!description || amount == null || !startDate || !frequency) {
      return;
    }

    const normalizedDaysOfWeek = daysOfWeek ?? [];

    if (this.isEditMode()) {
      const schedule = this.editingSchedule();
      if (!schedule) return;

      const patch: PatchScheduledTransactionRequest = {
        description,
        amount,
        categoryIds: categoryIds || [],
        frequency,
        interval: interval || 1,
        startDate,
        endDate,
        maxOccurrences,
        dayOfMonth,
        daysOfWeek: normalizedDaysOfWeek,
        isActive: isActive ?? true,
      };

      const updated = await this.transactionService.patchScheduledTransaction(schedule.id, patch);
      if (updated) {
        this.scheduledTransactionUpdated.emit();
        this.visible.set(false);
      }
    } else {
      const newSchedule: RecurringTransactions = {
        id: 0, // Will be assigned by backend
        accountId: this.selectedBudget()!.accountId,
        budgetId: this.budgetId()!,
        description: description || '',
        amount,
        categoryIds: categoryIds || [],
        frequency,
        interval: interval || 1,
        startDate,
        isActive: isActive ?? true,
        generatedCount: 0,
        createdAtUtc: new Date(),
        updatedAtUtc: new Date(),
        ...(endDate && { endDate }),
        ...(maxOccurrences && { maxOccurrences }),
        ...(dayOfMonth && { dayOfMonth }),
        ...(normalizedDaysOfWeek.length > 0 && { daysOfWeek: normalizedDaysOfWeek }),
      } as RecurringTransactions;

      const created = await this.transactionService.createScheduledTransaction(newSchedule);
      if (created) {
        this.scheduledTransactionCreated.emit();
        this.scheduledTransactionForm.reset();
        this.visible.set(false);
      }
    }
  }

  private async loadCategories(): Promise<void> {
    const response = await this.categoryService.getCategories();
    this.categories.set(response?.items || []);
  }

  private populateFormWithSchedule(schedule: RecurringTransactions): void {
    this.scheduledTransactionForm.patchValue({
      description: schedule.description || '',
      amount: schedule.amount,
      categoryIds: schedule.categoryIds,
      frequency: schedule.frequency,
      interval: schedule.interval,
      startDate: new Date(schedule.startDate),
      endDate: schedule.endDate ? new Date(schedule.endDate) : null,
      maxOccurrences: schedule.maxOccurrences || null,
      dayOfMonth: schedule.dayOfMonth || null,
      daysOfWeek: schedule.daysOfWeek || [],
      isActive: schedule.isActive,
    });
  }
}
