export interface Transaction {
  id: number;
  accountId: number;
  date: string;
  amount: number;
  description: string;
  budgetId: number;
  categoryIds: number[];
}
