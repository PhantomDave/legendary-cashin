import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ImportJobStatus, ImportService } from '../../../services/import.service';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-import-callback-page',
  standalone: true,
  imports: [CommonModule, ProgressSpinnerModule, ButtonModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-6">
      @if (status() === 'loading') {
        <p-progressSpinner></p-progressSpinner>
        <p class="text-gray-600">{{ loadingMessage() }}</p>
      } @else if (status() === 'success') {
        <i class="pi pi-check-circle text-green-500" style="font-size: 3rem"></i>
        @if (isForceSync()) {
          <h2 class="text-xl font-semibold">Force Sync completed successfully!</h2>
          <p class="text-gray-600">
            {{ successMessage() }}
          </p>
        } @else {
          <h2 class="text-xl font-semibold">Bank connected successfully!</h2>
          <p class="text-gray-600">Your bank account has been linked.</p>
        }
        <p-button
          [label]="isForceSync() ? 'View Transactions' : 'Back to Import'"
          (onClick)="goToImport(true)"
        ></p-button>
      } @else if (status() === 'error') {
        <i class="pi pi-times-circle text-red-500" style="font-size: 3rem"></i>
        <h2 class="text-xl font-semibold">{{ isForceSync() ? 'Force Sync failed' : 'Connection failed' }}</h2>
        <p class="text-red-600">{{ errorMessage() }}</p>
        <p-button label="Back to Import" (onClick)="goToImport()" severity="secondary"></p-button>
      }
    </div>
  `,
})
export class ImportCallbackPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly importService = inject(ImportService);
  private readonly toast = inject(ToastService);

  readonly status = signal<'loading' | 'success' | 'error'>('loading');
  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  readonly isForceSync = signal(false);
  readonly loadingMessage = signal('Completing bank connection...');

  ngOnInit(): void {
    this.initializeCallback();
  }

  private async initializeCallback(): Promise<void> {
    const params = this.route.snapshot.queryParamMap;
    const error = params.get('error');
    const code = params.get('code');
    const state = params.get('state');
    const isForceSync = params.get('force_sync') === 'true';

    this.isForceSync.set(isForceSync);
    this.loadingMessage.set(
      isForceSync ? 'Authenticating and preparing your import...' : 'Completing bank connection...'
    );

    if (error) {
      this.status.set('error');
      this.errorMessage.set(params.get('error_description') || error);
      return;
    }

    if (!code || !state) {
      this.status.set('error');
      this.errorMessage.set('Missing authorization code or state.');
      return;
    }

    if (isForceSync) {
      await this.handleForceSyncCallback(code, state);
    } else {
      await this.handleBankAuthCallback(code, state);
    }
  }

  private async handleForceSyncCallback(code: string, state: string): Promise<void> {
    const result = await this.importService.completeForceSyncCallback(code, state);
    if (result) {
      this.loadingMessage.set('Authentication completed. Import is queued and will start shortly...');
      await this.waitForImportJob(result.jobId);
    } else {
      this.status.set('error');
      this.errorMessage.set(
        this.importService.error() || 'Failed to complete Force Sync.'
      );
    }
  }

  private async handleBankAuthCallback(code: string, state: string): Promise<void> {
    this.loadingMessage.set('Completing bank connection...');
    const success = await this.importService.completeBankAuth(code, state);
    if (success) {
      this.status.set('success');
      setTimeout(() => this.goToImport(true), 2000);
    } else {
      this.status.set('error');
      this.errorMessage.set(
        this.importService.error() || 'Failed to complete bank authentication.'
      );
    }
  }

  private async waitForImportJob(jobId: string): Promise<void> {
    const maxAttempts = 240; // ~10 minutes at 2.5s polling interval

    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const status = await this.importService.getImportJobStatus(jobId);
      if (!status) {
        this.status.set('error');
        this.errorMessage.set(this.importService.error() || 'Failed to read import job status.');
        return;
      }

      if (status.state === 'Queued') {
        this.loadingMessage.set('Import is queued...');
      }

      if (status.state === 'Running') {
        this.loadingMessage.set('Import in progress. This may take a few minutes for large ranges...');
      }

      if (status.state === 'Completed') {
        this.onImportJobCompleted(status);
        return;
      }

      if (status.state === 'Failed') {
        this.status.set('error');
        this.errorMessage.set(status.error || 'Force Sync import failed.');
        return;
      }

      await this.delay(2500);
    }

    this.status.set('error');
    this.errorMessage.set('Import is still running. Please check transactions in a few minutes.');
  }

  private onImportJobCompleted(status: ImportJobStatus): void {
    const totalFetched = status.result?.totalFetched ?? 0;
    const totalInserted = status.result?.totalInserted ?? 0;
    const totalSkipped = status.result?.totalSkipped ?? 0;

    this.successMessage.set(
      `Imported ${totalInserted} new transaction(s), ${totalFetched} fetched, ${totalSkipped} skipped.`
    );
    this.status.set('success');
    this.toast.success(this.successMessage());
    setTimeout(() => this.goToImport(true), 3000);
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => {
      setTimeout(resolve, ms);
    });
  }

  goToImport(withSuccess = false): void {
    const destination = this.isForceSync() ? '/transactions' : '/configuration/import';
    this.router.navigate([destination], {
      queryParams: withSuccess ? { refreshed: 'true' } : {},
    });
  }
}
