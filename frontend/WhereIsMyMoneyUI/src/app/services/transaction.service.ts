import { inject, Injectable, signal } from '@angular/core';
import { Transaction } from '../models/transaction/Transaction';
import { TransactionMetrics } from '../models/transaction/TransactionMetrics';
import { ApiService } from './api.service';
import { PaginatedResponse } from '../models/api/paginated-response.model';
import { RecurringTransactions } from '../models/transaction/ScheduledTransaction';

export interface CreateTransactionRequest {
  description: string;
  amount: number;
  date: string;
  budgetId: number;
  categoryIds: number[];
}

export interface PatchTransactionRequest {
  description?: string;
  amount?: number;
  date?: string;
  budgetId?: number;
  categoryIds?: number[];
}

export interface PatchScheduledTransactionRequest {
  description?: string;
  amount?: number;
  categoryIds?: number[];
  frequency?: number;
  interval?: number;
  startDate?: Date;
  endDate?: Date | null;
  maxOccurrences?: number | null;
  daysOfWeek?: number[];
  dayOfMonth?: number | null;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly transactions = signal<Transaction[]>([]);
  readonly monthTransactions = signal<Transaction[]>([]);
  readonly scheduledTransactions = signal<RecurringTransactions[]>([]);
  readonly metrics = signal<TransactionMetrics | null>(null);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/transactions/';
  private readonly defaultPageSize = 15;

  async getMetrics(budgetId: number): Promise<TransactionMetrics | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.get<TransactionMetrics>(
        `${this.baseApiUrl}metrics?budgetId=${budgetId}`,
      );
      this.metrics.set(result);
      return result;
    } catch {
      this.error.set('Failed to load transaction metrics.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getTransactions(
    pageNumber: number = 1,
    pageSize: number = this.defaultPageSize,
  ): Promise<PaginatedResponse<Transaction> | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.get<PaginatedResponse<Transaction>>(this.baseApiUrl, {
        params: {
          pageNumber,
          pageSize,
        },
      });

      this.transactions.set(response.items);
      return response;
    } catch (error) {
      this.error.set('Failed to load transactions.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getByBudgetId(
    budgetId: number,
    pageNumber: number = 1,
    pageSize: number = this.defaultPageSize,
  ): Promise<PaginatedResponse<Transaction> | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.get<PaginatedResponse<Transaction>>(
        `${this.baseApiUrl}budget/${budgetId}`,
        {
          params: {
            pageNumber,
            pageSize,
          },
        },
      );

      this.transactions.set(response.items);
      return response;
    } catch (error) {
      this.error.set('Failed to load transactions for the selected budget.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getMonthByBudgetId(
    budgetId: number,
    from?: string,
    to?: string,
  ): Promise<Transaction[] | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const params: Record<string, string> = {};
      if (from) {
        params['from'] = from;
      }
      if (to) {
        params['to'] = to;
      }

      const response = await this.api.get<Transaction[]>(
        `${this.baseApiUrl}budget/${budgetId}/month`,
        {
          params,
        },
      );

      this.monthTransactions.set(response);
      return response;
    } catch {
      this.error.set('Failed to load month transactions for the selected budget.');
      this.monthTransactions.set([]);
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async patchTransaction(id: number, patch: PatchTransactionRequest): Promise<Transaction | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const updated = await this.api.patch<Transaction>(`${this.baseApiUrl}${id}`, patch);
      this.transactions.update((current) => current.map((t) => (t.id === id ? updated : t)));
      return updated;
    } catch {
      this.error.set('Failed to update transaction.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async createTransaction(transaction: CreateTransactionRequest): Promise<Transaction | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const createdTransaction = await this.api.post<Transaction>(this.baseApiUrl, transaction);
      this.transactions.update((current) => [...current, createdTransaction]);
      return createdTransaction;
    } catch (error) {
      this.error.set('Failed to create transaction.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteTransaction(id: number): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.delete(`${this.baseApiUrl}${id}`);
      this.transactions.update((current) => current.filter((t) => t.id !== id));
    } catch {
      this.error.set('Failed to delete transaction.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async getScheduledTransactions(): Promise<RecurringTransactions[]> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const scheduled = await this.api.get<RecurringTransactions[]>(`${this.baseApiUrl}recurring`);
      this.scheduledTransactions.set(scheduled);
      return scheduled;
    } catch {
      this.error.set('Failed to load scheduled transactions.');
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  async getScheduledTransactionsByBudgetId(
    budgetId: number,
    pageNumber: number = 1,
    pageSize: number = this.defaultPageSize,
  ): Promise<PaginatedResponse<RecurringTransactions> | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const response = await this.api.get<PaginatedResponse<RecurringTransactions>>(
        `${this.baseApiUrl}recurring/budget/${budgetId}`,
        {
          params: {
            pageNumber,
            pageSize,
          },
        },
      );

      this.scheduledTransactions.set(response.items);
      return response;
    } catch (error) {
      this.error.set('Failed to load scheduled transactions for the selected budget.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteScheduledTransaction(id: number): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.delete(`${this.baseApiUrl}recurring/${id}`);
      this.scheduledTransactions.update((current) => current.filter((t) => t.id !== id));
    } catch {
      this.error.set('Failed to delete scheduled transaction.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async createScheduledTransaction(
    transaction: RecurringTransactions,
  ): Promise<RecurringTransactions | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const createdTransaction = await this.api.post<RecurringTransactions>(
        `${this.baseApiUrl}recurring`,
        transaction,
      );
      this.scheduledTransactions.update((current) => [...current, createdTransaction]);
      return createdTransaction;
    } catch {
      this.error.set('Failed to create scheduled transaction.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async patchScheduledTransaction(
    id: number,
    patch: PatchScheduledTransactionRequest,
  ): Promise<RecurringTransactions | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const updated = await this.api.patch<RecurringTransactions>(
        `${this.baseApiUrl}recurring/${id}`,
        patch,
      );
      this.scheduledTransactions.update((current) =>
        current.map((t) => (t.id === id ? updated : t)),
      );
      return updated;
    } catch {
      this.error.set('Failed to update scheduled transaction.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  clearTransactions(): void {
    this.transactions.set([]);
    this.monthTransactions.set([]);
    this.scheduledTransactions.set([]);
  }
}
