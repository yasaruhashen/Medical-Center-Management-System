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

        /// <summary>
        /// Called when the user authenticates successfully.
        /// Hides the login screen and shows the main shell.
        /// </summary>
        private void OnLoginSucceeded(object? sender, LoginEventArgs e)
        {
            // Hide login, reveal application shell
            LoginScreen.Visibility = Visibility.Collapsed;
            ShellGrid.Visibility   = Visibility.Visible;

            // Update window title to reflect the authenticated role
            Title = $"Medi Help(University Medical Centre) [{e.Role}]";

            // TODO: Pass the role into the shell / navigation service
            //       so menus are filtered by permission level.
        }

        private void LoginScreen_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void LoginScreen_Loaded_1(object sender, RoutedEventArgs e)
        {

        }
    }
}