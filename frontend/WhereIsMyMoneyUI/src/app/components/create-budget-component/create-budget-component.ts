import { Component, inject, model } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { BudgetService } from '../../services/budget.service';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-create-budget-component',
  imports: [
    InputGroupModule,
    InputGroupAddon,
    DialogModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
  ],
  templateUrl: './create-budget-component.html',
  styleUrl: './create-budget-component.scss',
})
export class CreateBudgetComponent {
  visible = model<boolean>(false);
  private readonly formBuilder = inject(FormBuilder);
  readonly budgetForm = this.formBuilder.group({
    budgetName: ['', [Validators.required, Validators.minLength(3)]],
    currency: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    amount: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+(\.\d{1,2})?$/)]],
  });
  private readonly budgetService = inject(BudgetService);
  private readonly toast = inject(ToastService);

  isInvalid(controlName: 'budgetName' | 'currency' | 'amount'): boolean {
    const control = this.budgetForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  async onSubmit(): Promise<void> {
    if (this.budgetForm.invalid) {
      this.budgetForm.markAllAsTouched();
      return;
    }

    const { budgetName, currency, amount } = this.budgetForm.getRawValue();
    await this.budgetService.createBudget(budgetName!, currency!, amount!);

    if (this.budgetService.error()) {
      this.toast.error('Failed to create budget', this.budgetService.error()!);
    } else {
      this.toast.success('Budget created');
      this.visible.set(false);
      this.budgetForm.reset();
    }
  }
}
