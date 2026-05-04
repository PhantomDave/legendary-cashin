import { AppNavigationItem } from '../models/app-navigation.model';

export const APP_NAVIGATION_ITEMS: AppNavigationItem[] = [
  {
    label: 'Dashboard',
    icon: 'pi pi-home',
    route: '/dashboard',
  },
  {
    label: 'Transactions',
    icon: 'pi pi-wallet',
    route: '/transactions',
  },
];
