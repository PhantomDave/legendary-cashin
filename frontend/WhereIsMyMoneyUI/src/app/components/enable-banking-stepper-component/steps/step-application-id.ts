import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputTextModule } from 'primeng/inputtext';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-step-application-id',
  standalone: true,
  imports: [ReactiveFormsModule, InputGroupModule, InputGroupAddon, InputTextModule, CommonModule],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h3 class="text-lg font-semibold m-0 mb-2">Application ID</h3>
        <p class="text-sm text-surface-500 m-0">
          Enter your Enable Banking application ID. You can find this in your Enable Banking
          dashboard under API credentials.
        </p>
      </div>

      <div
        class="border-2 border-dashed border-surface-200 dark:border-surface-700 rounded bg-surface-50 dark:bg-surface-950 p-6"
      >
        <form [formGroup]="stepForm" class="flex flex-col gap-4">
          <div>
            <label class="block text-sm font-medium mb-3">Application ID *</label>
            <p-inputgroup>
              <p-inputgroup-addon><i class="pi pi-wallet"></i></p-inputgroup-addon>
              <input
                formControlName="applicationId"
                pInputText
                placeholder="e.g., 550e8400-e29b-41d4-a716-446655440000"
                class="w-full"
              />
            </p-inputgroup>
            @if (stepForm.get('applicationId')?.invalid && stepForm.get('applicationId')?.touched) {
              <small class="text-red-500 block mt-2">Application ID is required</small>
            }
          </div>
        </form>
      </div>
    </div>
  `,
})
export class StepApplicationIdComponent {
  private readonly formBuilder = inject(FormBuilder);

  @Input() applicationId: string = '';
  @Output() applicationIdChange = new EventEmitter<string>();

  readonly stepForm = this.formBuilder.group({
    applicationId: ['', Validators.required],
  });

  constructor() {
    this.stepForm.get('applicationId')?.valueChanges.subscribe((value) => {
      this.applicationIdChange.emit(value || '');
    });

    if (this.applicationId) {
      this.stepForm.patchValue({ applicationId: this.applicationId });
    }
  }

  isValid(): boolean {
    return this.stepForm.valid;
  }

  getValue(): string {
    return this.stepForm.get('applicationId')?.value || '';
  }
}
