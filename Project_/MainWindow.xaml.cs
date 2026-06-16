using System.Windows;
using Project_.Views;

namespace Project_
{
    /// <summary>
    /// Host window — swaps between LoginView and the main application shell.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to the login success event raised by LoginView
            LoginScreen.LoginSucceeded += OnLoginSucceeded;
        }

        private void OnLoginSucceeded(object? sender, LoginEventArgs e)
        {
            // Hide login, reveal application shell
            LoginScreen.Visibility = Visibility.Collapsed;
            ShellGrid.Visibility   = Visibility.Visible;

            // Update window title to reflect the authenticated role
            Title = $"Medi Help(University Medical Centre) [{e.Role}]";

            ShellGrid.Children.Clear();

            if (e.Role == "Admin")
            {
                var adminShell = new AdminShellView();
                adminShell.LogoutRequested += AdminShell_LogoutRequested;
                ShellGrid.Children.Add(adminShell);
            }
            else if (e.Role == "Doctor")
            {
                var doctorShell = new DoctorShellView(e.UserId);
                doctorShell.LogoutRequested += AdminShell_LogoutRequested; // We can reuse the same logout handler
                ShellGrid.Children.Add(doctorShell);
            }
<<<<<<< Updated upstream
=======
            else if (e.Role == "Staff")
            {
                var staffShell = new StaffShellView();
                staffShell.LogoutRequested += AdminShell_LogoutRequested;
                ShellGrid.Children.Add(staffShell);
            }
>>>>>>> Stashed changes
            else
            {
                // Placeholder for other roles
                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = $"{e.Role} Shell — (Not Implemented)",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 20
                };
                ShellGrid.Children.Add(textBlock);
            }
        }

        private void AdminShell_LogoutRequested(object? sender, System.EventArgs e)
        {
            ShellGrid.Children.Clear();
            ShellGrid.Visibility = Visibility.Collapsed;
            LoginScreen.Visibility = Visibility.Visible;
            Title = "Medi Help J'Pura";
        }

        private void LoginScreen_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void LoginScreen_Loaded_1(object sender, RoutedEventArgs e)
        {
        }
    }
}