import { Component, inject, input, model } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { BudgetService } from '../../../services/budget.service';

@Component({
  selector: 'app-create-budget-component',
  imports: [InputGroupModule, InputGroupAddon, DialogModule, ReactiveFormsModule, ButtonModule],
  templateUrl: './create-budget-component.html',
  styleUrl: './create-budget-component.scss',
})
export class CreateBudgetComponent {
  visible = model<boolean>(false);
  private readonly formBuilder = inject(FormBuilder);
  private readonly budgetService = inject(BudgetService);

  readonly budgetForm = this.formBuilder.group({
    budgetName: ['', [Validators.required, Validators.minLength(3)]],
    currency: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    amount: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+(\.\d{1,2})?$/)]],
  });

  isInvalid(controlName: 'budgetName' | 'currency' | 'amount'): boolean {
    const control = this.budgetForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  onSubmit() {
    if (this.budgetForm.invalid) {
      this.budgetForm.markAllAsTouched();
      return;
    }

    const { budgetName, currency, amount } = this.budgetForm.getRawValue();
    this.budgetService.createBudget(budgetName!, currency!, amount!);
    this.visible.set(false);
    this.budgetForm.reset();
  }
}
