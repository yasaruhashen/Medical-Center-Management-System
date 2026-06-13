using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class AppointmentsView : UserControl
    {
        public ObservableCollection<AppointmentDisplay> Appointments { get; set; } = new ObservableCollection<AppointmentDisplay>();

        public AppointmentsView()
        {
            InitializeComponent();
            LoadAppointments();
        }

        private void LoadAppointments()
        {
            Appointments.Clear();
            var sql = @"
                SELECT a.Id, a.AppointmentDate, a.Status, 
                       p.FirstName as PatientFirstName, p.LastName as PatientLastName, p.PhoneNumber as PatientContact,
                       u.FirstName as DoctorFirstName, u.LastName as DoctorLastName, u.Specialization
                FROM Appointments a
                INNER JOIN Patients p ON a.PatientId = p.Id
                INNER JOIN Users u ON a.DoctorId = u.Id";

            try
            {
                var data = App.Database.ExecuteQuery(sql);
                foreach (DataRow row in data.Rows)
                {
                    var status = row["Status"]?.ToString() ?? "Scheduled";
                    Appointments.Add(new AppointmentDisplay
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        PatientName = $"{row["PatientFirstName"]} {row["PatientLastName"]}",
                        PatientContact = row["PatientContact"]?.ToString() ?? "—",
                        DoctorName = $"Dr. {row["DoctorFirstName"]} {row["DoctorLastName"]}",
                        Specialization = row["Specialization"]?.ToString() ?? "General",
                        AppointmentDate = row["AppointmentDate"]?.ToString() ?? "—",
                        Status = status,
                        StatusColor = status == "Completed" ? "#065F46" : status == "Cancelled" ? "#991B1B" : "#92400E",
                        StatusBgColor = status == "Completed" ? "#D1FAE5" : status == "Cancelled" ? "#FEE2E2" : "#FEF3C7"
                    });
                }
                AppointmentsList.ItemsSource = Appointments;
            }
            catch
            {
                // Ignore DB errors on load
            }
        }

        private void BtnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("New Appointment clicked. Provide scheduling details window here.", "WIP");
        }

        private void BtnEditAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                MessageBox.Show($"Edit Appointment ID: {id}", "WIP");
            }
        }

        private void BtnDeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this appointment?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    int id = Convert.ToInt32(btn.Tag);
                    App.Database.ExecuteNonQuery("DELETE FROM Appointments WHERE Id = @Id", new System.Collections.Generic.Dictionary<string, object?> { { "Id", id } });
                    LoadAppointments();
                }
            }
        }
    }

    public class AppointmentDisplay
    {
        public int Id { get; set; }
        public string? PatientName { get; set; }
        public string? PatientContact { get; set; }
        public string? DoctorName { get; set; }
        public string? Specialization { get; set; }
        public string? AppointmentDate { get; set; }
        public string? Status { get; set; }
        public string? StatusColor { get; set; }
        public string? StatusBgColor { get; set; }
    }
}
