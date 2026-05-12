import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ImportService } from '../../../services/import.service';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-import-callback-page',
  standalone: true,
  imports: [CommonModule, ProgressSpinnerModule, ButtonModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-6">
      @if (status() === 'loading') {
        <p-progressSpinner></p-progressSpinner>
        <p class="text-gray-600">Completing bank connection...</p>
      } @else if (status() === 'success') {
        <i class="pi pi-check-circle text-green-500" style="font-size: 3rem"></i>
        <h2 class="text-xl font-semibold">Bank connected successfully!</h2>
        <p class="text-gray-600">Your bank account has been linked.</p>
        <p-button label="Back to Import" (onClick)="goToImport()"></p-button>
      } @else if (status() === 'error') {
        <i class="pi pi-times-circle text-red-500" style="font-size: 3rem"></i>
        <h2 class="text-xl font-semibold">Connection failed</h2>
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

  readonly status = signal<'loading' | 'success' | 'error'>('loading');
  readonly errorMessage = signal('');

  async ngOnInit(): Promise<void> {
    const params = this.route.snapshot.queryParamMap;
    const error = params.get('error');
    const code = params.get('code');
    const state = params.get('state');

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

    const success = await this.importService.completeBankAuth(code, state);
    if (success) {
      this.status.set('success');
      setTimeout(() => this.goToImport(true), 2000);
    } else {
      this.status.set('error');
      this.errorMessage.set(
        this.importService.error() || 'Failed to complete bank authentication.',
      );
    }
  }

  goToImport(withSuccess = false): void {
    this.router.navigate(['/configuration/import'], {
      queryParams: withSuccess ? { bankConnected: 'true' } : {},
    });
  }
}
