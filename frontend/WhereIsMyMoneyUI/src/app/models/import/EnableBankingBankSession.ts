export interface EnableBankingBankSession {
  id: number;
  integrationId: number;
  accountId: number;
  sessionId: string;
  aspspName: string;
  aspspCountry: string;
  validUntil: string;
  accountsJson: string;
  createdAtUtc: string;
}
