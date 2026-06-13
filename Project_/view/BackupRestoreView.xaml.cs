using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Project_.Views
{
    public partial class BackupRestoreView : UserControl
    {
        public BackupRestoreView()
        {
            InitializeComponent();
            LoadSummaries();
        }

        private void LoadSummaries()
        {
            try
            {
                var users = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
                var patients = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Patients");
                var appts = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Appointments");
                var inventory = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Inventory");
                var prescriptions = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Prescriptions");

                TxtUserCount.Text = users.ToString();
                TxtPatientCount.Text = patients.ToString();
                TxtAppointmentCount.Text = appts.ToString();
                TxtInventoryCount.Text = inventory.ToString();
                TxtPrescriptionCount.Text = prescriptions.ToString();
            }
            catch { }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db",
                Title = "Export Backup",
                FileName = $"MediHelp_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    App.Database.Backup(dialog.FileName);
                    MessageBox.Show("Backup exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db",
                Title = "Restore Backup"
            };

            if (dialog.ShowDialog() == true)
            {
                if (MessageBox.Show("Restoring a backup will overwrite all current system data. This action cannot be undone. Are you sure?", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        App.Database.Restore(dialog.FileName);
                        MessageBox.Show("Database restored successfully! Application will now restart to apply changes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Restart app after restore to refresh connections
                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Restore failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
