import { DayOfWeek } from '../enum/day-of-week';
import { RecurrenceFrequency } from '../enum/recurrence-frequency';

export interface RecurringTransactions {
  id: number;
  accountId: number;
  budgetId: number;

  description?: string;
  amount: number;
  categoryIds: number[];

  frequency: RecurrenceFrequency;
  interval: number;
  startDate: Date;
  endDate?: Date;
  maxOccurrences?: number;

  daysOfWeek?: DayOfWeek[];
  dayOfMonth?: number;

  lastGeneratedDate?: Date;
  generatedCount: number;
  isActive: boolean;

  createdAtUtc: Date;
  updatedAtUtc: Date;
}
