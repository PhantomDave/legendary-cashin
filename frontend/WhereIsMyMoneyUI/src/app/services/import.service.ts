import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { CreateEnableBankingRequest } from '../models/import/CreateEnableBankingRequest';

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
}
