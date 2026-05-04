import { Routes } from '@angular/router';
import { DashboardPageComponent } from './components/pages/dashboard/dashboard-page.component';
import { NotFoundPageComponent } from './components/pages/not-found/not-found-page.component';
import { TransactionsPageComponent } from './components/pages/transactions/transactions-page.component';
import { ShellComponent } from './components/shell/shell.component';
import { RegisterPageComponent } from './components/account/register-page-component/register-page-component';
import { LoginPageComponent } from './components/account/login-page-component/login-page-component';
import { authGuard, guestOnlyGuard } from './guards/auth.guard';

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
        component: TransactionsPageComponent,
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
