using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UMCMS.Domain.Entities;
using UMCMS.Services;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Doctor availability toggle. Going Away → On-site notifies all queued subscribers
    /// (mock notifications persisted to NotificationLogs) and clears the queue.
    /// </summary>
    public partial class AvailabilityView : UserControl
    {
        private readonly User _user;

        public AvailabilityView(User user)
        {
            InitializeComponent();
            _user = user;
            EnsureRow();
            RenderStatus(CurrentStatus());
            LoadNotifications();
            ShowSubscriberInfo();
        }

        private void EnsureRow()
        {
            var exists = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM DoctorAvailability WHERE DoctorId=@id;",
                new Dictionary<string, object?> { ["id"] = _user.Id });
            if (exists == 0)
                App.Database.ExecuteNonQuery(
                    "INSERT INTO DoctorAvailability (DoctorId, Status, LastChanged) VALUES (@id,'OnSite',@t);",
                    new Dictionary<string, object?> { ["id"] = _user.Id, ["t"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
        }

        private string CurrentStatus() => App.Database.ExecuteScalar<string>(
            "SELECT Status FROM DoctorAvailability WHERE DoctorId=@id;", new Dictionary<string, object?> { ["id"] = _user.Id }) ?? "OnSite";

        private void SetStatus_Click(object sender, RoutedEventArgs e)
        {
            string old = CurrentStatus();
            string status = (string)((Button)sender).Tag;

            App.Database.ExecuteNonQuery(
                "UPDATE DoctorAvailability SET Status=@s, LastChanged=@t WHERE DoctorId=@id;",
                new Dictionary<string, object?> { ["s"] = status, ["t"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ["id"] = _user.Id });

            if (old == "Away" && status == "OnSite")
            {
                int n = NotifySubscribers();
                TxtInfo.Text = n > 0
                    ? $"You're back on-site — notified {n} waiting subscriber(s)."
                    : "You're back on-site. No subscribers were waiting.";
            }
            RenderStatus(status);
            LoadNotifications();
            ShowSubscriberInfo();
        }

        private int NotifySubscribers()
        {
            var subs = App.Database.Query(
                @"SELECT s.Id, p.FirstName||' '||p.LastName AS Name, p.PhoneNumber AS Phone
                  FROM AvailabilitySubscriptions s JOIN Patients p ON p.Id=s.PatientId
                  WHERE s.DoctorId=@id AND s.Notified=0;",
                r => (Id: Convert.ToInt32(r["Id"]), Name: r["Name"] as string ?? "", Phone: r["Phone"] as string ?? ""),
                new Dictionary<string, object?> { ["id"] = _user.Id });

            foreach (var s in subs)
            {
                App.Notifications.Send("SMS",
                    string.IsNullOrWhiteSpace(s.Phone) ? s.Name : s.Phone,
                    NotificationTemplates.DoctorOnSite(_user.LastName));
                App.Database.ExecuteNonQuery("UPDATE AvailabilitySubscriptions SET Notified=1 WHERE Id=@id;",
                    new Dictionary<string, object?> { ["id"] = s.Id });
            }
            if (subs.Count > 0) App.Audit.Log("AvailabilityNotify", "Availability", $"Notified {subs.Count} subscriber(s)");
            return subs.Count;
        }

        private void ShowSubscriberInfo()
        {
            long waiting = App.Database.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM AvailabilitySubscriptions WHERE DoctorId=@id AND Notified=0;",
                new Dictionary<string, object?> { ["id"] = _user.Id });
            if (string.IsNullOrEmpty(TxtInfo.Text))
                TxtInfo.Text = $"{waiting} subscriber(s) waiting to be notified when you return on-site.";
        }

        private void RenderStatus(string status)
        {
            (string text, Color bg, Color fg) = status switch
            {
                "Away" => ("Away", Color.FromRgb(0xFD, 0xF3, 0xE0), Color.FromRgb(0xB5, 0x73, 0x00)),
                "InConsultation" => ("In Consultation", Color.FromRgb(0xFB, 0xE9, 0xE9), Color.FromRgb(0x8A, 0x00, 0x07)),
                _ => ("On-site", Color.FromRgb(0xE8, 0xF6, 0xEC), Color.FromRgb(0x1B, 0x7A, 0x3D)),
            };
            TxtStatus.Text = text;
            TxtStatus.Foreground = new SolidColorBrush(fg);
            StatusChip.Background = new SolidColorBrush(bg);
        }

        private void LoadNotifications() => NotifGrid.ItemsSource = App.Database.ExecuteQuery(
            @"SELECT Channel, Recipient, Body, Status, CreatedAt AS [Sent]
              FROM NotificationLogs ORDER BY Id DESC LIMIT 20;").DefaultView;
    }
}
