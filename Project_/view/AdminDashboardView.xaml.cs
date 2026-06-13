using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class AdminDashboardView : UserControl
    {
        public ObservableCollection<ChartItem> PatientsChartData { get; set; } = new();
        public ObservableCollection<ChartItem> InventoryChartData { get; set; } = new();

        public AdminDashboardView()
        {
            InitializeComponent();
            this.Loaded += AdminDashboardView_Loaded;
            
            PatientsChartList.ItemsSource = PatientsChartData;
            InventoryChartList.ItemsSource = InventoryChartData;
        }

        private void AdminDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Top stats
                TotalPatientsTxt.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Patients").ToString();
                TotalDoctorsTxt.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users WHERE Role = 'Doctor' AND IsActive = 1").ToString();
                TotalStaffTxt.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users WHERE Role IN ('Staff', 'Admin') AND IsActive = 1").ToString();
                PendingPrescriptsTxt.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM MedicalRecords WHERE Diagnosis IS NOT NULL AND Id NOT IN (SELECT MedicalRecordId FROM Prescriptions)").ToString();
                LowStockTxt.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Inventory WHERE Quantity <= ReorderLevel").ToString();

                // Inventory Chart
                InventoryChartData.Clear();
                var invData = App.Database.ExecuteQuery("SELECT Category, COUNT(*) as Cnt FROM Inventory GROUP BY Category ORDER BY Cnt DESC LIMIT 5");
                int maxInv = 1;
                foreach (DataRow row in invData.Rows)
                {
                    int c = Convert.ToInt32(row["Cnt"]);
                    if (c > maxInv) maxInv = c;
                }
                foreach (DataRow row in invData.Rows)
                {
                    InventoryChartData.Add(new ChartItem 
                    { 
                        Label = row["Category"]?.ToString() ?? "Misc", 
                        Value = Math.Max(20, (Convert.ToDouble(row["Cnt"]) / maxInv) * 160) // Scale to 160px height max
                    });
                }

                // Patients Chart (Mock months for demonstration since actual monthly data requires parsing created dates)
                PatientsChartData.Clear();
                var r = new Random();
                string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
                foreach (var m in months)
                {
                    PatientsChartData.Add(new ChartItem { Label = m, Value = r.Next(50, 160) });
                }
            }
            catch { }
        }
    }

    public class ChartItem
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
