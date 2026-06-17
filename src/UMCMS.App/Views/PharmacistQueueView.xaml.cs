using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UMCMS.App.Helpers;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Pharmacist dispensary: lists consultations with undispensed prescriptions (by uni-ID
    /// and patient name). Dispensing deducts linked stock via FEFO and marks each line done.
    /// Auto-refreshes so prescriptions sent by doctors appear without a manual reload.
    /// </summary>
    public partial class PharmacistQueueView : UserControl
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(4) };
        private long _lastSignature = -1;

        public PharmacistQueueView(User user)
        {
            InitializeComponent();
            LoadQueue();
            _timer.Tick += (_, _) => AutoRefresh();
            Loaded += (_, _) => _timer.Start();
            Unloaded += (_, _) => _timer.Stop();
        }

        /// <summary>Reloads only when the set of pending prescriptions changed; keeps the selection.</summary>
        private void AutoRefresh()
        {
            long sig = App.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM Prescriptions WHERE Dispensed=0;");
            if (sig == _lastSignature) return;
            _lastSignature = sig;

            int? keep = SelectedRecordId;
            QueueGrid.ItemsSource = App.MedicalRecords.GetPendingConsultations().DefaultView;
            if (keep is int id && QueueGrid.ItemsSource is DataView dv)
                foreach (DataRowView r in dv)
                    if (Convert.ToInt32(r["Id"]) == id) { QueueGrid.SelectedItem = r; break; }
        }

        private void HideId(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id") e.Cancel = true;
        }

        private void LoadQueue()
        {
            QueueGrid.ItemsSource = App.MedicalRecords.GetPendingConsultations().DefaultView;
            DetailGrid.ItemsSource = null;
            TxtDetailTitle.Text = "Select a prescription";
            BtnDispense.IsEnabled = false;
        }

        private int? SelectedRecordId => QueueGrid.SelectedItem is DataRowView r ? Convert.ToInt32(r["Id"]) : null;

        private void QueueGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QueueGrid.SelectedItem is DataRowView row)
            {
                TxtDetailTitle.Text = $"{row["Patient"]}  ·  Uni ID: {row["Uni ID"]}";
                DetailGrid.ItemsSource = App.MedicalRecords.GetPendingPrescriptions(Convert.ToInt32(row["Id"])).DefaultView;
                BtnDispense.IsEnabled = true;
                BtnPrint.IsEnabled = true;
                BtnLabels.IsEnabled = true;
            }
            else
            {
                DetailGrid.ItemsSource = null;
                BtnDispense.IsEnabled = false;
                BtnPrint.IsEnabled = false;
                BtnLabels.IsEnabled = false;
            }
        }

        private void BtnDispense_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecordId is not int recId) return;

            var lines = App.MedicalRecords.GetPendingPrescriptionEntities(recId);
            var report = new StringBuilder();
            int dispensed = 0, shortStock = 0;

            foreach (var line in lines)
            {
                if (line.InventoryItemId is int itemId)
                {
                    var result = App.Inventory.Dispense(itemId, line.Quantity);
                    if (result.Success)
                    {
                        App.MedicalRecords.MarkDispensed(line.Id);
                        report.AppendLine($"✓ {line.Medicine} ×{line.Quantity}");
                        dispensed++;
                    }
                    else
                    {
                        report.AppendLine($"✗ {line.Medicine}: {result.Message}");
                        shortStock++;
                    }
                }
                else
                {
                    // Not linked to stock (e.g. external medicine) — issue manually.
                    App.MedicalRecords.MarkDispensed(line.Id);
                    report.AppendLine($"✓ {line.Medicine} ×{line.Quantity} (issued, not stock-tracked)");
                    dispensed++;
                }
            }

            App.Audit.Log("Dispense", "Prescription", $"Record #{recId}: {dispensed} dispensed, {shortStock} short");
            LoadQueue();

            MessageBox.Show(
                report.ToString() + (shortStock > 0 ? "\nSome items were short on stock and were not dispensed." : ""),
                shortStock > 0 ? "Partly Dispensed" : "Dispensed",
                MessageBoxButton.OK, shortStock > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecordId is int recId) SlipPrinter.Print(recId);
        }

        private void BtnLabels_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecordId is int recId) SlipPrinter.PrintLabels(recId);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadQueue();
    }
}
