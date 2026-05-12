import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-step-review',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h3 class="text-lg font-semibold m-0 mb-2">Review & Confirm</h3>
        <p class="text-sm text-surface-500 m-0">
          Please review your information below before submitting. Once submitted, your Enable
          Banking integration will be created.
        </p>
      </div>

      <div
        class="border-2 border-dashed border-surface-200 dark:border-surface-700 rounded bg-surface-50 dark:bg-surface-950 p-6"
      >
        <div class="flex flex-col gap-4">
          <!-- Application ID Review -->
          <div class="border-b border-surface-200 dark:border-surface-700 pb-4">
            <label class="block text-sm font-medium text-surface-700 dark:text-surface-200 mb-2">
              Application ID
            </label>
            @if (applicationId) {
              <p class="m-0 text-surface-900 dark:text-surface-100 break-all">
                {{ applicationId }}
              </p>
            } @else {
              <p class="m-0 text-surface-500">Not provided</p>
            }
          </div>

          <!-- Certificate Review -->
          <div>
            <label class="block text-sm font-medium text-surface-700 dark:text-surface-200 mb-2">
              Certificate
            </label>
            @if (certificate) {
              <div class="flex flex-col gap-2">
                <div class="flex items-center gap-2 text-green-600">
                  <i class="pi pi-check text-sm"></i>
                  <span>Certificate provided</span>
                </div>
                <p class="m-0 text-xs text-surface-500">
                  Size: {{ certificateLength() }} characters
                </p>
              </div>
            } @else {
              <p class="m-0 text-surface-500">Not provided</p>
            }
          </div>
        </div>
      </div>

      <!-- Info Box -->
      <div
        class="bg-blue-50 dark:bg-blue-950 border border-blue-200 dark:border-blue-800 rounded-lg p-4 text-sm text-blue-800 dark:text-blue-200"
      >
        <p class="m-0 flex items-start gap-2">
          <i class="pi pi-info-circle shrink-0 mt-0.5"></i>
          <span>
            After submission, you'll be able to connect your bank accounts through Enable Banking
            and start importing transactions automatically.
          </span>
        </p>
      </div>
    </div>
  `,
})
export class StepReviewComponent {
  @Input() applicationId: string = '';
  @Input() certificate: string = '';

  certificateLength(): number {
    return this.certificate?.length || 0;
  }

  isValid(): boolean {
    return !!(this.applicationId && this.certificate);
  }
}
