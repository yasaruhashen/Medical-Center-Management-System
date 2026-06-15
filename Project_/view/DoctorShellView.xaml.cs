using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project_.Views
{
    public partial class DoctorShellView : UserControl
    {
        public event EventHandler? LogoutRequested;
        private readonly long? _doctorId;

        public DoctorShellView(long? doctorId = null)
        {
            _doctorId = doctorId;
            InitializeComponent();
            Loaded += DoctorShellView_Loaded;
        }

        private void DoctorShellView_Loaded(object sender, RoutedEventArgs e)
        {
            // Default to Dashboard
            NavigateTo("Dashboard");
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                NavigateTo(btn.Tag.ToString() ?? "");
            }
        }

        private void NavigateTo(string page)
        {
            // Update active styles
            UpdateNavStyles(page);

            // Load view
            UserControl view = page switch
            {
                "Dashboard" => new DashboardView(_doctorId),
                "Appointments" => new AppointmentsView(_doctorId),
                "Patients" => new PatientDetailsView(),
                "Consultations" => new ConsultationsView(_doctorId),
                "Reports" => new ReportsView(),
                _ => new DashboardView(_doctorId)
            };

            MainContent.Content = view;
        }

        private void UpdateNavStyles(string activePage)
        {
            var buttons = new[] { BtnDashboard, BtnAppointments, BtnConsultations, BtnPatients, BtnReports };
            var activeBg = new SolidColorBrush(Color.FromRgb(0x8A, 0x00, 0x07));
            var inactiveBg = new SolidColorBrush(Colors.Transparent);
            var activeFg = new SolidColorBrush(Colors.White);
            var inactiveFg = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));

            foreach (var btn in buttons)
            {
                if (btn.Tag.ToString() == activePage)
                {
                    btn.Background = activeBg;
                    btn.Foreground = activeFg;
                    btn.FontWeight = FontWeights.SemiBold;
                }
                else
                {
                    btn.Background = inactiveBg;
                    btn.Foreground = inactiveFg;
                    btn.FontWeight = FontWeights.Normal;
                }
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
