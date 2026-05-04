export interface Account {
  id: number;
  name: string;
  email: string;
}

export type CreateAccountResponse = Account;
export type AuthResponse = Account & { token: string };
