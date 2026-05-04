export interface TransactionItem {
  date: string;
  merchant: string;
  amount: string;
  category: string;
  status: 'Cleared' | 'Pending';
}

