import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { TransactionItem } from '../models/transaction-item.model';
import { Budget } from '../models/budget/Budget';

@Injectable({
  providedIn: 'root',
})
export class BudgetService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly budgets = signal<Budget[]>([]);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/budgets/';

  async getBudgets(): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.get<Budget[]>(this.baseApiUrl, undefined, true);
      this.budgets.set(result);
    } catch (e) {
      this.error.set((e as Error).message);
    } finally {
      this.isLoading.set(false);
    }
  }

  async createBudget(name: string, defaultCurrency: string, amount: number): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.post<Budget>(
        this.baseApiUrl,
        { name, defaultCurrency, amount },
        undefined,
        true,
      );
      this.budgets.update((budgets) => [...budgets, result]);
    } catch (e) {
      this.error.set((e as Error).message);
    } finally {
      this.isLoading.set(false);
    }
  }
}
