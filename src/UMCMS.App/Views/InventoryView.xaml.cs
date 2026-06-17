using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using UMCMS.Domain.Entities;

namespace UMCMS.App.Views
{
    /// <summary>
    /// Inventory. Admin manages every stream (add items + receive batches). Doctors see
    /// only their own stream and can dispense (FEFO deduct). Stream is fixed for doctors.
    /// </summary>
    public partial class InventoryView : UserControl
    {
        private readonly string? _stream;   // null = all streams (admin)
        private readonly bool _canManage;   // admin only

        public InventoryView(User user, string? stream)
        {
            InitializeComponent();
            _stream = stream;
            _canManage = user.Role is "Admin" or "Pharmacist";   // both manage stock

            TxtHeader.Text = _stream switch
            {
                "General" => "General Medication",
                "Dental" => "Dental Supplies",
                _ => "Inventory — All Streams"
            };

            BtnAddItem.Visibility = BtnAddBatch.Visibility = _canManage ? Visibility.Visible : Visibility.Collapsed;
            if (_stream is not null) { IStreamLockTo(_stream); }
            Load();
        }

        private void IStreamLockTo(string stream)
        {
            foreach (ComboBoxItem it in IStream.Items)
                if ((string?)it.Content == stream) { it.IsSelected = true; }
            IStream.IsEnabled = false;
        }

        private void Load()
        {
            var sql = @"SELECT Id, ItemName AS Item, Stream, Category, Quantity AS [On hand], Unit,
                               UnitPrice AS Price, ReorderLevel AS [Reorder at],
                               CASE WHEN Quantity<=ReorderLevel THEN 'LOW' ELSE 'OK' END AS Stock
                        FROM InventoryItems WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (_stream is not null) { sql += " AND Stream=@s"; p["s"] = _stream; }
            if (ChkLowOnly.IsChecked == true) sql += " AND Quantity<=ReorderLevel";
            sql += " ORDER BY ItemName;";
            Grid.ItemsSource = App.Database.ExecuteQuery(sql, p).DefaultView;
            UpdateButtons();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => Load();

        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id") e.Cancel = true;
        }

        private int? SelectedId => Grid.SelectedItem is DataRowView row ? Convert.ToInt32(row["Id"]) : null;
        private string SelectedName => Grid.SelectedItem is DataRowView row ? row["Item"].ToString() ?? "" : "";

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();
        private void UpdateButtons()
        {
            bool sel = SelectedId is not null;
            BtnDispense.IsEnabled = sel;
            if (_canManage) BtnAddBatch.IsEnabled = sel;
        }

        // ── Dispense ──
        private void BtnDispense_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is null) return;
            TxtDispItem.Text = SelectedName;
            FDispQty.Text = "";
            TxtDispError.Visibility = Visibility.Collapsed;
            OverlayDispense.Visibility = Visibility.Visible;
            FDispQty.Focus();
        }

        private void BtnDoDispense_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is not int id) return;
            if (!int.TryParse(FDispQty.Text, out int qty) || qty <= 0)
            {
                DispError("Enter a valid quantity."); return;
            }
            var result = App.Inventory.Dispense(id, qty);
            if (!result.Success) { DispError(result.Message); return; }
            App.Audit.Log("Dispense", "Inventory", $"{qty} x {SelectedName}");
            CloseOverlays(sender, e);
            Load();
            MessageBox.Show(result.Message, "Dispensed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Add item ──
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tb in new[] { IName, ICategory, IUnit, IPrice, IReorder }) tb.Text = "";
            TxtItemError.Visibility = Visibility.Collapsed;
            OverlayItem.Visibility = Visibility.Visible;
        }

        private void BtnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(IName.Text)) { ItemError("Item name is required."); return; }
            decimal.TryParse(IPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
            int.TryParse(IReorder.Text, out var reorder);

            App.InventoryRepo.Add(new InventoryItem
            {
                ItemName = IName.Text.Trim(),
                Stream = (IStream.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General",
                Category = ICategory.Text.Trim(),
                Unit = IUnit.Text.Trim(),
                UnitPrice = price,
                ReorderLevel = reorder,
                Quantity = 0
            });
            CloseOverlays(sender, e);
            Load();
        }

        // ── Add batch ──
        private void BtnAddBatch_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is null) return;
            TxtBatchItem.Text = SelectedName;
            foreach (var tb in new[] { BQty, BPrice, BSupplier }) tb.Text = "";
            BExpiry.SelectedDate = DateTime.Today.AddMonths(12);
            TxtBatchError.Visibility = Visibility.Collapsed;
            OverlayBatch.Visibility = Visibility.Visible;
        }

        private void BtnSaveBatch_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId is not int id) return;
            if (!int.TryParse(BQty.Text, out int qty) || qty <= 0) { BatchError("Enter a valid quantity."); return; }
            decimal.TryParse(BPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price);

            App.InventoryRepo.AddBatch(new InventoryBatch
            {
                InventoryItemId = id,
                Quantity = qty,
                PurchasePrice = price,
                Supplier = BSupplier.Text.Trim(),
                ReceivedDate = DateTime.Today,
                ExpiryDate = BExpiry.SelectedDate
            });
            CloseOverlays(sender, e);
            Load();
        }

        private void CloseOverlays(object sender, RoutedEventArgs e)
        {
            OverlayDispense.Visibility = OverlayItem.Visibility = OverlayBatch.Visibility = Visibility.Collapsed;
        }

        private void DispError(string m) { TxtDispError.Text = m; TxtDispError.Visibility = Visibility.Visible; }
        private void ItemError(string m) { TxtItemError.Text = m; TxtItemError.Visibility = Visibility.Visible; }
        private void BatchError(string m) { TxtBatchError.Text = m; TxtBatchError.Visibility = Visibility.Visible; }
    }
}
