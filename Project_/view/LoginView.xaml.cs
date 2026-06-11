using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// University Medical Centre Management System — Login Screen
    /// Supports role-based login: Admin | Doctor | Staff
    /// </summary>
    public partial class LoginView : UserControl
    {
        // ── Brushes matching the palette ──────────────────────────────
        private static readonly SolidColorBrush ActiveRoleBg  = new(Color.FromRgb(0x8A, 0x00, 0x07));
        private static readonly SolidColorBrush InactiveRoleBg = new(Colors.Transparent);
        private static readonly SolidColorBrush WhiteFg        = new(Colors.White);
        private static readonly SolidColorBrush GrayFg         = new(Color.FromRgb(0x55, 0x55, 0x55));

        // ── Currently selected role ────────────────────────────────────
        private string _selectedRole = "Admin";

        public LoginView()
        {
            InitializeComponent();
            Loaded += LoginView_Loaded;
        }

        // ── On load, focus the username field ─────────────────────────
        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            TxtUsername.Focus();
        }

        // ═══════════════════════════════════════════════════════════════
        //  ROLE SELECTION
        // ═══════════════════════════════════════════════════════════════

        private void BtnRoleAdmin_Click(object sender, RoutedEventArgs e)
            => SetActiveRole("Admin");

        private void BtnRoleDoctor_Click(object sender, RoutedEventArgs e)
            => SetActiveRole("Doctor");

        private void BtnRoleStaff_Click(object sender, RoutedEventArgs e)
            => SetActiveRole("Staff");

        /// <summary>
        /// Visually activates the selected role tab and updates state.
        /// </summary>
        private void SetActiveRole(string role)
        {
            _selectedRole = role;
            TxtCurrentRole.Text = role;

            // Reset all tabs to inactive
            ApplyInactiveTabStyle(BtnRoleAdmin);
            ApplyInactiveTabStyle(BtnRoleDoctor);
            ApplyInactiveTabStyle(BtnRoleStaff);

            // Activate the selected tab
            Button activeBtn = role switch
            {
                "Doctor" => BtnRoleDoctor,
                "Staff"  => BtnRoleStaff,
                _        => BtnRoleAdmin
            };
            ApplyActiveTabStyle(activeBtn);

            // Clear any previous error when switching roles
            HideError();
            TxtUsername.Text = string.Empty;
            TxtPassword.Password = string.Empty;
            UpdateUsernamePlaceholder();
            UpdatePasswordPlaceholder();
            TxtUsername.Focus();
        }

        private static void ApplyActiveTabStyle(Button btn)
        {
            btn.Background = ActiveRoleBg;
            btn.Foreground = WhiteFg;
            btn.FontWeight = FontWeights.SemiBold;
        }

        private static void ApplyInactiveTabStyle(Button btn)
        {
            btn.Background = InactiveRoleBg;
            btn.Foreground = GrayFg;
            btn.FontWeight = FontWeights.Normal;
        }

        // ═══════════════════════════════════════════════════════════════
        //  PLACEHOLDER TEXT SIMULATION
        // ═══════════════════════════════════════════════════════════════

        private void UpdateUsernamePlaceholder()
        {
            TxtUsernamePlaceholder.Visibility =
                string.IsNullOrEmpty(TxtUsername.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void UpdatePasswordPlaceholder()
        {
            TxtPasswordPlaceholder.Visibility =
                string.IsNullOrEmpty(TxtPassword.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
            => UpdatePasswordPlaceholder();

        // ═══════════════════════════════════════════════════════════════
        //  KEYBOARD SUPPORT — press Enter to login
        // ═══════════════════════════════════════════════════════════════

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateUsernamePlaceholder();
            if (e.Key == Key.Enter)
                TxtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AttemptLogin();
        }

        // ═══════════════════════════════════════════════════════════════
        //  LOGIN LOGIC
        // ═══════════════════════════════════════════════════════════════

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
            => AttemptLogin();

        private void AttemptLogin()
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            // ── Basic validation ──
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            // ── Authenticate against your data layer here ──
            // TODO: Replace with real DB / service call
            bool authenticated = AuthenticateUser(username, password, _selectedRole);

            if (authenticated)
            {
                HideError();
                OnLoginSuccess(_selectedRole);
            }
            else
            {
                ShowError($"Invalid credentials for {_selectedRole}. Please try again.");
                TxtPassword.Password = string.Empty;
                UpdatePasswordPlaceholder();
                TxtPassword.Focus();
            }
        }

        /// <summary>
        /// Stub authentication method.
        /// Replace with actual database lookup.
        /// Demo credentials: admin/admin123, doctor/doc123, staff/staff123
        /// </summary>
        private static bool AuthenticateUser(string username, string password, string role)
        {
            return role switch
            {
                "Admin"  => username == "admin"  && password == "admin123",
                "Doctor" => username == "doctor" && password == "doc123",
                "Staff"  => username == "staff"  && password == "staff123",
                _        => false
            };
        }

        /// <summary>
        /// Called when authentication succeeds.
        /// Raises navigation event for the host window.
        /// </summary>
        private void OnLoginSuccess(string role)
        {
            // Raise the event so the host (MainWindow) can navigate to the shell
            LoginSucceeded?.Invoke(this, new LoginEventArgs(role));
        }

        // ── Public event for the host window to subscribe to ──────────
        public event EventHandler<LoginEventArgs>? LoginSucceeded;

        // ═══════════════════════════════════════════════════════════════
        //  ERROR DISPLAY
        // ═══════════════════════════════════════════════════════════════

        private void ShowError(string message)
        {
            TxtErrorMessage.Text    = message;
            ErrorBorder.Visibility  = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════════
        //  FORGOT PASSWORD
        // ═══════════════════════════════════════════════════════════════

        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Please contact your system administrator to reset your password.\n\nEmail: admin@medihelp.ac.lk",
                "Forgot Password",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    /// <summary>Event args carrying the authenticated role.</summary>
    public class LoginEventArgs : EventArgs
    {
        public string Role { get; }
        public LoginEventArgs(string role) => Role = role;
    }
}
