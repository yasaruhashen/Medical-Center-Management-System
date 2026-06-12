using System.Windows;
using Project_.src.Services;

namespace Project_
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Shared database helper for the running app. Until a DI container is added,
        /// repositories/services can use <c>App.Database</c> to reach the same instance.
        /// </summary>
        public static DatabaseService Database { get; } = new DatabaseService();

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Create the SQLite file + tables and seed the default admin on first run.
                Database.InitializeDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The application could not initialize its database and will now close.\n\n" + ex.Message,
                    "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
}
