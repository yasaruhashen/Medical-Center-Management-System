using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_.Views
{
    /// <summary>
    /// Interaction logic for AppointmentsView.xaml
    /// UI-only stub: event handlers are present so the view compiles and renders.
    /// Wire real logic in here later.
    /// </summary>
    public partial class AppointmentsView : UserControl
    {
        public AppointmentsView()
        {
            InitializeComponent();
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e) { }
        private void BtnBook_Click(object sender, RoutedEventArgs e) { }
        private void Filter_Changed(object sender, RoutedEventArgs e) { }
        private void BtnToggleView_Click(object sender, RoutedEventArgs e) { }
        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) { }
        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void Status_Click(object sender, RoutedEventArgs e) { }
        private void BtnReschedule_Click(object sender, RoutedEventArgs e) { }
        private void FDoctor_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void BtnCancelBook_Click(object sender, RoutedEventArgs e) { }
        private void BtnSaveBook_Click(object sender, RoutedEventArgs e) { }
        private void BtnCancelResched_Click(object sender, RoutedEventArgs e) { }
        private void BtnSaveResched_Click(object sender, RoutedEventArgs e) { }
        private void BtnCancelSub_Click(object sender, RoutedEventArgs e) { }
        private void BtnSaveSub_Click(object sender, RoutedEventArgs e) { }
    }
}
