using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for PrescriptionEntry.xaml
    /// UI-only stub: event handlers are present so the control compiles and renders.
    /// Wire real logic in here later.
    /// </summary>
    public partial class PrescriptionEntry : UserControl
    {
        public PrescriptionEntry()
        {
            InitializeComponent();
        }

        private void RxMedicine_PreviewKeyDown(object sender, KeyEventArgs e) { }
        private void RxQty_PreviewKeyDown(object sender, KeyEventArgs e) { }
        private void BtnAddRx_Click(object sender, RoutedEventArgs e) { }
        private void BtnRemoveRx_Click(object sender, RoutedEventArgs e) { }
    }
}
