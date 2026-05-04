import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Account, AuthResponse } from '../models/Auth/Account';
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

  async register(email: string, username: string, password: string) {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const user = await this.api.post<Account>('/accounts', { email, username, password });
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
      const user = await this.api.post<AuthResponse>('/accounts/authenticate', {
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
