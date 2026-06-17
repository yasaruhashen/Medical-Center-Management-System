using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin reorder forecast table + suggested purchase-order export (FR-INV-10).</summary>
    public partial class ForecastView : UserControl
    {
        private readonly int _userId;

        public ForecastView(User user)
        {
            InitializeComponent();
            _userId = user.Id;
            Load();
        }

        private string? CurrentStream()
        {
            string? stream = (FStream.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return stream == "All" ? null : stream;
        }

        private void Load()
        {
            Grid.ItemsSource = App.Forecast.ForStream(CurrentStream()).Select(f => new ForecastRow(
                f.ItemName, f.Stream, f.OnHand, f.ReorderLevel,
                f.ProjectedDemand, f.SuggestedReorder,
                f.LowConfidence ? "Low (little history)" : "Good")).ToList();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Load(); }

        private void BtnPo_Click(object sender, RoutedEventArgs e)
        {
            // Build the purchase order from items the forecast says need reordering.
            var items = App.Forecast.ForStream(CurrentStream()).Where(f => f.SuggestedReorder > 0).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show("No items currently need reordering — nothing to order.",
                    "Purchase Order", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Item");
            dt.Columns.Add("Stream");
            dt.Columns.Add("On hand", typeof(int));
            dt.Columns.Add("Reorder at", typeof(int));
            dt.Columns.Add("Order qty", typeof(int));
            dt.Columns.Add("Confidence");
            foreach (var f in items)
                dt.Rows.Add(f.ItemName, f.Stream, f.OnHand, f.ReorderLevel, f.SuggestedReorder,
                    f.LowConfidence ? "Low" : "Good");

            var dlg = new SaveFileDialog
            {
                FileName = $"purchase_order_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Filter = "PDF|*.pdf|Excel|*.xlsx|CSV|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
            string format = ext switch { ".xlsx" => "Excel", ".csv" => "CSV", _ => "PDF" };

            try
            {
                var path = App.Reports.ExportDataTable("PurchaseOrder", "Suggested Purchase Order", dt, format, dlg.FileName, _userId);
                App.Audit.Log("PurchaseOrder", "Inventory", $"{items.Count} item(s)");
                if (MessageBox.Show($"Purchase order for {items.Count} item(s) saved. Open it now?", "Done",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not export the purchase order:\n\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public record ForecastRow(string ItemName, string Stream, int OnHand, int ReorderLevel,
                              double ProjectedDemand, int SuggestedReorder, string Confidence);
}
