export type MatchType = 'Exact' | 'Partial' | 'Regex';

export interface Rule {
  id: number;
  accountId: number;
  name: string;
  priority: number;
  isActive: boolean;
  matchType: MatchType;
  descriptionPattern: string;
  minAmount?: number;
  maxAmount?: number;
  budgetId?: number;
  daysOfWeek?: number[];
  dayOfMonth?: number;
  categoryIds: number[];
}

export interface CreateRuleRequest {
  name: string;
  matchType: MatchType;
  descriptionPattern: string;
  minAmount?: number;
  maxAmount?: number;
  budgetId?: number;
  daysOfWeek?: number[];
  dayOfMonth?: number;
  categoryIds: number[];
}

export interface UpdateRuleRequest extends CreateRuleRequest {
  isActive: boolean;
  priority: number;
}

export interface PatchRuleRequest {
  isActive?: boolean;
  priority?: number;
  name?: string;
  matchType?: MatchType;
  descriptionPattern?: string;
  minAmount?: number;
  maxAmount?: number;
  budgetId?: number;
  daysOfWeek?: number[];
  dayOfMonth?: number;
  categoryIds?: number[];
}

export interface ApplyToExistingRequest {
  fromDate: string;
  toDate: string;
  overwriteExisting: boolean;
}
