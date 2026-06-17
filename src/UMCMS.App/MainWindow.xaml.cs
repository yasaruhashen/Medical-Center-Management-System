using System.Windows;
using System.Windows.Threading;
using UMCMS.App.Views;
using UMCMS.Services;

namespace UMCMS.App
{
    /// <summary>Host window — swaps between the LoginView and the role-scoped shell.</summary>
    public partial class MainWindow : Window
    {
        // Auto-lock the session after a period of inactivity (FR-AUTH-6).
        private readonly DispatcherTimer _idleTimer = new();
        private bool _loggedIn;

        public MainWindow()
        {
            InitializeComponent();
            LoginScreen.LoginSucceeded += OnLoginSucceeded;

            _idleTimer.Tick += IdleTimer_Tick;
            // Any input anywhere in the window counts as activity.
            PreviewMouseDown += (_, _) => ResetIdle();
            PreviewMouseMove += (_, _) => ResetIdle();
            PreviewKeyDown += (_, _) => ResetIdle();
        }

        private void OnLoginSucceeded(object? sender, LoginEventArgs e)
        {
            var shell = new ShellView(e.User);
            shell.LogoutRequested += OnLogoutRequested;
            ShellHost.Content = shell;
            ShellHost.Visibility = Visibility.Visible;
            LoginScreen.Visibility = Visibility.Collapsed;
            App.Audit.Log("Login", "User", $"{e.User.Username} ({NavigationCatalog.RoleLabel(e.User)})");
            Title = $"Medi Help J'Pura — {NavigationCatalog.RoleLabel(e.User)}: {e.User.FullName}";

            _loggedIn = true;
            StartIdleTimer();
        }

        private void OnLogoutRequested(object? sender, EventArgs e) => ReturnToLogin();

        private void ReturnToLogin()
        {
            _loggedIn = false;
            _idleTimer.Stop();
            ShellHost.Content = null;
            ShellHost.Visibility = Visibility.Collapsed;
            LoginScreen.Visibility = Visibility.Visible;
            LoginScreen.Reset();
            Title = "Medi Help J'Pura";
        }

        private void StartIdleTimer()
        {
            int minutes = (int)App.Settings.GetDouble("session.idleMinutes", 10);
            if (minutes <= 0) { _idleTimer.Stop(); return; }   // 0 disables auto-lock
            _idleTimer.Interval = TimeSpan.FromMinutes(minutes);
            _idleTimer.Stop();
            _idleTimer.Start();
        }

        private void ResetIdle()
        {
            if (!_loggedIn) return;
            _idleTimer.Stop();
            _idleTimer.Start();   // restart the countdown on activity
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            if (!_loggedIn) return;
            App.Audit.Log("AutoLock", "User", Session.CurrentUser?.Username ?? "");
            Session.SignOut();
            ReturnToLogin();
            MessageBox.Show("Your session was locked due to inactivity. Please sign in again.",
                "Session locked", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
