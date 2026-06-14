using System.Windows;
using System.Windows.Controls;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for ConsultationsView.xaml
    /// UI-only stub: event handlers are present so the view compiles and renders.
    /// Wire real logic in here later.
    /// </summary>
    public partial class ConsultationsView : UserControl
    {
        public ConsultationsView()
        {
            InitializeComponent();
        }

        private void FPatient_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnExportEhr_Click(object sender, RoutedEventArgs e) { }
        private void HideId(object sender, DataGridAutoGeneratingColumnEventArgs e) { }
        private void RecordsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnSendPharmacy_Click(object sender, RoutedEventArgs e) { }
        private void BtnDispenseHere_Click(object sender, RoutedEventArgs e) { }
    }
}
