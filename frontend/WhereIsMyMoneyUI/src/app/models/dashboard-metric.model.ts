export interface DashboardMetric {
  label: string;
  value: string;
  trend: string;
  severity: 'success' | 'info' | 'warn';
}

