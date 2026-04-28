namespace WarehouseInvoiceSystem.Application.Helpers
{
    using WarehouseInvoiceSystem.Domain.Enums;

    public static class VatPeriodCalculator
    {
        public static (DateTime From, DateTime To, string Label) GetCurrentPeriod(VatPayerPeriod period, DateTime asOf)
            => period switch
            {
                VatPayerPeriod.Monthly => MonthPeriod(asOf.Year, asOf.Month),
                VatPayerPeriod.Annual  => AnnualPeriod(asOf.Year),
                _                      => QuarterPeriod(asOf.Year, asOf.Month)
            };

        // 25th of the calendar month that follows the period end.
        // e.g. Q1 ends 31-Mar → deadline = 25-Apr; Dec ends 31-Dec → deadline = 25-Jan next year.
        public static DateTime GetFilingDeadline(DateTime periodEnd)
            => new DateTime(periodEnd.Year, periodEnd.Month, 25).AddMonths(1);

        public static IEnumerable<(DateTime From, DateTime To, string Label)> GetPastPeriods(
            VatPayerPeriod period, int count, DateTime asOf)
        {
            (DateTime from, _, _) = GetCurrentPeriod(period, asOf);
            DateTime cursor = from.AddDays(-1); // last day of the previous period

            var results = new List<(DateTime, DateTime, string)>(count);
            for (int i = 0; i < count; i++)
            {
                (DateTime pFrom, DateTime pTo, string label) = PeriodContaining(period, cursor);
                results.Add((pFrom, pTo, label));
                cursor = pFrom.AddDays(-1);
            }
            return results;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static (DateTime From, DateTime To, string Label) MonthPeriod(int year, int month)
        {
            DateTime from = new(year, month, 1);
            DateTime to   = from.AddMonths(1).AddDays(-1);
            return (from, to, from.ToString("MMMM yyyy"));
        }

        private static (DateTime From, DateTime To, string Label) QuarterPeriod(int year, int month)
        {
            int q     = (month - 1) / 3;
            DateTime from = new(year, q * 3 + 1, 1);
            DateTime to   = from.AddMonths(3).AddDays(-1);
            return (from, to, $"Q{q + 1} {year}");
        }

        private static (DateTime From, DateTime To, string Label) AnnualPeriod(int year)
            => (new(year, 1, 1), new(year, 12, 31), year.ToString());

        private static (DateTime From, DateTime To, string Label) PeriodContaining(VatPayerPeriod period, DateTime date)
            => period switch
            {
                VatPayerPeriod.Monthly => MonthPeriod(date.Year, date.Month),
                VatPayerPeriod.Annual  => AnnualPeriod(date.Year),
                _                      => QuarterPeriod(date.Year, date.Month)
            };
    }
}
