using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UMCMS.Domain;
using UMCMS.Domain.Entities;
using UMCMS.Services;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Main application shell. Renders a role-scoped navigation rail (only the sections
    /// the signed-in user needs) and hosts the active section.
    /// </summary>
    public partial class ShellView : UserControl
    {
        private readonly User _user;
        private Button? _activeButton;

        public event EventHandler? LogoutRequested;

        public ShellView(User user)
        {
            InitializeComponent();
            _user = user;
            TxtUserName.Text = _user.FullName;
            TxtUserRole.Text = NavigationCatalog.RoleLabel(_user);
            TxtUserInitials.Text = Initials(_user);
            BuildNav();
        }

        private static string Initials(User u)
        {
            char a = u.FirstName.Length > 0 ? char.ToUpper(u.FirstName[0]) : 'U';
            string b = u.LastName.Length > 0 ? char.ToUpper(u.LastName[0]).ToString() : "";
            return $"{a}{b}";
        }

        private void BuildNav()
        {
            foreach (var item in NavigationCatalog.For(_user))
            {
                var btn = new Button
                {
                    Style = (Style)FindResource("NavButtonStyle"),
                    DataContext = item,
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new TextBlock { Text = item.Icon, FontSize = 16, Width = 28, VerticalAlignment = VerticalAlignment.Center },
                            new TextBlock { Text = item.Title, VerticalAlignment = VerticalAlignment.Center }
                        }
                    }
                };
                btn.Click += (s, _) => Activate((Button)s);
                NavPanel.Children.Add(btn);
            }
            if (NavPanel.Children.Count > 0) Activate((Button)NavPanel.Children[0]);
        }

        private void Activate(Button btn)
        {
            if (_activeButton is not null)
            {
                _activeButton.Tag = null;
                // Clear the local value so the style default + ACTIVE trigger control the colour.
                // (Setting Foreground locally here would override the trigger next time → text stays cream.)
                _activeButton.ClearValue(ForegroundProperty);
            }
            btn.Tag = "ACTIVE";
            _activeButton = btn;

            if (btn.DataContext is NavItem item)
            {
                TxtPageTitle.Text = item.Title;
                var view = ResolveView(item);
                ContentHost.Content = view;
                AnimateIn(view);
            }
        }

        /// <summary>Fade + slide-up entrance for the newly navigated screen.</summary>
        private static void AnimateIn(UIElement element)
        {
            var ease = new System.Windows.Media.Animation.CubicEase
            { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

            element.Opacity = 0;
            var slide = new TranslateTransform(0, 22);
            element.RenderTransform = slide;

            element.BeginAnimation(UIElement.OpacityProperty,
                new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
            slide.BeginAnimation(TranslateTransform.YProperty,
                new System.Windows.Media.Animation.DoubleAnimation(22, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });
        }

        // Pending context when a doctor checks a patient in and jumps to the consultation.
        private int? _pendingPatientId;
        private int? _pendingAppointmentId;

        /// <summary>Called by the appointment queue: open the consultation for a checked-in patient.</summary>
        private void OpenConsultationFor(int patientId, int appointmentId)
        {
            _pendingPatientId = patientId;
            _pendingAppointmentId = appointmentId;
            // Dentists work in Dental Records; general physicians in Consultations.
            ActivateKey(_user.IsDentist ? "dental" : "consultations");
        }

        /// <summary>Activates the nav button with the given key (if the role has it).</summary>
        private void ActivateKey(string key)
        {
            foreach (var child in NavPanel.Children)
                if (child is Button b && b.DataContext is NavItem ni && ni.Key == key)
                {
                    Activate(b);
                    return;
                }
        }

        /// <summary>Maps a nav key to its real, role-scoped screen.</summary>
        private UIElement ResolveView(NavItem item)
        {
            switch (item.Key)
            {
                case "dashboard": return new DashboardView(_user);
                case "patients": return new PatientsView(_user);
                case "appointments":
                case "my-queue":
                    var appts = new AppointmentsView(_user) { OpenConsultation = OpenConsultationFor };
                    return appts;
                case "waitlist": return new WaitlistView(_user);
                case "inventory": return new InventoryView(_user, null);
                case "gen-meds": return new InventoryView(_user, "General");
                case "dental-stock": return new InventoryView(_user, "Dental");
                case "consultations":
                    var consult = new ConsultationsView(_user, _pendingPatientId, _pendingAppointmentId);
                    _pendingPatientId = null; _pendingAppointmentId = null;
                    return consult;
                case "dental":
                    var dental = new DentalView(_user, _pendingPatientId);
                    _pendingPatientId = null; _pendingAppointmentId = null;
                    return dental;
                case "dispensary": return new PharmacistQueueView(_user);
                case "forecast": return new ForecastView(_user);
                case "reports": return new ReportsView(_user);
                case "notifications": return new NotificationsView(_user);
                case "audit": return new AuditLogView(_user);
                case "users": return new UsersView(_user);
                case "backup": return new BackupView(_user);
                case "availability": return new AvailabilityView(_user);
                case "settings": return new SettingsView(_user);
                default: return BuildPlaceholder(item);
            }
        }

        private UIElement BuildPlaceholder(NavItem item)
        {
            var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(new TextBlock { Text = item.Icon, FontSize = 64, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock
            {
                Text = item.Title, FontSize = 24, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0x02, 0x05)),
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 4)
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"“{item.Title}” for {NavigationCatalog.RoleLabel(_user)} — screen coming next.",
                FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return panel;
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ChangePasswordWindow { Owner = Window.GetWindow(this) };
            dlg.ShowDialog();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Session.SignOut();
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
