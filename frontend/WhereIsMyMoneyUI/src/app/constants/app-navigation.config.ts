import { MenuItem } from 'primeng/api';

export const APP_NAVIGATION_ITEMS: MenuItem[] = [
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
        routerLink: '/transactions/list',
      },
      {
        label: 'Schedules',
        icon: 'pi pi-calendar',
        routerLink: '/transactions/schedules',
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
        routerLink: 'configuration/categories',
      },
      {
        label: 'Import',
        icon: 'pi pi-file-import',
        routerLink: 'configuration/import',
      },
      {
        label: 'Rules',
        icon: 'pi pi-sliders-v',
        routerLink: 'configuration/rules',
      },
    ],
  },
];
