using System.Windows;
using System.Windows.Controls;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin user management: add/edit users, toggle active, reset passwords.</summary>
    public partial class UsersView : UserControl
    {
        private User? _editing;

        public UsersView(User user)
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            Grid.ItemsSource = App.Users.GetAll();
            UpdateButtons();
        }

        private User? Selected => Grid.SelectedItem as User;
        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();
        private void UpdateButtons() => BtnEdit.IsEnabled = BtnToggle.IsEnabled = BtnReset.IsEnabled = Selected is not null;

        private void URole_Changed(object sender, SelectionChangedEventArgs e)
        {
            bool doctor = (URole.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Doctor";
            if (PanelSpec != null) PanelSpec.IsEnabled = doctor;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            TxtTitle.Text = "Add User";
            PanelPassword.Visibility = Visibility.Visible;
            UFirst.Text = ULast.Text = UUsername.Text = UPhone.Text = UEmail.Text = "";
            UUsername.IsEnabled = true;
            URole.SelectedIndex = 1; USpec.SelectedIndex = 0;
            TxtError.Visibility = Visibility.Collapsed;
            OverlayEdit.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not User u) return;
            _editing = u;
            TxtTitle.Text = "Edit User";
            PanelPassword.Visibility = Visibility.Collapsed;   // password changed via reset
            UFirst.Text = u.FirstName; ULast.Text = u.LastName;
            UUsername.Text = u.Username; UUsername.IsEnabled = false;
            UPhone.Text = u.PhoneNumber; UEmail.Text = u.Email;
            SelectCombo(URole, u.Role);
            SelectCombo(USpec, u.Specialization ?? "General");
            TxtError.Visibility = Visibility.Collapsed;
            OverlayEdit.Visibility = Visibility.Visible;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UFirst.Text) || string.IsNullOrWhiteSpace(ULast.Text) || string.IsNullOrWhiteSpace(UUsername.Text))
            { Error("First name, last name and username are required."); return; }

            string role = (URole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Receptionist";
            string? spec = role == "Doctor" ? (USpec.SelectedItem as ComboBoxItem)?.Content?.ToString() : null;

            var u = _editing ?? new User();
            u.FirstName = UFirst.Text.Trim();
            u.LastName = ULast.Text.Trim();
            u.Role = role;
            u.Specialization = spec;
            u.PhoneNumber = UPhone.Text.Trim();
            u.Email = UEmail.Text.Trim();

            try
            {
                if (_editing is null)
                {
                    if (UPassword.Password.Length < 4) { Error("Password must be at least 4 characters."); return; }
                    u.Username = UUsername.Text.Trim();
                    u.PasswordHash = App.Auth.HashPassword(UPassword.Password);
                    u.IsActive = true;
                    App.Users.Add(u);
                }
                else App.Users.Update(u);
            }
            catch (Exception ex)
            {
                Error(ex.Message.Contains("UNIQUE") ? "That username is already taken." : ex.Message);
                return;
            }
            App.Audit.Log(_editing is null ? "CreateUser" : "EditUser", "User", $"{u.Username} ({u.Role})");
            CloseOverlays(sender, e);
            Load();
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not User u) return;
            u.IsActive = !u.IsActive;
            App.Users.Update(u);
            App.Audit.Log(u.IsActive ? "ActivateUser" : "DeactivateUser", "User", u.Username);
            Load();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not User u) return;
            TxtResetFor.Text = $"Reset Password — {u.FullName}";
            RPassword.Password = "";
            TxtResetError.Visibility = Visibility.Collapsed;
            OverlayReset.Visibility = Visibility.Visible;
        }

        private void BtnDoReset_Click(object sender, RoutedEventArgs e)
        {
            if (Selected is not User u) return;
            if (RPassword.Password.Length < 4) { TxtResetError.Text = "Password must be at least 4 characters."; TxtResetError.Visibility = Visibility.Visible; return; }
            App.Database.ExecuteNonQuery("UPDATE Users SET PasswordHash=@p WHERE Id=@id;",
                new Dictionary<string, object?> { ["p"] = App.Auth.HashPassword(RPassword.Password), ["id"] = u.Id });
            App.Audit.Log("ResetPassword", "User", u.Username);
            CloseOverlays(sender, e);
            MessageBox.Show("Password reset.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseOverlays(object sender, RoutedEventArgs e) =>
            OverlayEdit.Visibility = OverlayReset.Visibility = Visibility.Collapsed;

        private void Error(string m) { TxtError.Text = m; TxtError.Visibility = Visibility.Visible; }

        private static void SelectCombo(ComboBox cb, string? value)
        {
            foreach (ComboBoxItem it in cb.Items)
                if (string.Equals(it.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase)) { cb.SelectedItem = it; return; }
            cb.SelectedIndex = -1;
        }
    }
}
