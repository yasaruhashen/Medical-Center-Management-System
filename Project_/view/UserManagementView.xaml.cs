using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Project_.src.Models;

namespace Project_.Views
{
    public partial class UserManagementView : UserControl
    {
        public ObservableCollection<UserDisplay> Users { get; set; } = new();

        public UserManagementView()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtSearch.Text == "Search users...")
            {
                TxtSearch.Text = "";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                TxtSearch.Text = "Search users...";
                TxtSearch.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }

        private void LoadUsers(string filter = "")
        {
            Users.Clear();
            var sql = "SELECT * FROM Users WHERE IsActive = 1";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                sql += $" AND (FirstName LIKE '%{filter}%' OR LastName LIKE '%{filter}%' OR Username LIKE '%{filter}%')";
            }

            var data = App.Database.ExecuteQuery(sql);
            foreach (DataRow row in data.Rows)
            {
                var role = row["Role"]?.ToString() ?? "Staff";
                Users.Add(new UserDisplay
                {
                    Id = Convert.ToInt32(row["Id"]),
                    FullName = $"{row["FirstName"]} {row["LastName"]}",
                    Username = row["Username"]?.ToString() ?? "",
                    Email = row["Email"]?.ToString() ?? "—",
                    Role = role,
                    Initial = (row["FirstName"]?.ToString() ?? "U").Substring(0, 1).ToUpper(),
                    IsCurrentUser = row["Username"]?.ToString() == "admin" ? Visibility.Visible : Visibility.Collapsed,
                    RoleColor = role == "Admin" ? "#8A0007" : role == "Doctor" ? "#4E0205" : "#b45309",
                    RoleBgColor = role == "Admin" ? "#FFF0F0" : role == "Doctor" ? "#FDF0F0" : "#fffbeb"
                });
            }
            UsersList.ItemsSource = Users;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = ((TextBox)sender).Text;
            if (txt == "Search users...") return;
            LoadUsers(txt);
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var win = new UserEditWindow { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    int id = Convert.ToInt32(btn.Tag);
                    var win = new UserEditWindow(id) { Owner = Window.GetWindow(this) };
                    if (win.ShowDialog() == true)
                    {
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error editing user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    int id = Convert.ToInt32(btn.Tag);
                    if (MessageBox.Show("Are you sure you want to delete this user?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        App.Database.ExecuteNonQuery("UPDATE Users SET IsActive = 0 WHERE Id = @Id", new Dictionary<string, object?> { { "Id", id } });
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class UserDisplay
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Initial { get; set; } = string.Empty;
        public Visibility IsCurrentUser { get; set; }
        public string RoleColor { get; set; } = string.Empty;
        public string RoleBgColor { get; set; } = string.Empty;
    }
}
