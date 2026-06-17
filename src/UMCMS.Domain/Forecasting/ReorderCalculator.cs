namespace UMCMS.Domain.Forecasting
{
    public record ReorderResult(double ProjectedDemand, double SafetyStock, int ReorderQuantity, bool LowConfidence);

    /// <summary>
    /// Reorder quantity = max(0, demand + safety − onHand − onOrder),
    /// with safety stock = z·σ (std-dev of recent consumption).
    /// </summary>
    public class ReorderCalculator
    {
        private readonly ExponentialSmoothingForecaster _forecaster;
        public double ServiceFactorZ { get; }

        public ReorderCalculator(double alpha = 0.5, double serviceFactorZ = 1.5)
        {
            _forecaster = new ExponentialSmoothingForecaster(alpha);
            ServiceFactorZ = serviceFactorZ;
        }

        public ReorderResult Calculate(IReadOnlyList<int> monthlySeries, int onHand, int onOrder = 0)
        {
            bool lowConfidence = ExponentialSmoothingForecaster.IsLowConfidence(monthlySeries);

            double demand = lowConfidence
                ? (monthlySeries != null && monthlySeries.Count > 0 ? monthlySeries[^1] : 0)  // naive fallback
                : _forecaster.Forecast(monthlySeries);

            double safety = ServiceFactorZ * StdDev(monthlySeries);
            int reorder = (int)Math.Ceiling(Math.Max(0, demand + safety - onHand - onOrder));
            return new ReorderResult(demand, safety, reorder, lowConfidence);
        }

        private static double StdDev(IReadOnlyList<int>? series)
        {
            if (series == null || series.Count < 2) return 0;
            double mean = series.Average();
            double variance = series.Sum(v => (v - mean) * (v - mean)) / series.Count;
            return Math.Sqrt(variance);
        }
    }
}
