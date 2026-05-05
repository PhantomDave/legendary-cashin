import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Budget } from '../models/budget/Budget';

@Injectable({
  providedIn: 'root',
})
export class BudgetService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedBudget = signal<Budget | null>(null);
  readonly budgets = signal<Budget[]>([]);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/budgets/';

  private syncSelectedBudget(budgets: Budget[]): void {
    const current = this.selectedBudget();
    if (!current || !budgets.some((budget) => budget.id === current.id)) {
      this.selectedBudget.set(budgets[0] ?? null);
    }
  }

  async getBudgets(): Promise<Budget[] | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.get<Budget[]>(this.baseApiUrl, undefined, true);
      this.budgets.set(result);
      this.syncSelectedBudget(result);
      return result;
    } catch (e) {
      this.error.set((e as Error).message);
      return null;
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
      this.budgets.update((budgets) => {
        const updatedBudgets = [...budgets, result];
        this.syncSelectedBudget(updatedBudgets);
        return updatedBudgets;
      });
    } catch (e) {
      this.error.set((e as Error).message);
    } finally {
      this.isLoading.set(false);
    }
  }
}
