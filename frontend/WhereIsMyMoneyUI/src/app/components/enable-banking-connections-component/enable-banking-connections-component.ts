import {Component, effect, inject, signal} from '@angular/core';
import {CommonModule, DatePipe} from '@angular/common';
import {ButtonModule} from 'primeng/button';
import {TableModule} from 'primeng/table';
import {TagModule} from 'primeng/tag';
import {ProgressSpinnerModule} from 'primeng/progressspinner';
import {TooltipModule} from 'primeng/tooltip';
import {ConfirmDialog} from 'primeng/confirmdialog';
import {ImportService} from '../../services/import.service';
import {EnableBanking} from '../../models/import/EnableBanking';
import {EnableBankingBankSession} from '../../models/import/EnableBankingBankSession';
import {ToastService} from '../../services/toast.service';
import {ConfirmationService} from 'primeng/api';
import {ConfigureAspspDialogComponent} from '../configure-aspsp-dialog-component/configure-aspsp-dialog-component';
import {ConnectBankDialogComponent} from '../connect-bank-dialog-component/connect-bank-dialog-component';
import {
  ManualImportDialogComponent,
  ManualImportRequest,
} from '../manual-import-dialog-component/manual-import-dialog-component';
import {
  ForceSyncDialogComponent,
  ForceSyncRequest,
} from '../force-sync-dialog-component/force-sync-dialog-component';

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
    ConfigureAspspDialogComponent,
    ConnectBankDialogComponent,
    ManualImportDialogComponent,
    ForceSyncDialogComponent,
  ],
  providers: [ConfirmationService],
  templateUrl: './enable-banking-connections-component.html',
  styleUrl: './enable-banking-connections-component.scss',
})
export class EnableBankingConnectionsComponent {
  readonly integrations = signal<EnableBanking[]>([]);
  readonly sessions = signal<EnableBankingBankSession[]>([]);
  readonly configureDialogVisible = signal(false);
  readonly selectedIntegration = signal<EnableBanking | null>(null);
  readonly connectDialogVisible = signal(false);
  readonly connectIntegration = signal<EnableBanking | null>(null);
  readonly manualImportDialogVisible = signal(false);
  readonly manualImportSession = signal<EnableBankingBankSession | null>(null);
  readonly forceSyncDialogVisible = signal(false);
  readonly forceSyncSession = signal<EnableBankingBankSession | null>(null);
  private readonly importService = inject(ImportService);
  readonly isLoading = this.importService.isLoading;
  readonly error = this.importService.error;
  private readonly confirmationService = inject(ConfirmationService);
  private readonly toast = inject(ToastService);

  constructor() {
    effect(() => {
      this.loadIntegrations();
      this.loadSessions();
    });
  }

  async loadIntegrations(): Promise<void> {
    const integrations = await this.importService.getEnableBankingIntegrations();
    this.integrations.set(integrations);
  }

  configureIntegration(integration: EnableBanking): void {
    this.selectedIntegration.set(integration);
    this.configureDialogVisible.set(true);
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

  async onConfigurationComplete(): Promise<void> {
    await this.loadIntegrations();
  }

  connectBank(integration: EnableBanking): void {
    this.connectIntegration.set(integration);
    this.connectDialogVisible.set(true);
  }

  async loadSessions(): Promise<void> {
    this.sessions.set(await this.importService.getBankSessions());
  }

  async onBankConnected(): Promise<void> {
    await this.loadSessions();
  }

  async disconnectSession(event: Event, session: EnableBankingBankSession): Promise<void> {
    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: 'Are you sure you want to disconnect this bank session?',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        const success = await this.importService.deleteBankSession(session.id);
        if (success) {
          await this.loadSessions();
          this.toast.success('Bank session disconnected successfully');
        } else {
          this.toast.error('Failed to disconnect bank session');
        }
      },
    });
  }

  manualImport(session: EnableBankingBankSession): void {
    this.manualImportSession.set(session);
    this.manualImportDialogVisible.set(true);
  }

  async onManualImportRequested(request: ManualImportRequest): Promise<void> {
    const resp = await this.importService.startImportFromBankSession(
      request.session.id,
      request.from,
    );
    resp
      ? this.toast.success('Import started for ' + request.session.aspspName)
      : this.toast.error('Failed Import for ' + request.session.aspspName);
  }

  forceSync(session: EnableBankingBankSession): void {
    this.forceSyncSession.set(session);
    this.forceSyncDialogVisible.set(true);
  }

  async onForceSyncRequested(request: ForceSyncRequest): Promise<void> {
    const integration = this.integrations().find((i) => i.id === request.session.integrationId);
    if (!integration) {
      this.toast.error('Integration not found');
      return;
    }

    // Step 1: Initiate Force Sync OAuth flow
    const oauthResult = await this.importService.forceSyncWithSca(
      integration.id,
      request.session.aspspName,
      request.session.aspspCountry,
      request.startDate,
      request.endDate
    );

    if (!oauthResult) {
      this.toast.error('Failed to initiate Force Sync');
      return;
    }

    // Redirect to the bank's OAuth login. The server-issued state already keys the callback.
    globalThis.location.href = oauthResult.url;
  }
}
