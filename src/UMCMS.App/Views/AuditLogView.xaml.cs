using System.Windows;
using System.Windows.Controls;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>Admin view of the immutable audit trail.</summary>
    public partial class AuditLogView : UserControl
    {
        public AuditLogView(User user)
        {
            InitializeComponent();
            Load();
        }

        private void Load() => Grid.ItemsSource = App.Audit.Recent().DefaultView;

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => Load();
    }
}
