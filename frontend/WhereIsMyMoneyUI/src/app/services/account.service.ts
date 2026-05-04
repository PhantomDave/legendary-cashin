import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Account, AuthResponse } from '../models/auth/Account';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly user = signal<Account | null>(null);

  private router: Router = inject(Router);
  private api: ApiService = inject(ApiService);
  private baseApiUrl = '/accounts/';
  async register(email: string, username: string, password: string) {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const user = await this.api.post<Account>(`${this.baseApiUrl}`, {
        email,
        username,
        password,
      });
      this.user.set(user!);
    } catch (e) {
      this.error.set((e as Error).message);
    } finally {
      this.isLoading.set(false);
    }
  }

  async login(username: string, password: string) {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const user = await this.api.post<AuthResponse>(`${this.baseApiUrl}authenticate`, {
        username,
        password,
      });
      this.user.set(user!);
      cookieStore.set('authToken', user!.token);
      this.router.navigate(['/dashboard']);
    } catch (e) {
      this.error.set((e as Error).message);
    } finally {
      this.isLoading.set(false);
    }
  }

  logout() {
    this.user.set(null);
  }
}
