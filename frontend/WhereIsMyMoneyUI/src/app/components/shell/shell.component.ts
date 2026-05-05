import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { APP_NAVIGATION_ITEMS } from '../../constants/app-navigation.config';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { Budget } from '../../models/budget/Budget';
import { BudgetService } from '../../services/budget.service';
import { CreateBudgetComponent } from '../create-budget-component/create-budget-component';

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    ButtonModule,
    SelectModule,
    FormsModule,
    CreateBudgetComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  readonly isDarkMode = signal(false);
  readonly navItems = APP_NAVIGATION_ITEMS;
  readonly themeIcon = computed(() => (this.isDarkMode() ? 'pi pi-moon' : 'pi pi-sun'));
  isCreateModalVisible: boolean = false;
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly budgetService = inject(BudgetService);
  readonly selectedBudget = this.budgetService.selectedBudget;
  readonly budgets = this.budgetService.budgets;

  async ngOnInit(): Promise<void> {
    const budgets = await this.budgetService.getBudgets();
    if (!this.selectedBudget() && budgets && budgets.length > 0) {
      this.budgetService.selectedBudget.set(budgets[0]);
    }
    if (budgets && budgets.length == 0) {
      this.isCreateModalVisible = true;
    }
  }

  readonly isRouteActive = (route: string): boolean =>
    this.router.isActive(route, {
      paths: 'exact',
      fragment: 'ignored',
      queryParams: 'ignored',
      matrixParams: 'ignored',
    });

  toggleTheme(): void {
    const nextValue = !this.isDarkMode();
    this.isDarkMode.set(nextValue);
    this.document.documentElement.classList.toggle('app-dark', nextValue);
  }

  onChange(selectedBudget: Budget): void {
    this.budgetService.selectedBudget.set(selectedBudget);
  }
}
