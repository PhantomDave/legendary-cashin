import {Routes} from '@angular/router';
import {DashboardPageComponent} from './components/pages/dashboard/dashboard-page.component';
import {NotFoundPageComponent} from './components/pages/not-found/not-found-page.component';
import {TransactionsPageComponent} from './components/pages/transactions/transactions-page.component';
import {ShellComponent} from './components/shell/shell.component';
import {RegisterPageComponent} from './components/account/register-page-component/register-page-component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
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
        component: RegisterPageComponent,
      }
    ]
  },
  {
    path: '**',
    component: NotFoundPageComponent,
  },
];
