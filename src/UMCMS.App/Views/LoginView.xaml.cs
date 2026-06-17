using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UMCMS.Domain.Entities;
using UMCMS.Services;

namespace UMCMS.App.Views
{
    /// <summary>Login screen. Role tabs: Admin | Doctor | Reception. Real BCrypt auth.</summary>
    public partial class LoginView : UserControl
    {
        private static readonly SolidColorBrush ActiveBg = new(Color.FromRgb(0x8A, 0x00, 0x07));
        private static readonly SolidColorBrush InactiveBg = new(Colors.Transparent);
        private static readonly SolidColorBrush WhiteFg = new(Colors.White);
        private static readonly SolidColorBrush GrayFg = new(Color.FromRgb(0x55, 0x55, 0x55));

        private string _selectedRole = "Admin";

        public event EventHandler<LoginEventArgs>? LoginSucceeded;

        public LoginView()
        {
            InitializeComponent();
            Loaded += (_, _) => { SetActiveRole("Admin"); TxtUsername.Focus(); };
        }

        /// <summary>Clears the form (used when returning from sign-out).</summary>
        public void Reset()
        {
            TxtUsername.Text = string.Empty;
            TxtPassword.Password = string.Empty;
            HideError();
            SetActiveRole("Admin");
        }

        private void BtnRoleAdmin_Click(object sender, RoutedEventArgs e) => SetActiveRole("Admin");
        private void BtnRoleDoctor_Click(object sender, RoutedEventArgs e) => SetActiveRole("Doctor");
        private void BtnRoleStaff_Click(object sender, RoutedEventArgs e) => SetActiveRole("Receptionist");
        private void BtnRolePharma_Click(object sender, RoutedEventArgs e) => SetActiveRole("Pharmacist");

        private void SetActiveRole(string role)
        {
            _selectedRole = role;
            TxtCurrentRole.Text = role == "Receptionist" ? "Reception" : role == "Pharmacist" ? "Pharmacy" : role;

            foreach (var b in new[] { BtnRoleAdmin, BtnRoleDoctor, BtnRoleStaff, BtnRolePharma })
            {
                b.Background = InactiveBg; b.Foreground = GrayFg; b.FontWeight = FontWeights.Normal;
            }
            Button active = role switch
            {
                "Doctor" => BtnRoleDoctor,
                "Receptionist" => BtnRoleStaff,
                "Pharmacist" => BtnRolePharma,
                _ => BtnRoleAdmin
            };
            active.Background = ActiveBg; active.Foreground = WhiteFg; active.FontWeight = FontWeights.SemiBold;

            HideError();
        }

        private void UpdateUsernamePlaceholder() =>
            TxtUsernamePlaceholder.Visibility = string.IsNullOrEmpty(TxtUsername.Text) ? Visibility.Visible : Visibility.Collapsed;
        private void UpdatePasswordPlaceholder() =>
            TxtPasswordPlaceholder.Visibility = string.IsNullOrEmpty(TxtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e) => UpdatePasswordPlaceholder();

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateUsernamePlaceholder();
            if (e.Key == Key.Enter) TxtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AttemptLogin();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e) => AttemptLogin();

        private void AttemptLogin()
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            try
            {
                var result = App.Auth.Authenticate(username, password, _selectedRole);
                if (result.Success)
                {
                    HideError();
                    Session.SignIn(result.User!);
                    LoginSucceeded?.Invoke(this, new LoginEventArgs(result.User!));
                }
                else
                {
                    App.Audit.Log("LoginFailed", "User", $"{username} ({_selectedRole})");
                    ShowError(result.Error ?? "Invalid credentials. Please try again.");
                    TxtPassword.Password = string.Empty;
                    UpdatePasswordPlaceholder();
                    TxtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not sign in: " + ex.Message);
            }
        }

        private void BtnForgotPassword_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Please contact your system administrator to reset your password.",
                "Forgot Password", MessageBoxButton.OK, MessageBoxImage.Information);

        private void ShowError(string message)
        {
            TxtErrorMessage.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideError() => ErrorBorder.Visibility = Visibility.Collapsed;
    }

    public class LoginEventArgs : EventArgs
    {
        public User User { get; }
        public LoginEventArgs(User user) => User = user;
    }
}
