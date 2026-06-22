import { Component, Input, Output, EventEmitter, inject, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StepperModule } from 'primeng/stepper';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { StepApplicationIdComponent } from './steps/step-application-id';
import { StepCertificateComponent } from './steps/step-certificate';
import { StepReviewComponent } from './steps/step-review';
import { ImportService } from '../../services/import.service';

@Component({
  selector: 'app-enable-banking-stepper-component',
  standalone: true,
  imports: [
    CommonModule,
    StepperModule,
    ButtonModule,
    DialogModule,
    StepApplicationIdComponent,
    StepCertificateComponent,
    StepReviewComponent,
  ],
  templateUrl: './enable-banking-stepper-component.html',
  styleUrl: './enable-banking-stepper-component.scss',
})
export class EnableBankingStepperComponent {
  @Input() isOpen = false;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() onSuccess = new EventEmitter<void>();

  @ViewChild('appStepApplicationId') applicationIdStep!: StepApplicationIdComponent;
  @ViewChild('appStepCertificate') certificateStep!: StepCertificateComponent;
  @ViewChild('appStepReview') reviewStep!: StepReviewComponent;

  private readonly importService = inject(ImportService);

  // Form data
  applicationId = signal('');
  certificate = signal('');
  textAreaEnabled = signal(false);
  isLoading = signal(false);
  errorMessage = signal('');

  onApplicationIdChange(value: string): void {
    this.applicationId.set(value);
  }

  onCertificateChange(value: string): void {
    this.certificate.set(value);
  }

  onTextAreaEnabledChange(value: boolean): void {
    this.textAreaEnabled.set(value);
  }

  canProceedToStep2(): boolean {
    return !!this.applicationId();
  }

  canProceedToStep3(): boolean {
    return this.canProceedToStep2() && !!this.certificate();
  }

  canSubmit(): boolean {
    return this.canProceedToStep3();
  }

  async onSubmit(): Promise<void> {
    if (!this.canSubmit()) {
      this.errorMessage.set('Please complete all required fields');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    try {
      await this.importService.createEnableBankingIntegration({
        applicationId: this.applicationId(),
        certificate: this.certificate(),
      });

      // Success - close the stepper
      this.close();
      this.onSuccess.emit();
    } catch (error: any) {
      this.errorMessage.set(
        error?.message || 'Failed to create Enable Banking integration. Please try again.',
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  close(): void {
    this.isOpenChange.emit(false);
  }

  reset(): void {
    this.applicationId.set('');
    this.certificate.set('');
    this.textAreaEnabled.set(false);
    this.errorMessage.set('');
  }
}
