import { Component, inject, signal, effect } from '@angular/core';
import { DatePipe, CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ImportService } from '../../services/import.service';
import { EnableBanking } from '../../models/import/EnableBanking';
import { ToastService } from '../../services/toast.service';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-enable-banking-connections-component',
  imports: [
    CommonModule,
    DatePipe,
    ButtonModule,
    TableModule,
    TagModule,
    ProgressSpinnerModule,
    TooltipModule,
    ConfirmDialog,
  ],
  providers: [ConfirmationService],
  templateUrl: './enable-banking-connections-component.html',
  styleUrl: './enable-banking-connections-component.scss',
})
export class EnableBankingConnectionsComponent {
  private readonly importService = inject(ImportService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toast = inject(ToastService);

  readonly integrations = signal<EnableBanking[]>([]);
  readonly isLoading = this.importService.isLoading;
  readonly error = this.importService.error;

  constructor() {
    effect(() => {
      this.loadIntegrations();
    });
  }

  async loadIntegrations(): Promise<void> {
    const integrations = await this.importService.getEnableBankingIntegrations();
    this.integrations.set(integrations);
  }

  configureIntegration(integration: EnableBanking): void {
    // NOOP - placeholder for configuration logic
    console.log('Configure integration:', integration);
  }

  async deleteIntegration(event: Event, integration: EnableBanking): Promise<void> {
    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: 'Are you sure you want to delete this integration?',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        const success = await this.importService.deleteEnableBankingIntegration(integration.id);
        if (success) {
          await this.loadIntegrations();
          this.toast.success('Integration deleted successfully');
        } else {
          this.toast.error('Failed to delete integration');
        }
      },
    });
  }

  getAspsLabel(asps: string | null): string {
    if (!asps) return 'Not configured';
    return asps
      .split(',')
      .map((asp) => asp.trim())
      .join(', ');
  }
}
