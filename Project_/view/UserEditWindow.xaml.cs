using System.Windows;

namespace Project_.Views
{
    public partial class UserEditWindow : Window
    {
        public int? EditingUserId { get; set; }

        public UserEditWindow(int? userId = null)
        {
            InitializeComponent();
            EditingUserId = userId;
            
            if (userId.HasValue)
            {
                TitleTxt.Text = "Edit User";
                LoadUser(userId.Value);
            }
            else
            {
                TitleTxt.Text = "Add New User";
                CmbRole.SelectedIndex = 2; // Default to Staff
            }
        }

        private void LoadUser(int id)
        {
            var data = App.Database.ExecuteQuery("SELECT * FROM Users WHERE Id = @Id", new Dictionary<string, object?> { { "Id", id } });
            if (data.Rows.Count > 0)
            {
                var row = data.Rows[0];
                TxtFirstName.Text = row["FirstName"]?.ToString();
                TxtLastName.Text = row["LastName"]?.ToString();
                TxtUsername.Text = row["Username"]?.ToString();
                
                var role = row["Role"]?.ToString();
                if (role == "Admin") CmbRole.SelectedIndex = 0;
                else if (role == "Doctor") CmbRole.SelectedIndex = 1;
                else CmbRole.SelectedIndex = 2;

                PasswordLabel.Text = "Password (leave blank to keep current)";
                BtnSaveTxt.Text = "Update";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var fn = TxtFirstName.Text.Trim();
            var ln = TxtLastName.Text.Trim();
            var un = TxtUsername.Text.Trim();
            var pw = TxtPassword.Password;
            var role = (CmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(fn) || string.IsNullOrEmpty(ln) || string.IsNullOrEmpty(un) || string.IsNullOrEmpty(role))
            {
                MessageBox.Show("Please fill all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (EditingUserId.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(pw))
                    {
                        App.Database.ExecuteNonQuery(
                            "UPDATE Users SET FirstName=@Fn, LastName=@Ln, Username=@Un, Role=@Role WHERE Id=@Id",
                            new Dictionary<string, object?> { { "Fn", fn }, { "Ln", ln }, { "Un", un }, { "Role", role }, { "Id", EditingUserId.Value } });
                    }
                    else
                    {
                        var hash = BCrypt.Net.BCrypt.HashPassword(pw);
                        App.Database.ExecuteNonQuery(
                            "UPDATE Users SET FirstName=@Fn, LastName=@Ln, Username=@Un, PasswordHash=@Hash, Role=@Role WHERE Id=@Id",
                            new Dictionary<string, object?> { { "Fn", fn }, { "Ln", ln }, { "Un", un }, { "Hash", hash }, { "Role", role }, { "Id", EditingUserId.Value } });
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(pw))
                    {
                        MessageBox.Show("Password is required for new users.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var hash = BCrypt.Net.BCrypt.HashPassword(pw);
                    App.Database.ExecuteNonQuery(
                        "INSERT INTO Users (FirstName, LastName, Username, PasswordHash, Role, CreatedAt, IsActive) VALUES (@Fn, @Ln, @Un, @Hash, @Role, @Date, 1)",
                        new Dictionary<string, object?> { { "Fn", fn }, { "Ln", ln }, { "Un", un }, { "Hash", hash }, { "Role", role }, { "Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") } });
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
