using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin reporting: generate and export inventory/appointment reports.</summary>
    public partial class ReportsView : UserControl
    {
        private readonly int _userId;

        public ReportsView(User user)
        {
            InitializeComponent();
            _userId = user.Id;
            FFrom.SelectedDate = DateTime.Today.AddDays(-30);
            FTo.SelectedDate = DateTime.Today;
            LoadRecent();
        }

        private void FType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string t = TypeText();
            bool appts = t.StartsWith("Appointments");
            bool inventory = t is "Inventory Stock" or "Low Stock";
            PanelStream.Visibility = inventory ? Visibility.Visible : Visibility.Collapsed;
            PanelFrom.Visibility = appts ? Visibility.Visible : Visibility.Collapsed;
            PanelTo.Visibility = appts ? Visibility.Visible : Visibility.Collapsed;
        }

        private string TypeText() => (FType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Inventory Stock";

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            string format = (string)((Button)sender).Tag;
            string ext = format switch { "PDF" => "pdf", "Excel" => "xlsx", _ => "csv" };

            var dlg = new SaveFileDialog
            {
                FileName = $"{TypeText().Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmm}.{ext}",
                Filter = $"{format} file|*.{ext}"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                string type = TypeText();
                string file = dlg.FileName;
                string? stream = (FStream.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (stream == "All") stream = null;

                string path = type switch
                {
                    "Appointments (date range)" => App.Reports.GenerateAppointmentsReport(
                        FFrom.SelectedDate ?? DateTime.Today.AddDays(-30),
                        FTo.SelectedDate ?? DateTime.Today, null, format, file, _userId),
                    "Expenditure per Faculty" => App.Reports.GenerateExpenseByFaculty(format, file, _userId),
                    "Top Consumed Drugs" => App.Reports.GenerateTopConsumed(format, file, _userId),
                    "Quarterly Expenditure" => App.Reports.GenerateQuarterlyExpense(format, file, _userId),
                    "Low Stock" => App.Reports.GenerateInventoryStockReport(stream, true, format, file, _userId),
                    _ => App.Reports.GenerateInventoryStockReport(stream, false, format, file, _userId)
                };

                TxtStatus.Text = $"Saved: {Path.GetFileName(path)}";
                LoadRecent();
                if (MessageBox.Show("Report saved. Open it now?", "Done", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not generate report:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecent() => RecentGrid.ItemsSource = App.Database.ExecuteQuery(
            @"SELECT ReportType AS Type, Format, GeneratedDate AS [Generated], FilePath AS [Saved to]
              FROM Reports ORDER BY Id DESC LIMIT 20;").DefaultView;
    }
}
