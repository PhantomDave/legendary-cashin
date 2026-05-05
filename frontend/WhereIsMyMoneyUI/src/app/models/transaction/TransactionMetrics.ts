export interface TransactionMetrics {
  yearToDate: number;
  yearToDateTrend: number | null;
  monthToDate: number;
  monthToDateTrend: number | null;
  predictedEndOfMonth: number;
  predictedEndOfMonthTrend: number | null;
  balance30DaysAgo: number;
  balance30DaysAgoTrend: number | null;
}
