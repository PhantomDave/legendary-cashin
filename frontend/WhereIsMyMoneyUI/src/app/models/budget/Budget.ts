export interface Budget {
  id: number;
  accountId: number;
  name: string;
  defaultCurrency: string;
  amount: number;
  createdAtUtc: string;
}
