import { Component, inject, input, model } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputTextModule } from 'primeng/inputtext';

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

  readonly transactionForm = this.formBuilder.group({
    date: ['', [Validators.required]],
    amount: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+(\.\d{1,2})?$/)]],
    description: [''],
    category: [''],
  });

  isInvalid(controlName: 'date' | 'amount'): boolean {
    const control = this.transactionForm.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  onSubmit() {
    throw new Error('Method not implemented.');
  }
}
