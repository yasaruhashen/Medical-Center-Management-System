using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for PatientsView.xaml
    /// UI-only stub: event handlers are present so the view compiles and renders.
    /// Wire real logic in here later.
    /// </summary>
    public partial class PatientsView : UserControl
    {
        public PatientsView()
        {
            InitializeComponent();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) { }
        private void BtnImport_Click(object sender, RoutedEventArgs e) { }
        private void BtnEdit_Click(object sender, RoutedEventArgs e) { }
        private void BtnArchive_Click(object sender, RoutedEventArgs e) { }
        private void TxtSearch_KeyDown(object sender, KeyEventArgs e) { }
        private void BtnSearch_Click(object sender, RoutedEventArgs e) { }
        private void BtnClear_Click(object sender, RoutedEventArgs e) { }
        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }
        private void FType_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { }
        private void BtnSave_Click(object sender, RoutedEventArgs e) { }
    }
}
