import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Rule, CreateRuleRequest, UpdateRuleRequest, PatchRuleRequest, ApplyToExistingRequest } from '../models/rule/Rule';
import { PaginatedResponse } from '../models/api/paginated-response.model';
import { Transaction } from '../models/transaction/Transaction';

@Injectable({
  providedIn: 'root',
})
export class RuleService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly api = inject(ApiService);
  private readonly baseApiUrl = '/rules/';

  async getRules(pageNumber = 1, pageSize = 10): Promise<PaginatedResponse<Rule> | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.get<PaginatedResponse<Rule>>(this.baseApiUrl, {
        params: { pageNumber, pageSize },
      });
    } catch {
      this.error.set('Failed to load rules.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async getActiveRules(): Promise<Rule[] | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.get<Rule[]>(`${this.baseApiUrl}active`);
    } catch {
      this.error.set('Failed to load active rules.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async createRule(request: CreateRuleRequest): Promise<Rule | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.post<Rule>(this.baseApiUrl, request);
    } catch (err: unknown) {
      this.error.set(this.extractError(err, 'Failed to create rule.'));
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async updateRule(id: number, request: UpdateRuleRequest): Promise<Rule | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.put<Rule>(`${this.baseApiUrl}${id}`, request);
    } catch (err: unknown) {
      this.error.set(this.extractError(err, 'Failed to update rule.'));
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async patchRule(id: number, request: PatchRuleRequest): Promise<Rule | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.patch<Rule>(`${this.baseApiUrl}${id}`, request);
    } catch (err: unknown) {
      this.error.set(this.extractError(err, 'Failed to update rule.'));
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteRule(id: number): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.delete<void>(`${this.baseApiUrl}${id}`);
      return true;
    } catch {
      this.error.set('Failed to delete rule.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }

  async reorderRules(ruleIds: number[]): Promise<boolean> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      await this.api.post<void>(`${this.baseApiUrl}reorder`, { ruleIds });
      return true;
    } catch {
      this.error.set('Failed to reorder rules.');
      return false;
    } finally {
      this.isLoading.set(false);
    }
  }

  async previewRule(id: number, pageNumber = 1, pageSize = 10): Promise<PaginatedResponse<Transaction> | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      return await this.api.post<PaginatedResponse<Transaction>>(
        `${this.baseApiUrl}${id}/preview`,
        {},
        { params: { pageNumber, pageSize } },
      );
    } catch {
      this.error.set('Failed to load rule preview.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async applyToExisting(request: ApplyToExistingRequest): Promise<number | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.post<{ updated: number }>(`${this.baseApiUrl}apply-existing`, request);
      return result.updated;
    } catch {
      this.error.set('Failed to apply rules to existing transactions.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  async countExisting(request: ApplyToExistingRequest): Promise<number | null> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.api.post<{ count: number }>(`${this.baseApiUrl}count-existing`, request);
      return result.count;
    } catch {
      this.error.set('Failed to count matching transactions.');
      return null;
    } finally {
      this.isLoading.set(false);
    }
  }

  private extractError(err: unknown, fallback: string): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const e = err as { error?: { message?: string } };
      if (e.error?.message) return e.error.message;
    }
    return fallback;
  }
}
