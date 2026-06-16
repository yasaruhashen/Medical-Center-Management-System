using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class StaffReportsView : UserControl
    {
        public ObservableCollection<StaffLowStockDisplay> LowStockItems { get; set; } = new();

        public StaffReportsView()
        {
            InitializeComponent();
            Loaded += StaffReportsView_Loaded;
            LowStockList.ItemsSource = LowStockItems;
        }

        private void StaffReportsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLowStockItems();
        }

        private void LoadLowStockItems()
        {
            try
            {
                LowStockItems.Clear();
                var data = App.Database.ExecuteQuery("SELECT ItemName, Category, Quantity, Unit, ReorderLevel FROM Inventory WHERE Quantity <= ReorderLevel AND ReorderLevel > 0 ORDER BY Quantity ASC");

                foreach (DataRow row in data.Rows)
                {
                    int qty = Convert.ToInt32(row["Quantity"] ?? 0);
                    int min = Convert.ToInt32(row["ReorderLevel"] ?? 0);

                    string status = qty == 0 ? "Out of Stock" : "Low Stock";
                    string bg = qty == 0 ? "#fef2f2" : "#fffbeb";
                    string color = qty == 0 ? "#991b1b" : "#92400e";

                    LowStockItems.Add(new StaffLowStockDisplay
                    {
                        Name = row["ItemName"]?.ToString() ?? "",
                        Category = row["Category"]?.ToString() ?? "",
                        Quantity = $"{qty} {row["Unit"]}",
                        MinStock = min,
                        Status = status,
                        StatusBg = bg,
                        StatusColor = color
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading low stock report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportTableToCsv(string query, string fileNamePrefix)
        {
            try
            {
                var dt = App.Database.ExecuteQuery(query);
                string fileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (var sw = new StreamWriter(path))
                {
                    var headers = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                    sw.WriteLine(string.Join(",", headers));

                    foreach (DataRow row in dt.Rows)
                    {
                        var fields = row.ItemArray.Select(field =>
                        {
                            string s = field?.ToString() ?? "";
                            return "\"" + s.Replace("\"", "\"\"") + "\"";
                        });
                        sw.WriteLine(string.Join(",", fields));
                    }
                }

                MessageBox.Show($"Report successfully exported to Desktop:\n{fileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting report: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDownloadPatients_Click(object sender, RoutedEventArgs e)
        {
            ExportTableToCsv("SELECT * FROM Patients WHERE IsArchived = 0", "PatientsReport");
        }

        private void BtnDownloadAppointments_Click(object sender, RoutedEventArgs e)
        {
            ExportTableToCsv("SELECT a.Id, p.FirstName || ' ' || p.LastName AS Patient, d.FirstName || ' ' || d.LastName AS Doctor, a.AppointmentDate, a.Status, a.Notes FROM Appointments a JOIN Patients p ON a.PatientId = p.Id JOIN Users d ON a.DoctorId = d.Id", "AppointmentsReport");
        }

        private void BtnDownloadInventory_Click(object sender, RoutedEventArgs e)
        {
            ExportTableToCsv("SELECT * FROM Inventory", "InventoryReport");
        }

        private void BtnDownloadPrescriptions_Click(object sender, RoutedEventArgs e)
        {
            ExportTableToCsv("SELECT p.Id, mr.PatientId, p.Medicine, p.Dosage, p.Quantity, p.Status, p.CreatedAt FROM Prescriptions p JOIN MedicalRecords mr ON p.MedicalRecordId = mr.Id", "PrescriptionsReport");
        }
    }

    public class StaffLowStockDisplay
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public int MinStock { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBg { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}
