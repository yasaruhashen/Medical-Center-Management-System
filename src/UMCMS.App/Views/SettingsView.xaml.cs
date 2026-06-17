using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using UMCMS.App.Helpers;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin settings: system info, forecast parameters, and the shared-DB path.</summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView(User user)
        {
            InitializeComponent();
            TxtDb.Text = "Active database: " + App.Database.DatabasePath;
            FAlpha.Text = App.Settings.GetDouble("forecast.alpha", 0.5).ToString(CultureInfo.InvariantCulture);
            FZ.Text = App.Settings.GetDouble("forecast.z", 1.5).ToString(CultureInfo.InvariantCulture);
            FIdle.Text = ((int)App.Settings.GetDouble("session.idleMinutes", 10)).ToString();
            FDbPath.Text = DbConfig.ConfiguredPath() ?? "";
        }

        private void BtnSaveDb_Click(object sender, RoutedEventArgs e)
        {
            DbConfig.Save(FDbPath.Text);
            App.Audit.Log("Settings", "Database", $"Shared path set to {FDbPath.Text}");
            ShowRestartNotice();
        }

        private void BtnLocalDb_Click(object sender, RoutedEventArgs e)
        {
            FDbPath.Text = "";
            DbConfig.Save(null);
            App.Audit.Log("Settings", "Database", "Reverted to local database");
            ShowRestartNotice();
        }

        private void ShowRestartNotice()
        {
            TxtDbSaved.Text = "Saved — restart the app to connect.";
            MessageBox.Show("Database location saved. Please close and reopen the app for it to take effect.",
                "Restart required", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(FAlpha.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var alpha) || alpha <= 0 || alpha > 1)
            { TxtSaved.Text = "α must be between 0 and 1."; return; }
            if (!double.TryParse(FZ.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var z) || z < 0)
            { TxtSaved.Text = "z must be 0 or greater."; return; }

            if (!int.TryParse(FIdle.Text, out var idle) || idle < 0)
            { TxtSaved.Text = "Auto-lock minutes must be 0 or greater."; return; }

            App.Settings.SetDouble("forecast.alpha", alpha);
            App.Settings.SetDouble("forecast.z", z);
            App.Settings.Set("session.idleMinutes", idle.ToString());
            App.Audit.Log("Settings", "Forecast", $"alpha={alpha}, z={z}, idleMinutes={idle}");
            TxtSaved.Text = "Saved. (Auto-lock applies at next sign-in.)";
        }
    }
}
