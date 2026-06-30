import { Routes } from '@angular/router';
import { ShellComponent } from './components/shell/shell.component';
import { authGuard, guestOnlyGuard } from './guards/auth.guard';
import { CategoriesPageComponent } from './pages/categories/categories-page-component';
import { DashboardPageComponent } from './pages/dashboard/dashboard-page.component';
import { TransactionsPageComponent } from './pages/transactions/transactions-page.component';
import { LoginPageComponent } from './pages/account/login-page-component/login-page-component';
import { RegisterPageComponent } from './pages/account/register-page-component/register-page-component';
import { NotFoundPageComponent } from './pages/not-found/not-found-page.component';
import { ScheduledPageComponent } from './pages/scheduled/scheduled-page-component';
import { ImportPageComponent } from './pages/import/import-page-component';
import { ImportCallbackPageComponent } from './pages/import/callback/import-callback-page.component';
import { RulesPageComponent } from './pages/rules/rules-page.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        component: DashboardPageComponent,
      },
      {
        path: 'transactions',
        children: [
          {
            path: 'list',
            component: TransactionsPageComponent,
          },
          {
            path: 'schedules',
            component: ScheduledPageComponent,
          },
        ],
      },
      {
        path: 'configuration',
        children: [
          {
            path: 'categories',
            component: CategoriesPageComponent,
          },
          {
            path: 'import',
            component: ImportPageComponent,
          },
          {
            path: 'rules',
            component: RulesPageComponent,
          },
        ],
      },
    ],
  },
  {
    path: 'import',
    canActivate: [authGuard],
    children: [
      {
        path: 'callback',
        component: ImportCallbackPageComponent,
      },
    ],
  },
  {
    path: 'account',
    children: [
      {
        path: 'register',
        canActivate: [guestOnlyGuard],
        component: RegisterPageComponent,
      },
      {
        path: 'login',
        canActivate: [guestOnlyGuard],
        component: LoginPageComponent,
      },
    ],
  },
  {
    path: '**',
    component: NotFoundPageComponent,
  },
];
