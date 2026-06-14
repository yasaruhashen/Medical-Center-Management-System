using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for DentalView.xaml
    /// UI-only stub: event handlers are present so the view compiles and renders.
    /// Wire real logic in here later.
    /// </summary>
    public partial class DentalView : UserControl
    {
        public DentalView()
        {
            InitializeComponent();
        }

        private void FPatient_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void HideId(object sender, DataGridAutoGeneratingColumnEventArgs e) { }
        private void BtnNew_Click(object sender, RoutedEventArgs e) { }
        private void BtnSavePharmacy_Click(object sender, RoutedEventArgs e) { }
        private void BtnSaveDispense_Click(object sender, RoutedEventArgs e) { }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { }
    }
}
