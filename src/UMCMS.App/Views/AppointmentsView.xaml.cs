using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Appointments. Receptionists see all appointments and can book (with collision
    /// check). Doctors see only their own queue and can update statuses.
    /// </summary>
    public partial class AppointmentsView : UserControl
    {
        private readonly User _user;
        private readonly bool _isDoctor;
        private readonly int? _doctorFilter;

        /// <summary>Set by the shell: doctor check-in opens the consultation for (patientId, appointmentId).</summary>
        public Action<int, int>? OpenConsultation { get; set; }

        public AppointmentsView(User user)
        {
            InitializeComponent();
            _user = user;
            _isDoctor = user.Role == "Doctor";
            _doctorFilter = _isDoctor ? user.Id : null;

            BtnBook.Visibility = _isDoctor ? Visibility.Collapsed : Visibility.Visible;
            BtnSubscribe.Visibility = _isDoctor ? Visibility.Collapsed : Visibility.Visible;
            FDay.SelectedDate = DateTime.Today;

            for (int m = 0; m < 24 * 60; m += 30)
                FTime.Items.Add(new TimeSpan(0, m, 0).ToString(@"hh\:mm"));
            foreach (var d in new[] { 15, 30, 45, 60 }) FDuration.Items.Add(d);
            FDuration.SelectedIndex = 1;

            Load();

            _timer.Tick += (_, _) => AutoRefresh();
            Loaded += (_, _) => _timer.Start();
            Unloaded += (_, _) => _timer.Stop();
        }

        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
        private long _lastSignature = -1;

        /// <summary>Refreshes the list when appointments change (added/rescheduled/status), keeping selection.</summary>
        private void AutoRefresh()
        {
            // Don't disrupt an open dialog.
            if (Overlay.Visibility == Visibility.Visible ||
                OverlayResched.Visibility == Visibility.Visible ||
                OverlaySub.Visibility == Visibility.Visible) return;

            long sig = App.Database.ExecuteScalar<long>(
                @"SELECT COALESCE(SUM(Id*7 + (CASE Status WHEN 'Scheduled' THEN 1 WHEN 'CheckedIn' THEN 2
                                                          WHEN 'Completed' THEN 3 WHEN 'Cancelled' THEN 4
                                                          WHEN 'NoShow' THEN 5 ELSE 9 END)),0) FROM Appointments;");
            if (sig == _lastSignature) return;
            _lastSignature = sig;

            int? keep = SelectedId;
            Load();
            if (_dayView) BuildDayView();
            if (keep is int id && Grid.ItemsSource is DataView dv)
                foreach (DataRowView r in dv)
                    if (Convert.ToInt32(r["Id"]) == id) { Grid.SelectedItem = r; break; }
        }

        private void Load()
        {
            DateTime? day = ChkAllDays.IsChecked == true ? null : FDay.SelectedDate;
            Grid.ItemsSource = App.Appointments.GetView(day, _doctorFilter).DefaultView;
            UpdateButtons();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            Load();
            if (DayHost.Visibility == Visibility.Visible) BuildDayView();
        }

        private bool _dayView;

        private void BtnToggleView_Click(object sender, RoutedEventArgs e)
        {
            _dayView = !_dayView;
            BtnToggleView.Content = _dayView ? "☰ List View" : "📅 Day View";
            ListHost.Visibility = _dayView ? Visibility.Collapsed : Visibility.Visible;
            DayHost.Visibility = _dayView ? Visibility.Visible : Visibility.Collapsed;
            ActionBar.Visibility = _dayView ? Visibility.Collapsed : Visibility.Visible;
            if (_dayView) BuildDayView();
        }

        /// <summary>Builds a per-doctor day view (one column per doctor, time-ordered chips).</summary>
        private void BuildDayView()
        {
            DayPanel.Children.Clear();
            DateTime day = FDay.SelectedDate ?? DateTime.Today;   // day view always uses a specific day
            var rows = App.Appointments.GetView(day, _doctorFilter);

            var byDoctor = new Dictionary<string, List<DataRow>>();
            foreach (DataRow r in rows.Rows)
            {
                string doc = r["Doctor"].ToString() ?? "";
                if (!byDoctor.TryGetValue(doc, out var list)) { list = new(); byDoctor[doc] = list; }
                list.Add(r);
            }

            if (byDoctor.Count == 0)
            {
                DayPanel.Children.Add(new TextBlock
                {
                    Text = $"No appointments on {day:ddd dd MMM yyyy}.",
                    Foreground = (Brush)FindResource("Subtle"), Margin = new Thickness(16)
                });
                return;
            }

            foreach (var grp in byDoctor)
            {
                var col = new StackPanel { Width = 230, Margin = new Thickness(8) };
                col.Children.Add(new TextBlock
                {
                    Text = grp.Key, FontWeight = FontWeights.SemiBold, FontSize = 13,
                    Foreground = (Brush)FindResource("Maroon"), Margin = new Thickness(2, 0, 0, 8)
                });

                foreach (var r in grp.Value.OrderBy(x => x["When"].ToString()))
                {
                    string time = DateTime.TryParse(r["When"].ToString(), out var dt) ? dt.ToString("HH:mm") : "";
                    string status = r["Status"].ToString() ?? "";
                    Color accent = status switch
                    {
                        "Completed" => Color.FromRgb(0x1B, 0x7A, 0x3D),
                        "Cancelled" or "NoShow" => Color.FromRgb(0x99, 0x99, 0x99),
                        "CheckedIn" => Color.FromRgb(0xF7, 0xAA, 0x37),
                        _ => Color.FromRgb(0x8A, 0x00, 0x07),
                    };
                    var chip = new Border
                    {
                        Background = Brushes.White, BorderBrush = new SolidColorBrush(accent), BorderThickness = new Thickness(0, 0, 0, 0),
                        CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(0)
                    };
                    var inner = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(0x14, accent.R, accent.G, accent.B)),
                        BorderBrush = new SolidColorBrush(accent), BorderThickness = new Thickness(3, 0, 0, 0),
                        CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6, 10, 6)
                    };
                    inner.Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = $"{time}  ·  {r["Patient"]}", FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = (Brush)FindResource("Ink") },
                            new TextBlock { Text = status, FontSize = 11, Foreground = new SolidColorBrush(accent) }
                        }
                    };
                    chip.Child = inner;
                    col.Children.Add(chip);
                }
                DayPanel.Children.Add(col);
            }
        }

        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName is "Id" or "PatientId") e.Cancel = true;
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

        private int? SelectedId =>
            Grid.SelectedItem is DataRowView row ? Convert.ToInt32(row["Id"]) : null;

        private int? SelectedPatientId =>
            Grid.SelectedItem is DataRowView row ? Convert.ToInt32(row["PatientId"]) : null;

        private void UpdateButtons()
        {
            bool sel = SelectedId is not null;
            BtnCheckIn.IsEnabled = BtnComplete.IsEnabled = BtnNoShow.IsEnabled = BtnCancelAppt.IsEnabled = sel;
            BtnReschedule.IsEnabled = sel && !_isDoctor;   // reception manages the schedule
        }

        // ── Reschedule ──
        private void BtnReschedule_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is not int id) return;
            var appt = App.Appointments.GetById(id);
            if (appt is null) return;

            FReTime.Items.Clear();
            for (int m = 0; m < 24 * 60; m += 30) FReTime.Items.Add(new TimeSpan(0, m, 0).ToString(@"hh\:mm"));
            FReDate.SelectedDate = appt.AppointmentDate.Date;
            FReTime.SelectedItem = appt.AppointmentDate.ToString(@"hh\:mm");
            TxtReschedFor.Text = $"Currently {appt.AppointmentDate:ddd dd MMM HH:mm} · {appt.Stream}";
            TxtReschedError.Visibility = Visibility.Collapsed;
            OverlayResched.Visibility = Visibility.Visible;
        }

        private void BtnSaveResched_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is not int id) return;
            var appt = App.Appointments.GetById(id);
            if (appt is null) return;
            if (FReDate.SelectedDate is null || FReTime.SelectedItem is null)
            { ReschedError("Please choose a new date and time."); return; }

            var start = FReDate.SelectedDate.Value.Date + TimeSpan.Parse((string)FReTime.SelectedItem);
            if (App.Appointments.HasCollision(appt.DoctorId, start, appt.DurationMinutes, excludeId: id))
            { ReschedError($"That time clashes with another appointment for this doctor."); return; }

            App.Appointments.Reschedule(id, start);
            App.Audit.Log("Reschedule", "Appointment", $"#{id} -> {start:yyyy-MM-dd HH:mm}");
            OverlayResched.Visibility = Visibility.Collapsed;
            if (ChkAllDays.IsChecked != true) FDay.SelectedDate = start.Date;
            Load();
        }

        private void BtnCancelResched_Click(object sender, RoutedEventArgs e) => OverlayResched.Visibility = Visibility.Collapsed;
        private void ReschedError(string m) { TxtReschedError.Text = m; TxtReschedError.Visibility = Visibility.Visible; }

        private void Status_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is not int id) return;
            string status = (string)((Button)sender).Tag;

            if (status == "Cancelled")
            {
                // Smart-queue: cancelling frees the slot and offers it to the waitlist.
                string message = App.AppointmentSvc.CancelAndBackfill(id);
                Load();
                MessageBox.Show(message, "Appointment Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            App.Appointments.SetStatus(id, status);
            App.Audit.Log("StatusChange", "Appointment", $"#{id} -> {status}");

            // Doctor checking a patient in jumps straight to the consultation, ready to treat.
            if (status == "CheckedIn" && _isDoctor && OpenConsultation is not null && SelectedPatientId is int pid)
            {
                OpenConsultation(pid, id);
                return;
            }
            Load();
        }

        // ── Availability subscription ──
        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            SPatient.ItemsSource = App.Patients.GetAll();
            SDoctor.ItemsSource = App.Users.GetDoctors();
            TxtSubError.Visibility = Visibility.Collapsed;
            OverlaySub.Visibility = Visibility.Visible;
        }

        private void BtnSaveSub_Click(object sender, RoutedEventArgs e)
        {
            if (SPatient.SelectedItem is not Patient p || SDoctor.SelectedItem is not User doc)
            {
                TxtSubError.Text = "Choose both a patient and a doctor."; TxtSubError.Visibility = Visibility.Visible; return;
            }
            App.Database.ExecuteNonQuery(
                @"INSERT INTO AvailabilitySubscriptions (DoctorId, PatientId, Notified, CreatedAt)
                  VALUES (@d, @p, 0, @c);",
                new Dictionary<string, object?>
                {
                    ["d"] = doc.Id, ["p"] = p.Id, ["c"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            App.Audit.Log("Subscribe", "Availability", $"{p.FullName} -> {doc.FullName}");
            OverlaySub.Visibility = Visibility.Collapsed;
            MessageBox.Show($"{p.FullName} will be notified when {doc.FullName} is next on-site.",
                "Subscribed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancelSub_Click(object sender, RoutedEventArgs e) => OverlaySub.Visibility = Visibility.Collapsed;

        // ── Booking ──
        private void BtnBook_Click(object sender, RoutedEventArgs e)
        {
            FPatient.ItemsSource = App.Patients.GetAll();
            FDoctor.ItemsSource = App.Users.GetDoctors();
            FDate.SelectedDate = FDay.SelectedDate ?? DateTime.Today;
            FTime.SelectedItem = "09:00";
            FNotes.Text = "";
            TxtError.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Visible;
        }

        private void FDoctor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FDoctor.SelectedItem is User doc)
                TxtStreamHint.Text = $"Stream: {(doc.IsDentist ? "Dental" : "General")}  ·  {(doc.IsDentist ? "Dentist" : "General Physician")}";
        }

        private void BtnSaveBook_Click(object sender, RoutedEventArgs e)
        {
            if (FPatient.SelectedItem is not Patient patient) { Error("Please choose a patient."); return; }
            if (FDoctor.SelectedItem is not User doctor) { Error("Please choose a doctor."); return; }
            if (FDate.SelectedDate is null || FTime.SelectedItem is null) { Error("Please choose date and time."); return; }

            var time = TimeSpan.Parse((string)FTime.SelectedItem);
            var start = FDate.SelectedDate.Value.Date + time;
            int duration = (int)(FDuration.SelectedItem ?? 30);

            if (App.Appointments.HasCollision(doctor.Id, start, duration))
            {
                Error($"{doctor.FullName} already has an appointment overlapping {start:HH:mm}. Choose another time.");
                return;
            }

            App.Appointments.Add(new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                AppointmentDate = start,
                DurationMinutes = duration,
                Stream = doctor.IsDentist ? "Dental" : "General",
                Status = "Scheduled",
                Notes = FNotes.Text.Trim()
            });
            App.Audit.Log("Book", "Appointment", $"{patient.FullName} with {doctor.FullName} @ {start:yyyy-MM-dd HH:mm}");

            Overlay.Visibility = Visibility.Collapsed;
            if (ChkAllDays.IsChecked != true) FDay.SelectedDate = start.Date;
            Load();
        }

        private void BtnCancelBook_Click(object sender, RoutedEventArgs e) => Overlay.Visibility = Visibility.Collapsed;
        private void Error(string msg) { TxtError.Text = msg; TxtError.Visibility = Visibility.Visible; }
    }
}
