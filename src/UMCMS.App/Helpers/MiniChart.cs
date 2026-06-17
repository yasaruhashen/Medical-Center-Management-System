using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UMCMS.App.Helpers
{
    /// <summary>
    /// Lightweight, dependency-free chart cards drawn with native WPF shapes.
    /// Keeps the app free of native charting libraries while still visualising data.
    /// </summary>
    public static class MiniChart
    {
        private static readonly Brush Maroon = new SolidColorBrush(Color.FromRgb(0x4E, 0x02, 0x05));
        private static readonly Brush Subtle = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));
        private static readonly Brush Line = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEC));

        private static readonly Color[] Palette =
        {
            Color.FromRgb(0x8A, 0x00, 0x07), Color.FromRgb(0xF7, 0xAA, 0x37),
            Color.FromRgb(0x4E, 0x02, 0x05), Color.FromRgb(0xC1, 0x44, 0x3B),
            Color.FromRgb(0xE0, 0x86, 0x2A), Color.FromRgb(0x99, 0x4E, 0x52),
        };

        /// <summary>Builds a card containing a vertical bar chart of the given label/value pairs.</summary>
        public static Border BarChart(string title, IReadOnlyList<(string Label, double Value)> data,
                                      double width = 360, string? valueFormat = null)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 12, 12),
                MinWidth = width,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                { Color = Color.FromArgb(0x2A, 0, 0, 0), BlurRadius = 16, ShadowDepth = 2, Direction = 270, Opacity = 0.25 }
            };

            var root = new StackPanel();
            root.Children.Add(new TextBlock
            {
                Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = Maroon, Margin = new Thickness(0, 0, 0, 12)
            });

            if (data.Count == 0 || data.All(d => d.Value <= 0))
            {
                root.Children.Add(new TextBlock
                {
                    Text = "No data yet.", FontSize = 12, Foreground = Subtle,
                    Margin = new Thickness(0, 18, 0, 18), HorizontalAlignment = HorizontalAlignment.Center
                });
                card.Child = root;
                return card;
            }

            const double plotHeight = 140;
            double max = Math.Max(1, data.Max(d => d.Value));

            var columns = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var (label, value) in data)
            {
                // Plot cell of fixed height; a bottom-aligned stack puts the value label
                // directly above each bar while every bar shares the same baseline.
                var plot = new Grid { Height = plotHeight, VerticalAlignment = VerticalAlignment.Bottom };
                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center };
                stack.Children.Add(new TextBlock
                {
                    Text = valueFormat is null ? value.ToString("0.##") : value.ToString(valueFormat),
                    FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Maroon,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 2)
                });
                stack.Children.Add(new Border
                {
                    Width = 34,
                    Height = Math.Max(2, value / max * (plotHeight - 22)),
                    Background = new SolidColorBrush(Palette[columns.Children.Count % Palette.Length]),
                    CornerRadius = new CornerRadius(5, 5, 0, 0)
                });
                plot.Children.Add(stack);

                var column = new StackPanel { Width = 64 };
                column.Children.Add(plot);
                column.Children.Add(new Border   // baseline rule
                {
                    Height = 1, Background = Line, Margin = new Thickness(2, 0, 2, 0)
                });
                column.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 10, Foreground = Subtle, TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(2, 4, 2, 0)
                });
                columns.Children.Add(column);
            }

            root.Children.Add(columns);
            card.Child = root;
            return card;
        }
    }
}
