import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Budget } from '../models/budget/Budget';
import { PaginatedResponse } from '../models/api/paginated-response.model';

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
  private readonly defaultPageSize = 15;

  private syncSelectedBudget(budgets: Budget[]): void {
    const current = this.selectedBudget();
    if (!current || !budgets.some((budget) => budget.id === current.id)) {
      this.selectedBudget.set(budgets[0] ?? null);
    }
  }

  async getBudgets(
    pageNumber: number = 1,
    pageSize: number = this.defaultPageSize,
  ): Promise<PaginatedResponse<Budget> | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.get<PaginatedResponse<Budget>>(this.baseApiUrl, {
        params: {
          pageNumber,
          pageSize,
        },
      });

      this.budgets.set(result.items);
      this.syncSelectedBudget(result.items);
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

  async patchBudget(
    id: number,
    patch: Partial<Pick<Budget, 'name' | 'defaultCurrency' | 'amount'>>,
  ): Promise<Budget | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const updated = await this.api.patch<Budget>(`${this.baseApiUrl}${id}`, patch);
      this.budgets.update((budgets) =>
        budgets.map((budget) => (budget.id === id ? updated : budget)),
      );
      const current = this.selectedBudget();
      if (current?.id === updated.id) {
        this.selectedBudget.set(updated);
      }
      return updated;
    } catch (e) {
      this.error.set((e as Error).message);
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }
}
