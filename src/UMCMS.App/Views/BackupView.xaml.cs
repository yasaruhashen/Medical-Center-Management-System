using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin backup &amp; restore of the SQLite database file.</summary>
    public partial class BackupView : UserControl
    {
        public BackupView(User user)
        {
            InitializeComponent();
            TxtPath.Text = App.Database.DatabasePath;
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                FileName = $"umcms_backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                Filter = "Database backup|*.db"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                App.Database.Backup(dlg.FileName);
                App.Audit.Log("Backup", "Database", Path.GetFileName(dlg.FileName));
                Status($"Backup saved to {Path.GetFileName(dlg.FileName)}");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Backup failed", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restoring will overwrite ALL current data and close the app. Continue?",
                "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var dlg = new OpenFileDialog { Filter = "Database backup|*.db" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                App.Database.Restore(dlg.FileName);
                MessageBox.Show("Database restored. The application will now close — reopen it to use the restored data.",
                    "Restored", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Restore failed", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Status(string msg) { TxtStatus.Text = msg; TxtStatus.Visibility = Visibility.Visible; }
    }
}
