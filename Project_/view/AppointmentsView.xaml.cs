using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class AppointmentsView : UserControl
    {
        private readonly long? _doctorId;
        
        public AppointmentsView(long? doctorId = null)
        {
            _doctorId = doctorId;
            InitializeComponent();
            Loaded += AppointmentsView_Loaded;
        }

        private void AppointmentsView_Loaded(object sender, RoutedEventArgs e)
        {
            FDay.SelectedDate = DateTime.Today;
            LoadAppointments();
            LoadDropdowns();
        }

        private void LoadDropdowns()
        {
            try
            {
                FPatient.ItemsSource = App.Database.ExecuteQuery("SELECT Id, FirstName || ' ' || LastName AS FullName FROM Patients WHERE IsArchived = 0 ORDER BY FirstName, LastName").DefaultView;
                FDoctor.ItemsSource = App.Database.ExecuteQuery("SELECT Id, FirstName || ' ' || LastName AS FullName FROM Users WHERE Role = 'Doctor' AND IsActive = 1 ORDER BY FirstName, LastName").DefaultView;
                
                // Populate times
                for(int i=8; i<=18; i++)
                {
                    FTime.Items.Add($"{i:D2}:00");
                    FTime.Items.Add($"{i:D2}:30");
                    FReTime.Items.Add($"{i:D2}:00");
                    FReTime.Items.Add($"{i:D2}:30");
                }
                FDuration.Items.Add("15"); FDuration.Items.Add("30"); FDuration.Items.Add("60");
            }
            catch {}
        }

        private void LoadAppointments()
        {
            try
            {
                var sql = "SELECT a.Id, p.FirstName || ' ' || p.LastName AS Patient, d.FirstName || ' ' || d.LastName AS Doctor, DATE(a.AppointmentDate) AS Date, TIME(a.AppointmentDate) AS Time, a.DurationMinutes AS Duration, a.Status " +
                          "FROM Appointments a " +
                          "JOIN Patients p ON a.PatientId = p.Id " +
                          "JOIN Users d ON a.DoctorId = d.Id " +
                          "WHERE 1=1";
                
                var p = new Dictionary<string, object?>();

                if (_doctorId.HasValue)
                {
                    sql += " AND a.DoctorId = @DocId";
                    p.Add("DocId", _doctorId.Value);
                }

                if (ChkAllDays.IsChecked != true && FDay.SelectedDate.HasValue)
                {
                    sql += " AND DATE(a.AppointmentDate) = @Date";
                    p.Add("Date", FDay.SelectedDate.Value.ToString("yyyy-MM-dd"));
                }

                sql += " ORDER BY a.AppointmentDate";

                var data = App.Database.ExecuteQuery(sql, p);
                Grid.ItemsSource = data.DefaultView;
            }
            catch { }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) LoadAppointments(); }
        private void Filter_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) LoadAppointments(); }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = Grid.SelectedItem != null;
            BtnCheckIn.IsEnabled = hasSelection;
            BtnComplete.IsEnabled = hasSelection;
            BtnNoShow.IsEnabled = hasSelection;
            BtnReschedule.IsEnabled = hasSelection;
            BtnCancelAppt.IsEnabled = hasSelection;
        }

        private void Status_Click(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem is DataRowView row && sender is Button btn && btn.Tag != null)
            {
                string status = btn.Tag.ToString()!;
                long id = Convert.ToInt64(row["Id"]);
                
                var sql = "UPDATE Appointments SET Status = @Status WHERE Id = @Id";
                App.Database.ExecuteNonQuery(sql, new Dictionary<string, object?> { { "Status", status }, { "Id", id } });
                LoadAppointments();
            }
        }

        private void BtnToggleView_Click(object sender, RoutedEventArgs e) { }
        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) 
        {
            if (e.PropertyName == "Id") e.Column.Visibility = Visibility.Collapsed;
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e) { OverlaySub.Visibility = Visibility.Visible; }
        private void BtnCancelSub_Click(object sender, RoutedEventArgs e) { OverlaySub.Visibility = Visibility.Collapsed; }
        private void BtnSaveSub_Click(object sender, RoutedEventArgs e) { OverlaySub.Visibility = Visibility.Collapsed; }

        private void BtnBook_Click(object sender, RoutedEventArgs e) { Overlay.Visibility = Visibility.Visible; }
        private void BtnCancelBook_Click(object sender, RoutedEventArgs e) { Overlay.Visibility = Visibility.Collapsed; }
        private void BtnSaveBook_Click(object sender, RoutedEventArgs e) 
        { 
            try
            {
                if (FPatient.SelectedValue == null || FDoctor.SelectedValue == null || !FDate.SelectedDate.HasValue || FTime.SelectedItem == null)
                {
                    TxtError.Text = "Please fill all required fields.";
                    TxtError.Visibility = Visibility.Visible;
                    return;
                }
                var sql = "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, DurationMinutes, Status, Notes) VALUES (@P, @D, @ApptDateTime, @Dur, 'Scheduled', @Notes)";
                var p = new Dictionary<string, object?> {
                    {"P", ((DataRowView)FPatient.SelectedItem)["Id"]},
                    {"D", ((DataRowView)FDoctor.SelectedItem)["Id"]},
                    {"ApptDateTime", FDate.SelectedDate.Value.ToString("yyyy-MM-dd") + " " + FTime.SelectedItem.ToString() + ":00"},
                    {"Dur", FDuration.SelectedItem != null ? int.Parse(FDuration.SelectedItem.ToString()!) : 15},
                    {"Notes", FNotes.Text}
                };
                App.Database.ExecuteNonQuery(sql, p);
                Overlay.Visibility = Visibility.Collapsed; 
                LoadAppointments(); 
            }
            catch (Exception ex)
            {
                TxtError.Text = "Error: " + ex.Message;
                TxtError.Visibility = Visibility.Visible;
            }
        }
        private void FDoctor_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnReschedule_Click(object sender, RoutedEventArgs e) { OverlayResched.Visibility = Visibility.Visible; }
        private void BtnCancelResched_Click(object sender, RoutedEventArgs e) { OverlayResched.Visibility = Visibility.Collapsed; }
        private void BtnSaveResched_Click(object sender, RoutedEventArgs e) 
        { 
            if (Grid.SelectedItem is DataRowView row && FReDate.SelectedDate.HasValue && FReTime.SelectedItem != null)
            {
                var sql = "UPDATE Appointments SET AppointmentDate = @ApptDateTime, Status = 'Rescheduled' WHERE Id = @Id";
                App.Database.ExecuteNonQuery(sql, new Dictionary<string, object?> {
                    {"ApptDateTime", FReDate.SelectedDate.Value.ToString("yyyy-MM-dd") + " " + FReTime.SelectedItem.ToString() + ":00"},
                    {"Id", Convert.ToInt64(row["Id"])}
                });
                OverlayResched.Visibility = Visibility.Collapsed; 
                LoadAppointments(); 
            }
        }
    }
}
