using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace Project_.Views
{
    public partial class AdminReportsView : UserControl
    {
        public ObservableCollection<LowStockItem> LowStockItems { get; set; } = new();

        public AdminReportsView()
        {
            InitializeComponent();
            LoadLowStock();
        }

        private void LoadLowStock()
        {
            LowStockItems.Clear();
            var data = App.Database.ExecuteQuery("SELECT * FROM Inventory WHERE Quantity <= ReorderLevel ORDER BY Quantity ASC");
            int i = 0;
            foreach (DataRow row in data.Rows)
            {
                var qty = Convert.ToInt32(row["Quantity"]);
                var min = Convert.ToInt32(row["ReorderLevel"]);
                var name = row["ItemName"]?.ToString() ?? "";
                var cat = row["Category"]?.ToString() ?? "";
                
                LowStockItems.Add(new LowStockItem
                {
                    Name = name,
                    Category = cat,
                    Quantity = $"{qty} units",
                    MinStock = $"{min} units",
                    BgColor = i++ % 2 == 0 ? "White" : "#F9FAFB",
                    StatusText = qty == 0 ? "Out of Stock" : "Low Stock",
                    StatusBg = qty == 0 ? "#FEF2F2" : "#FFFBEB",
                    StatusFg = qty == 0 ? "#991B1B" : "#92400E"
                });
            }
            LowStockList.ItemsSource = LowStockItems;
            LowStockWarningTxt.Text = $"{LowStockItems.Count} items need restocking";
        }

        private void DownloadCsv(string query, string defaultName)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"{defaultName}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = App.Database.ExecuteQuery(query);
                    using var sw = new StreamWriter(dialog.FileName);
                    
                    var headers = data.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"");
                    sw.WriteLine(string.Join(",", headers));
                    
                    foreach (DataRow row in data.Rows)
                    {
                        var fields = row.ItemArray.Select(field => $"\"{field?.ToString()?.Replace("\"", "\"\"")}\"");
                        sw.WriteLine(string.Join(",", fields));
                    }
                    MessageBox.Show("Report exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnStaffReport_Click(object sender, MouseButtonEventArgs e)
        {
            DownloadCsv("SELECT FirstName, LastName, Username, Role, Email, PhoneNumber FROM Users WHERE IsActive = 1", "StaffReport");
        }

        private void BtnPatientReport_Click(object sender, MouseButtonEventArgs e)
        {
            DownloadCsv("SELECT FirstName, LastName, Gender, PhoneNumber, RegistrationNumber AS StudentID, CreatedAt AS Registered FROM Patients WHERE IsArchived = 0", "PatientReport");
        }

        private void BtnInventoryReport_Click(object sender, MouseButtonEventArgs e)
        {
            DownloadCsv("SELECT ItemName, Category, Quantity, Unit, UnitPrice, ReorderLevel AS MinStock, ExpiryDate FROM Inventory", "InventoryReport");
        }
    }

    public class LowStockItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string MinStock { get; set; } = string.Empty;
        public string BgColor { get; set; } = "White";
        public string StatusText { get; set; } = string.Empty;
        public string StatusBg { get; set; } = string.Empty;
        public string StatusFg { get; set; } = string.Empty;
    }
}
