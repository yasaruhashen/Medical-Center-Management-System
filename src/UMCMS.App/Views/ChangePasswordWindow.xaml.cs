using System.Windows;
using UMCMS.Services;

namespace UMCMS.App.Views
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (Session.CurrentUser is null) { Close(); return; }
            if (New1.Password != New2.Password) { Error("New passwords do not match."); return; }

            var err = App.Auth.ChangePassword(Session.CurrentUser.Username, Current.Password, New1.Password);
            if (err is not null) { Error(err); return; }

            App.Audit.Log("ChangePassword", "User", Session.CurrentUser.Username);
            MessageBox.Show("Your password has been updated.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Error(string m) { TxtError.Text = m; TxtError.Visibility = Visibility.Visible; }
    }
}
