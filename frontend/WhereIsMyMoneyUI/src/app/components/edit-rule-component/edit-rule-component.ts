import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  model,
  OnChanges,
  output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { PrimeTemplate } from 'primeng/api';
import { RuleService } from '../../services/rule.service';
import { CategoryService } from '../../services/category.service';
import { BudgetService } from '../../services/budget.service';
import { Rule, MatchType } from '../../models/rule/Rule';

@Component({
  selector: 'app-edit-rule-component',
  imports: [
    Button,
    Dialog,
    InputText,
    InputNumberModule,
    SelectModule,
    MultiSelectModule,
    CheckboxModule,
    ReactiveFormsModule,
    PrimeTemplate,
  ],
  templateUrl: './edit-rule-component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditRuleComponent implements OnChanges {
  visible = model<boolean>(false);
  readonly rule = input<Rule | null>(null);
  readonly updated = output<Rule>();

  private readonly fb = inject(FormBuilder);
  private readonly ruleService = inject(RuleService);
  private readonly categoryService = inject(CategoryService);
  private readonly budgetService = inject(BudgetService);

  readonly isLoading = this.ruleService.isLoading;
  readonly categoryOptions = signal<{ label: string; value: number }[]>([]);
  readonly budgetOptions = signal<{ label: string; value: number }[]>([]);

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
    categoryIds: new FormControl<number[]>([], { nonNullable: true, validators: [Validators.required, Validators.minLength(1)] }),
    isActive: [true],
    priority: [1, [Validators.required, Validators.min(1)]],
  });

  async ngOnChanges(changes: SimpleChanges): Promise<void> {
    if (changes['visible']?.currentValue === true || changes['rule']?.currentValue) {
      await this.loadOptions();
      const r = this.rule();
      if (r) this.patchForm(r);
    }
  }

  private async loadOptions(): Promise<void> {
    if (this.categoryOptions().length === 0) {
      const [cats, buds] = await Promise.all([
        this.categoryService.getCategories(1, 100),
        this.budgetService.getBudgets(1, 100),
      ]);
      if (cats) this.categoryOptions.set(cats.items.map((c) => ({ label: c.name, value: c.id })));
      if (buds) this.budgetOptions.set(buds.items.map((b) => ({ label: b.name, value: b.id })));
    }
  }

  private patchForm(r: Rule): void {
    this.form.patchValue({
      name: r.name,
      matchType: r.matchType,
      descriptionPattern: r.descriptionPattern,
      minAmount: r.minAmount ?? null,
      maxAmount: r.maxAmount ?? null,
      budgetId: r.budgetId ?? null,
      daysOfWeek: r.daysOfWeek ?? null,
      dayOfMonth: r.dayOfMonth ?? null,
      categoryIds: r.categoryIds,
      isActive: r.isActive,
      priority: r.priority,
    });
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

    const r = this.rule();
    if (!r) return;

    const v = this.form.getRawValue();
    const result = await this.ruleService.updateRule(r.id, {
      name: v.name!,
      matchType: v.matchType!,
      descriptionPattern: v.descriptionPattern!,
      minAmount: v.minAmount ?? undefined,
      maxAmount: v.maxAmount ?? undefined,
      budgetId: v.budgetId ?? undefined,
      daysOfWeek: v.daysOfWeek ?? undefined,
      dayOfMonth: v.dayOfMonth ?? undefined,
      categoryIds: v.categoryIds,
      isActive: v.isActive ?? true,
      priority: v.priority ?? 1,
    });

    if (!result) return;

    this.visible.set(false);
    this.updated.emit(result);
  }
}
