import { inject, Injectable, signal } from '@angular/core';
import { Transaction } from '../models/transaction/Transaction';
import { TransactionMetrics } from '../models/transaction/TransactionMetrics';
import { ApiService } from './api.service';
import { PaginatedResponse } from '../models/api/paginated-response.model';

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

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly transactions = signal<Transaction[]>([]);
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

  clearTransactions(): void {
    this.transactions.set([]);
  }
}
