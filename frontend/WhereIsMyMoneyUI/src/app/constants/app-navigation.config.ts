import { AppNavigationItem } from '../models/app-navigation.model';

export const APP_NAVIGATION_ITEMS: AppNavigationItem[] = [
  {
    label: 'Overview',
    icon: 'pi pi-home',
    expanded: true,
    items: [
      {
        label: 'Dashboard',
        icon: 'pi pi-chart-line',
        routerLink: '/dashboard',
      },
    ],
  },
  {
    label: 'Money',
    icon: 'pi pi-wallet',
    expanded: true,
    items: [
      {
        label: 'Transactions',
        icon: 'pi pi-list',
        routerLink: '/transactions',
      },
    ],
  },
  {
    label: 'Configuration',
    icon: 'pi pi-cog',
    expanded: true,
    items: [
      {
        label: 'Categories',
        icon: 'pi pi-sliders-h',
        routerLink: '/categories',
      },
    ],
  },
];
