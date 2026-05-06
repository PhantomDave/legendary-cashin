import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { APP_NAVIGATION_ITEMS } from '../../constants/app-navigation.config';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { Budget } from '../../models/budget/Budget';
import { BudgetService } from '../../services/budget.service';
import { CreateBudgetComponent } from '../create-budget-component/create-budget-component';
import { PanelMenuModule } from 'primeng/panelmenu';
import { AvatarModule } from 'primeng/avatar';
import { AccountService } from '../../services/account.service';

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    ButtonModule,
    PanelMenuModule,
    SelectModule,
    FormsModule,
    CreateBudgetComponent,
    AvatarModule,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  readonly isDarkMode = signal(false);
  readonly navItems = APP_NAVIGATION_ITEMS;
  readonly themeIcon = computed(() => (this.isDarkMode() ? 'pi pi-moon' : 'pi pi-sun'));
  readonly displayName = signal('User');
  readonly userInitial = computed(() => this.displayName().trim().charAt(0).toUpperCase() || 'U');
  isCreateModalVisible: boolean = false;
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly budgetService = inject(BudgetService);
  private readonly accountService = inject(AccountService);
  readonly selectedBudget = this.budgetService.selectedBudget;
  readonly budgets = this.budgetService.budgets;

  async ngOnInit(): Promise<void> {
    this.syncThemeState();
    await this.resolveDisplayName();

    localStorage.getItem('theme') === 'dark' && this.toggleTheme();

    const budgetResponse = await this.budgetService.getBudgets();
    const budgets = budgetResponse?.items ?? [];

    if (!this.selectedBudget() && budgets.length > 0) {
      this.budgetService.selectedBudget.set(budgets[0]);
    }

    if (budgets.length === 0) {
      this.isCreateModalVisible = true;
    }
  }

  toggleTheme(): void {
    const nextValue = !this.isDarkMode();
    this.isDarkMode.set(nextValue);
    this.document.documentElement.classList.toggle('app-dark', nextValue);

    localStorage.setItem('theme', nextValue ? 'dark' : 'light');
  }

  onChange(selectedBudget: Budget): void {
    this.budgetService.selectedBudget.set(selectedBudget);
  }

  async logout(): Promise<void> {
    this.accountService.logout();
    await cookieStore.delete('authToken');
    await this.router.navigate(['/account/login']);
  }

  private syncThemeState(): void {
    this.isDarkMode.set(this.document.documentElement.classList.contains('app-dark'));
  }

  private async resolveDisplayName(): Promise<void> {
    const account = this.accountService.user();

    if (account?.name?.trim()) {
      this.displayName.set(account.name.trim());
      return;
    }

    const authToken = await cookieStore.get('authToken');
    const jwt = this.parseJwtPayload(authToken?.value);
    const name =
      jwt?.['name'] ?? jwt?.['unique_name'] ?? jwt?.['preferred_username'] ?? jwt?.['sub'];

    if (typeof name === 'string' && name.trim()) {
      this.displayName.set(name.trim());
    }
  }

  private parseJwtPayload(token?: string): Record<string, unknown> | null {
    if (!token) {
      return null;
    }

    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    try {
      const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const decoded = atob(payload.padEnd(Math.ceil(payload.length / 4) * 4, '='));
      return JSON.parse(decoded) as Record<string, unknown>;
    } catch {
      return null;
    }
  }
}
