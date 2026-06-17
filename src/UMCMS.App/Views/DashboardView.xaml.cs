using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UMCMS.App.Helpers;
using UMCMS.Domain.Entities;
using UMCMS.Services;

namespace UMCMS.App.Views
{
    /// <summary>Role-aware dashboard: each role sees only the metrics relevant to it.</summary>
    public partial class DashboardView : UserControl
    {
        private readonly User _user;
        private static UMCMS.Data.DatabaseService Db => App.Database;

        public DashboardView(User user)
        {
            InitializeComponent();
            _user = user;
            Render();
        }

        private long Count(string sql, Dictionary<string, object?>? p = null) => Db.ExecuteScalar<long>(sql, p);

        /// <summary>Runs a 2-column query (label, value) and returns it as chart data.</summary>
        private List<(string, double)> Pairs(string sql, Dictionary<string, object?>? p = null)
        {
            var dt = Db.ExecuteQuery(sql, p);
            var list = new List<(string, double)>();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                string label = r[0]?.ToString() ?? "";
                double value = r[1] is null or DBNull ? 0 : Convert.ToDouble(r[1]);
                list.Add((label, value));
            }
            return list;
        }

        private void Render()
        {
            TxtGreeting.Text = $"Welcome, {_user.FirstName}";
            TxtSub.Text = $"{NavigationCatalog.RoleLabel(_user)} · {DateTime.Now:dddd, dd MMM yyyy}";

            string today = DateTime.Today.ToString("yyyy-MM-dd");

            switch (_user.Role)
            {
                case "Admin":
                    AddCard("Total Patients", Count("SELECT COUNT(*) FROM Patients WHERE IsArchived=0").ToString(), "🧑‍🤝‍🧑");
                    AddCard("Appointments Today", Count("SELECT COUNT(*) FROM Appointments WHERE date(AppointmentDate)=@d", new() { ["d"] = today }).ToString(), "📅");
                    AddCard("Low-Stock Items", Count("SELECT COUNT(*) FROM InventoryItems WHERE Quantity<=ReorderLevel").ToString(), "⚠️");
                    AddCard("Expiring ≤30d", Count(ExpiringSql(null)).ToString(), "⏰");
                    AddCard("Active Doctors", Count("SELECT COUNT(*) FROM Users WHERE Role='Doctor' AND IsActive=1").ToString(), "🩺");
                    ChartsPanel.Children.Add(MiniChart.BarChart("Appointments by status",
                        Pairs("SELECT Status, COUNT(*) FROM Appointments GROUP BY Status ORDER BY COUNT(*) DESC;")));
                    ChartsPanel.Children.Add(MiniChart.BarChart("Stock value by stream (Rs)",
                        Pairs("SELECT Stream, SUM(Quantity*UnitPrice) FROM InventoryItems GROUP BY Stream;"), valueFormat: "0"));
                    ChartsPanel.Children.Add(MiniChart.BarChart("Patients by type",
                        Pairs("SELECT PatientType, COUNT(*) FROM Patients WHERE IsArchived=0 GROUP BY PatientType;")));
                    RenderDoctorStatus();
                    ShowAlerts(null);
                    break;

                case "Doctor":
                    string stream = _user.IsDentist ? "Dental" : "General";
                    AddCard("My Appointments Today", Count("SELECT COUNT(*) FROM Appointments WHERE DoctorId=@id AND date(AppointmentDate)=@d", new() { ["id"] = _user.Id, ["d"] = today }).ToString(), "📅");
                    AddCard("Completed (7 days)", Count("SELECT COUNT(*) FROM Appointments WHERE DoctorId=@id AND Status='Completed' AND AppointmentDate>=date('now','-7 day')", new() { ["id"] = _user.Id }).ToString(), "✅");
                    AddCard(_user.IsDentist ? "Dental Supplies Low" : "Medication Low", Count("SELECT COUNT(*) FROM InventoryItems WHERE Stream=@s AND Quantity<=ReorderLevel", new() { ["s"] = stream }).ToString(), "📦");
                    AddCard("Expiring ≤30d", Count(ExpiringSql(stream), new() { ["s"] = stream }).ToString(), "⏰");
                    ChartsPanel.Children.Add(MiniChart.BarChart("My appointments by status",
                        Pairs("SELECT Status, COUNT(*) FROM Appointments WHERE DoctorId=@id GROUP BY Status;",
                              new() { ["id"] = _user.Id })));
                    ChartsPanel.Children.Add(MiniChart.BarChart($"{stream} stock on hand (top 5)",
                        Pairs("SELECT ItemName, Quantity FROM InventoryItems WHERE Stream=@s ORDER BY Quantity DESC LIMIT 5;",
                              new() { ["s"] = stream })));
                    ShowMyAppointments();
                    break;

                case "Receptionist":
                    AddCard("Total Patients", Count("SELECT COUNT(*) FROM Patients WHERE IsArchived=0").ToString(), "🧑‍🤝‍🧑");
                    AddCard("Appointments Today", Count("SELECT COUNT(*) FROM Appointments WHERE date(AppointmentDate)=@d", new() { ["d"] = today }).ToString(), "📅");
                    AddCard("On Waitlist", Count("SELECT COUNT(*) FROM WaitlistEntries WHERE Status='Waiting'").ToString(), "⏳");
                    ChartsPanel.Children.Add(MiniChart.BarChart("Appointments next 7 days",
                        Pairs(@"SELECT strftime('%m-%d', AppointmentDate) AS Day, COUNT(*) FROM Appointments
                                WHERE date(AppointmentDate) BETWEEN date('now') AND date('now','+6 day')
                                GROUP BY Day ORDER BY Day;"), width: 480));
                    ChartsPanel.Children.Add(MiniChart.BarChart("Patients by type",
                        Pairs("SELECT PatientType, COUNT(*) FROM Patients WHERE IsArchived=0 GROUP BY PatientType;")));
                    RenderDoctorStatus();
                    ShowTodaysAppointments();
                    break;

                case "Pharmacist":
                    AddCard("Prescriptions Pending", Count("SELECT COUNT(*) FROM Prescriptions WHERE Dispensed=0").ToString(), "💊");
                    AddCard("Patients Waiting", Count("SELECT COUNT(DISTINCT MedicalRecordId) FROM Prescriptions WHERE Dispensed=0").ToString(), "🧑‍🤝‍🧑");
                    AddCard("Low-Stock Items", Count("SELECT COUNT(*) FROM InventoryItems WHERE Quantity<=ReorderLevel").ToString(), "⚠️");
                    AddCard("Expiring ≤30d", Count(ExpiringSql(null)).ToString(), "⏰");
                    AddCard("Total Stock Value (Rs)", Count("SELECT CAST(COALESCE(SUM(Quantity*UnitPrice),0) AS INTEGER) FROM InventoryItems").ToString("N0"), "💰");
                    ChartsPanel.Children.Add(MiniChart.BarChart("Stock value by stream (Rs)",
                        Pairs("SELECT Stream, SUM(Quantity*UnitPrice) FROM InventoryItems GROUP BY Stream;"), valueFormat: "0"));
                    ShowDispenseQueue();
                    break;

                default:
                    AddCard("Welcome", "—", "🏠");
                    break;
            }

            // Lay out responsively: one row by default, reflowed by width on resize.
            CardsPanel.Columns = Math.Max(1, CardsPanel.Children.Count);
            ChartsPanel.Columns = Math.Max(1, ChartsPanel.Children.Count);
            RecomputeColumns(ActualWidth);
        }

        /// <summary>Reflows the metric cards and charts to fit the available width.</summary>
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) => RecomputeColumns(e.NewSize.Width);

        private void RecomputeColumns(double width)
        {
            if (width <= 0) return;
            int cards = CardsPanel.Children.Count;
            if (cards > 0) CardsPanel.Columns = Math.Max(1, Math.Min(cards, (int)(width / 210)));
            int charts = ChartsPanel.Children.Count;
            if (charts > 0) ChartsPanel.Columns = Math.Max(1, Math.Min(charts, (int)(width / 360)));
        }

        private void AddCard(string label, string value, string icon)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                BorderBrush = (Brush)FindResource("LineBrush"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 12, 12),
                MinWidth = 150,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                { Color = Color.FromArgb(0x2A, 0, 0, 0), BlurRadius = 16, ShadowDepth = 2, Direction = 270, Opacity = 0.25 }
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = icon, FontSize = 22 });
            sp.Children.Add(new TextBlock { Text = value, FontSize = 30, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("DeepRed"), Margin = new Thickness(0, 6, 0, 0) });
            sp.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = (Brush)FindResource("Subtle") });
            card.Child = sp;
            CardsPanel.Children.Add(card);
        }

        /// <summary>Counts batches expiring within 30 days (or already expired) for a stream.</summary>
        private static string ExpiringSql(string? stream) =>
            @"SELECT COUNT(*) FROM InventoryBatches b JOIN InventoryItems i ON i.Id=b.InventoryItemId
              WHERE b.Quantity>0 AND b.ExpiryDate IS NOT NULL AND date(b.ExpiryDate)<=date('now','+30 day')"
            + (stream != null ? " AND i.Stream=@s" : "");

        private void ShowAlerts(string? stream)
        {
            TxtListTitle.Text = "Stock Alerts (low stock or expiring batches)";
            var sql = @"SELECT i.ItemName AS Item, i.Stream,
                               CASE WHEN i.Quantity<=i.ReorderLevel THEN 'Low stock' ELSE 'Expiring batch' END AS Alert,
                               i.Quantity AS [On hand], i.ReorderLevel AS [Reorder at]
                        FROM InventoryItems i
                        WHERE (i.Quantity<=i.ReorderLevel
                               OR EXISTS (SELECT 1 FROM InventoryBatches b
                                          WHERE b.InventoryItemId=i.Id AND b.Quantity>0
                                            AND b.ExpiryDate IS NOT NULL AND date(b.ExpiryDate)<=date('now','+30 day')))";
            var p = new Dictionary<string, object?>();
            if (stream != null) { sql += " AND i.Stream=@s"; p["s"] = stream; }
            sql += " ORDER BY i.Quantity;";
            BindList(Db.ExecuteQuery(sql, p), "No stock alerts — everything is healthy.");
        }

        private void ShowMyAppointments()
        {
            TxtListTitle.Text = "My Appointments Today";
            var dt = Db.ExecuteQuery(
                @"SELECT time(a.AppointmentDate) AS Time, p.FirstName||' '||p.LastName AS Patient, a.Status, a.Notes
                  FROM Appointments a JOIN Patients p ON p.Id=a.PatientId
                  WHERE a.DoctorId=@id AND date(a.AppointmentDate)=@d ORDER BY a.AppointmentDate;",
                new Dictionary<string, object?> { ["id"] = _user.Id, ["d"] = DateTime.Today.ToString("yyyy-MM-dd") });
            BindList(dt, "No appointments scheduled for you today.");
        }

        private void ShowTodaysAppointments()
        {
            TxtListTitle.Text = "Today's Appointments";
            var dt = Db.ExecuteQuery(
                @"SELECT time(a.AppointmentDate) AS Time, p.FirstName||' '||p.LastName AS Patient,
                         u.FirstName||' '||u.LastName AS Doctor, a.Stream, a.Status
                  FROM Appointments a JOIN Patients p ON p.Id=a.PatientId JOIN Users u ON u.Id=a.DoctorId
                  WHERE date(a.AppointmentDate)=@d ORDER BY a.AppointmentDate;",
                new Dictionary<string, object?> { ["d"] = DateTime.Today.ToString("yyyy-MM-dd") });
            BindList(dt, "No appointments scheduled today.");
        }

        /// <summary>At-a-glance availability chips for every active doctor (FR-DOC-4).</summary>
        private void RenderDoctorStatus()
        {
            var rows = Db.ExecuteQuery(
                @"SELECT u.FirstName||' '||u.LastName AS Name, u.Specialization AS Spec,
                         COALESCE(a.Status,'OnSite') AS Status
                  FROM Users u LEFT JOIN DoctorAvailability a ON a.DoctorId=u.Id
                  WHERE u.Role='Doctor' AND u.IsActive=1 ORDER BY u.FirstName;");
            if (rows.Rows.Count == 0) return;

            DocStatusTitle.Visibility = Visibility.Visible;
            foreach (System.Data.DataRow r in rows.Rows)
            {
                string status = r["Status"]?.ToString() ?? "OnSite";
                (Color bg, Color fg, string label) = status switch
                {
                    "Away" => (Color.FromRgb(0xFD, 0xF3, 0xE0), Color.FromRgb(0xB5, 0x73, 0x00), "Away"),
                    "InConsultation" => (Color.FromRgb(0xFB, 0xE9, 0xE9), Color.FromRgb(0x8A, 0x00, 0x07), "In Consultation"),
                    _ => (Color.FromRgb(0xE8, 0xF6, 0xEC), Color.FromRgb(0x1B, 0x7A, 0x3D), "On-site"),
                };
                string spec = (r["Spec"]?.ToString() == "Dental") ? "Dentist" : "GP";
                var chip = new Border
                {
                    Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 10, 10)
                };
                chip.Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = $"Dr. {r["Name"]}", FontWeight = FontWeights.SemiBold, FontSize = 13, Foreground = (Brush)FindResource("Ink") },
                        new TextBlock { Text = $"{spec} · {label}", FontSize = 11, Foreground = new SolidColorBrush(fg) }
                    }
                };
                DocStatusPanel.Children.Add(chip);
            }
        }

        private void ShowDispenseQueue()
        {
            TxtListTitle.Text = "Pending dispensary queue";
            var dt = Db.ExecuteQuery(
                @"SELECT COALESCE(p.RegistrationNumber,'—') AS [Uni ID],
                         p.FirstName||' '||p.LastName AS Patient,
                         u.FirstName||' '||u.LastName AS Doctor,
                         COUNT(rx.Id) AS Medicines, m.RecordDate AS [Prescribed]
                  FROM MedicalRecords m
                  JOIN Patients p ON p.Id=m.PatientId
                  JOIN Users u ON u.Id=m.DoctorId
                  JOIN Prescriptions rx ON rx.MedicalRecordId=m.Id AND rx.Dispensed=0
                  GROUP BY m.Id ORDER BY m.RecordDate DESC;");
            BindList(dt, "Nothing waiting to be dispensed.");
        }

        private void BindList(System.Data.DataTable dt, string emptyMessage)
        {
            if (dt.Rows.Count == 0)
            {
                ListGrid.Visibility = Visibility.Collapsed;
                TxtEmpty.Text = emptyMessage;
                TxtEmpty.Visibility = Visibility.Visible;
            }
            else
            {
                ListGrid.ItemsSource = dt.DefaultView;
                ListGrid.Visibility = Visibility.Visible;
                TxtEmpty.Visibility = Visibility.Collapsed;
            }
        }
    }
}
