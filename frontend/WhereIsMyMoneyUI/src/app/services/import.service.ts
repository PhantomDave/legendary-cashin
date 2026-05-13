import {inject, Injectable, signal} from '@angular/core';
import {ApiService} from './api.service';
import {CreateEnableBankingRequest} from '../models/import/CreateEnableBankingRequest';
import {EnableBanking} from '../models/import/EnableBanking';
import {AspspData} from '../models/import/AspspData';
import {EnableBankingBankSession} from '../models/import/EnableBankingBankSession';

@Injectable({
  providedIn: 'root',
})
export class ImportService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/import/';

  async createEnableBankingIntegration(request: CreateEnableBankingRequest) {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.post(`${this.baseApiUrl}enablebanking`, request);
      return response;
    } catch (err) {
      this.error.set('Failed to create EnableBanking integration.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getEnableBankingIntegrations(): Promise<EnableBanking[]> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.get<EnableBanking[]>(
        `${this.baseApiUrl}enablebanking/integrations`,
      );
      return response || [];
    } catch (err) {
      this.error.set('Failed to fetch EnableBanking integrations.');
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteEnableBankingIntegration(id: number): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      await this.api.delete(`${this.baseApiUrl}enablebanking/${id}`);
      return true;
    } catch (err) {
      this.error.set('Failed to delete EnableBanking integration.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }

  async configureCountries(id: number, countries: string[]): Promise<AspspData[]> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const aspsp = await this.api.post<AspspData[]>(
        `${this.baseApiUrl}enablebanking/${id}/start-configuration`,
        {
          countries,
        },
      );
      return aspsp || [];
    } catch (err) {
      this.error.set('Failed to configure countries.');
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  async saveAspspsConfiguration(
    id: number,
    selectedAspsps: string[],
    selectedCountries: string[],
  ): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.post<{ message: string; integrationId: number }>(
        `${this.baseApiUrl}enablebanking/${id}/configure-aspsps`,
        {
          selectedAspsps,
          selectedCountries,
        },
      );
      return !!response;
    } catch (err) {
      this.error.set('Failed to save ASPSPs configuration.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }

  async startBankAuth(
    integrationId: number,
    aspspName: string,
    aspspCountry: string,
  ): Promise<{ url: string; state: string } | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.post<{ url: string; state: string }>(
        `${this.baseApiUrl}enablebanking/${integrationId}/start-bank-auth`,
        { aspspName, aspspCountry },
      );
    } catch (err) {
      this.error.set('Failed to start bank authentication.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async completeBankAuth(code: string, state: string): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.post(`${this.baseApiUrl}enablebanking/complete-bank-auth`, { code, state });
      return true;
    } catch (err: any) {
      this.error.set(err?.error?.error || 'Failed to complete bank authentication.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getBankSessions(): Promise<EnableBankingBankSession[]> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return (
        (await this.api.get<EnableBankingBankSession[]>(
          `${this.baseApiUrl}enablebanking/sessions`,
        )) || []
      );
    } catch (err) {
      this.error.set('Failed to fetch bank sessions.');
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteBankSession(id: number): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.delete(`${this.baseApiUrl}enablebanking/sessions/${id}`);
      return true;
    } catch (err) {
      this.error.set('Failed to delete bank session.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }

  async startImportFromBankSession(sessionId: number, startDate: Date): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.post(`${this.baseApiUrl}enablebanking/sessions/${sessionId}/start-import`, {
        startDate,
      });
      return true;
    } catch (err) {
      this.error.set('Failed to start import from bank session.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }
}
