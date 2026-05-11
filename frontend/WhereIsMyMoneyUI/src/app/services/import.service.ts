import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { CreateEnableBankingRequest } from '../models/import/CreateEnableBankingRequest';
import { EnableBanking } from '../models/import/EnableBanking';

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
      const response = await this.api.get<EnableBanking[]>(`${this.baseApiUrl}enablebanking`);
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
}
