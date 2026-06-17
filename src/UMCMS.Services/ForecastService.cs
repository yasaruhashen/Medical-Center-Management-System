using UMCMS.Data.Repositories;
using UMCMS.Domain.Forecasting;

namespace UMCMS.Services
{
    public record ItemForecast(
        int ItemId, string ItemName, string Stream, int OnHand, int ReorderLevel,
        double ProjectedDemand, int SuggestedReorder, bool LowConfidence);

    /// <summary>Produces reorder forecasts per inventory item using exponential smoothing.</summary>
    public class ForecastService
    {
        private readonly IInventoryRepository _inventory;
        private readonly SettingsService? _settings;
        private readonly double _alpha;
        private readonly double _z;

        public ForecastService(IInventoryRepository inventory, SettingsService? settings = null,
                               double alpha = 0.5, double serviceFactorZ = 1.5)
        {
            _inventory = inventory;
            _settings = settings;
            _alpha = alpha;
            _z = serviceFactorZ;
        }

        public List<ItemForecast> ForStream(string? stream = null)
        {
            // Read tunables live so admin changes in Settings take effect immediately.
            double alpha = _settings?.GetDouble("forecast.alpha", _alpha) ?? _alpha;
            double z = _settings?.GetDouble("forecast.z", _z) ?? _z;
            var calculator = new ReorderCalculator(alpha, z);

            var results = new List<ItemForecast>();
            foreach (var item in _inventory.GetByStream(stream))
            {
                var series = _inventory.GetMonthlyConsumption(item.Id);
                var r = calculator.Calculate(series, item.Quantity);
                results.Add(new ItemForecast(
                    item.Id, item.ItemName, item.Stream, item.Quantity, item.ReorderLevel,
                    Math.Round(r.ProjectedDemand, 1), r.ReorderQuantity, r.LowConfidence));
            }
            return results;
        }
    }
}
