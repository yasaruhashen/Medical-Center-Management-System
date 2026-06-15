using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project_.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly long? _doctorId;

        public ObservableCollection<ApptItem> TodayAppts { get; set; } = new();
        public ObservableCollection<ChartBar> WeekChart { get; set; } = new();
        public ObservableCollection<RxItem> RecentRx { get; set; } = new();

        public DashboardView(long? doctorId = null)
        {
            _doctorId = doctorId;
            InitializeComponent();
            Loaded += DashboardView_Loaded;

            TodayScheduleList.ItemsSource = TodayAppts;
            WeekChartList.ItemsSource = WeekChart;
            RecentRxGrid.ItemsSource = RecentRx;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_doctorId.HasValue) return;

            try
            {
                var docName = App.Database.ExecuteScalar<string>("SELECT FirstName || ' ' || LastName FROM Users WHERE Id = @Id", new Dictionary<string, object?> { { "Id", _doctorId.Value } });
                TxtGreeting.Text = $"Welcome, Dr. {docName}";
                TxtSub.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var p = new Dictionary<string, object?> { { "DocId", _doctorId.Value }, { "Today", today } };

                // Top stats
                TxtTotalAppts.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DocId", new Dictionary<string, object?> { { "DocId", _doctorId.Value } }).ToString();
                TxtTodayAppts.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DocId AND DATE(AppointmentDate) = @Today", p).ToString();
                TxtPending.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DocId AND DATE(AppointmentDate) = @Today AND Status = 'Scheduled'", p).ToString();
                TxtMyRx.Text = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM MedicalRecords WHERE DoctorId = @DocId", new Dictionary<string, object?> { { "DocId", _doctorId.Value } }).ToString();

                // Today's schedule
                TodayAppts.Clear();
                var sqlList = "SELECT p.FirstName || ' ' || p.LastName AS PatientName, p.RegistrationNumber, TIME(a.AppointmentDate) AS Time, a.Notes " +
                              "FROM Appointments a JOIN Patients p ON a.PatientId = p.Id " +
                              "WHERE a.DoctorId = @DocId AND DATE(a.AppointmentDate) = @Today AND a.Status = 'Scheduled' ORDER BY TIME(a.AppointmentDate)";
                var data = App.Database.ExecuteQuery(sqlList, p);
                
                if (data.Rows.Count == 0) TxtEmptySchedule.Visibility = Visibility.Visible;
                else
                {
                    TxtEmptySchedule.Visibility = Visibility.Collapsed;
                    foreach (DataRow row in data.Rows)
                    {
                        var name = row["PatientName"]?.ToString() ?? "Unknown";
                        TodayAppts.Add(new ApptItem
                        {
                            PatientName = name,
                            Initial = name.Substring(0, 1).ToUpper(),
                            Time = row["Time"]?.ToString()?.Substring(0, 5),
                            Details = $"{row["RegistrationNumber"]} {(row["Notes"] != DBNull.Value ? "• " + row["Notes"] : "")}"
                        });
                    }
                }

                // Chart
                WeekChart.Clear();
                var maxCount = 1L;
                var weekData = new List<long>();
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                
                for (int i = 0; i < 7; i++)
                {
                    var d = DateTime.Today;
                    int diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7;
                    var targetDate = d.AddDays(i - diff).ToString("yyyy-MM-dd");
                    
                    var cnt = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @DocId AND DATE(AppointmentDate) = @Date", 
                        new Dictionary<string, object?> { { "DocId", _doctorId.Value }, { "Date", targetDate } });
                    
                    weekData.Add(cnt);
                    if (cnt > maxCount) maxCount = cnt;
                }

                for (int i = 0; i < 7; i++)
                {
                    double height = (double)weekData[i] / maxCount * 150.0;
                    WeekChart.Add(new ChartBar { Day = days[i], Count = weekData[i], BarHeight = Math.Max(2, height) });
                }

                // Recent Prescriptions
                RecentRx.Clear();
                var sqlRx = "SELECT p.FirstName || ' ' || p.LastName AS PatientName, m.RecordDate, m.Diagnosis, " +
                            "(SELECT COUNT(*) FROM Prescriptions pr WHERE pr.MedicalRecordId = m.Id) AS ItemsCount, " +
                            "CASE WHEN EXISTS(SELECT 1 FROM Prescriptions pr WHERE pr.MedicalRecordId = m.Id AND pr.Quantity > 0) THEN 'Processed' ELSE 'Pending' END AS Status " +
                            "FROM MedicalRecords m JOIN Patients p ON m.PatientId = p.Id " +
                            "WHERE m.DoctorId = @DocId " +
                            "ORDER BY m.RecordDate DESC LIMIT 5";
                
                var rxData = App.Database.ExecuteQuery(sqlRx, new Dictionary<string, object?> { { "DocId", _doctorId.Value } });
                
                if (rxData.Rows.Count == 0) TxtEmptyRx.Visibility = Visibility.Visible;
                else
                {
                    TxtEmptyRx.Visibility = Visibility.Collapsed;
                    foreach (DataRow row in rxData.Rows)
                    {
                        var status = row["Status"]?.ToString() ?? "Pending";
                        RecentRx.Add(new RxItem
                        {
                            PatientName = row["PatientName"]?.ToString(),
                            Date = Convert.ToDateTime(row["RecordDate"]).ToString("yyyy-MM-dd"),
                            Diagnosis = row["Diagnosis"]?.ToString() ?? "—",
                            ItemsCount = row["ItemsCount"]?.ToString(),
                            Status = status,
                            StatusBg = status == "Pending" ? new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xEB)) : new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4)),
                            StatusFg = status == "Pending" ? new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)) : new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46))
                        });
                    }
                }
            }
            catch { }
        }
    }

    public class ApptItem
    {
        public string? Initial { get; set; }
        public string? PatientName { get; set; }
        public string? Time { get; set; }
        public string? Details { get; set; }
    }

    public class ChartBar
    {
        public string? Day { get; set; }
        public long Count { get; set; }
        public double BarHeight { get; set; }
    }

    public class RxItem
    {
        public string? PatientName { get; set; }
        public string? Date { get; set; }
        public string? Diagnosis { get; set; }
        public string? ItemsCount { get; set; }
        public string? Status { get; set; }
        public SolidColorBrush? StatusBg { get; set; }
        public SolidColorBrush? StatusFg { get; set; }
    }
}
