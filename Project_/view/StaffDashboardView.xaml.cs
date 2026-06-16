using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class StaffDashboardView : UserControl
    {
        public StaffDashboardView()
        {
            InitializeComponent();
            Loaded += StaffDashboardView_Loaded;
        }

        private void StaffDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            TxtDate.Text = "Today: " + DateTime.Now.ToString("dddd, MMMM d, yyyy");
            
            try
            {
                // Fetch stats from DB
                var totalPatients = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Patients WHERE IsArchived = 0");
                TxtTotalPatients.Text = totalPatients.ToString();

                var todayAppointments = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Appointments WHERE DATE(AppointmentDate) = DATE('now')");
                TxtTodayAppointments.Text = todayAppointments.ToString();

                var pendingRx = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Prescriptions WHERE Status = 'Pending'");
                TxtPendingRx.Text = pendingRx.ToString();

                var lowStock = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Inventory WHERE Quantity <= ReorderLevel AND ReorderLevel > 0");
                TxtLowStock.Text = lowStock.ToString();

                LoadTodayAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard data: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTodayAppointments()
        {
            var appts = new ObservableCollection<StaffAppointmentDisplay>();
            var sql = "SELECT p.FirstName || ' ' || p.LastName AS Patient, d.FirstName || ' ' || d.LastName AS Doctor, TIME(a.AppointmentDate) AS Time, a.Status " +
                      "FROM Appointments a " +
                      "JOIN Patients p ON a.PatientId = p.Id " +
                      "JOIN Users d ON a.DoctorId = d.Id " +
                      "WHERE DATE(a.AppointmentDate) = DATE('now') " +
                      "ORDER BY a.AppointmentDate";

            var data = App.Database.ExecuteQuery(sql);
            foreach (System.Data.DataRow row in data.Rows)
            {
                string status = row["Status"]?.ToString() ?? "Scheduled";
                string statusBg = "#eff6ff";
                string statusColor = "#1d4ed8";

                if (status == "Completed") { statusBg = "#f0fdf4"; statusColor = "#16a34a"; }
                else if (status == "Cancelled") { statusBg = "#fef2f2"; statusColor = "#991b1b"; }
                else if (status == "CheckedIn") { statusBg = "#fdf0f0"; statusColor = "#4E0205"; }

                appts.Add(new StaffAppointmentDisplay 
                { 
                    PatientName = row["Patient"]?.ToString() ?? "", 
                    DoctorName = row["Doctor"]?.ToString() ?? "", 
                    Time = row["Time"]?.ToString() ?? "", 
                    Status = status, 
                    StatusBg = statusBg, 
                    StatusColor = statusColor 
                });
            }
            
            AppointmentsList.ItemsSource = appts;
        }
    }

    public class StaffAppointmentDisplay
    {
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBg { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}
