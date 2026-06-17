using System.Windows;
using System.Windows.Controls;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin view of all logged notifications.</summary>
    public partial class NotificationsView : UserControl
    {
        public NotificationsView(User user)
        {
            InitializeComponent();
            Load();
        }

        private void Load() => Grid.ItemsSource = App.Database.ExecuteQuery(
            @"SELECT Channel, Recipient, Body, Status, CreatedAt AS [Sent]
              FROM NotificationLogs ORDER BY Id DESC LIMIT 200;").DefaultView;

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => Load();
    }
}
