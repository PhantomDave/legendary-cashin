import { inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
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
      this.error.set(this.extractErrorMessage(e));
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
      await cookieStore.set('authToken', user!.token);
      await this.router.navigate(['/dashboard']);
    } catch (e) {
      const errorMsg = this.extractErrorMessage(e);
      this.error.set(errorMsg);
    } finally {
      this.isLoading.set(false);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      // Try to get the message from the API response body
      if (error.error && typeof error.error === 'object' && 'message' in error.error) {
        return String((error.error as Record<string, unknown>)['message']);
      }
      // Fall back to HTTP status text
      return error.statusText || `HTTP Error ${error.status}`;
    }
    if (error instanceof Error) {
      return error.message;
    }
    if (typeof error === 'object' && error !== null && 'message' in error) {
      return String((error as Record<string, unknown>)['message']);
    }
    return 'An unexpected error occurred';
  }

  logout() {
    this.user.set(null);
  }
}
