import { inject, Injectable, signal } from '@angular/core';
import { Transaction } from '../models/transaction/Transaction';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly transactions = signal<Transaction[]>([]);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/transactions/';

  async getTransactions(): Promise<Transaction[] | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const transactions = await this.api.get<Transaction[]>(this.baseApiUrl);
      this.transactions.set(transactions);
      return transactions;
    } catch (error) {
      this.error.set('Failed to load transactions.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getByBudgetId(budgetId: number): Promise<Transaction[] | null> {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const transactions = await this.api.get<Transaction[]>(
        `${this.baseApiUrl}budget/${budgetId}`,
      );
      this.transactions.set(transactions);
      return transactions;
    } catch (error) {
      this.error.set('Failed to load transactions for the selected budget.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async createTransaction(transaction: Omit<Transaction, 'id'>): Promise<Transaction | null> {
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
}
