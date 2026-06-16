using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Project_.Views
{
    public partial class StaffShellView : UserControl
    {
        public event EventHandler? LogoutRequested;

        // Shared view instances
        private StaffDashboardView _dashboardView;
        private PatientsView _patientsView;
        private AppointmentsView _appointmentsView;
        private InventoryView _inventoryView;
        private PrescriptionProcessingView _prescriptionsView;
        private StaffReportsView _reportsView;

        public StaffShellView()
        {
            InitializeComponent();
            
            // Initialize views
            _dashboardView = new StaffDashboardView();
            _patientsView = new PatientsView();
            _appointmentsView = new AppointmentsView();
            _inventoryView = new InventoryView();
            _prescriptionsView = new PrescriptionProcessingView();
            _reportsView = new StaffReportsView();

            // Set default view
            MainContent.Content = _dashboardView;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                // Reset styles for all navigation buttons
                ResetButtonStyles();

                // Highlight the active button
                btn.Background = new SolidColorBrush(Color.FromRgb(138, 0, 7)); // #8A0007
                btn.Foreground = new SolidColorBrush(Colors.White);
                btn.FontWeight = FontWeights.SemiBold;

                // Navigate to the appropriate view based on the button's Tag
                switch (btn.Tag?.ToString())
                {
                    case "Dashboard":
                        MainContent.Content = _dashboardView;
                        break;
                    case "Patients":
                        MainContent.Content = _patientsView;
                        break;
                    case "Appointments":
                        MainContent.Content = _appointmentsView;
                        break;
                    case "Inventory":
                        MainContent.Content = _inventoryView;
                        break;
                    case "Prescriptions":
                        MainContent.Content = _prescriptionsView;
                        break;
                    case "Reports":
                        MainContent.Content = _reportsView;
                        break;
                }
            }
        }

        private void ResetButtonStyles()
        {
            var defaultBg = new SolidColorBrush(Colors.Transparent);
            var defaultFg = new SolidColorBrush(Color.FromRgb(55, 65, 81)); // #374151
            var defaultWeight = FontWeights.Normal;

            BtnDashboard.Background = defaultBg;
            BtnDashboard.Foreground = defaultFg;
            BtnDashboard.FontWeight = defaultWeight;

            BtnPatients.Background = defaultBg;
            BtnPatients.Foreground = defaultFg;
            BtnPatients.FontWeight = defaultWeight;

            BtnAppointments.Background = defaultBg;
            BtnAppointments.Foreground = defaultFg;
            BtnAppointments.FontWeight = defaultWeight;

            BtnInventory.Background = defaultBg;
            BtnInventory.Foreground = defaultFg;
            BtnInventory.FontWeight = defaultWeight;

            BtnPrescriptions.Background = defaultBg;
            BtnPrescriptions.Foreground = defaultFg;
            BtnPrescriptions.FontWeight = defaultWeight;

            BtnReports.Background = defaultBg;
            BtnReports.Foreground = defaultFg;
            BtnReports.FontWeight = defaultWeight;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
