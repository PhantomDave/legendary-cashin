import { Component, inject, model, output } from '@angular/core';
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
import { CreateTransactionRequest } from '../../services/transaction.service';

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
  readonly transactionCreated = output<Transaction>();
  readonly formBuilder = inject(FormBuilder);
  private readonly transactionService = inject(TransactionService);
  private readonly budgetService = inject(BudgetService);
  private readonly selectedBudget = this.budgetService.selectedBudget;

  readonly transactionForm = this.formBuilder.group({
    date: [null as Date | null, [Validators.required]],
    amount: [0, [Validators.required]],
    description: ['', [Validators.required, Validators.minLength(3)]],
  });

  isInvalid(controlName: 'date' | 'amount'): boolean {
    const control = this.transactionForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  onSubmit() {
    if (this.transactionForm.valid && this.selectedBudget()) {
      const { date, amount, description } = this.transactionForm.getRawValue();
      if (!date || amount == null || description == null) {
        return;
      }

      const transactionData: CreateTransactionRequest = {
        date: date.toISOString(),
        amount,
        description: description.trim(),
        budgetId: this.selectedBudget()!.id,
        categoryIds: [],
      };

      this.transactionService.createTransaction(transactionData).then((created) => {
        if (created) {
          this.transactionCreated.emit(created);
          this.transactionForm.reset();
          this.visible.set(false);
        }
      });
    }
  }
}
