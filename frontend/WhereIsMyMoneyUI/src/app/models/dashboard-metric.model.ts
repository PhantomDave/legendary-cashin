export interface DashboardMetric {
  label: string;
  value: string;
  trend: string;
  isCurrency?: boolean;
  severity: 'success' | 'info' | 'warn';
}
