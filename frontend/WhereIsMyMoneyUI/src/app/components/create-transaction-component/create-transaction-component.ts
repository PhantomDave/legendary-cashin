import { Component, inject, input, model } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputTextModule } from 'primeng/inputtext';
import { TransactionService } from '../../services/transaction.service';
import { BudgetService } from '../../services/budget.service';
import { Transaction } from '../../models/transaction/Transaction';

@Component({
  selector: 'app-create-transaction-component',
  imports: [
    DialogModule,
    InputGroupModule,
    InputGroupAddon,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    DatePickerModule,
  ],
  templateUrl: './create-transaction-component.html',
  styleUrl: './create-transaction-component.scss',
})
export class CreateTransactionComponent {
  visible = model<boolean>(false);
  readonly formBuilder = inject(FormBuilder);
  private readonly transactionService = inject(TransactionService);
  private readonly budgetService = inject(BudgetService);
  private readonly selectedBudget = this.budgetService.selectedBudget;

  readonly transactionForm = this.formBuilder.group({
    date: ['', [Validators.required]],
    amount: [0, [Validators.required]],
    description: [''],
    category: [''],
  });

  isInvalid(controlName: 'date' | 'amount'): boolean {
    const control = this.transactionForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  onSubmit() {
    if (this.transactionForm.valid && this.selectedBudget()) {
      const transactionData = {
        ...this.transactionForm.value,
        budgetId: this.selectedBudget()!.id,
      } as Omit<Transaction, 'id'>;
      this.transactionService.createTransaction(transactionData).then((created) => {
        if (created) {
          this.transactionForm.reset();
          this.visible.set(false);
        }
      });
    }
  }
}
