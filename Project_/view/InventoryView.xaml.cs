using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for InventoryView.xaml
    /// UI-only stub: event handlers are present so the view compiles and renders.
    /// Wire real logic in here later.
    /// </summary>
    public partial class InventoryView : UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
        }

        private void BtnDispense_Click(object sender, RoutedEventArgs e) { }
        private void BtnAddItem_Click(object sender, RoutedEventArgs e) { }
        private void BtnAddBatch_Click(object sender, RoutedEventArgs e) { }
        private void Filter_Changed(object sender, RoutedEventArgs e) { }
        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) { }
        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnDoDispense_Click(object sender, RoutedEventArgs e) { }
        private void CloseOverlays(object sender, RoutedEventArgs e) { }
        private void BtnSaveItem_Click(object sender, RoutedEventArgs e) { }
        private void BtnSaveBatch_Click(object sender, RoutedEventArgs e) { }
    }
}
