using System;
using System.Collections.Generic;
<<<<<<< Updated upstream
=======
using System.Collections.ObjectModel;
>>>>>>> Stashed changes
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    public partial class AppointmentsView : UserControl
    {
        private readonly long? _doctorId;
<<<<<<< Updated upstream
        
=======
        public ObservableCollection<AppointmentDisplay> Appointments { get; set; } = new();

>>>>>>> Stashed changes
        public AppointmentsView(long? doctorId = null)
        {
            _doctorId = doctorId;
            InitializeComponent();
            Loaded += AppointmentsView_Loaded;
<<<<<<< Updated upstream
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
=======
            AppointmentsList.ItemsSource = Appointments;
        }

        private void AppointmentsView_Loaded(object sender, RoutedEventArgs e)
        {
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
                }
            }
            catch {}
        }

        private void LoadAppointments(string query = "", string filterStatus = "All")
        {
            try
            {
                Appointments.Clear();
                var sql = "SELECT a.Id, p.FirstName || ' ' || p.LastName AS Patient, d.FirstName || ' ' || d.LastName AS Doctor, DATE(a.AppointmentDate) AS Date, TIME(a.AppointmentDate) AS Time, a.DurationMinutes AS Duration, a.Status, a.Notes " +
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

                if (filterStatus != "All")
                {
                    sql += " AND a.Status = @Status";
                    p.Add("Status", filterStatus);
                }

                if (!string.IsNullOrWhiteSpace(query) && query != "Search by patient, doctor, or date...")
                {
                    sql += " AND (p.FirstName LIKE @Q OR p.LastName LIKE @Q OR d.FirstName LIKE @Q OR d.LastName LIKE @Q OR DATE(a.AppointmentDate) LIKE @Q)";
                    p.Add("Q", "%" + query + "%");
                }

                sql += " ORDER BY a.AppointmentDate";

                var data = App.Database.ExecuteQuery(sql, p);
                
                foreach (DataRow row in data.Rows)
                {
                    string status = row["Status"]?.ToString() ?? "Scheduled";
                    string statusBg = "#eff6ff";
                    string statusColor = "#1d4ed8";

                    if (status == "Completed") { statusBg = "#f0fdf4"; statusColor = "#16a34a"; }
                    else if (status == "Cancelled") { statusBg = "#fef2f2"; statusColor = "#991b1b"; }
                    else if (status == "CheckedIn") { statusBg = "#fdf0f0"; statusColor = "#4E0205"; }

                    Appointments.Add(new AppointmentDisplay
                    {
                        Id = Convert.ToInt64(row["Id"]),
                        PatientName = row["Patient"]?.ToString() ?? "",
                        DoctorName = row["Doctor"]?.ToString() ?? "",
                        Date = row["Date"]?.ToString() ?? "",
                        Time = row["Time"]?.ToString() ?? "",
                        Reason = row["Notes"]?.ToString() ?? "—",
                        Status = status,
                        StatusBg = statusBg,
                        StatusColor = statusColor
                    });
                }
            }
            catch { }
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Search by patient, doctor, or date...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Search by patient, doctor, or date...";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) 
        { 
            if (IsLoaded) LoadAppointments(TxtSearch.Text, CmbFilterStatus.Text); 
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) 
        { 
            if (IsLoaded) LoadAppointments(TxtSearch.Text, (CmbFilterStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All"); 
        }

        private void Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.Tag != null)
            {
                string status = (cb.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Scheduled";
                long id = Convert.ToInt64(cb.Tag);
                
                var sql = "UPDATE Appointments SET Status = @Status WHERE Id = @Id";
                App.Database.ExecuteNonQuery(sql, new Dictionary<string, object?> { { "Status", status }, { "Id", id } });
                
                // Note: Updating UI directly might be cleaner, but reloading ensures sync
                // We shouldn't reload immediately to avoid breaking the combobox focus, but in this simple version we will.
                Dispatcher.BeginInvoke(new Action(() => LoadAppointments(TxtSearch.Text, CmbFilterStatus.Text)));
            }
        }
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
                var sql = "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, DurationMinutes, Status, Notes) VALUES (@P, @D, @ApptDateTime, @Dur, 'Scheduled', @Notes)";
=======
                var sql = "INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, DurationMinutes, Status, Notes) VALUES (@P, @D, @ApptDateTime, 15, 'Scheduled', @Notes)";
>>>>>>> Stashed changes
                var p = new Dictionary<string, object?> {
                    {"P", ((DataRowView)FPatient.SelectedItem)["Id"]},
                    {"D", ((DataRowView)FDoctor.SelectedItem)["Id"]},
                    {"ApptDateTime", FDate.SelectedDate.Value.ToString("yyyy-MM-dd") + " " + FTime.SelectedItem.ToString() + ":00"},
<<<<<<< Updated upstream
                    {"Dur", FDuration.SelectedItem != null ? int.Parse(FDuration.SelectedItem.ToString()!) : 15},
=======
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
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
=======

        private void BtnEditAppt_Click(object sender, RoutedEventArgs e)
        {
            // Future feature: detail edit
        }

        private void BtnDeleteAppt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    long id = Convert.ToInt64(btn.Tag);
                    if (MessageBox.Show("Are you sure you want to cancel this appointment?", "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        var sql = "UPDATE Appointments SET Status = 'Cancelled' WHERE Id = @Id";
                        App.Database.ExecuteNonQuery(sql, new Dictionary<string, object?> { { "Id", id } });
                        LoadAppointments();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class AppointmentDisplay
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBg { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
>>>>>>> Stashed changes
    }
}
