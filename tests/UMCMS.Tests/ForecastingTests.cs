using UMCMS.Domain.Forecasting;
using Xunit;

namespace UMCMS.Tests
{
    public class ForecastingTests
    {
        [Fact]
        public void Forecast_FollowsExponentialSmoothing()
        {
            var f = new ExponentialSmoothingForecaster(alpha: 0.5);
            // Seed F0 = 10; then F = 0.5*Y + 0.5*F each step.
            // Y = [10,20,30]: F0=10 -> 0.5*10+0.5*10=10 -> 0.5*20+0.5*10=15 -> 0.5*30+0.5*15=22.5
            var result = f.Forecast(new[] { 10, 20, 30 });
            Assert.Equal(22.5, result, 3);
        }

        [Fact]
        public void Forecast_EmptySeries_IsZero()
        {
            Assert.Equal(0, new ExponentialSmoothingForecaster().Forecast(Array.Empty<int>()));
        }

        [Theory]
        [InlineData(new int[] { }, true)]
        [InlineData(new[] { 5 }, true)]
        [InlineData(new[] { 5, 6 }, false)]
        public void LowConfidence_WhenLessThanTwoMonths(int[] series, bool expected)
            => Assert.Equal(expected, ExponentialSmoothingForecaster.IsLowConfidence(series));

        [Fact]
        public void ReorderQuantity_IsZero_WhenStockCoversForecast()
        {
            var calc = new ReorderCalculator(alpha: 0.5, serviceFactorZ: 1.5);
            var r = calc.Calculate(new[] { 10, 10, 10, 10 }, onHand: 1000);
            Assert.Equal(0, r.ReorderQuantity);
            Assert.False(r.LowConfidence);
        }

        [Fact]
        public void ReorderQuantity_IsPositive_WhenStockLow()
        {
            var calc = new ReorderCalculator(alpha: 0.5, serviceFactorZ: 1.5);
            var r = calc.Calculate(new[] { 40, 45, 50, 55, 60 }, onHand: 5);
            Assert.True(r.ReorderQuantity > 0);
        }

        [Fact]
        public void LowConfidence_FallsBackToLastMonth()
        {
            var calc = new ReorderCalculator();
            var r = calc.Calculate(new[] { 17 }, onHand: 0);   // single month
            Assert.True(r.LowConfidence);
            Assert.Equal(17, r.ProjectedDemand, 3);            // naive = last month
        }
    }
}
