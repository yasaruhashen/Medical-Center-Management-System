namespace UMCMS.Domain.Forecasting
{
    /// <summary>
    /// Simple exponential smoothing: F(t+1) = α·Y(t) + (1−α)·F(t).
    /// Seeded from the first observed period.
    /// </summary>
    public class ExponentialSmoothingForecaster
    {
        public double Alpha { get; }

        public ExponentialSmoothingForecaster(double alpha = 0.5)
        {
            Alpha = alpha <= 0 || alpha > 1 ? 0.5 : alpha;
        }

        /// <summary>Forecasts the next period from a monthly consumption series.</summary>
        public double Forecast(IReadOnlyList<int> series)
        {
            if (series == null || series.Count == 0) return 0;
            double f = series[0];
            for (int t = 0; t < series.Count; t++)
                f = Alpha * series[t] + (1 - Alpha) * f;
            return f;
        }

        /// <summary>True when there isn't enough history for a confident forecast.</summary>
        public static bool IsLowConfidence(IReadOnlyList<int> series) => series == null || series.Count < 2;
    }
}
