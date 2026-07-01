import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  model,
  output,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { RuleService } from '../../services/rule.service';
import { CategoryService } from '../../services/category.service';
import { BudgetService } from '../../services/budget.service';
import { Rule, MatchType, CreateRuleRequest } from '../../models/rule/Rule';
import { Category } from '../../models/category/Category';
import { Budget } from '../../models/budget/Budget';

@Component({
  selector: 'app-create-rule-component',
  imports: [
    Button,
    Dialog,
    InputText,
    InputNumberModule,
    SelectModule,
    MultiSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './create-rule-component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateRuleComponent implements OnInit {
  visible = model<boolean>(false);
  readonly created = output<Rule>();
  @ViewChild('nameInput') private readonly nameInput?: ElementRef<HTMLInputElement>;

  private readonly fb = inject(FormBuilder);
  private readonly ruleService = inject(RuleService);
  private readonly categoryService = inject(CategoryService);
  private readonly budgetService = inject(BudgetService);

  readonly categories = signal<Category[]>([]);
  readonly budgets = signal<Budget[]>([]);
  readonly isLoading = this.ruleService.isLoading;
  readonly error = this.ruleService.error;

  readonly matchTypeOptions: { label: string; value: MatchType }[] = [
    { label: 'Exact', value: 'Exact' },
    { label: 'Partial (contains)', value: 'Partial' },
    { label: 'Regex', value: 'Regex' },
  ];

  readonly dayOfWeekOptions = [
    { label: 'Sunday', value: 0 },
    { label: 'Monday', value: 1 },
    { label: 'Tuesday', value: 2 },
    { label: 'Wednesday', value: 3 },
    { label: 'Thursday', value: 4 },
    { label: 'Friday', value: 5 },
    { label: 'Saturday', value: 6 },
  ];

  readonly form = this.fb.group({
    name: ['', [Validators.required]],
    matchType: ['Partial' as MatchType, [Validators.required]],
    descriptionPattern: ['', [Validators.required]],
    minAmount: [null as number | null],
    maxAmount: [null as number | null],
    budgetId: [null as number | null],
    daysOfWeek: new FormControl<number[] | null>(null),
    dayOfMonth: [null as number | null],
    categoryIds: new FormControl<number[]>([], {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(1)],
    }),
  });

  readonly categoryOptions = signal<{ label: string; value: number }[]>([]);
  readonly budgetOptions = signal<{ label: string; value: number }[]>([]);

  async ngOnInit(): Promise<void> {
    const [cats, buds] = await Promise.all([
      this.categoryService.getCategories(1, 100),
      this.budgetService.getBudgets(1, 100),
    ]);
    if (cats) {
      this.categories.set(cats.items);
      this.categoryOptions.set(cats.items.map((c) => ({ label: c.name, value: c.id })));
    }
    if (buds) {
      this.budgets.set(buds.items);
      this.budgetOptions.set(buds.items.map((b) => ({ label: b.name, value: b.id })));
    }
  }

  protected isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  protected async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const request: CreateRuleRequest = {
      name: v.name!,
      matchType: v.matchType!,
      descriptionPattern: v.descriptionPattern!,
      categoryIds: v.categoryIds,
      ...(v.minAmount != null ? { minAmount: v.minAmount } : {}),
      ...(v.maxAmount != null ? { maxAmount: v.maxAmount } : {}),
      ...(v.budgetId != null ? { budgetId: v.budgetId } : {}),
      ...(v.daysOfWeek != null ? { daysOfWeek: v.daysOfWeek } : {}),
      ...(v.dayOfMonth != null ? { dayOfMonth: v.dayOfMonth } : {}),
    };

    const rule = await this.ruleService.createRule(request);

    if (!rule) return;

    this.visible.set(false);
    this.form.reset({ matchType: 'Partial', categoryIds: [] });
    this.created.emit(rule);
  }

  protected onHide(): void {
    this.form.reset({ matchType: 'Partial', categoryIds: [] });
  }

  protected onShow(): void {
    queueMicrotask(() => this.nameInput?.nativeElement.focus());
  }
}
