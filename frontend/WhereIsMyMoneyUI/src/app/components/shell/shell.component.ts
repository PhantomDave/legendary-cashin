import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { APP_NAVIGATION_ITEMS } from '../../constants/app-navigation.config';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { Budget } from '../../models/budget/Budget';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, ButtonModule, SelectModule, FormsModule],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  readonly selectedBudget = signal<Budget | null>(null);

  readonly isDarkMode = signal(false);
  readonly navItems = APP_NAVIGATION_ITEMS;
  readonly themeIcon = computed(() => (this.isDarkMode() ? 'pi pi-moon' : 'pi pi-sun'));

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
}
